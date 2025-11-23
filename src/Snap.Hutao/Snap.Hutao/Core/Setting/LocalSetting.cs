// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ApplicationModel;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Factory.Process;
using Snap.Hutao.Win32;
using Snap.Hutao.Win32.Foundation;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Snap.Hutao.Core.Setting;

internal static class LocalSetting
{
    private static readonly FrozenSet<Type> SupportedTypes =
    [
        typeof(byte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(bool),
        typeof(char),
        typeof(string),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Windows.Foundation.Point),
        typeof(Windows.Foundation.Size),
        typeof(Windows.Foundation.Rect),
        typeof(ApplicationDataCompositeValue)
    ];

    private static readonly Lazy<ISettingStorage> LazyStorage = new(CreateStorage);

    private static ISettingStorage Storage => LazyStorage.Value;

    public static T Get<T>(string key, T defaultValue = default!)
    {
        Debug.Assert(SupportedTypes.Contains(typeof(T)));
        return Storage.Get(key, defaultValue);
    }

    public static void Set<T>(string key, T value)
    {
        Debug.Assert(SupportedTypes.Contains(typeof(T)));
        Storage.Set(key, value);
    }

    public static void SetIf<T>(bool condition, string key, T value)
    {
        if (condition)
        {
            Set(key, value);
        }
    }

    public static void SetIfNot<T>(bool condition, string key, T value)
    {
        if (!condition)
        {
            Set(key, value);
        }
    }

    public static T Update<T>(string key, T defaultValue, Func<T, T> modifier)
    {
        Debug.Assert(SupportedTypes.Contains(typeof(T)));
        T oldValue = Get(key, defaultValue);
        Set(key, modifier(oldValue));
        return oldValue;
    }

    public static T Update<T>(string key, T defaultValue, T newValue)
    {
        Debug.Assert(SupportedTypes.Contains(typeof(T)));
        T oldValue = Get(key, defaultValue);
        Set(key, newValue);
        return oldValue;
    }

    private static ISettingStorage CreateStorage()
    {
        if (PackageIdentityAdapter.HasPackageIdentity)
        {
            return new PackagedSettingStorage();
        }

        return new UnpackagedSettingStorage();
    }

    private interface ISettingStorage
    {
        T Get<T>(string key, T defaultValue);
        void Set<T>(string key, T value);
    }

    private sealed class PackagedSettingStorage : ISettingStorage
    {
        private readonly ApplicationDataContainer container = ApplicationData.Current.LocalSettings;

        public T Get<T>(string key, T defaultValue)
        {
            if (container.Values.TryGetValue(key, out object? value))
            {
                // unbox the value
                return value is null ? defaultValue : (T)value;
            }

            Set(key, defaultValue);
            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            try
            {
                container.Values[key] = value;
            }
            catch (Exception ex)
            {
                // 状态管理器无法写入设置
                if (HutaoNative.IsWin32(ex.HResult, WIN32_ERROR.ERROR_STATE_WRITE_SETTING_FAILED))
                {
                    HutaoNative.Instance.ShowErrorMessage(ex.Message, ExceptionFormat.Format(ex));
                    ProcessFactory.KillCurrent();
                }

                throw;
            }
        }
    }

    private sealed class UnpackagedSettingStorage : ISettingStorage
    {
        private readonly string settingsFilePath;
        private readonly ConcurrentDictionary<string, object?> cache = new();
        private readonly object fileLock = new();
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            Converters =
            {
                new ApplicationDataCompositeValueJsonConverter(),
            }
        };

        public UnpackagedSettingStorage()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            const string FolderName
#if IS_ALPHA_BUILD
                = "HutaoAlpha";
#elif IS_CANARY_BUILD
                = "HutaoCanary";
#else
                = "Hutao";
#endif
            string settingsDir = Path.Combine(localAppData, FolderName, "Settings");
            Directory.CreateDirectory(settingsDir);
            settingsFilePath = Path.Combine(settingsDir, "LocalSettings.json");

            LoadFromFile();
        }

        public T Get<T>(string key, T defaultValue)
        {
            if (cache.TryGetValue(key, out object? value))
            {
                if (value is null)
                {
                    return defaultValue;
                }

                // Handle JSON deserialization for complex types
                if (value is JsonElement jsonElement)
                {
                    try
                    {
                        // ⚠️ 特殊处理：JSON 数字类型转换
                        Type targetType = typeof(T);
                        
                        if (jsonElement.ValueKind == JsonValueKind.Number)
                        {
                            if (targetType == typeof(int))
                            {
                                return (T)(object)jsonElement.GetInt32();
                            }
                            if (targetType == typeof(long))
                            {
                                return (T)(object)jsonElement.GetInt64();
                            }
                            if (targetType == typeof(short))
                            {
                                return (T)(object)jsonElement.GetInt16();
                            }
                            if (targetType == typeof(byte))
                            {
                                return (T)(object)jsonElement.GetByte();
                            }
                            if (targetType == typeof(uint))
                            {
                                return (T)(object)jsonElement.GetUInt32();
                            }
                            if (targetType == typeof(ulong))
                            {
                                return (T)(object)jsonElement.GetUInt64();
                            }
                            if (targetType == typeof(ushort))
                            {
                                return (T)(object)jsonElement.GetUInt16();
                            }
                            if (targetType == typeof(float))
                            {
                                return (T)(object)jsonElement.GetSingle();
                            }
                            if (targetType == typeof(double))
                            {
                                return (T)(object)jsonElement.GetDouble();
                            }
                        }

                        // 其他类型使用标准反序列化
                        return jsonElement.Deserialize<T>(jsonOptions) ?? defaultValue;
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                // ⚠️ 如果是直接从 cache 读取的值，也可能需要类型转换
                // 例如：double -> int
                if (value is double doubleValue)
                {
                    Type targetType = typeof(T);
                    if (targetType == typeof(int))
                    {
                        return (T)(object)(int)doubleValue;
                    }
                    if (targetType == typeof(long))
                    {
                        return (T)(object)(long)doubleValue;
                    }
                    if (targetType == typeof(short))
                    {
                        return (T)(object)(short)doubleValue;
                    }
                    if (targetType == typeof(byte))
                    {
                        return (T)(object)(byte)doubleValue;
                    }
                }

                return (T)value;
            }

            Set(key, defaultValue);
            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            cache[key] = value;
            SaveToFile();
        }

        private void LoadFromFile()
        {
            lock (fileLock)
            {
                if (!File.Exists(settingsFilePath))
                {
                    return;
                }

                try
                {
                    string json = File.ReadAllText(settingsFilePath);
                    Dictionary<string, JsonElement>? data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, jsonOptions);
                    if (data is not null)
                    {
                        foreach ((string key, JsonElement value) in data)
                        {
                            cache[key] = value;
                        }
                    }
                }
                catch
                {
                    // If file is corrupted, start fresh
                }
            }
        }

        private void SaveToFile()
        {
            lock (fileLock)
            {
                try
                {
                    // Convert cache to serializable dictionary
                    Dictionary<string, object?> serializableData = new(cache);
                    string json = JsonSerializer.Serialize(serializableData, jsonOptions);
                    File.WriteAllText(settingsFilePath, json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to save settings: {ex.Message}");
                }
            }
        }
    }

    // Converter for ApplicationDataCompositeValue
    private sealed class ApplicationDataCompositeValueJsonConverter : System.Text.Json.Serialization.JsonConverter<ApplicationDataCompositeValue>
    {
        public override ApplicationDataCompositeValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return null;
            }

            ApplicationDataCompositeValue composite = new();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return composite;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                string? key = reader.GetString();
                reader.Read();

                if (key is not null)
                {
                    composite[key] = reader.TokenType switch
                    {
                        JsonTokenType.String => reader.GetString(),
                        JsonTokenType.Number => reader.TryGetInt64(out long l) ? l : reader.GetDouble(),
                        JsonTokenType.True => true,
                        JsonTokenType.False => false,
                        _ => null
                    };
                }
            }

            return composite;
        }

        public override void Write(Utf8JsonWriter writer, ApplicationDataCompositeValue value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach ((string key, object? val) in value)
            {
                writer.WritePropertyName(key);
                if (val is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    JsonSerializer.Serialize(writer, val, val.GetType(), options);
                }
            }

            writer.WriteEndObject();
        }
    }
}