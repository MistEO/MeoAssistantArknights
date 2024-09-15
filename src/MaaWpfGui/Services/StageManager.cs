// <copyright file="StageManager.cs" company="MaaAssistantArknights">
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Data;
using MaaWpfGui.Constants;
using MaaWpfGui.Helper;
using MaaWpfGui.Models;
using MaaWpfGui.Utilities.ValueType;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;
using Serilog;
using Stylet;

namespace MaaWpfGui.Services
{
    /// <summary>
    /// Stage manager
    /// </summary>
    public class StageManager
    {
        private const string StageApi = "gui/StageActivity.json";
        private const string TasksApi = "resource/tasks.json";

        private static readonly ILogger _logger = Log.ForContext<StageManager>();

        // data
        private Dictionary<string, StageInfo> _stages;

        /// <summary>
        /// Initializes a new instance of the <see cref="StageManager"/> class.
        /// </summary>
        public StageManager()
        {
            UpdateStageLocal();

            Task.Run(async () =>
            {
                await UpdateStageWeb();
                if (Instances.TaskQueueViewModel != null)
                {
                    _ = Execute.OnUIThreadAsync(() =>
                    {
                        Instances.TaskQueueViewModel.UpdateDatePrompt();
                        Instances.TaskQueueViewModel.UpdateStageList(true);
                    });
                }
            });
        }

        public void UpdateStageLocal()
        {
            UpdateStageInternal(LoadLocalStages());
        }

        public async Task UpdateStageWeb()
        {
            if (!await CheckWebUpdate())
            {
                return;
            }

            const string FilePath = "cache/allFileDownloadComplete.json";
            await File.WriteAllTextAsync(FilePath, GenerateJsonString(false));
            UpdateStageInternal(await LoadWebStages());
            await File.WriteAllTextAsync(FilePath, GenerateJsonString(true));

            _ = Execute.OnUIThreadAsync(() =>
            {
                var growlInfo = new GrowlInfo
                {
                    IsCustom = true,
                    Message = LocalizationHelper.GetString("ApiUpdateSuccess"),
                    IconKey = "HangoverGeometry",
                    IconBrushKey = "PallasBrush",
                };
                Growl.Info(growlInfo);
            });

            return;

            static string GenerateJsonString(bool allFileDownloadComplete)
            {
                JObject json = new JObject
                {
                    ["allFileDownloadComplete"] = allFileDownloadComplete,
                };
                return JsonConvert.SerializeObject(json);
            }
        }

        private static string GetClientType()
        {
            var clientType = ConfigurationHelper.GetValue(ConfigurationKeys.ClientType, string.Empty);

            // 官服和B服使用同样的资源
            if (clientType is "Bilibili" or "")
            {
                clientType = "Official";
            }

            return clientType;
        }

        private static JObject LoadLocalStages()
        {
            JObject activity = Instances.MaaApiService.LoadApiCache(StageApi);
            return activity;
        }

        private static async Task<bool> CheckWebUpdate()
        {
            // Check if we need to update from the web
            const string LastUpdateTimeFile = "lastUpdateTime.json";
            const string AllFileDownloadCompleteFile = "allFileDownloadComplete.json";
            JObject localLastUpdatedJson = Instances.MaaApiService.LoadApiCache(LastUpdateTimeFile);
            JObject allFileDownloadCompleteJson = Instances.MaaApiService.LoadApiCache(AllFileDownloadCompleteFile);
            JObject webLastUpdatedJson = await Instances.MaaApiService.RequestMaaApiWithCache(LastUpdateTimeFile).ConfigureAwait(false);

            if (localLastUpdatedJson?["timestamp"] == null || webLastUpdatedJson?["timestamp"] == null)
            {
                return true;
            }

            long localTimestamp = localLastUpdatedJson["timestamp"].ToObject<long>();
            long webTimestamp = webLastUpdatedJson["timestamp"].ToObject<long>();
            bool allFileDownloadComplete = allFileDownloadCompleteJson?["allFileDownloadComplete"]?.ToObject<bool>() ?? false;
            return webTimestamp > localTimestamp || !allFileDownloadComplete;
        }

        private static async Task<JObject> LoadWebStages()
        {
            var clientType = GetClientType();

            var activityTask = Instances.MaaApiService.RequestMaaApiWithCache(StageApi);
            var tasksTask = Instances.MaaApiService.RequestMaaApiWithCache(TasksApi);

            await Task.WhenAll(activityTask, tasksTask);

            JObject activity = await activityTask;
            JObject tasksJson = await tasksTask;

            if (clientType != "Official" && tasksJson != null)
            {
                var tasksPath = "resource/global/" + clientType + '/' + TasksApi;

                // Download the client specific resources only when the Official ones are successfully downloaded so that the client specific resource version is the actual version
                // TODO: There may be an issue when the CN resource is loaded from cache (e.g. network down) while global resource is downloaded (e.g. network up again)
                // var tasksJsonClient = fromWeb ? WebService.RequestMaaApiWithCache(tasksPath) : WebService.RequestMaaApiWithCache(tasksPath);
                await Instances.MaaApiService.RequestMaaApiWithCache(tasksPath);
            }

            Instances.AsstProxy.LoadResource();
            return activity;
        }

        private void UpdateStageInternal(JObject activity)
        {
            var tempStage = new Dictionary<string, StageInfo>
            {
                // 这里会被 “剩余理智” 复用，第一个必须是 string.Empty 的
                // 「当前/上次」关卡导航
                { string.Empty, new StageInfo { Display = LocalizationHelper.GetString("DefaultStage"), Value = string.Empty } },
            };

            var clientType = GetClientType();

            bool isDebugVersion = Marshal.PtrToStringAnsi(MaaService.AsstGetVersion())!.Contains("DEBUG");
            bool curVerParsed = SemVersion.TryParse(Marshal.PtrToStringAnsi(MaaService.AsstGetVersion()), SemVersionStyles.AllowLowerV, out var curVersionObj);

            // bool curResourceVerParsed = SemVersion.TryParse(
            //    tasksJsonClient?["ResourceVersion"]?.ToString() ?? tasksJson?["ResourceVersion"]?.ToString() ?? string.Empty,
            //    SemVersionStyles.AllowLowerV, out var curResourceVersionObj);
            var resourceCollection = new StageActivityInfo
            {
                IsResourceCollection = true,
            };

            if (activity?[clientType] != null)
            {
                try
                {
                    // 资源全开放活动
                    var resourceCollectionData = activity[clientType]["resourceCollection"];
                    if (resourceCollectionData != null)
                    {
                        resourceCollection.Tip = resourceCollectionData["Tip"]?.ToString();
                        resourceCollection.UtcStartTime = GetDateTime(resourceCollectionData, "UtcStartTime");
                        resourceCollection.UtcExpireTime = GetDateTime(resourceCollectionData, "UtcExpireTime");
                    }

                    // 活动关卡
                    foreach (var stageObj in activity[clientType]["sideStoryStage"] ?? Enumerable.Empty<JToken>())
                    {
                        // 现在只有导航，暂不判断版本
                        // MinimumResourceRequired is not necessarily provided in json, in which case it is ok even if there are no cached resources
                        // bool minResourceRequiredParsed = SemVersion.TryParse(stageObj?["MinimumResourceRequired"]?.ToString() ?? string.Empty, SemVersionStyles.AllowLowerV, out var minResourceRequiredObj);
                        bool minRequiredParsed = SemVersion.TryParse(stageObj?["MinimumRequired"]?.ToString() ?? string.Empty, SemVersionStyles.AllowLowerV, out var minRequiredObj);

                        StageInfo stageInfo;

                        // && (!minResourceRequiredParsed || curResourceVerParsed))
                        if (isDebugVersion || (curVerParsed && minRequiredParsed))
                        {
                            // Debug Version will be considered satisfying min version requirement, but the resource version needs a comparison
                            if (!isDebugVersion)
                            {
                                // &&(!minResourceRequiredParsed || curResourceVersionObj.CompareSortOrderTo(minResourceRequiredObj) < 0)
                                if (curVersionObj.CompareSortOrderTo(minRequiredObj) < 0)
                                {
                                    if (!tempStage.ContainsKey(LocalizationHelper.GetString("UnsupportedStages")))
                                    {
                                        stageInfo = new StageInfo
                                        {
                                            Display = LocalizationHelper.GetString("UnsupportedStages"),
                                            Value = LocalizationHelper.GetString("UnsupportedStages"),
                                            Drop = LocalizationHelper.GetString("LowVersion") + '\n' +
                                                   LocalizationHelper.GetString("MinimumRequirements") + minRequiredObj,
                                            Activity = new StageActivityInfo
                                            {
                                                Tip = stageObj?["Activity"]?["Tip"]?.ToString(),
                                                StageName = stageObj?["Activity"]?["StageName"]?.ToString(),
                                                UtcStartTime = GetDateTime(stageObj?["Activity"], "UtcStartTime"),
                                                UtcExpireTime = GetDateTime(stageObj?["Activity"], "UtcExpireTime"),
                                            },
                                        };

                                        tempStage.Add(stageInfo.Display, stageInfo);
                                    }

                                    continue;
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }

                        stageInfo = new StageInfo
                        {
                            Display = stageObj?["Display"]?.ToString() ?? string.Empty,
                            Value = stageObj?["Value"]?.ToString(),
                            Drop = stageObj?["Drop"]?.ToString(),
                            Activity = new StageActivityInfo
                            {
                                Tip = stageObj?["Activity"]?["Tip"]?.ToString(),
                                StageName = stageObj?["Activity"]?["StageName"]?.ToString(),
                                UtcStartTime = GetDateTime(stageObj?["Activity"], "UtcStartTime"),
                                UtcExpireTime = GetDateTime(stageObj?["Activity"], "UtcExpireTime"),
                            },
                        };

                        tempStage.Add(stageInfo.Display, stageInfo);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to parse Cache Stage resources");
                }
            }

            foreach (var kvp in new Dictionary<string, StageInfo>
            {
                // 主线关卡
                { "1-7", new StageInfo { Display = "1-7", Value = "1-7" } },
                { "R8-11", new StageInfo { Display = "R8-11", Value = "R8-11" } },
                { "12-17-HARD", new StageInfo { Display = "12-17-HARD", Value = "12-17-HARD" } },

                // 资源本
                { "CE-6", new StageInfo("CE-6", "CETip", [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday], resourceCollection) },
                { "AP-5", new StageInfo("AP-5", "APTip", [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday], resourceCollection) },
                { "CA-5", new StageInfo("CA-5", "CATip", [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], resourceCollection) },
                { "LS-6", new StageInfo("LS-6", "LSTip", [], resourceCollection) },
                { "SK-5", new StageInfo("SK-5", "SKTip", [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Saturday], resourceCollection) },

                // 剿灭模式
                { "Annihilation", new StageInfo { Display = LocalizationHelper.GetString("Annihilation"), Value = "Annihilation" } },

                // 芯片本
                { "PR-A-1", new StageInfo("PR-A-1", "PR-ATip", [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday], resourceCollection) },
                { "PR-A-2", new StageInfo("PR-A-2", string.Empty, [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday], resourceCollection) },
                { "PR-B-1", new StageInfo("PR-B-1", "PR-BTip", [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Friday, DayOfWeek.Saturday], resourceCollection) },
                { "PR-B-2", new StageInfo("PR-B-2", string.Empty, [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Friday, DayOfWeek.Saturday], resourceCollection) },
                { "PR-C-1", new StageInfo("PR-C-1", "PR-CTip", [DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday], resourceCollection) },
                { "PR-C-2", new StageInfo("PR-C-2", string.Empty, [DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday], resourceCollection) },
                { "PR-D-1", new StageInfo("PR-D-1", "PR-DTip", [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Saturday, DayOfWeek.Sunday], resourceCollection) },
                { "PR-D-2", new StageInfo("PR-D-2", string.Empty, [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Saturday, DayOfWeek.Sunday], resourceCollection) },

                // 周一和周日的关卡提示
                { "Pormpt1", new StageInfo { Tip = LocalizationHelper.GetString("Pormpt1"), OpenDays = [DayOfWeek.Monday], IsHidden = true } },
                { "Pormpt2", new StageInfo { Tip = LocalizationHelper.GetString("Pormpt2"), OpenDays = [DayOfWeek.Sunday], IsHidden = true } },
            })
            {
                tempStage.Add(kvp.Key, kvp.Value);
            }

            _stages = tempStage;
            return;

            static DateTime GetDateTime(JToken keyValuePairs, string key)
                => DateTime.ParseExact(keyValuePairs[key].ToString(),
                    "yyyy/MM/dd HH:mm:ss",
                    CultureInfo.InvariantCulture).AddHours(-Convert.ToInt32(keyValuePairs["TimeZone"].ToString()));
        }

        /// <summary>
        /// Gets stage by name
        /// </summary>
        /// <param name="stage">Stage name</param>
        /// <returns>Stage info</returns>
        public StageInfo GetStageInfo(string stage)
        {
            _stages.TryGetValue(stage, out var stageInfo);
            stageInfo ??= new StageInfo { Display = stage, Value = stage };
            return stageInfo;
        }

        /// <summary>
        /// Determine whether stage is open
        /// </summary>
        /// <param name="stage">Stage name</param>
        /// <param name="dayOfWeek">Current day of week</param>
        /// <returns>Whether stage is open</returns>
        public bool IsStageOpen(string stage, DayOfWeek dayOfWeek)
        {
            if (stage == null)
            {
                return false;
            }

            return GetStageInfo(stage)?.IsStageOpen(dayOfWeek) == true;
        }

        /// <summary>
        /// Gets open stage tips at specified day of week
        /// </summary>
        /// <param name="dayOfWeek">Day of week</param>
        /// <returns>Open stages</returns>
        public string GetStageTips(DayOfWeek dayOfWeek)
        {
            var builder = new StringBuilder();
            var sideStoryFlags = new Dictionary<string, bool>();
            foreach (var item in _stages.Where(item => item.Value.IsStageOpen(dayOfWeek)))
            {
                if (!string.IsNullOrEmpty(item.Value.Activity?.StageName)
                    && !sideStoryFlags.ContainsKey(item.Value.Activity.StageName))
                {
                    DateTime dateTime = DateTime.UtcNow;
                    var daysLeftOpen = (item.Value.Activity.UtcExpireTime - dateTime).Days;
                    builder.AppendLine(item.Value.Activity.StageName
                        + " "
                        + LocalizationHelper.GetString("DaysLeftOpen")
                        + (daysLeftOpen > 0 ? daysLeftOpen.ToString() : LocalizationHelper.GetString("LessThanOneDay")));
                    sideStoryFlags[item.Value.Activity.StageName] = true;
                }

                if (!string.IsNullOrEmpty(item.Value.Tip))
                {
                    builder.AppendLine(item.Value.Tip);
                }

                if (!string.IsNullOrEmpty(item.Value.Drop))
                {
                    builder.AppendLine(item.Value.Display + ": " + ItemListHelper.GetItemName(item.Value.Drop));
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets open stage list at specified day of week
        /// </summary>
        /// <param name="dayOfWeek">Day of week</param>
        /// <returns>Open stage list</returns>
        public IEnumerable<CombinedData> GetStageList(DayOfWeek dayOfWeek)
        {
            return _stages.Values.Where(stage => !stage.IsHidden && stage.IsStageOpen(dayOfWeek));
        }

        /// <summary>
        /// Gets all open or will open stage list
        /// </summary>
        /// <returns>Open or will open stage list</returns>
        public IEnumerable<CombinedData> GetStageList()
        {
            return _stages.Values.Where(stage => !stage.IsHidden && stage.IsStageOpenOrWillOpen());
        }
    }
}
