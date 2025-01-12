// <copyright file="GuiSettingsUserControlModel.cs" company="MaaAssistantArknights">
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

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MaaWpfGui.Configuration;
using MaaWpfGui.Constants;
using MaaWpfGui.Helper;
using MaaWpfGui.Main;
using MaaWpfGui.Utilities.ValueType;
using MaaWpfGui.ViewModels.UI;
using Stylet;
using DarkModeType = MaaWpfGui.Configuration.GUI.DarkModeType;

namespace MaaWpfGui.ViewModels.UserControl.Settings;

public class GuiSettingsUserControlModel : PropertyChangedBase
{
    static GuiSettingsUserControlModel()
    {
        Instance = new();
    }

    public static GuiSettingsUserControlModel Instance { get; }

    /// <summary>
    /// Gets or sets the language list.
    /// </summary>
    public List<CombinedData> LanguageList { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of operator name language settings
    /// </summary>
    public List<CombinedData> OperNameLanguageModeList { get; set; } =
        [
            new() { Display = LocalizationHelper.GetString("OperNameLanguageMAA"), Value = "OperNameLanguageMAA" },
            new() { Display = LocalizationHelper.GetString("OperNameLanguageClient"), Value = "OperNameLanguageClient" }
        ];

    /// <summary>
    /// Gets the list of dark mode.
    /// </summary>
    public List<GenericCombinedData<DarkModeType>> DarkModeList { get; } =
        [
            new() { Display = LocalizationHelper.GetString("Light"), Value = DarkModeType.Light },
            new() { Display = LocalizationHelper.GetString("Dark"), Value = DarkModeType.Dark },
            new() { Display = LocalizationHelper.GetString("SyncWithOs"), Value = DarkModeType.SyncWithOs },
        ];

    /// <summary>
    /// Gets the list of inverse clear modes.
    /// </summary>
    public List<CombinedData> InverseClearModeList { get; } =
        [
            new() { Display = LocalizationHelper.GetString("Clear"), Value = "Clear" },
            new() { Display = LocalizationHelper.GetString("Inverse"), Value = "Inverse" },
            new() { Display = LocalizationHelper.GetString("Switchable"), Value = "ClearInverse" },
         ];

    private bool _useTray = Convert.ToBoolean(ConfigurationHelper.GetGlobalValue(ConfigurationKeys.UseTray, bool.TrueString));

    /// <summary>
    /// Gets or sets a value indicating whether to use tray icon.
    /// </summary>
    public bool UseTray
    {
        get => _useTray;
        set
        {
            if (!value)
            {
                MinimizeToTray = false;
            }

            SetAndNotify(ref _useTray, value);
            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.UseTray, value.ToString());
            Instances.MainWindowManager.SetUseTrayIcon(value);
        }
    }

    private bool _minimizeToTray = Convert.ToBoolean(ConfigurationHelper.GetGlobalValue(ConfigurationKeys.MinimizeToTray, bool.FalseString));

    /// <summary>
    /// Gets or sets a value indicating whether to minimize to tray.
    /// </summary>
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set
        {
            SetAndNotify(ref _minimizeToTray, value);
            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.MinimizeToTray, value.ToString());
            Instances.MainWindowManager.SetMinimizeToTray(value);
        }
    }

    private bool _windowTitleScrollable = Convert.ToBoolean(ConfigurationHelper.GetGlobalValue(ConfigurationKeys.WindowTitleScrollable, bool.FalseString));

    /// <summary>
    /// Gets or sets a value indicating whether to make window title scrollable.
    /// </summary>
    public bool WindowTitleScrollable
    {
        get => _windowTitleScrollable;
        set
        {
            SetAndNotify(ref _windowTitleScrollable, value);
            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.WindowTitleScrollable, value.ToString());
            var rvm = (RootViewModel)Instances.SettingsViewModel.Parent;
            rvm.WindowTitleScrollable = value;
        }
    }

    private bool _hideCloseButton = Convert.ToBoolean(ConfigurationHelper.GetGlobalValue(ConfigurationKeys.HideCloseButton, bool.FalseString));

    /// <summary>
    /// Gets or sets a value indicating whether to hide close button.
    /// </summary>
    public bool HideCloseButton
    {
        get => _hideCloseButton;
        set
        {
            SetAndNotify(ref _hideCloseButton, value);
            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.HideCloseButton, value.ToString());
            var rvm = (RootViewModel)Instances.SettingsViewModel.Parent;
            rvm.ShowCloseButton = !value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use notification.
    /// </summary>
    public bool UseNotify
    {
        get => ConfigFactory.Root.GUI.UseNotify;
        set
        {
            ConfigFactory.Root.GUI.UseNotify = value;
            if (value)
            {
                ToastNotification.ShowDirect("Test test");
            }
        }
    }

    public bool InvertNullFunction
    {
        get => ConfigFactory.Root.GUI.InvertNullFunction;
        set
        {
            ConfigFactory.Root.GUI.InvertNullFunction = value;
        }
    }

    public List<string> LogItemDateFormatStringList { get; } =
    [
        "HH:mm:ss",
        "MM-dd  HH:mm:ss",
        "MM/dd  HH:mm:ss",
        "MM.dd  HH:mm:ss",
        "dd-MM  HH:mm:ss",
        "dd/MM  HH:mm:ss",
        "dd.MM  HH:mm:ss",
    ];

    private string _logItemDateFormatString = ConfigurationHelper.GetGlobalValue(ConfigurationKeys.LogItemDateFormat, "HH:mm:ss");

    public string LogItemDateFormatString
    {
        get => _logItemDateFormatString;
        set
        {
            SetAndNotify(ref _logItemDateFormatString, value);
            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.LogItemDateFormat, value);
        }
    }

    /// <summary>
    /// Gets or sets the dark mode.
    /// </summary>
    public DarkModeType DarkMode
    {
        get => ConfigFactory.Root.GUI.DarkMode;
        set
        {
            ConfigFactory.Root.GUI.DarkMode = value;
            NotifyOfPropertyChange();
            SwitchDarkMode();

            /*
            AskToRestartToApplySettings();
            */
        }
    }

    public void SwitchDarkMode()
    {
        DarkModeType darkModeType = ConfigFactory.Root.GUI.DarkMode;
        switch (darkModeType)
        {
            case DarkModeType.Light:
                ThemeHelper.SwitchToLightMode();
                break;

            case DarkModeType.Dark:
                ThemeHelper.SwitchToDarkMode();
                break;

            case DarkModeType.SyncWithOs:
                ThemeHelper.SwitchToSyncWithOsMode();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private enum InverseClearType
    {
        Clear,
        Inverse,
        ClearInverse,
    }

    private InverseClearType _inverseClearMode =
        Enum.TryParse(ConfigurationHelper.GetGlobalValue(ConfigurationKeys.InverseClearMode, InverseClearType.Clear.ToString()), out InverseClearType temp)
            ? temp
            : InverseClearType.Clear;

    /// <summary>
    /// Gets or sets the inverse clear mode.
    /// </summary>
    public string InverseClearMode
    {
        get => _inverseClearMode.ToString();
        set
        {
            if (!Enum.TryParse(value, out InverseClearType tempEnumValue))
            {
                return;
            }

            SetAndNotify(ref _inverseClearMode, tempEnumValue);
            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.InverseClearMode, value);
            switch (tempEnumValue)
            {
                case InverseClearType.Clear:
                    Instances.TaskQueueViewModel.InverseMode = false;
                    Instances.TaskQueueViewModel.ShowInverse = false;
                    Instances.TaskQueueViewModel.SelectedAllWidth = 90;
                    break;

                case InverseClearType.Inverse:
                    Instances.TaskQueueViewModel.InverseMode = true;
                    Instances.TaskQueueViewModel.ShowInverse = false;
                    Instances.TaskQueueViewModel.SelectedAllWidth = 90;
                    break;

                case InverseClearType.ClearInverse:
                    Instances.TaskQueueViewModel.ShowInverse = true;
                    Instances.TaskQueueViewModel.SelectedAllWidth = TaskQueueViewModel.SelectedAllWidthWhenBoth;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static readonly Dictionary<string, string> _windowTitleAllShowDict = new()
        {
            { LocalizationHelper.GetString("ConfigurationName"), "1" },
            { LocalizationHelper.GetString("ConnectionPreset"), "2" },
            { LocalizationHelper.GetString("ConnectionAddress"), "3" },
            { LocalizationHelper.GetString("ClientType"), "4" },
        };

    public static Dictionary<string, string> WindowTitleAllShowDict { get => _windowTitleAllShowDict; }

    private static List<string> _windowTitleAllShowList = [.. _windowTitleAllShowDict.Keys];

    public List<string> WindowTitleAllShowList
    {
        get => _windowTitleAllShowList;
        set => SetAndNotify(ref _windowTitleAllShowList, value);
    }

    private static object[] _windowTitleSelectShowList = ConfigurationHelper.GetGlobalValue(ConfigurationKeys.WindowTitleSelectShowList, "1 2 3 4")
        .Split(' ')
        .Where(s => _windowTitleAllShowDict.ContainsValue(s.ToString()))
        .Select(s => _windowTitleAllShowDict.FirstOrDefault(pair => pair.Value == s).Key)
        .ToArray();

    public object[] WindowTitleSelectShowList
    {
        get => _windowTitleSelectShowList;
        set
        {
            SetAndNotify(ref _windowTitleSelectShowList, value);
            Instances.SettingsViewModel.UpdateWindowTitle();
            var config = string.Join(' ', _windowTitleSelectShowList.Cast<string>().Select(s => _windowTitleAllShowDict[s]));
            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.WindowTitleSelectShowList, config);
        }
    }

    private string _language = ConfigurationHelper.GetGlobalValue(ConfigurationKeys.Localization, LocalizationHelper.DefaultLanguage);

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string Language
    {
        get => _language;
        set
        {
            if (value == _language)
            {
                return;
            }

            if (_language == SettingsViewModel.PallasLangKey)
            {
                Instances.SettingsViewModel.Hangover = true;
                Instances.SettingsViewModel.Cheers = false;
            }

            if (value != SettingsViewModel.PallasLangKey)
            {
                Instances.SettingsViewModel.SoberLanguage = value;
            }

            // var backup = _language;
            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.Localization, value);

            var mainWindow = Application.Current.MainWindow;

            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }

            var result = MessageBoxHelper.Show(
                FormatText("{0}\n{1}", "LanguageChangedTip"),
                FormatText("{0}({1})", "Tip"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question,
                ok: FormatText("{0}({1})", "Ok"),
                cancel: FormatText("{0}({1})", "ManualRestart"));
            if (result == MessageBoxResult.OK)
            {
                Bootstrapper.ShutdownAndRestartWithoutArgs();
            }

            SetAndNotify(ref _language, value);

            return;

            string FormatText(string text, string key)
                => string.Format(text, LocalizationHelper.GetString(key, value), LocalizationHelper.GetString(key, _language));
        }
    }

    /// <summary>
    /// Gets the language info.
    /// </summary>
    public string LanguageInfo
    {
        get
        {
            var language = (string?)Application.Current.Resources["Language"];
            return language == "Language" ? language : language + " / Language";
        }
    }

    /// <summary>
    /// Opername display language, can set force display when it was set as "OperNameLanguageForce.en-us"
    /// </summary>
    private string _operNameLanguage = ConfigurationHelper.GetGlobalValue(ConfigurationKeys.OperNameLanguage, "OperNameLanguageMAA");

    public string OperNameLanguage
    {
        get
        {
            if (!_operNameLanguage.Contains('.'))
            {
                return _operNameLanguage;
            }

            if (_operNameLanguage.Split('.')[0] != "OperNameLanguageForce" || !LocalizationHelper.SupportedLanguages.ContainsKey(_operNameLanguage.Split('.')[1]))
            {
                return _operNameLanguage;
            }

            OperNameLanguageModeList.Add(new CombinedData { Display = LocalizationHelper.GetString("OperNameLanguageForce"), Value = "OperNameLanguageForce" });
            return "OperNameLanguageForce";
        }

        set
        {
            if (value == _operNameLanguage.Split('.')[0])
            {
                return;
            }

            switch (value)
            {
                case "OperNameLanguageClient":
                    ConfigurationHelper.SetGlobalValue(ConfigurationKeys.OperNameLanguage, value);
                    break;

                case "OperNameLanguageMAA":
                default:
                    ConfigurationHelper.SetGlobalValue(ConfigurationKeys.OperNameLanguage, "OperNameLanguageMAA");
                    break;
            }

            var mainWindow = Application.Current.MainWindow;

            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }

            var result = MessageBoxHelper.Show(
                LocalizationHelper.GetString("LanguageChangedTip"),
                LocalizationHelper.GetString("Tip"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question,
                ok: LocalizationHelper.GetString("Ok"),
                cancel: LocalizationHelper.GetString("ManualRestart"));
            if (result == MessageBoxResult.OK)
            {
                Bootstrapper.ShutdownAndRestartWithoutArgs();
            }

            SetAndNotify(ref _operNameLanguage, value);
        }
    }

    public string OperNameLocalization
    {
        get
        {
            if (_operNameLanguage == "OperNameLanguageClient")
            {
                return DataHelper.ClientLanguageMapper[SettingsViewModel.GameSettings.ClientType];
            }

            if (!_operNameLanguage.Contains('.'))
            {
                return _language;
            }

            if (_operNameLanguage.Split('.')[0] == "OperNameLanguageForce" && LocalizationHelper.SupportedLanguages.ContainsKey(_operNameLanguage.Split('.')[1]))
            {
                return _operNameLanguage.Split('.')[1];
            }

            return _language;
        }
    }
}
