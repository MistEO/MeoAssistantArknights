// <copyright file="RecognizerViewModel.cs" company="MaaAssistantArknights">
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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MaaWpfGui.Constants;
using MaaWpfGui.Helper;
using MaaWpfGui.States;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stylet;

namespace MaaWpfGui.ViewModels.UI
{
    /// <summary>
    /// The view model of recruit.
    /// </summary>
    public class RecognizerViewModel : Screen
    {
        private readonly RunningState _runningState;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecognizerViewModel"/> class.
        /// </summary>
        public RecognizerViewModel()
        {
            DisplayName = LocalizationHelper.GetString("Toolbox");
            _runningState = RunningState.Instance;
            _runningState.IdleChanged += RunningState_IdleChanged;
        }

        private void RunningState_IdleChanged(object? sender, bool e)
        {
            Idle = e;
        }

        private bool _idle;

        /// <summary>
        /// Gets or sets a value indicating whether it is idle.
        /// </summary>
        public bool Idle
        {
            get => _idle;
            set
            {
                SetAndNotify(ref _idle, value);

                if (!value)
                {
                    return;
                }

                // 识别完成、主界面暂停或者连接出错时
                GachaDone = true;
                DepotDone = true;
                OperBoxDone = true;
            }
        }

        #region Recruit

        private string _recruitInfo = LocalizationHelper.GetString("RecruitmentRecognitionTip");

        /// <summary>
        /// Gets or sets the recruit info.
        /// </summary>
        public string RecruitInfo
        {
            get => _recruitInfo;
            set => SetAndNotify(ref _recruitInfo, value);
        }

        private string _recruitResult = string.Empty;

        /// <summary>
        /// Gets or sets the recruit result.
        /// </summary>
        public string RecruitResult
        {
            get => _recruitResult;
            set => SetAndNotify(ref _recruitResult, value);
        }

        private bool _chooseLevel3 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel3, bool.FalseString));

        /// <summary>
        /// Gets or sets a value indicating whether to choose level 3.
        /// </summary>
        public bool ChooseLevel3
        {
            get => _chooseLevel3;
            set
            {
                SetAndNotify(ref _chooseLevel3, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.ChooseLevel3, value.ToString());
            }
        }

        private bool _chooseLevel4 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel4, bool.TrueString));

        /// <summary>
        /// Gets or sets a value indicating whether to choose level 4.
        /// </summary>
        public bool ChooseLevel4
        {
            get => _chooseLevel4;
            set
            {
                SetAndNotify(ref _chooseLevel4, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.ChooseLevel4, value.ToString());
            }
        }

        private bool _chooseLevel5 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel5, bool.TrueString));

        /// <summary>
        /// Gets or sets a value indicating whether to choose level 5.
        /// </summary>
        public bool ChooseLevel5
        {
            get => _chooseLevel5;
            set
            {
                SetAndNotify(ref _chooseLevel5, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.ChooseLevel5, value.ToString());
            }
        }

        private bool _chooseLevel6 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel6, bool.TrueString));

        /// <summary>
        /// Gets or sets a value indicating whether to choose level 6.
        /// </summary>
        public bool ChooseLevel6
        {
            get => _chooseLevel6;
            set
            {
                SetAndNotify(ref _chooseLevel6, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.ChooseLevel6, value.ToString());
            }
        }

        private bool _autoSetTime = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.AutoSetTime, bool.TrueString));

        /// <summary>
        /// Gets or sets a value indicating whether to set time automatically.
        /// </summary>
        public bool RecruitAutoSetTime
        {
            get => _autoSetTime;
            set
            {
                SetAndNotify(ref _autoSetTime, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.AutoSetTime, value.ToString());
            }
        }

        private bool _isLevel3UseShortTime = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.Level3UseShortTime, bool.FalseString));

        /// <summary>
        /// Gets or sets a value indicating whether to shorten the time for level 3.
        /// </summary>
        public bool IsLevel3UseShortTime
        {
            get => _isLevel3UseShortTime;
            set
            {
                if (value)
                {
                    IsLevel3UseShortTime2 = false;
                }

                SetAndNotify(ref _isLevel3UseShortTime, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.Level3UseShortTime, value.ToString());
            }
        }

        private bool _isLevel3UseShortTime2 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.Level3UseShortTime2, bool.FalseString));

        /// <summary>
        /// Gets or sets a value indicating whether to shorten the time for level 3.
        /// </summary>
        public bool IsLevel3UseShortTime2
        {
            get => _isLevel3UseShortTime2;
            set
            {
                if (value)
                {
                    IsLevel3UseShortTime = false;
                }

                SetAndNotify(ref _isLevel3UseShortTime2, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.Level3UseShortTime2, value.ToString());
            }
        }

        private bool _recruitCaught;

        /// <summary>
        /// Starts calculation.
        /// </summary>
        // UI 绑定的方法
        // ReSharper disable once UnusedMember.Global
        public async void RecruitStartCalc()
        {
            string errMsg = string.Empty;
            RecruitInfo = LocalizationHelper.GetString("ConnectingToEmulator");
            _recruitCaught = await Task.Run(() => Instances.AsstProxy.AsstConnect(ref errMsg));
            if (!_recruitCaught)
            {
                RecruitInfo = errMsg;
                return;
            }

            RecruitInfo = LocalizationHelper.GetString("Identifying");
            RecruitResult = string.Empty;

            var levelList = new List<int>();

            if (ChooseLevel3)
            {
                levelList.Add(3);
            }

            if (ChooseLevel4)
            {
                levelList.Add(4);
            }

            if (ChooseLevel5)
            {
                levelList.Add(5);
            }

            if (ChooseLevel6)
            {
                levelList.Add(6);
            }

            Instances.AsstProxy.AsstStartRecruitCalc(levelList.ToArray(), RecruitAutoSetTime);
        }

        private bool _recruitmentShowPotential = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.RecruitmentShowPotential, bool.TrueString));

        public bool RecruitmentShowPotential
        {
            get => _recruitmentShowPotential;
            set
            {
                SetAndNotify(ref _recruitmentShowPotential, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.RecruitmentShowPotential, value.ToString());
            }
        }

        public void ProcRecruitMsg(JObject details)
        {
            string? what = details["what"]?.ToString();
            var subTaskDetails = details["details"];

            switch (what)
            {
                case "RecruitTagsDetected":
                    {
                        JArray? tags = (JArray?)subTaskDetails?["tags"];
                        string infoContent = LocalizationHelper.GetString("RecruitTagsDetected");
                        tags ??= [];
                        infoContent = tags.Select(tagName => tagName.ToString()).Aggregate(infoContent, (current, tagStr) => current + (tagStr + "    "));

                        RecruitInfo = infoContent;
                    }

                    break;

                case "RecruitResult":
                    {
                        string resultContent = string.Empty;
                        JArray? resultArray = (JArray?)subTaskDetails?["result"];
                        /* int level = (int)subTaskDetails["level"]; */
                        foreach (var combs in resultArray ?? [])
                        {
                            int tagLevel = (int)(combs["level"] ?? -1);
                            resultContent += tagLevel + "★ Tags:    ";
                            resultContent = (((JArray?)combs["tags"]) ?? []).Aggregate(resultContent, (current, tag) => current + (tag + "    "));

                            resultContent += "\n\t";
                            foreach (var oper in (JArray?)combs["opers"] ?? [])
                            {
                                int operLevel = (int)(oper["level"] ?? -1);
                                var operId = oper["id"]?.ToString();
                                var operName = DataHelper.GetLocalizedCharacterName(oper["name"]?.ToString());

                                string potential = string.Empty;

                                if (RecruitmentShowPotential && OperBoxPotential != null && operId != null
                                    && (tagLevel >= 4 || operLevel == 1))
                                {
                                    if (OperBoxPotential.ContainsKey(operId))
                                    {
                                        potential = " ( " + OperBoxPotential[operId] + " )";
                                    }
                                    else
                                    {
                                        potential = " ( !!! NEW !!! )";
                                    }
                                }

                                resultContent += operLevel + "★ " + operName + potential + "    ";
                            }

                            resultContent += "\n\n";
                        }

                        RecruitResult = resultContent;
                    }

                    break;
            }
        }

        #endregion Recruit

        #region Depot

        private string _depotInfo = LocalizationHelper.GetString("DepotRecognitionTip");

        /// <summary>
        /// Gets or sets the depot info.
        /// </summary>
        public string DepotInfo
        {
            get => _depotInfo;
            set => SetAndNotify(ref _depotInfo, value);
        }

        private ObservableCollection<DepotResultDate> _depotResult = [];

        /// <summary>
        /// Gets or sets the depot result.
        /// </summary>
        public ObservableCollection<DepotResultDate> DepotResult
        {
            get => _depotResult;
            set => SetAndNotify(ref _depotResult, value);
        }

        public class DepotResultDate
        {
            public string? Name { get; set; }

            public string Id { get; set; } = null!;

            public BitmapImage? Image { get; set; }

            public int Count { get; set; }
        }

        /// <summary>
        /// Gets or sets the ArkPlanner result.
        /// </summary>
        public string ArkPlannerResult { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Lolicon result.
        /// </summary>
        public string LoliconResult { get; set; } = string.Empty;

        /// <summary>
        /// parse of depot recognition result
        /// </summary>
        /// <param name="details">detailed json-style parameters</param>
        /// <returns>Success or not</returns>
        public bool DepotParse(JObject? details)
        {
            if (details == null)
            {
                return false;
            }

            DepotResult.Clear();
            foreach (var item in details["arkplanner"]?["object"]?["items"]?.Cast<JObject>()!)
            {
                var id = (string?)item["id"];
                if (id == null)
                {
                    continue;
                }

                DepotResultDate result = new DepotResultDate
                {
                    Id = id,
                    Name = ItemListHelper.GetItemName(id),
                    Image = ItemListHelper.GetItemImage(id),
                    Count = (int)(item["have"] ?? -1),
                };
                DepotResult.Add(result);
            }

            ArkPlannerResult = details["arkplanner"]?["data"]?.ToString() ?? string.Empty;
            LoliconResult = details["lolicon"]?["data"]?.ToString() ?? string.Empty;

            bool done = (bool)(details["done"] ?? false);
            if (!done)
            {
                return true;
            }

            DepotInfo = LocalizationHelper.GetString("IdentificationCompleted") + "\n" + LocalizationHelper.GetString("DepotRecognitionTip");
            DepotDone = true;

            return true;
        }

        private bool _depotDone = true;

        /// <summary>
        /// Gets or sets a value indicating whether depot info is parsed.
        /// </summary>
        public bool DepotDone
        {
            get => _depotDone;
            set
            {
                SetAndNotify(ref _depotDone, value);
                if (value)
                {
                    _runningState.SetIdle(true);
                }
            }
        }

        /// <summary>
        /// Export depot info to ArkPlanner.
        /// </summary>
        // xaml 中用到了
        // ReSharper disable once UnusedMember.Global
        public void ExportToArkplanner()
        {
            Clipboard.SetDataObject(ArkPlannerResult);
            DepotInfo = LocalizationHelper.GetString("CopiedToClipboard");
        }

        /// <summary>
        /// Export depot info to Lolicon.
        /// </summary>
        // xaml 中用到了
        // ReSharper disable once UnusedMember.Global
        public void ExportToLolicon()
        {
            Clipboard.SetDataObject(LoliconResult);
            DepotInfo = LocalizationHelper.GetString("CopiedToClipboard");
        }

        private void DepotClear()
        {
            DepotResult.Clear();
            ArkPlannerResult = string.Empty;
            LoliconResult = string.Empty;
            DepotDone = false;
        }

        /// <summary>
        /// Starts depot recognition.
        /// </summary>
        // xaml 中用到了
        // ReSharper disable once UnusedMember.Global
        public async void StartDepot()
        {
            _runningState.SetIdle(false);
            string errMsg = string.Empty;
            DepotInfo = LocalizationHelper.GetString("ConnectingToEmulator");
            bool caught = await Task.Run(() => Instances.AsstProxy.AsstConnect(ref errMsg));
            if (!caught)
            {
                DepotInfo = errMsg;
                _runningState.SetIdle(true);
                return;
            }

            DepotInfo = LocalizationHelper.GetString("Identifying");
            DepotClear();

            Instances.AsstProxy.AsstStartDepot();
        }

        #endregion Depot

        #region OperBox

        /// <summary>
        /// 未实装干员，但在battle_data中，
        /// </summary>
        private static readonly HashSet<string?> _virtuallyOpers =
        [
            "char_504_rguard",
            "char_505_rcast",
            "char_506_rmedic",
            "char_507_rsnipe",
            "char_514_rdfend",
            "char_513_apionr",
            "char_511_asnipe",
            "char_510_amedic",
            "char_509_acast",
            "char_508_aguard",
            "char_1001_amiya2",
            "char_1037_amiya3"
        ];

        private string _operBoxInfo = LocalizationHelper.GetString("OperBoxRecognitionTip");

        public string OperBoxInfo
        {
            get => _operBoxInfo;
            set => SetAndNotify(ref _operBoxInfo, value);
        }

        private bool _operBoxDone = true;

        /// <summary>
        /// Gets or sets a value indicating whether operBox info is parsed.
        /// </summary>
        public bool OperBoxDone
        {
            get => _operBoxDone;
            set
            {
                SetAndNotify(ref _operBoxDone, value);
                if (value)
                {
                    _runningState.SetIdle(true);
                }
            }
        }

        public string OperBoxExportData { get; set; } = string.Empty;

        private JArray _operBoxDataArray = (JArray)(JsonConvert.DeserializeObject(ConfigurationHelper.GetValue(ConfigurationKeys.OperBoxData, new JArray().ToString())) ?? new JArray());

        public JArray OperBoxDataArray
        {
            get => _operBoxDataArray;
            set
            {
                SetAndNotify(ref _operBoxDataArray, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.OperBoxData, value.ToString());
                _operBoxPotential = null; // reset
            }
        }

        private Dictionary<string, int>? _operBoxPotential;

        public Dictionary<string, int>? OperBoxPotential
        {
            get
            {
                if (_operBoxPotential != null)
                {
                    return _operBoxPotential;
                }

                _operBoxPotential = new Dictionary<string, int>();
                foreach (JObject operBoxData in OperBoxDataArray.OfType<JObject>())
                {
                    var id = (string?)operBoxData["id"];
                    var potential = (int)(operBoxData["potential"] ?? -1);
                    if (id != null)
                    {
                        _operBoxPotential[id] = potential;
                    }
                }

                return _operBoxPotential;
            }
        }

        private ObservableCollection<string> _operBoxHaveList = [];

        public ObservableCollection<string> OperBoxHaveList
        {
            get => _operBoxHaveList;
            set
            {
                SetAndNotify(ref _operBoxHaveList, value);
            }
        }

        private ObservableCollection<string> _operBoxNotHaveList = [];

        public ObservableCollection<string> OperBoxNotHaveList
        {
            get => _operBoxNotHaveList;
            set
            {
                SetAndNotify(ref _operBoxNotHaveList, value);
            }
        }

        public bool OperBoxParse(JObject? details)
        {
            var operBoxes = (JArray?)details?["all_opers"];

            if (operBoxes == null)
            {
                return false;
            }

            List<Tuple<string, int>> operHave = [];
            List<Tuple<string, int>> operNotHave = [];

            foreach (JObject operBox in operBoxes.Cast<JObject>())
            {
                var tuple = new Tuple<string, int>(DataHelper.GetLocalizedCharacterName((string?)operBox["name"]) ?? "???", (int)(operBox["rarity"] ?? -1));

                if (_virtuallyOpers.Contains((string?)operBox["id"]))
                {
                    continue;
                }

                if ((bool)(operBox["own"] ?? false))
                {
                    /*已拥有干员*/
                    operHave.Add(tuple);
                }
                else
                {
                    /*未拥有干员,包含预备干员等*/
                    operNotHave.Add(tuple);
                }
            }

            operHave.Sort((x, y) => y.Item2.CompareTo(x.Item2));
            operNotHave.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            OperBoxHaveList = new ObservableCollection<string>(operHave.Select(tuple => tuple.Item1));
            OperBoxNotHaveList = new ObservableCollection<string>(operNotHave.Select(tuple => tuple.Item1));

            bool done = (bool)(details["done"] ?? false);
            if (done)
            {
                OperBoxInfo = LocalizationHelper.GetString("IdentificationCompleted") + "\n" + LocalizationHelper.GetString("OperBoxRecognitionTip");
                OperBoxExportData = details["own_opers"]?.ToString() ?? string.Empty;
                OperBoxDataArray = (JArray)(details["own_opers"] ?? new JArray());
                OperBoxDone = true;
            }

            return true;
        }

        /// <summary>
        /// 开始识别干员
        /// </summary>
        /// xaml 中用到了
        /// ReSharper disable once UnusedMember.Global
        public async void StartOperBox()
        {
            OperBoxExportData = string.Empty;
            OperBoxDone = false;
            _runningState.SetIdle(false);

            string errMsg = string.Empty;
            OperBoxInfo = LocalizationHelper.GetString("ConnectingToEmulator");
            bool caught = await Task.Run(() => Instances.AsstProxy.AsstConnect(ref errMsg));
            if (!caught)
            {
                OperBoxInfo = errMsg;
                _runningState.SetIdle(true);
                return;
            }

            if (Instances.AsstProxy.AsstStartOperBox())
            {
                OperBoxInfo = LocalizationHelper.GetString("Identifying");
            }
        }

        // xaml 中用到了
        // ReSharper disable once UnusedMember.Global
        public void ExportOperBox()
        {
            if (string.IsNullOrEmpty(OperBoxExportData))
            {
                return;
            }

            Clipboard.SetDataObject(OperBoxExportData);
            OperBoxInfo = LocalizationHelper.GetString("CopiedToClipboard");
        }

        #endregion OperBox

        #region Gacha

        private bool _gachaDone = true;

        public bool GachaDone
        {
            get => _gachaDone;
            set
            {
                bool stop = value && !_gachaDone;
                SetAndNotify(ref _gachaDone, value);
                if (value)
                {
                    _runningState.SetIdle(true);
                }

                if (!stop)
                {
                    return;
                }

                _gachaImageTimer.Stop();

                // 强制再刷一下
                RefreshGachaImage(null, null);
            }
        }

        private string _gachaInfo = LocalizationHelper.GetString("GachaInitTip");

        public string GachaInfo
        {
            get => _gachaInfo;
            set => SetAndNotify(ref _gachaInfo, value);
        }

        private double _gachaScreenFpf;

        public double GachaScreenFpf
        {
            get => _gachaScreenFpf;
            set => SetAndNotify(ref _gachaScreenFpf, value);
        }

        // xaml 中用到了
        // ReSharper disable once UnusedMember.Global
        public void GachaOnce()
        {
            StartGacha();
        }

        // xaml 中用到了
        // ReSharper disable once UnusedMember.Global
        public void GachaTenTimes()
        {
            StartGacha(false);
        }

        public async void StartGacha(bool once = true)
        {
            GachaDone = false;
            GachaImage = null;
            _runningState.SetIdle(false);

            string errMsg = string.Empty;
            GachaInfo = LocalizationHelper.GetString("ConnectingToEmulator");
            bool caught = await Task.Run(() => Instances.AsstProxy.AsstConnect(ref errMsg));
            if (!caught)
            {
                GachaInfo = errMsg;
                _runningState.SetIdle(true);
                return;
            }

            if (!Instances.AsstProxy.AsstStartGacha(once))
            {
                return;
            }

            _gachaImageTimer.Interval = TimeSpan.FromMilliseconds(10);
            _gachaImageTimer.Tick += RefreshGachaImage;
            _gachaImageTimer.Start();
        }

        private readonly DispatcherTimer _gachaImageTimer = new();

        private BitmapImage? _gachaImage;

        public BitmapImage? GachaImage
        {
            get => _gachaImage;
            set => SetAndNotify(ref _gachaImage, value);
        }

        private DateTime _lastTipUpdateTime = DateTime.MinValue;
        private DateTime _lastFpsUpdateTime = DateTime.MinValue;
        private int _frameCount;

        private void RefreshGachaImage(object? sender, EventArgs? e)
        {
            GachaImage = Instances.AsstProxy.AsstGetFreshImage();

            var now = DateTime.Now;
            _frameCount++;
            if ((now - _lastFpsUpdateTime).TotalSeconds >= 1)
            {
<<<<<<< HEAD
                GachaScreenFpf = _frameCount / (now - _lastFpsUpdateTime).TotalSeconds;
                _frameCount = 0;
                _lastFpsUpdateTime = now;
=======
                var image = Instances.AsstProxy.AsstGetImage();
                if (GachaImage.IsEqual(image))
                {
                    return;
                }

                GachaImage = image;

                var rd = new Random();
                GachaInfo = LocalizationHelper.GetString("GachaTip" + rd.Next(1, 18));
            }
        }

        // DO NOT CHANGE
        // 请勿更改
        // 請勿更改
        // このコードを変更しないでください
        // 변경하지 마십시오
        private bool _gachaShowDisclaimer = true; // !Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.ShowDisclaimerNoMore, bool.FalseString));

        public bool GachaShowDisclaimer
        {
            get => _gachaShowDisclaimer;
            set
            {
                SetAndNotify(ref _gachaShowDisclaimer, value);
            }
        }

        private bool _gachaShowDisclaimerNoMore = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.GachaShowDisclaimerNoMore, bool.FalseString));

        public bool GachaShowDisclaimerNoMore
        {
            get => _gachaShowDisclaimerNoMore;
            set
            {
                SetAndNotify(ref _gachaShowDisclaimerNoMore, value);
                ConfigurationHelper.SetValue(ConfigurationKeys.GachaShowDisclaimerNoMore, value.ToString());
            }
        }

        // xaml 中用到了
        // ReSharper disable once UnusedMember.Global
        public void GachaAgreeDisclaimer()
        {
            var result = MessageBoxHelper.Show(
                LocalizationHelper.GetString("GachaWarning"),
                LocalizationHelper.GetString("Warning"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                no: LocalizationHelper.GetString("Confirm"),
                yes: LocalizationHelper.GetString("Cancel"),
                iconBrushKey: "DangerBrush");
            if (result != MessageBoxResult.No)
            {
                return;
            }

            GachaShowDisclaimer = false;
        }

        #endregion Gacha
    }
}
