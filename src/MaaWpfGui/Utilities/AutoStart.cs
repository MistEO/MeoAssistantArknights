// <copyright file="AutoStart.cs" company="MaaAssistantArknights">
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
using System.IO;
using System.Security;
using Microsoft.Win32;
using Serilog;

namespace MaaWpfGui.Utilities
{
    /// <summary>
    /// The model of auto-starting settings.
    /// </summary>
    public static class AutoStart
    {
        private static readonly ILogger _logger = Log.ForContext("SourceContext", "AutoStart");

        private static readonly string _fileValue = Environment.ProcessPath;
        private static readonly string _uniqueIdentifier = GetHashCode(_fileValue);

        private static readonly string _startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private static readonly string _registryKeyName = $"MAA_{_uniqueIdentifier}";
        private static readonly string _startupShortcutPath = Path.Combine(_startupFolderPath, _registryKeyName + ".lnk");

        private const string CurrentUserRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private static string GetHashCode(string input)
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < input.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ input[i];
                if (i == input.Length - 1)
                {
                    break;
                }

                hash2 = ((hash2 << 5) + hash2) ^ input[i + 1];
            }

            return (hash1 + (hash2 * 1566083941)).ToString("X");
        }

        /// <summary>
        /// Checks whether this program starts up with OS.
        /// </summary>
        /// <returns>The value.</returns>
        public static bool CheckStart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(CurrentUserRunKey, false);
                return key?.GetValue(_registryKeyName) != null;
            }
            catch (Exception e)
            {
                _logger.Error("Failed to check startup: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets whether this program starts up with OS.
        /// </summary>
        /// <param name="set">The new value.</param>
        /// <param name="error">Outputs the error message in case of failure.</param>
        /// <returns>Whether the operation is successful.</returns>
        public static bool SetStart(bool set, out string error)
        {
            error = string.Empty;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(CurrentUserRunKey, true);
                if (key == null)
                {
                    error = "Failed to open registry key.";
                    _logger.Error(error);
                    return false;
                }

                if (set)
                {
                    key.SetValue(_registryKeyName, "\"" + _fileValue + "\"");
                    _logger.Information($"Set [{_registryKeyName}, \"{_fileValue}\"] into \"{CurrentUserRunKey}\"");
                }
                else
                {
                    key.DeleteValue(_registryKeyName);
                }

                return set == CheckStart();
            }
            catch (UnauthorizedAccessException uae)
            {
                error = "Unauthorized access: " + uae.Message;
                _logger.Error(error);
                return false;
            }
            catch (SecurityException se)
            {
                error = "Security error: " + se.Message;
                _logger.Error(error);
                return false;
            }
            catch (Exception e)
            {
                error = "Failed to set startup: " + e.Message;
                _logger.Error(error);
                return false;
            }
        }
    }
}
