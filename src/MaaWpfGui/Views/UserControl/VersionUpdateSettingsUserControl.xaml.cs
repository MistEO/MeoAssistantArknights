// <copyright file="VersionUpdateSettingsUserControl.xaml.cs" company="MaaAssistantArknights">
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
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HandyControl.Controls;
using HandyControl.Data;
using MaaWpfGui.Helper;
using MaaWpfGui.ViewModels.UI;
using Serilog;

namespace MaaWpfGui.Views.UserControl
{
    /// <summary>
    /// VersionUpdateSettingsUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class VersionUpdateSettingsUserControl
    {
        private static readonly ILogger _logger = Log.ForContext<VersionUpdateSettingsUserControl>();

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionUpdateSettingsUserControl"/> class.
        /// </summary>
        public VersionUpdateSettingsUserControl()
        {
            InitializeComponent();
            _timer.Tick += (s, e1) =>
            {
                _easterEggsCount = 0;
                _timer.IsEnabled = false;
            };
        }

        private readonly DispatcherTimer _timer = new()
        {
            Interval = new TimeSpan(0, 0, 6),
        };

        private void MaaVersionClick(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboardAsync($"UI Version: {SettingsViewModel.UiVersion}\nCore Version: {SettingsViewModel.CoreVersion}\nBuild Time: {SettingsViewModel.BuildDateTimeCurrentCultureString}");
        }

        private void CoreVersionClick(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboardAsync("Core Version: " + SettingsViewModel.CoreVersion);
            EasterEggs();
        }

        private void UiVersionClick(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboardAsync("UI Version: " + SettingsViewModel.UiVersion);
        }

        private void ResourceVersionClick(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboardAsync($"Resource Version: {Instances.SettingsViewModel.ResourceVersion}\nResource Time: {Instances.SettingsViewModel.ResourceDateTimeCurrentCultureString}");
        }

        private static void CopyToClipboardAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                Thread clipboardThread = new Thread(() =>
                {
                    try
                    {
                        Clipboard.SetData(DataFormats.UnicodeText, text);
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Clipboard operation failed: " + e.Message);
                    }
                });

                clipboardThread.SetApartmentState(ApartmentState.STA);
                clipboardThread.Start();
            }
            catch (Exception e)
            {
                _logger.Error("Clipboard operation failed: " + e.Message);
            }
        }

        private int _easterEggsCount;

        private void EasterEggs()
        {
            if (!_timer.IsEnabled)
            {
                _timer.IsEnabled = true;
            }

            if (_easterEggsCount <= 1)
            {
                ++_easterEggsCount;
                return;
            }

            var growlInfo = new GrowlInfo
            {
                IsCustom = true,
                Message = LocalizationHelper.GetString("BuyWineOnAprilFoolsDay"),
                IconKey = "HangoverGeometry",
                IconBrushKey = "PallasBrush",
            };

            Growl.Info(growlInfo);
        }
    }
}
