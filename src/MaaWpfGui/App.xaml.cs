// <copyright file="App.xaml.cs" company="MaaAssistantArknights">
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
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using MaaWpfGui.Helper;
using MaaWpfGui.Main;
using MaaWpfGui.WineCompat;
using MaaWpfGui.WineCompat.FontConfig;
using Serilog;

namespace MaaWpfGui
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App
    {
        private static readonly ILogger _logger = Log.ForContext<App>();

        public void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            if (!string.IsNullOrEmpty(link?.NavigateUri?.AbsoluteUri))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = link.NavigateUri.AbsoluteUri,
                    UseShellExecute = true,
                });
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (WineRuntimeInformation.IsRunningUnderWine && MaaDesktopIntegration.Availabile)
            {
                // override buintin font map as early as possible
                FontConfigIntegration.Install();
            }

            base.OnStartup(e);

            string[] args = e.Args;
            const string ConfigFlag = "--config";

            string configArgs = string.Empty;
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case ConfigFlag when i + 1 < args.Length:
                        configArgs = args[i + 1];
                        i += 1;
                        break;
                }
            }

            Config(configArgs);
        }

        private static void Config(string desiredConfig)
        {
            const string ConfigFile = @".\config\gui.json";
            if (!File.Exists(ConfigFile) || string.IsNullOrEmpty(desiredConfig))
            {
                return;
            }

            try
            {
                if (!UpdateConfiguration(desiredConfig))
                {
                    return;
                }

                Bootstrapper.ShutdownAndRestartWithoutArgs();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error updating configuration: {desiredConfig}, ex: {ex.Message}");
            }
        }

        private static bool UpdateConfiguration(string desiredConfig)
        {
            // 配置名可能就包在引号中，需要转义符，如 \"a\"
            string currentConfig = ConfigurationHelper.GetCurrentConfiguration();
            return currentConfig != desiredConfig && ConfigurationHelper.SwitchConfiguration(desiredConfig);
        }
    }
}
