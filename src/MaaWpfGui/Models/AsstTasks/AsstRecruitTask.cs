// <copyright file="AsstRecruitTask.cs" company="MaaAssistantArknights">
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
using System.Collections.Generic;
using MaaWpfGui.Services;
using Newtonsoft.Json.Linq;

namespace MaaWpfGui.Models.AsstTasks;

/// <summary>
/// 公开招募。
/// </summary>
public class AsstRecruitTask : AsstBaseTask
{
    public override AsstTaskType TaskType => AsstTaskType.Recruit;

    /// <summary>
    /// Gets or sets 招募次数，可选，默认 0。若仅公招计算，可设置为 0
    /// </summary>
    public int RecruitTimes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets 是否刷新三星 Tags, 可选，默认false
    /// </summary>
    public bool Refresh { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 无招募许可时是否继续尝试刷新 Tags。
    /// </summary>
    public bool ForceRefresh { get; set; }

    /// <summary>
    /// Gets or sets 会去点击标签的 Tag 等级，必选
    /// </summary>
    public List<int> SelectList { get; set; } = [];

    /// <summary>
    /// Gets or sets 会去点击确认的 Tag 等级，必选。若仅公招计算，可设置为空数组
    /// </summary>
    public List<int> ConfirmList { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether 是否设置招募时限。仅在 times 为 0 时生效，可选，默认 true
    /// </summary>
    public bool SetRecruitTime { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether 是否使用加急许可，可选，默认 false
    /// </summary>
    public bool UseExpedited { get; set; }

    /// <summary>
    /// Gets or sets 使用加急许可
    /// </summary>
    public int ExpeditedTimes { get; set; }

    /// <summary>
    /// Gets or sets 公招选择额外tag的模式。可用值包括：
    /// <list type="bullet">
    ///     <item>
    ///         <term><c>0</c></term>
    ///         <description>默认不选择额外tag。</description>
    ///     </item>
    ///     <item>
    ///         <term><c>1</c></term>
    ///         <description>选满至3个tag。</description>
    ///     </item>
    ///     <item>
    ///         <term><c>2</c></term>
    ///         <description>尽可能多选且只选四星以上的tag。</description>
    ///     </item>
    /// </list>
    /// </summary>
    public int SelectExtraTags { get; set; }

    /// <summary>
    /// Gets or sets 首选 Tags，仅在 Tag 等级为 3 时有效
    /// </summary>
    public List<string> Level3FirstList { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether 遇到小车词条时是否招募
    /// </summary>
    public bool ChooseLevel1 { get; set; }

    /// <summary>
    /// Gets or sets 3 星招募时间
    /// </summary>
    public int ChooseLevel3Time { get; set; }

    /// <summary>
    /// Gets or sets 4 星招募时间
    /// </summary>
    public int ChooseLevel4Time { get; set; }

    /// <summary>
    /// Gets or sets 5 星招募时间
    /// </summary>
    public int ChooseLevel5Time { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 是否回报企鹅物流，默认 false
    /// </summary>
    public bool ReportToPenguin { get; set; }

    /// <summary>
    /// Gets or sets 企鹅物流回报id, 可选，默认为空。仅在 <see cref="ReportToPenguin"/> 为 true 时有效
    /// </summary>
    public string PenguinId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether 是否回报一图流，默认 false
    /// </summary>
    public bool ReportToYituliu { get; set; }

    /// <summary>
    ///  Gets or sets 一图流回报id，可选，默认为空。仅在 <see cref="ReportToYituliu"/> 为 true 时有效
    /// </summary>
    public string YituliuId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets 服务器，可选，默认 "CN", 会影响上传。选项："CN" | "US" | "JP" | "KR"
    /// </summary>
    public string ServerType { get; set; } = string.Empty;

    public override (AsstTaskType TaskType, JObject Params) Serialize()
    {
        var param = new JObject
        {
            ["refresh"] = Refresh,
            ["force_refresh"] = ForceRefresh,
            ["select"] = JArray.FromObject(SelectList),
            ["confirm"] = JArray.FromObject(ConfirmList),
            ["times"] = RecruitTimes,
            ["set_time"] = SetRecruitTime,
            ["expedite"] = UseExpedited,
            ["skip_robot"] = !ChooseLevel1,
            ["extra_tags_mode"] = SelectExtraTags,
            ["first_tags"] = JArray.FromObject(Level3FirstList),
            ["recruitment_time"] = new JObject
            {
                ["3"] = ChooseLevel3Time,
                ["4"] = ChooseLevel4Time,
                ["5"] = ChooseLevel5Time,
            },
            ["report_to_penguin"] = ReportToPenguin,
            ["report_to_yituliu"] = ReportToYituliu,
            ["server"] = ServerType,
        };

        if (UseExpedited)
        {
            param["expedite_times"] = ExpeditedTimes;
        }

        if (ReportToPenguin)
        {
            param["penguin_id"] = PenguinId;
        }

        if (ReportToYituliu)
        {
            param["yituliu_id"] = YituliuId;
        }

        return (AsstTaskType.Recruit, param);
    }
}
