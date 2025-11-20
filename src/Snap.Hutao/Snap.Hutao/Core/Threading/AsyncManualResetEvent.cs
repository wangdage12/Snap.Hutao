// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.Threading;

using System.Threading; // 添加此 using 语句
using System.Threading.Tasks; // 添加此 using 语句

// https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-1-asyncmanualresetevent/
[SuppressMessage("", "SH003")]
internal sealed class AsyncManualResetEvent
{
    private volatile TaskCompletionSource tcs = new();

    public Task WaitAsync()
    {
        return tcs.Task;
    }

    [SuppressMessage("", "SH007")]
    public void Set()
    {
        TaskCompletionSource tcs = this.tcs;
        Task.Factory.StartNew(s => ((TaskCompletionSource)s!).TrySetResult(), tcs, System.Threading.CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
        tcs.Task.Wait();
    }

    public void Reset()
    {
        while (true)
        {
            TaskCompletionSource tcs = this.tcs;
            if (!tcs.Task.IsCompleted || Interlocked.CompareExchange(ref this.tcs, new(), tcs) == tcs)
            {
                return;
            }
        }
    }
}