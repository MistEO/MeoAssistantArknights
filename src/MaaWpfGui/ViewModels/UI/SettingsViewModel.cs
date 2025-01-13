// <copyright file="SettingsViewModel.cs" company="MaaAssistantArknights">
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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Controls;
using HandyControl.Data;
using MaaWpfGui.Constants;
using MaaWpfGui.Extensions;
using MaaWpfGui.Helper;
using MaaWpfGui.Main;
using MaaWpfGui.Models;
using MaaWpfGui.Services.HotKeys;
using MaaWpfGui.States;
using MaaWpfGui.Utilities.ValueType;
using MaaWpfGui.ViewModels.UserControl.Settings;
using MaaWpfGui.ViewModels.UserControl.TaskQueue;
using Newtonsoft.Json;
using Serilog;
using Stylet;
using ComboBox = System.Windows.Controls.ComboBox;
using Timer = System.Timers.Timer;

namespace MaaWpfGui.ViewModels.UI
{
    /// <summary>
    /// The view model of settings.
    /// </summary>
    public class SettingsViewModel : Screen
    {
        private readonly RunningState _runningState;

        private static readonly ILogger _logger = Log.ForContext<SettingsViewModel>();

        /// <summary>
        /// Gets the visibility of task setting views.
        /// </summary>
        public TaskSettingVisibilityInfo TaskSettingVisibilities { get; } = TaskSettingVisibilityInfo.Current;

        /// <summary>
        /// Gets the after action setting.
        /// </summary>
        public PostActionSetting PostActionSetting { get; } = PostActionSetting.Current;

        #region 长草任务Model

        /// <summary>
        /// Gets 连接任务Model
        /// </summary>
        public static StartUpTaskUserControlModel StartUpTask { get; } = new();

        /// <summary>
        /// Gets 战斗任务Model
        /// </summary>
        public static FightSettingsUserControlModel FightTask { get; } = new();

        /// <summary>
        /// Gets 招募任务Model
        /// </summary>
        public static RecruitSettingsUserControlModel RecruitTask { get; } = new();

        /// <summary>
        /// Gets 信用及购物任务Model
        /// </summary>
        public static MallSettingsUserControlModel MallTask { get; } = new();

        /// <summary>
        /// Gets 基建任务Model
        /// </summary>
        public static InfrastSettingsUserControlModel InfrastTask { get; } = new();

        /// <summary>
        /// Gets 领取奖励任务
        /// </summary>
        public static AwardSettingsUserControlModel AwardTask { get; } = new();

        /// <summary>
        /// Gets 肉鸽任务Model
        /// </summary>
        public static RoguelikeSettingsUserControlModel RoguelikeTask { get; } = new();

        /// <summary>
        /// Gets 生稀盐酸任务Model
        /// </summary>
        public static ReclamationSettingsUserControlModel ReclamationTask { get; } = new();

        #endregion 长草任务Model

        #region 设置界面Model

        /// <summary>
        /// Gets 游戏设置model
        /// </summary>
        public static GameSettingsUserControlModel GameSettings { get; } = GameSettingsUserControlModel.Instance;

        /// <summary>
        /// Gets 连接设置model
        /// </summary>
        public static ConnectSettingsUserControlModel ConnectSettings { get; } = ConnectSettingsUserControlModel.Instance;

        /// <summary>
        /// Gets 启动设置Model
        /// </summary>
        public static StartSettingsUserControlModel StartSettings { get; } = StartSettingsUserControlModel.Instance;

        /// <summary>
        /// Gets 界面设置model
        /// </summary>
        public static GuiSettingsUserControlModel GuiSettings { get; } = GuiSettingsUserControlModel.Instance;

        /// <summary>
        /// Gets 定时设置model
        /// </summary>
        public static TimerSettingsUserControlModel TimerSettings { get; } = TimerSettingsUserControlModel.Instance;

        /// <summary>
        /// Gets 远程控制model
        /// </summary>
        public static RemoteControlUserControlModel RemoteControlSettings { get; } = RemoteControlUserControlModel.Instance;

        /// <summary>
        /// Gets 软件更新model
        /// </summary>
        public static VersionUpdateSettingsUserControlModel VersionUpdateSettings { get; } = VersionUpdateSettingsUserControlModel.Instance;

        /// <summary>
        /// Gets 外部通知model
        /// </summary>
        public static ExternalNotificationSettingsUserControlModel ExternalNotificationSettings { get; } = ExternalNotificationSettingsUserControlModel.Instance;

        /// <summary>
        /// Gets 性能设置model
        /// </summary>
        public static PerformanceUserControlModel PerformanceSettings { get; } = PerformanceUserControlModel.Instance;

        #endregion 设置界面Model

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        public SettingsViewModel()
        {
            DisplayName = LocalizationHelper.GetString("Settings");

            Init();

            HangoverEnd();

            _runningState = RunningState.Instance;
        }

        #region Init

        private List<string> _listTitle =
        [
            LocalizationHelper.GetString("SwitchConfiguration"),
            LocalizationHelper.GetString("ScheduleSettings"),
            LocalizationHelper.GetString("PerformanceSettings"),
            LocalizationHelper.GetString("GameSettings"),
            LocalizationHelper.GetString("ConnectionSettings"),
            LocalizationHelper.GetString("StartupSettings"),
            LocalizationHelper.GetString("RemoteControlSettings"),
            LocalizationHelper.GetString("UiSettings"),
            LocalizationHelper.GetString("ExternalNotificationSettings"),
            LocalizationHelper.GetString("HotKeySettings"),
            LocalizationHelper.GetString("UpdateSettings"),
            LocalizationHelper.GetString("AboutUs"),
        ];

        /// <summary>
        /// Gets or sets the list title.
        /// </summary>
        public List<string> ListTitle
        {
            get => _listTitle;
            set => SetAndNotify(ref _listTitle, value);
        }

        private bool _idle;

        /// <summary>
        /// Gets or sets a value indicating whether it is idle.
        /// </summary>
        public bool Idle
        {
            get => _idle;
            set => SetAndNotify(ref _idle, value);
        }

        private void Init()
        {
            InfrastTask.InitInfrast();
            RoguelikeTask.InitRoguelike();
            InitConfiguration();
            InitUiSettings();
            InitConnectConfig();
            InitVersionUpdate();
        }

        private void InitConfiguration()
        {
            var configurations = new ObservableCollection<CombinedData>();
            foreach (var conf in ConfigurationHelper.GetConfigurationList())
            {
                configurations.Add(new CombinedData { Display = conf, Value = conf });
            }

            ConfigurationList = configurations;
        }

        private void InitUiSettings()
        {
            var languageList = (from pair in LocalizationHelper.SupportedLanguages
                                where pair.Key != PallasLangKey || Cheers
                                select new CombinedData { Display = pair.Value, Value = pair.Key })
               .ToList();

            GuiSettings.LanguageList = languageList;
            GuiSettings.SwitchDarkMode();
        }

        private void InitConnectConfig()
        {
            var addressListJson = ConfigurationHelper.GetValue(ConfigurationKeys.AddressHistory, string.Empty);
            if (!string.IsNullOrEmpty(addressListJson))
            {
                ConnectSettings.ConnectAddressHistory = JsonConvert.DeserializeObject<ObservableCollection<string>>(addressListJson) ?? [];
            }
        }

        private void InitVersionUpdate()
        {
            if (VersionUpdateSettings.VersionType == VersionUpdateSettingsUserControlModel.UpdateVersionType.Nightly && !VersionUpdateSettings.AllowNightlyUpdates)
            {
                VersionUpdateSettings.VersionType = VersionUpdateSettingsUserControlModel.UpdateVersionType.Beta;
            }
        }

        #endregion Init

        #region EasterEggs

        /// <summary>
        /// The Pallas language key.
        /// </summary>
        public const string PallasLangKey = "pallas";

        private bool _cheers = Convert.ToBoolean(ConfigurationHelper.GetGlobalValue(ConfigurationKeys.Cheers, bool.FalseString));

        /// <summary>
        /// Gets or sets a value indicating whether to cheer.
        /// </summary>
        public bool Cheers
        {
            get => _cheers;
            set
            {
                if (_cheers == value)
                {
                    return;
                }

                SetAndNotify(ref _cheers, value);
                ConfigurationHelper.SetGlobalValue(ConfigurationKeys.Cheers, value.ToString());
                if (_cheers)
                {
                    ConfigurationHelper.SetGlobalValue(ConfigurationKeys.Localization, PallasLangKey);
                }
            }
        }

        private bool _hangover = Convert.ToBoolean(ConfigurationHelper.GetGlobalValue(ConfigurationKeys.Hangover, bool.FalseString));

        /// <summary>
        /// Gets or sets a value indicating whether to hangover.
        /// </summary>
        public bool Hangover
        {
            get => _hangover;
            set
            {
                SetAndNotify(ref _hangover, value);
                ConfigurationHelper.SetGlobalValue(ConfigurationKeys.Hangover, value.ToString());
            }
        }

        private string _lastBuyWineTime = ConfigurationHelper.GetGlobalValue(ConfigurationKeys.LastBuyWineTime, DateTime.UtcNow.ToYjDate().AddDays(-1).ToFormattedString());

        public string LastBuyWineTime
        {
            get => _lastBuyWineTime;
            set
            {
                SetAndNotify(ref _lastBuyWineTime, value);
                ConfigurationHelper.SetGlobalValue(ConfigurationKeys.LastBuyWineTime, value);
            }
        }

        public void HangoverEnd()
        {
            if (!Hangover)
            {
                return;
            }

            Hangover = false;
            MessageBoxHelper.Show(
                LocalizationHelper.GetString("Hangover"),
                LocalizationHelper.GetString("Burping"),
                iconKey: "HangoverGeometry",
                iconBrushKey: "PallasBrush");
            Bootstrapper.ShutdownAndRestartWithoutArgs();
        }

        public void Sober()
        {
            if (!Cheers || GuiSettings.Language != PallasLangKey)
            {
                return;
            }

            ConfigurationHelper.SetGlobalValue(ConfigurationKeys.Localization, SoberLanguage);
            Hangover = true;
            Cheers = false;
        }

        private string _soberLanguage = ConfigurationHelper.GetGlobalValue(ConfigurationKeys.SoberLanguage, LocalizationHelper.DefaultLanguage);

        public string SoberLanguage
        {
            get => _soberLanguage;
            set
            {
                SetAndNotify(ref _soberLanguage, value);
                ConfigurationHelper.SetGlobalValue(ConfigurationKeys.SoberLanguage, value);
            }
        }

        /// <summary>
        /// Did you buy wine?
        /// </summary>
        /// <returns>The answer.</returns>
        public bool DidYouBuyWine()
        {
            var now = DateTime.UtcNow.ToYjDate();
            if (now == DateTime.ParseExact(LastBuyWineTime.Replace('-', '/'), "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture))
            {
                return false;
            }

            if (now.IsAprilFoolsDay())
            {
                return true;
            }

            string[] wineList = ["酒", "liquor", "drink", "wine", "beer", "술", "🍷", "🍸", "🍺", "🍻", "🥃", "🍶"];
            return wineList.Any(MallTask.CreditFirstList.Contains);
        }

        #endregion EasterEggs

        #region HotKey

        /// <summary>
        /// Gets or sets the hotkey: ShowGui.
        /// </summary>
        public static MaaHotKey HotKeyShowGui
        {
            get => Instances.MaaHotKeyManager.GetOrNull(MaaHotKeyAction.ShowGui);
            set => SetHotKey(MaaHotKeyAction.ShowGui, value);
        }

        /// <summary>
        /// Gets or sets the hotkey: LinkStart.
        /// </summary>
        public static MaaHotKey HotKeyLinkStart
        {
            get => Instances.MaaHotKeyManager.GetOrNull(MaaHotKeyAction.LinkStart);
            set => SetHotKey(MaaHotKeyAction.LinkStart, value);
        }

        private static void SetHotKey(MaaHotKeyAction action, MaaHotKey value)
        {
            if (value != null)
            {
                Instances.MaaHotKeyManager.TryRegister(action, value);
            }
            else
            {
                Instances.MaaHotKeyManager.UnRegister(action);
            }
        }

        #endregion HotKey

        #region 配置

        public ObservableCollection<CombinedData> ConfigurationList { get; set; }

        private string? _currentConfiguration = ConfigurationHelper.GetCurrentConfiguration();

        public string? CurrentConfiguration
        {
            get => _currentConfiguration;
            set
            {
                SetAndNotify(ref _currentConfiguration, value);
                ConfigurationHelper.SwitchConfiguration(value);

                Bootstrapper.ShutdownAndRestartWithoutArgs();
            }
        }

        private string _newConfigurationName;

        public string NewConfigurationName
        {
            get => _newConfigurationName;
            set => SetAndNotify(ref _newConfigurationName, value);
        }

        // UI 绑定的方法
        // ReSharper disable once UnusedMember.Global
        public void AddConfiguration()
        {
            if (string.IsNullOrEmpty(NewConfigurationName))
            {
                NewConfigurationName = DateTime.Now.ToString("yy/MM/dd HH:mm:ss");
            }

            if (ConfigurationHelper.AddConfiguration(NewConfigurationName, CurrentConfiguration))
            {
                ConfigurationList.Add(new CombinedData { Display = NewConfigurationName, Value = NewConfigurationName });

                var growlInfo = new GrowlInfo
                {
                    IsCustom = true,
                    Message = string.Format(LocalizationHelper.GetString("AddConfigSuccess"), NewConfigurationName),
                    IconKey = "HangoverGeometry",
                    IconBrushKey = "PallasBrush",
                };
                Growl.Info(growlInfo);
            }
            else
            {
                var growlInfo = new GrowlInfo
                {
                    IsCustom = true,
                    Message = string.Format(LocalizationHelper.GetString("ConfigExists"), NewConfigurationName),
                    IconKey = "HangoverGeometry",
                    IconBrushKey = "PallasBrush",
                };
                Growl.Info(growlInfo);
            }
        }

        // UI 绑定的方法
        // ReSharper disable once UnusedMember.Global
        public void DeleteConfiguration(CombinedData delete)
        {
            if (ConfigurationHelper.DeleteConfiguration(delete.Display))
            {
                ConfigurationList.Remove(delete);
            }
        }

        #endregion 配置

        #region SettingsGuide

        // 目前共4步，再多塞不下了，后续可以整个新功能展示（
        public const int GuideMaxStep = 4;

        private int _guideStepIndex = Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.GuideStepIndex, "0"));

        public int GuideStepIndex
        {
            get => _guideStepIndex;
            set
            {
                SetAndNotify(ref _guideStepIndex, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.GuideStepIndex, value.ToString());
            }
        }

        private string _guideTransitionMode = "Bottom2Top";

        public string GuideTransitionMode
        {
            get => _guideTransitionMode;
            set => SetAndNotify(ref _guideTransitionMode, value);
        }

        // UI 绑定的方法
        // ReSharper disable once UnusedMember.Global
        public void NextGuide(StepBar stepBar)
        {
            GuideTransitionMode = "Bottom2Top";
            stepBar.Next();
        }

        // UI 绑定的方法
        // ReSharper disable once UnusedMember.Global
        public void PrevGuide(StepBar stepBar)
        {
            GuideTransitionMode = "Top2Bottom";
            stepBar.Prev();
        }

        // UI 绑定的方法
        // ReSharper disable once UnusedMember.Global
        public void DoneGuide()
        {
            TaskSettingVisibilities.Guide = false;
            GuideStepIndex = GuideMaxStep;
        }

        #endregion SettingsGuide

        #region 设置页面列表和滚动视图联动绑定

        private enum NotifyType
        {
            None,
            SelectedIndex,
            ScrollOffset,
        }

        private NotifyType _notifySource = NotifyType.None;

        private Timer _resetNotifyTimer;

        private void ResetNotifySource()
        {
            if (_resetNotifyTimer != null)
            {
                _resetNotifyTimer.Stop();
                _resetNotifyTimer.Close();
            }

            _resetNotifyTimer = new Timer(20);
            _resetNotifyTimer.Elapsed += (_, _) =>
            {
                _notifySource = NotifyType.None;
            };
            _resetNotifyTimer.AutoReset = false;
            _resetNotifyTimer.Enabled = true;
            _resetNotifyTimer.Start();
        }

        /// <summary>
        /// Gets or sets the height of scroll viewport.
        /// </summary>
        public double ScrollViewportHeight { get; set; }

        /// <summary>
        /// Gets or sets the extent height of scroll.
        /// </summary>
        public double ScrollExtentHeight { get; set; }

        /// <summary>
        /// Gets or sets the list of divider vertical offset.
        /// </summary>
        public List<double> DividerVerticalOffsetList { get; set; } = new();

        private int _selectedIndex;

        /// <summary>
        /// Gets or sets the index selected.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                switch (_notifySource)
                {
                    case NotifyType.None:
                        _notifySource = NotifyType.SelectedIndex;
                        SetAndNotify(ref _selectedIndex, value);

                        if (DividerVerticalOffsetList?.Count > 0 && value < DividerVerticalOffsetList.Count)
                        {
                            ScrollOffset = DividerVerticalOffsetList[value];
                        }

                        ResetNotifySource();
                        break;

                    case NotifyType.ScrollOffset:
                        SetAndNotify(ref _selectedIndex, value);
                        break;

                    case NotifyType.SelectedIndex:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private double _scrollOffset;

        /// <summary>
        /// Gets or sets the scroll offset.
        /// </summary>
        public double ScrollOffset
        {
            get => _scrollOffset;
            set
            {
                switch (_notifySource)
                {
                    case NotifyType.None:
                        _notifySource = NotifyType.ScrollOffset;
                        SetAndNotify(ref _scrollOffset, value);

                        // 设置 ListBox SelectedIndex 为当前 ScrollOffset 索引
                        if (DividerVerticalOffsetList?.Count > 0)
                        {
                            // 滚动条滚动到底部，返回最后一个 Divider 索引
                            if (value + ScrollViewportHeight >= ScrollExtentHeight)
                            {
                                SelectedIndex = DividerVerticalOffsetList.Count - 1;
                                ResetNotifySource();
                                break;
                            }

                            // 根据出当前 ScrollOffset 选出最后一个在可视范围的 Divider 索引
                            var dividerSelect = DividerVerticalOffsetList.Select((n, i) => (
                                dividerAppeared: value >= n,
                                index: i));

                            var index = dividerSelect.LastOrDefault(n => n.dividerAppeared).index;
                            SelectedIndex = index;
                        }

                        ResetNotifySource();
                        break;

                    case NotifyType.SelectedIndex:
                        SetAndNotify(ref _scrollOffset, value);
                        break;

                    case NotifyType.ScrollOffset:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion 设置页面列表和滚动视图联动绑定

        /// <summary>
        /// Requires the user to restart to apply settings.
        /// </summary>
        /// <param name="isYostarEN">Whether to include the YostarEN resolution tip.</param>
        public static void AskRestartToApplySettings(bool isYostarEN = false)
        {
            var resolutionTip = isYostarEN ? "\n" + LocalizationHelper.GetString("SwitchResolutionTip") : string.Empty;

            var result = MessageBoxHelper.Show(
                LocalizationHelper.GetString("PromptRestartForSettingsChange") + resolutionTip,
                LocalizationHelper.GetString("Tip"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                Bootstrapper.ShutdownAndRestartWithoutArgs();
            }
        }

        /// <summary>
        /// Make comboBox searchable
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        // UI 绑定的方法
        // EventArgs 不能省略，否则会报错
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once UnusedParameter.Global
        public static void MakeComboBoxSearchable(object sender, EventArgs e)
        {
            (sender as ComboBox)?.MakeComboBoxSearchable();
        }

        // UI 绑定的方法
        // ReSharper disable once UnusedMember.Global
        public async Task CheckAndDownloadAnnouncement()
        {
            await Instances.AnnouncementViewModel.CheckAndDownloadAnnouncement();
            _ = Execute.OnUIThreadAsync(() => Instances.WindowManager.ShowWindow(Instances.AnnouncementViewModel));
        }

        // UI 绑定的方法
        // ReSharper disable once UnusedMember.Global
        public void SetAcknowledgedNightlyWarning()
        {
            VersionUpdateSettings.HasAcknowledgedNightlyWarning = true;
        }

        /// <summary>
        /// 标题栏显示模拟器名称和IP端口。
        /// </summary>
        public void UpdateWindowTitle()
        {
            var rvm = (RootViewModel)this.Parent;

            string prefix = ConfigurationHelper.GetValue(ConfigurationKeys.WindowTitlePrefix, string.Empty);
            if (!string.IsNullOrEmpty(prefix))
            {
                prefix += " - ";
            }

            List<string> windowTitleSelectShowList = GuiSettings.WindowTitleSelectShowList
                .Where(x => GuiSettingsUserControlModel.WindowTitleAllShowDict.ContainsKey(x?.ToString() ?? string.Empty))
                .Select(x => GuiSettingsUserControlModel.WindowTitleAllShowDict[x?.ToString() ?? string.Empty]).ToList();

            string currentConfiguration = string.Empty;
            string connectConfigName = string.Empty;
            string connectAddress = string.Empty;
            string clientName = string.Empty;

            foreach (var select in windowTitleSelectShowList)
            {
                switch (select)
                {
                    case "1": // 配置名
                        currentConfiguration = $" ({CurrentConfiguration})";
                        break;

                    case "2": // 连接模式
                        foreach (var data in ConnectSettings.ConnectConfigList.Where(data => data.Value == ConnectSettings.ConnectConfig))
                        {
                            connectConfigName = $" - {data.Display}";
                        }

                        break;

                    case "3": // 端口地址
                        connectAddress = $" ({ConnectSettings.ConnectAddress})";
                        break;

                    case "4": // 客户端类型
                        clientName = $" - {ClientName}";
                        break;
                }
            }

            string resourceVersion = !string.IsNullOrEmpty(VersionUpdateSettings.ResourceVersion)
                ? LocalizationHelper.CustomCultureInfo.Name.ToLowerInvariant() switch
                {
                    "zh-cn" => $" - {VersionUpdateSettings.ResourceVersion}{VersionUpdateSettings.ResourceDateTime:#MMdd}",
                    "zh-tw" => $" - {VersionUpdateSettings.ResourceVersion}{VersionUpdateSettings.ResourceDateTime:#MMdd}",
                    "en-us" => $" - {VersionUpdateSettings.ResourceDateTime:dd/MM} {VersionUpdateSettings.ResourceVersion}",
                    _ => $" - {VersionUpdateSettings.ResourceDateTime.ToString(LocalizationHelper.CustomCultureInfo.DateTimeFormat.ShortDatePattern.Replace("yyyy", string.Empty).Trim('/', '.'))} {VersionUpdateSettings.ResourceVersion}",
                }
                : string.Empty;
            rvm.WindowTitle = $"{prefix}MAA{currentConfiguration} - {VersionUpdateSettingsUserControlModel.CoreVersion}{resourceVersion}{connectConfigName}{connectAddress}{clientName}";
        }

        /// <summary>
        /// Gets the client type.
        /// </summary>
        private string ClientName
        {
            get
            {
                foreach (var item in GameSettings.ClientTypeList.Where(item => item.Value == GameSettings.ClientType))
                {
                    return item.Display;
                }

                return "Unknown Client";
            }
        }

        private readonly Dictionary<string, string> _serverMapping = new()
        {
            { string.Empty, "CN" },
            { "Official", "CN" },
            { "Bilibili", "CN" },
            { "YoStarEN", "US" },
            { "YoStarJP", "JP" },
            { "YoStarKR", "KR" },
            { "txwy", "ZH_TW" },
        };

        /// <summary>
        /// Gets the server type.
        /// </summary>
        public string ServerType => _serverMapping[GameSettings.ClientType];
    }
}
