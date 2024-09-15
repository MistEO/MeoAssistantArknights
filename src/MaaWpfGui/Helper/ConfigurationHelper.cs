// <copyright file="ConfigurationHelper.cs" company="MaaAssistantArknights">
// MaaWpfGui - A part of the MaaCoreArknights project
// Copyright (C) 2021 MistEO and Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License v3.0 only as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaaWpfGui.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MaaWpfGui.Helper
{
    public class ConfigurationHelper
    {
        private static readonly string _configurationFile = Path.Combine(Environment.CurrentDirectory, "config/gui.json");
        private static readonly string _configurationBakFile = Path.Combine(Environment.CurrentDirectory, "config/gui.json.bak");
        private static Dictionary<string, Dictionary<string, string>> _kvsMap;
        private static string _current = ConfigurationKeys.DefaultConfiguration;
        private static Dictionary<string, string> _kvs;
        private static Dictionary<string, string> _globalKvs;

        private static readonly ILogger _logger = Log.ForContext<ConfigurationHelper>();

        private static readonly object _lock = new();

        public delegate void ConfigurationUpdateEventHandler(string key, string oldValue, string newValue);

        public static event ConfigurationUpdateEventHandler ConfigurationUpdateEvent;

        private static bool Released { get; set; }

        /// <summary>
        /// Get a configuration value
        /// </summary>
        /// <param name="key">The config key</param>
        /// <param name="defaultValue">The default value to return if the key is not existed</param>
        /// <returns>The config value</returns>
        public static string GetValue(string key, string defaultValue)
        {
            var hasValue = _kvs.TryGetValue(key, out var value);

            // _logger.Debug("Read configuration key {Key} with default value {DefaultValue}, configuration hit: {HasValue}, configuration value {Value}", key, defaultValue, hasValue, value);
            if (hasValue)
            {
                return value;
            }

            // return hasValue ? value : defaultValue;
            SetValue(key, defaultValue);
            return defaultValue;
        }

        public static string GetGlobalValue(string key, string defaultValue)
        {
            var hasValue = _globalKvs.TryGetValue(key, out var value);

            // _logger.Debug("Read global configuration key {Key} with default value {DefaultValue}, configuration hit: {HasValue}, configuration value {Value}", key, defaultValue, hasValue, value);
            if (hasValue)
            {
                return value;
            }

            hasValue = _kvs.TryGetValue(key, out value);
            if (hasValue)
            {
                _logger.Information("Read global configuration key {Key} with current configuration value {Value}, configuration hit: {HasValue}, configuration value {Value}", key, value, true, value);
                SetGlobalValue(key, value);
                return value;
            }

            // 保证有全局配置
            SetGlobalValue(key, defaultValue);
            return defaultValue;
        }

        /// <summary>
        /// Set a configuration value
        /// </summary>
        /// <param name="key">The config key</param>
        /// <param name="value">The config value</param>
        /// <returns>The return value of <see cref="Save"/></returns>
        public static bool SetValue(string key, string value)
        {
            lock (_lock)
            {
                var old = string.Empty;
                if (!_kvs.TryAdd(key, value))
                {
                    old = _kvs[key];
                    _kvs[key] = value;
                }

                var result = Save();
                if (result)
                {
                    ConfigurationUpdateEvent?.Invoke(key, old, value);
                    _logger.Debug("Configuration {Key} has been set to {Value}", key, value);
                }
                else
                {
                    _logger.Warning("Failed to save configuration {Key} to {Value}", key, value);
                }

                return result;
            }
        }

        public static bool SetGlobalValue(string key, string value)
        {
            lock (_lock)
            {
                var old = string.Empty;
                if (!_globalKvs.TryAdd(key, value))
                {
                    old = _globalKvs[key];
                    _globalKvs[key] = value;
                }

                var result = Save();
                if (result)
                {
                    ConfigurationUpdateEvent?.Invoke(key, old, value);
                    _logger.Debug("Global configuration {Key} has been set to {Value}", key, value);
                }
                else
                {
                    _logger.Warning("Failed to save global configuration {Key} to {Value}", key, value);
                }

                return result;
            }
        }

        /// <summary>
        /// Deletes a configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The return value of <see cref="Save"/>.</returns>
        // ReSharper disable once UnusedMember.Global
        public static bool DeleteValue(string key)
        {
            lock (_lock)
            {
                var old = string.Empty;
                if (_kvs.TryGetValue(key, out var kv))
                {
                    old = kv;
                }

                _kvs.Remove(key);
                var result = Save();
                if (result)
                {
                    ConfigurationUpdateEvent?.Invoke(key, old, string.Empty);
                    _logger.Debug("Configuration {Key} has been deleted", key);
                }
                else
                {
                    _logger.Warning("Failed to save configuration file when deleted {Key}", key);
                }

                return result;
            }
        }

        private static void EnsureConfigDirectoryExists()
        {
            if (Directory.Exists("config") is false)
            {
                Directory.CreateDirectory("config");
            }
        }

        private static bool ParseConfig(out JObject parsed)
        {
            parsed = ParseJsonFile(_configurationFile);
            if (parsed is null && File.Exists(_configurationBakFile))
            {
                _logger.Warning("Failed to load configuration file, trying to use backup file");
                parsed = ParseJsonFile(_configurationFile);
                if (parsed is not null)
                {
                    File.Copy(_configurationBakFile, _configurationFile, true);
                }
            }

            if (parsed is null)
            {
                _logger.Warning("Failed to load configuration file and/or backup file, using default settings");

                _kvsMap = new Dictionary<string, Dictionary<string, string>>();
                _current = ConfigurationKeys.DefaultConfiguration;
                _kvsMap[_current] = new Dictionary<string, string>();
                _kvs = _kvsMap[_current];
                _globalKvs = new Dictionary<string, string>();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Load configuration file
        /// </summary>
        /// <returns>True if success, false if failed</returns>
        public static bool Load()
        {
            lock (_lock)
            {
                EnsureConfigDirectoryExists();

                // Load configuration file
                if (!ParseConfig(out var parsed))
                {
                    return false;
                }

                SetKvOrMigrate(parsed);

                return true;
            }
        }

        private static void SetKvOrMigrate(JObject parsed)
        {

            bool hasConfigMap = parsed.ContainsKey(ConfigurationKeys.ConfigurationMap);
            bool hasCurrentConfig = parsed.ContainsKey(ConfigurationKeys.CurrentConfiguration);
            if (hasConfigMap)
            {
                // new version
                _kvsMap = parsed[ConfigurationKeys.ConfigurationMap]?.ToObject<Dictionary<string, Dictionary<string, string>>>()
                          ?? new Dictionary<string, Dictionary<string, string>>();
                _current = parsed[ConfigurationKeys.CurrentConfiguration]?.ToString()
                           ?? ConfigurationKeys.DefaultConfiguration;
            }
            else
            {
                // old version
                _logger.Information("Configuration file is in old version, migrating to new version");

                _kvsMap = new Dictionary<string, Dictionary<string, string>>();
                _current = ConfigurationKeys.DefaultConfiguration;
                _kvsMap[_current] = parsed.ToObject<Dictionary<string, string>>();
            }

            _kvs = _kvsMap[_current];

            _globalKvs = hasCurrentConfig
                ? parsed[ConfigurationKeys.GlobalConfiguration]?.ToObject<Dictionary<string, string>>()
                : new Dictionary<string, string>();
        }

        /// <summary>
        /// Save configuration file
        /// </summary>
        /// <returns>The result of saving process</returns>
        private static bool Save(string file = null)
        {
            if (Released)
            {
                _logger.Warning("Attempting to save configuration file after release, this is not allowed");
                return false;
            }

            try
            {
                var jsonStr = JsonConvert.SerializeObject(
                    new Dictionary<string, object> { { ConfigurationKeys.ConfigurationMap, _kvsMap }, { ConfigurationKeys.CurrentConfiguration, _current }, { ConfigurationKeys.GlobalConfiguration, _globalKvs }, },
                    Formatting.Indented);

                File.WriteAllText(file ?? _configurationFile, jsonStr);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to save configuration file.");
                return false;
            }

            return true;
        }

        public static string GetCheckedStorage(string storageKey, string name, string defaultValue)
        {
            return GetValue(storageKey + name + ".IsChecked", defaultValue);
        }

        public static bool SetCheckedStorage(string storageKey, string name, string value)
        {
            return SetValue(storageKey + name + ".IsChecked", value);
        }

        public static string GetFacilityOrder(string facility, string defaultValue)
        {
            return GetValue("Infrast.Order." + facility, defaultValue);
        }

        public static bool SetFacilityOrder(string facility, string value)
        {
            return SetValue("Infrast.Order." + facility, value);
        }

        public static string GetTimer(int i, string defaultValue)
        {
            return GetGlobalValue($"Timer.Timer{i + 1}", defaultValue);
        }

        public static bool SetTimer(int i, string value)
        {
            return SetGlobalValue($"Timer.Timer{i + 1}", value);
        }

        public static string GetTimerHour(int i, string defaultValue)
        {
            return GetGlobalValue($"Timer.Timer{i + 1}Hour", defaultValue);
        }

        public static bool SetTimerHour(int i, string value)
        {
            return SetGlobalValue($"Timer.Timer{i + 1}Hour", value);
        }

        public static string GetTimerMin(int i, string defaultValue)
        {
            return GetGlobalValue($"Timer.Timer{i + 1}Min", defaultValue);
        }

        public static bool SetTimerMin(int i, string value)
        {
            return SetGlobalValue($"Timer.Timer{i + 1}Min", value);
        }

        public static string GetTimerConfig(int i, string defaultValue)
        {
            return GetGlobalValue($"Timer.Timer{i + 1}.Config", defaultValue);
        }

        public static bool SetTimerConfig(int i, string value)
        {
            return SetGlobalValue($"Timer.Timer{i + 1}.Config", value);
        }

        public static string GetTaskOrder(string task, string defaultValue)
        {
            return GetValue("TaskQueue.Order." + task, defaultValue);
        }

        public static bool SetTaskOrder(string task, string value)
        {
            return SetValue("TaskQueue.Order." + task, value);
        }

        public static void Release()
        {
            lock (_lock)
            {
                Save();
                Save(_configurationBakFile);
                Released = true;
            }
        }

        private static JObject ParseJsonFile(string filePath)
        {
            if (File.Exists(filePath) is false)
            {
                return null;
            }

            var str = File.ReadAllText(filePath);

            try
            {
                var obj = (JObject)JsonConvert.DeserializeObject(str);
                return obj ?? throw new Exception("Failed to parse json file");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to deserialize json file: {FilePath}", filePath);
            }

            return null;
        }

        public static bool SwitchConfiguration(string configName)
        {
            if (_kvsMap.ContainsKey(configName) is false)
            {
                _logger.Warning("Configuration {ConfigName} does not exist", configName);
                return false;
            }

            _current = configName;
            _kvs = _kvsMap[_current];
            return true;
        }

        public static bool AddConfiguration(string configName, string copyFrom = null)
        {
            if (string.IsNullOrEmpty(configName))
            {
                return false;
            }

            if (_kvsMap.ContainsKey(configName))
            {
                _logger.Warning("Configuration {ConfigName} already exists", configName);
                return false;
            }

            if (copyFrom is null)
            {
                _kvsMap[configName] = new Dictionary<string, string>();
            }
            else
            {
                if (_kvsMap.ContainsKey(copyFrom) is false)
                {
                    _logger.Warning("Configuration {ConfigName} does not exist", copyFrom);
                    return false;
                }

                _kvsMap[configName] = new Dictionary<string, string>(_kvsMap[copyFrom]);
            }

            return true;
        }

        public static bool DeleteConfiguration(string configName)
        {
            if (_kvsMap.ContainsKey(configName) is false)
            {
                _logger.Warning("Configuration {ConfigName} does not exist", configName);
                return false;
            }

            if (_current == configName)
            {
                _logger.Warning("Configuration {ConfigName} is current configuration, cannot delete", configName);
                return false;
            }

            _kvsMap.Remove(configName);
            return true;
        }

        public static List<string> GetConfigurationList()
        {
            return _kvsMap.Keys.ToList();
        }

        public static string GetCurrentConfiguration()
        {
            return _current;
        }
    }
}
