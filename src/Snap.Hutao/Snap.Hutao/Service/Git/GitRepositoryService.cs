// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using LibGit2Sharp;
using Snap.Hutao.Core;
using Snap.Hutao.Core.IO;
using Snap.Hutao.Core.IO.Http.Proxy;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Service.BackgroundActivity;
using Snap.Hutao.Web.Hutao;
using Snap.Hutao.Web.Hutao.Response;
using Snap.Hutao.Web.Response;
using System.Collections.Immutable;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Snap.Hutao.Service.Git;

[Service(ServiceLifetime.Singleton, typeof(IGitRepositoryService))]
internal sealed partial class GitRepositoryService : IGitRepositoryService
{
    private readonly AsyncKeyedLock<string> repoLock = new();
    private readonly BackgroundActivityOptions backgroundActivityOptions;
    private readonly ILogger<GitRepositoryService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private static readonly TimeSpan GitTransferInactivityTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MirrorListRequestTimeout = TimeSpan.FromSeconds(6);
    private const int MirrorProgressFailureLimit = 10;
    private const int MirrorNoProgressFailureLimit = 3;
    private const int MirrorListMaxAttempts = 3;

    [GeneratedConstructor]
    public partial GitRepositoryService(IServiceProvider serviceProvider);

    static GitRepositoryService()
    {
        GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.ProgramData, string.Empty);
        GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, string.Empty);
        GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.System, string.Empty);
        GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Xdg, string.Empty);
        GlobalSettings.SetOwnerValidation(false);
    }

    public async ValueTask<ValueResult<bool, ValueDirectory>> EnsureRepositoryAsync(string name)
    {
        if (LocalSetting.Get("Snap::Hutao::Git::Repository::Override", false))
        {
            return new(true, Path.GetFullPath(Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), name)));
        }

        using (await repoLock.LockAsync(name).ConfigureAwait(false))
        {
            ImmutableArray<GitRepository> infos;
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                ImmutableArray<GitRepository>? fetchedInfos = await TryGetRepositoryInfosAsync(scope.ServiceProvider, name).ConfigureAwait(false);
                if (fetchedInfos is not { } validInfos)
                {
                    return new(false, default);
                }

                infos = validInfos;
            }

            string directory = Path.GetFullPath(Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), name));
            BackgroundActivity.BackgroundActivity activity = GetActivityByName(name);

            bool failed = false;
            bool succeeded = false;
            List<Exception> exceptions = [];
            try
            {
                await activity.NotifyAsync(taskContext).ConfigureAwait(false);
                await activity.UpdateAsync(taskContext, SH.ServiceBackgroundActivityDefaultDescription, false, false, false, false).ConfigureAwait(false);

                foreach (GitRepository info in RepositoryAffinity.Sort(infos))
                {
                    if (!await ProbeRepositoryAsync(activity, info).ConfigureAwait(false))
                    {
                        continue;
                    }

                    if (TryEnsureRepositoryWithRetries(activity, directory, info, exceptions, out ValueResult<bool, ValueDirectory> ensuredRepository))
                    {
                        succeeded = true;
                        return ensuredRepository;
                    }
                }
            }
            catch (Exception)
            {
                failed = true;
                throw;
            }
            finally
            {
                if (!failed && succeeded)
                {
                    await activity.NotifyAsync(taskContext).ConfigureAwait(false);
                    await activity.UpdateAsync(taskContext, SH.ServiceGitRepositoryOperationCompleted, true, false, false, false).ConfigureAwait(false);
                }
            }

            await activity.NotifyAsync(taskContext).ConfigureAwait(false);
            await activity.UpdateAsync(taskContext, SH.ServiceGitRepositoryOperationFailed, false, true, false, false).ConfigureAwait(false);
            throw new GitRepositoryException(SH.ServiceGitRepositoryOperationFailed, exceptions);
        }
    }

    private async ValueTask<ImmutableArray<GitRepository>?> TryGetRepositoryInfosAsync(IServiceProvider scopedServiceProvider, string name)
    {
        HutaoInfrastructureClient infrastructureClient = scopedServiceProvider.GetRequiredService<HutaoInfrastructureClient>();

        for (int attempt = 1; attempt <= MirrorListMaxAttempts; attempt++)
        {
            try
            {
                using CancellationTokenSource timeoutCts = new(MirrorListRequestTimeout);
                HutaoResponse<ImmutableArray<GitRepository>> response = await infrastructureClient.GetGitRepositoryAsync(name, timeoutCts.Token).ConfigureAwait(false);

                if (ResponseValidator.TryValidateWithoutUINotification(response, scopedServiceProvider, out ImmutableArray<GitRepository> infos))
                {
                    if (attempt > 1)
                    {
                        logger.LogInformation("[Metadata] Repository mirror list recovered: Name={Name}, Attempt={Attempt}", name, attempt);
                    }

                    return infos;
                }

                logger.LogWarning("[Metadata] Repository mirror list request returned invalid response: Name={Name}, Attempt={Attempt}/{MaxAttempts}, ReturnCode={ReturnCode}, Message={Message}",
                    name,
                    attempt,
                    MirrorListMaxAttempts,
                    response.ReturnCode,
                    response.Message);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "[Metadata] Repository mirror list request timed out: Name={Name}, Attempt={Attempt}/{MaxAttempts}, RequestTimeout={RequestTimeout}s",
                    name,
                    attempt,
                    MirrorListMaxAttempts,
                    MirrorListRequestTimeout.TotalSeconds);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Metadata] Repository mirror list request failed: Name={Name}, Attempt={Attempt}/{MaxAttempts}",
                    name,
                    attempt,
                    MirrorListMaxAttempts);
            }
        }

        logger.LogError("[Metadata] Repository mirror list request exhausted retries: Name={Name}, Attempts={Attempts}", name, MirrorListMaxAttempts);
        return default;
    }

    private async ValueTask<bool> ProbeRepositoryAsync(BackgroundActivity.BackgroundActivity activity, GitRepository info)
    {
        string probeUrl = $"{info.HttpsUrl.OriginalString.TrimEnd('/')}/info/refs?service=git-upload-pack";
        logger.LogInformation("[Metadata] Probing repository mirror: Url={Url}", probeUrl);
        activity.Update(taskContext, $"{SH.ServiceGitRepositoryProbingMirror}: {info.Name}", false, false, false, true);

        try
        {
            using SocketsHttpHandler handler = new()
            {
                UseProxy = true,
                Proxy = HttpProxyUsingSystemProxy.Instance,
                ConnectTimeout = TimeSpan.FromSeconds(3),
            };

            using HttpClient client = new(handler)
            {
                Timeout = TimeSpan.FromSeconds(5),
            };

            using HttpRequestMessage request = new(HttpMethod.Get, probeUrl);
            if (!string.IsNullOrEmpty(info.Token))
            {
                string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{info.Username}:{info.Token}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }

            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[Metadata] Probe failed: Url={Url}, StatusCode={StatusCode}", probeUrl, (int)response.StatusCode);
                return false;
            }

            logger.LogInformation("[Metadata] Probe succeeded: Url={Url}", probeUrl);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Metadata] Probe failed: Url={Url}", probeUrl);
            return false;
        }
    }

    private bool TryEnsureRepositoryWithRetries(BackgroundActivity.BackgroundActivity activity, string directory, GitRepository info, List<Exception> exceptions, out ValueResult<bool, ValueDirectory> result)
    {
        int progressFailures = 0;
        int noProgressFailures = 0;
        bool forceInvalid = false;

        while (true)
        {
            using GitTransferWatchdog watchdog = new(GitTransferInactivityTimeout);

            try
            {
                result = EnsureRepository(activity, directory, info, forceInvalid, watchdog);
                RepositoryAffinity.DecreaseFailure(info);
                return true;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);

                if (watchdog.WasSoftCanceled)
                {
                    if (watchdog.HasAnyProgress)
                    {
                        progressFailures++;
                        noProgressFailures = 0;
                        logger.LogWarning(ex, "[Metadata] Repository transfer stalled after progress, retrying same mirror: Failure={Failure}/{FailureLimit}, Directory={Directory}, Url={Url}",
                            progressFailures,
                            MirrorProgressFailureLimit,
                            directory,
                            info.HttpsUrl.OriginalString);

                        if (progressFailures >= MirrorProgressFailureLimit)
                        {
                            logger.LogWarning("[Metadata] Switching mirror after {FailureLimit} stalled transfers with progress: Directory={Directory}, Url={Url}",
                                MirrorProgressFailureLimit,
                                directory,
                                info.HttpsUrl.OriginalString);
                            RepositoryAffinity.IncreaseFailure(info);
                            result = default;
                            return false;
                        }

                        activity.Update(taskContext, $"{SH.ServiceGitRepositoryRetryingMirror}: {info.Name}", false, false, false, true);
                    }
                    else
                    {
                        progressFailures = 0;
                        noProgressFailures++;
                        logger.LogWarning(ex, "[Metadata] Repository transfer made no progress before timeout: Failure={Failure}/{FailureLimit}, Directory={Directory}, Url={Url}",
                            noProgressFailures,
                            MirrorNoProgressFailureLimit,
                            directory,
                            info.HttpsUrl.OriginalString);

                        if (noProgressFailures >= MirrorNoProgressFailureLimit)
                        {
                            logger.LogWarning("[Metadata] Switching mirror after {FailureLimit} consecutive no-progress failures: Directory={Directory}, Url={Url}",
                                MirrorNoProgressFailureLimit,
                                directory,
                                info.HttpsUrl.OriginalString);
                            RepositoryAffinity.IncreaseFailure(info);
                            result = default;
                            return false;
                        }
                    }

                    forceInvalid |= !Repository.IsValid(directory);
                    continue;
                }

                if (!forceInvalid)
                {
                    logger.LogWarning(ex, "[Metadata] Failed to update existing repository, fallback to reclone: Directory={Directory}, Url={Url}", directory, info.HttpsUrl.OriginalString);
                    forceInvalid = true;
                    continue;
                }

                logger.LogWarning(ex, "[Metadata] Repository operation failed on mirror: Directory={Directory}, Url={Url}", directory, info.HttpsUrl.OriginalString);
                RepositoryAffinity.IncreaseFailure(info);
                result = default;
                return false;
            }
        }
    }

    private ValueResult<bool, ValueDirectory> EnsureRepository(BackgroundActivity.BackgroundActivity activity, string directory, GitRepository info, bool forceInvalid, GitTransferWatchdog watchdog)
    {
        // Debug: Log the initial state
        bool isRepoValid = Repository.IsValid(directory);
        bool directoryExists = Directory.Exists(directory);

        logger.LogInformation("[Metadata] Checking repository: Directory={Directory}, Exists={Exists}, IsValid={IsValid}, ForceInvalid={ForceInvalid}",
            directory, directoryExists, isRepoValid, forceInvalid);

        FetchOptions fetchOptions = new()
        {
            Depth = 1,
            Prune = true,
            TagFetchMode = TagFetchMode.None,
            ProxyOptions =
            {
                ProxyType = ProxyType.Auto,
                Url = HttpProxyUsingSystemProxy.Instance.CurrentProxyUri,
            },
            CredentialsProvider = (url, user, types) => string.IsNullOrEmpty(info.Token)
                ? default
                : new UsernamePasswordCredentials
                {
                    Username = info.Username,
                    Password = info.Token,
            },
            OnProgress = output =>
            {
                if (!watchdog.TryReportProgress($"progress:{output}"))
                {
                    logger.LogWarning("[Metadata] Cancelling repository transfer after {TimeoutSeconds}s without progress: Directory={Directory}, Url={Url}",
                        GitTransferInactivityTimeout.TotalSeconds,
                        directory,
                        info.HttpsUrl.OriginalString);
                    return false;
                }

                int idx = output.AsSpan().IndexOfAny("\r\n");
                activity.Update(taskContext, idx > 0 ? output.Substring(0, idx) : output, false, false, false, false);
                return true;
            },
            OnTransferProgress = progress =>
            {
                if (!watchdog.TryReportProgress($"transfer:{progress.ReceivedObjects}:{progress.TotalObjects}:{progress.ReceivedBytes}"))
                {
                    logger.LogWarning("[Metadata] Cancelling repository transfer after {TimeoutSeconds}s without progress: Directory={Directory}, Url={Url}",
                        GitTransferInactivityTimeout.TotalSeconds,
                        directory,
                        info.HttpsUrl.OriginalString);
                    return false;
                }

                double progressValue = progress.TotalObjects == 0 ? 0 : (double)progress.ReceivedObjects / progress.TotalObjects;
                activity.Update(taskContext, $"{progress.ReceivedObjects}/{progress.TotalObjects}, {Converters.ToFileSizeString(progress.ReceivedBytes)}", false, false, true, false, progressValue);
                return true;
            },
            CertificateCheck = static (cert, valid, host) => true,
        };

        if (forceInvalid || !isRepoValid)
        {
            // Debug: Log why we're cloning
            string reason = forceInvalid
                ? SH.ServiceGitRepositoryCloneReasonForceInvalid
                : SH.ServiceGitRepositoryCloneReasonInvalidRepo;
            logger.LogInformation("[Metadata] Cloning repository: Reason={Reason}, Url={Url}", reason, info.HttpsUrl.OriginalString);
            activity.Update(taskContext, reason, false, false, false, false);

            if (directoryExists)
            {
                logger.LogInformation("[Metadata] Deleting existing directory before clone");
                Directory.SetReadOnly(directory, false);
                Directory.Delete(directory, true);
            }

            Repository.AdvancedClone(info.HttpsUrl.OriginalString, directory, new(fetchOptions)
            {
                Checkout = true,
                OnCheckoutProgress = (path, completedSteps, totalSteps) =>
                {
                    watchdog.TryReportProgress($"checkout:{completedSteps}:{totalSteps}:{path}");
                },
            });

            logger.LogInformation("[Metadata] Clone completed successfully");
        }
        else
        {
            // Debug: Log that we're updating
            logger.LogInformation("[Metadata] Updating existing repository");
            activity.Update(taskContext, SH.ServiceGitRepositoryUpdatingExisting, false, false, false, false);

            // We need to ensure local repo is up to date
            using (Repository repo = new(directory))
            {
                Configuration config = repo.Config;
                config.Set("core.longpaths", true);
                config.Set("safe.directory", true);
                if (string.IsNullOrEmpty(fetchOptions.ProxyOptions.Url))
                {
                    config.Unset("http.proxy");
                    config.Unset("https.proxy");
                }
                else
                {
                    config.Set("http.proxy", fetchOptions.ProxyOptions.Url);
                    config.Set("https.proxy", fetchOptions.ProxyOptions.Url);
                }

                repo.Network.Remotes.Update("origin", remote => remote.Url = info.HttpsUrl.OriginalString);
                repo.RemoveUntrackedFiles();
                fetchOptions.UpdateFetchHead = false;
                Commands.Fetch(repo, "origin", Array.Empty<string>(), fetchOptions, default);

                // Manually patch .git/shallow file
                File.WriteAllText(Path.Combine(directory, ".git", "shallow"), string.Join("", repo.Branches.Where(static branch => branch.IsRemote).Select(static branch => $"{branch.Tip.Sha}\n")));

                Branch remoteBranch = repo.Branches["origin/main"];
                Branch localBranch = repo.Branches["main"] ?? repo.CreateBranch("main", remoteBranch.Tip);
                repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
                Commands.Checkout(repo, localBranch);
                repo.Reset(ResetMode.Hard, remoteBranch.Tip);
                repo.RemoveUntrackedFiles();
            }

            logger.LogInformation("[Metadata] Update completed successfully");
        }

        return new(true, directory);
    }

    private BackgroundActivity.BackgroundActivity GetActivityByName(string name)
    {
        return name switch
        {
            "Snap.Metadata" => backgroundActivityOptions.MetadataInitialization,
            "Snap.ContentDelivery" => backgroundActivityOptions.FullTrustInitialization,
            _ => backgroundActivityOptions.Default,
        };
    }

    private sealed class GitTransferWatchdog : IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Task monitorTask;
        private readonly object syncRoot = new();
        private readonly long timeoutMilliseconds;
        private bool hasAnyProgress;
        private bool cancelRequested;
        private long lastProgressTick;
        private string? lastProgressSignature;

        public GitTransferWatchdog(TimeSpan timeout)
        {
            timeoutMilliseconds = (long)timeout.TotalMilliseconds;
            lastProgressTick = Environment.TickCount64;
            monitorTask = Task.Run(MonitorAsync);
        }

        public bool HasAnyProgress
        {
            get
            {
                lock (syncRoot)
                {
                    return hasAnyProgress;
                }
            }
        }

        public bool WasSoftCanceled
        {
            get
            {
                lock (syncRoot)
                {
                    return cancelRequested;
                }
            }
        }

        public bool TryReportProgress(string signature)
        {
            lock (syncRoot)
            {
                if (cancelRequested)
                {
                    return false;
                }

                if (!string.Equals(lastProgressSignature, signature, StringComparison.Ordinal))
                {
                    hasAnyProgress = true;
                    lastProgressTick = Environment.TickCount64;
                    lastProgressSignature = signature;
                }

                return true;
            }
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();

            try
            {
                monitorTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException)
            {
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }

        private async Task MonitorAsync()
        {
            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(250, cancellationTokenSource.Token).ConfigureAwait(false);

                    lock (syncRoot)
                    {
                        if (cancelRequested)
                        {
                            return;
                        }

                        if (Environment.TickCount64 - lastProgressTick >= timeoutMilliseconds)
                        {
                            cancelRequested = true;
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
