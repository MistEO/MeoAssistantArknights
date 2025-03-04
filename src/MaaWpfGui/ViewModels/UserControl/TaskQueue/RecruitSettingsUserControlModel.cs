// <copyright file="RecruitSettingsUserControlModel.cs" company="MaaAssistantArknights">
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
using MaaWpfGui.Constants;
using MaaWpfGui.Helper;
using MaaWpfGui.Models.AsstTasks;
using MaaWpfGui.Services;
using MaaWpfGui.Utilities.ValueType;
using MaaWpfGui.ViewModels.UI;
using Newtonsoft.Json.Linq;

namespace MaaWpfGui.ViewModels.UserControl.TaskQueue;

/// <summary>
/// 自动公招model
/// </summary>
public class RecruitSettingsUserControlModel : TaskViewModel
{
    static RecruitSettingsUserControlModel()
    {
        Instance = new();
    }

    public static RecruitSettingsUserControlModel Instance { get; }

    /// <summary>
    /// Gets the list of auto recruit selecting extra tags.
    /// </summary>
    public List<CombinedData> AutoRecruitSelectExtraTagsList { get; } =
        [
            new() { Display = LocalizationHelper.GetString("DefaultNoExtraTags"), Value = "0" },
            new() { Display = LocalizationHelper.GetString("SelectExtraTags"), Value = "1" },
            new() { Display = LocalizationHelper.GetString("SelectExtraOnlyRareTags"), Value = "2" },
        ];

    private static readonly List<string> _autoRecruitTagList = ["近战位", "远程位", "先锋干员", "近卫干员", "狙击干员", "重装干员", "医疗干员", "辅助干员", "术师干员", "治疗", "费用回复", "输出", "生存", "群攻", "防护", "减速",];

    private static readonly Lazy<List<CombinedData>> _autoRecruitTagShowList = new(() =>
        _autoRecruitTagList.Select<string, (string, string)?>(tag => DataHelper.RecruitTags.TryGetValue(tag, out var value) ? value : null)
            .Where(tag => tag is not null)
            .Cast<(string Display, string Client)>()
            .Select(tag => new CombinedData() { Display = tag.Display, Value = tag.Client })
            .ToList());

    public static List<CombinedData> AutoRecruitTagShowList
    {
        get => _autoRecruitTagShowList.Value;
    }

    private object[] _autoRecruitFirstList = ConfigurationHelper
        .GetValue(ConfigurationKeys.AutoRecruitFirstList, string.Empty)
        .Split(';')
        .Select(tag => _autoRecruitTagShowList.Value.FirstOrDefault(i => i.Value == tag))
        .Where(tag => tag is not null)
        .Cast<CombinedData>()
        .ToArray();

    public object[] AutoRecruitFirstList
    {
        get => _autoRecruitFirstList;
        set
        {
            SetAndNotify(ref _autoRecruitFirstList, value);
            var config = string.Join(';', value.Cast<CombinedData>().Select(i => i.Value));
            ConfigurationHelper.SetValue(ConfigurationKeys.AutoRecruitFirstList, config);
        }
    }

    private int _recruitMaxTimes = int.TryParse(ConfigurationHelper.GetValue(ConfigurationKeys.RecruitMaxTimes, "4"), out var outMaxTimes) ? outMaxTimes : 4;

    /// <summary>
    /// Gets or sets the maximum times of recruit.
    /// </summary>
    public int RecruitMaxTimes
    {
        get => _recruitMaxTimes;
        set
        {
            SetAndNotify(ref _recruitMaxTimes, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.RecruitMaxTimes, value.ToString());
        }
    }

    private bool _refreshLevel3 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.RefreshLevel3, bool.TrueString));

    /// <summary>
    /// Gets or sets a value indicating whether to refresh level 3.
    /// </summary>
    public bool RefreshLevel3
    {
        get => _refreshLevel3;
        set
        {
            SetAndNotify(ref _refreshLevel3, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.RefreshLevel3, value.ToString());
        }
    }

    private bool _forceRefresh = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.ForceRefresh, bool.TrueString));

    /// <summary>
    /// Gets or Sets a value indicating whether to refresh when recruit permit ran out.
    /// </summary>
    public bool ForceRefresh
    {
        get => _forceRefresh;
        set
        {
            SetAndNotify(ref _forceRefresh, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.ForceRefresh, value.ToString());
        }
    }

    private bool _useExpedited;

    /// <summary>
    /// Gets or sets a value indicating whether to use expedited.
    /// </summary>
    public bool UseExpedited
    {
        get => _useExpedited;
        set => SetAndNotify(ref _useExpedited, value);
    }

    private int _selectExtraTags = int.TryParse(ConfigurationHelper.GetValue(ConfigurationKeys.SelectExtraTags, "0"), out var outTags) ? outTags : 0;

    /// <summary>
    /// Gets or sets a value indicating three tags are always selected or select only rare tags as many as possible .
    /// </summary>
    public int SelectExtraTags
    {
        get => _selectExtraTags;
        set
        {
            SetAndNotify(ref _selectExtraTags, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.SelectExtraTags, value.ToString());
        }
    }

    private bool _notChooseLevel1 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.NotChooseLevel1, bool.TrueString));

    /// <summary>
    /// Gets or sets a value indicating whether not to choose level 1.
    /// </summary>
    public bool NotChooseLevel1
    {
        get => _notChooseLevel1;
        set
        {
            SetAndNotify(ref _notChooseLevel1, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.NotChooseLevel1, value.ToString());
        }
    }

    private bool _chooseLevel3 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.RecruitChooseLevel3, bool.TrueString));

    /// <summary>
    /// Gets or sets a value indicating whether to choose level 3.
    /// </summary>
    public bool ChooseLevel3
    {
        get => _chooseLevel3;
        set
        {
            SetAndNotify(ref _chooseLevel3, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.RecruitChooseLevel3, value.ToString());
        }
    }

    private bool _chooseLevel4 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.RecruitChooseLevel4, bool.TrueString));

    /// <summary>
    /// Gets or sets a value indicating whether to choose level 4.
    /// </summary>
    public bool ChooseLevel4
    {
        get => _chooseLevel4;
        set
        {
            SetAndNotify(ref _chooseLevel4, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.RecruitChooseLevel4, value.ToString());
        }
    }

    private bool _chooseLevel5 = Convert.ToBoolean(ConfigurationHelper.GetValue(ConfigurationKeys.RecruitChooseLevel5, bool.FalseString));

    /// <summary>
    /// Gets or sets a value indicating whether to choose level 5.
    /// </summary>
    public bool ChooseLevel5
    {
        get => _chooseLevel5;
        set
        {
            SetAndNotify(ref _chooseLevel5, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.RecruitChooseLevel5, value.ToString());
        }
    }

    private int _chooseLevel3Hour = Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel3Time, "540")) / 60;

    public int ChooseLevel3Hour
    {
        get => _chooseLevel3Hour;
        set
        {
            if (!SetAndNotify(ref _chooseLevel3Hour, value))
            {
                return;
            }

            ChooseLevel3Time = (value * 60) + ChooseLevel3Min;
        }
    }

    private int _chooseLevel3Min = (Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel3Time, "540")) % 60) / 10 * 10;

    public int ChooseLevel3Min
    {
        get => _chooseLevel3Min;
        set
        {
            if (!SetAndNotify(ref _chooseLevel3Min, value))
            {
                return;
            }

            ChooseLevel3Time = (ChooseLevel3Hour * 60) + value;
        }
    }

    private int _chooseLevel3Time = Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel3Time, "540"));

    public int ChooseLevel3Time
    {
        get => _chooseLevel3Time;
        set
        {
            value = value switch
            {
                < 60 => 540,
                > 540 => 60,
                _ => value / 10 * 10,
            };

            SetAndNotify(ref _chooseLevel3Time, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.ChooseLevel3Time, value.ToString());
            SetAndNotify(ref _chooseLevel3Hour, value / 60, nameof(ChooseLevel3Hour));
            SetAndNotify(ref _chooseLevel3Min, value % 60, nameof(ChooseLevel3Min));
        }
    }

    private int _chooseLevel4Hour = Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel4Time, "540")) / 60;

    public int ChooseLevel4Hour
    {
        get => _chooseLevel4Hour;
        set
        {
            if (!SetAndNotify(ref _chooseLevel4Hour, value))
            {
                return;
            }

            ChooseLevel4Time = (value * 60) + ChooseLevel4Min;
        }
    }

    private int _chooseLevel4Min = (Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel4Time, "540")) % 60) / 10 * 10;

    public int ChooseLevel4Min
    {
        get => _chooseLevel4Min;
        set
        {
            if (!SetAndNotify(ref _chooseLevel4Min, value))
            {
                return;
            }

            ChooseLevel4Time = (ChooseLevel4Hour * 60) + value;
        }
    }

    private int _chooseLevel4Time = Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel4Time, "540"));

    public int ChooseLevel4Time
    {
        get => _chooseLevel4Time;
        set
        {
            value = value switch
            {
                < 60 => 540,
                > 540 => 60,
                _ => value / 10 * 10,
            };

            SetAndNotify(ref _chooseLevel4Time, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.ChooseLevel4Time, value.ToString());
            SetAndNotify(ref _chooseLevel4Hour, value / 60, nameof(ChooseLevel4Hour));
            SetAndNotify(ref _chooseLevel4Min, value % 60, nameof(ChooseLevel4Min));
        }
    }

    private int _chooseLevel5Hour = Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel5Time, "540")) / 60;

    public int ChooseLevel5Hour
    {
        get => _chooseLevel5Hour;
        set
        {
            if (!SetAndNotify(ref _chooseLevel5Hour, value))
            {
                return;
            }

            ChooseLevel5Time = (value * 60) + ChooseLevel5Min;
        }
    }

    private int _chooseLevel5Min = (Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel5Time, "540")) % 60) / 10 * 10;

    public int ChooseLevel5Min
    {
        get => _chooseLevel5Min;
        set
        {
            if (!SetAndNotify(ref _chooseLevel5Min, value))
            {
                return;
            }

            ChooseLevel5Time = (ChooseLevel5Hour * 60) + value;
        }
    }

    private int _chooseLevel5Time = Convert.ToInt32(ConfigurationHelper.GetValue(ConfigurationKeys.ChooseLevel5Time, "540"));

    public int ChooseLevel5Time
    {
        get => _chooseLevel5Time;
        set
        {
            value = value switch
            {
                < 60 => 540,
                > 540 => 60,
                _ => value / 10 * 10,
            };

            SetAndNotify(ref _chooseLevel5Time, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.ChooseLevel5Time, value.ToString());
            SetAndNotify(ref _chooseLevel5Hour, value / 60, nameof(ChooseLevel5Hour));
            SetAndNotify(ref _chooseLevel5Min, value % 60, nameof(ChooseLevel5Min));
        }
    }

    public override (AsstTaskType Type, JObject Params) Serialize()
    {
        var task = new AsstRecruitTask()
        {
            Refresh = RefreshLevel3,
            ForceRefresh = ForceRefresh,
            SetRecruitTime = true,
            RecruitTimes = RecruitMaxTimes,
            UseExpedited = UseExpedited,
            ExpeditedTimes = RecruitMaxTimes,
            SelectExtraTags = SelectExtraTags,
            Level3FirstList = AutoRecruitFirstList.Cast<CombinedData>().Select(i => i.Value).ToList(),
            ChooseLevel1 = !NotChooseLevel1,
            ChooseLevel3Time = ChooseLevel3Time,
            ChooseLevel4Time = ChooseLevel4Time,
            ChooseLevel5Time = ChooseLevel5Time,
            ReportToPenguin = SettingsViewModel.GameSettings.EnablePenguin,
            ReportToYituliu = SettingsViewModel.GameSettings.EnableYituliu,
            PenguinId = SettingsViewModel.GameSettings.PenguinId,
            YituliuId = SettingsViewModel.GameSettings.PenguinId,
            ServerType = Instances.SettingsViewModel.ServerType,
        };

        if (task.ChooseLevel1)
        {
            task.ConfirmList.Add(1);
        }

        if (ChooseLevel3)
        {
            task.ConfirmList.Add(3);
        }

        if (ChooseLevel4)
        {
            task.SelectList.Add(4);
            task.ConfirmList.Add(4);
        }

        // ReSharper disable once InvertIf
        if (ChooseLevel5)
        {
            task.SelectList.Add(5);
            task.ConfirmList.Add(5);
        }

        return task.Serialize();
    }
}
