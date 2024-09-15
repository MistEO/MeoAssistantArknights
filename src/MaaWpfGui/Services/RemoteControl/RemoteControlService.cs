// <copyright file="RemoteControlService.cs" company="MaaAssistantArknights">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MaaWpfGui.Constants;
using MaaWpfGui.Helper;
using MaaWpfGui.States;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Stylet;

namespace MaaWpfGui.Services.RemoteControl
{
    /// <summary>
    /// The view model of remote control.
    /// </summary>
    // 通过 container.Get<RemoteControlService>(); 实例化或获取实例
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RemoteControlService
    {
        private Task _pollJobTask = Task.CompletedTask;
        private readonly List<string> _enqueueTaskIds = [];
        private readonly ConcurrentQueue<JObject> _sequentialTaskQueue = new ConcurrentQueue<JObject>();
        private readonly ConcurrentQueue<JObject> _instantTaskQueue = new ConcurrentQueue<JObject>();
        private Task _executeSequentialJobTask = Task.CompletedTask;
        private Task _executeInstantJobTask = Task.CompletedTask;
        private readonly RunningState _runningState;
        private bool _inited = false;

        private string _currentSequentialTaskId = string.Empty;

        public RemoteControlService()
        {
            InitializePollJobTask();
            _runningState = RunningState.Instance;
        }

        public void InitializePollJobTask()
        {
            if (_inited)
            {
                return;
            }

            if (!IsEndpointValid(Instances.SettingsViewModel.RemoteControlGetTaskEndpointUri))
            {
                return;
            }

            _inited = true;

            _pollJobTask = _pollJobTask.ContinueWith(async _ =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    try
                    {
                        if (!IsEndpointValid(Instances.SettingsViewModel.RemoteControlGetTaskEndpointUri))
                        {
                            Log.Logger.Information("RemoteControlGetTaskEndpointUri is not valid, return");
                            _inited = false;
                            return;
                        }

                        await PollJobTaskLoop();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "RemoteControl service raises unknown error.");
                    }
                }

                // ReSharper disable once FunctionNeverReturns
            });

            _executeSequentialJobTask = _executeSequentialJobTask.ContinueWith(async _ =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    try
                    {
                        if (!IsEndpointValid(Instances.SettingsViewModel.RemoteControlGetTaskEndpointUri))
                        {
                            Log.Logger.Information("RemoteControlGetTaskEndpointUri is not valid, return");
                            return;
                        }

                        await ExecuteSequentialJobLoop();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "RemoteControl service raises unknown error.");
                    }
                }

                // ReSharper disable once FunctionNeverReturns
            });

            _executeInstantJobTask = _executeInstantJobTask.ContinueWith(async _ =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    try
                    {
                        if (!IsEndpointValid(Instances.SettingsViewModel.RemoteControlGetTaskEndpointUri))
                        {
                            Log.Logger.Information("RemoteControlGetTaskEndpointUri is not valid, return");
                            return;
                        }

                        await ExecuteInstantJobLoop();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "RemoteControl service raises unknown error.");
                    }
                }

                // ReSharper disable once FunctionNeverReturns
            });
        }

        #region Private Method Invoker

        private static T GetPrivateFieldValue<T>(object instance, string fieldName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            Type type = instance.GetType();
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                throw new ArgumentException($"Field '{fieldName}' not found in type '{type.FullName}'.");
            }

            if (!typeof(T).IsAssignableFrom(fieldInfo.FieldType))
            {
                throw new ArgumentException($"Field '{fieldName}' is not of type {typeof(T)}.");
            }

            return (T)fieldInfo.GetValue(instance);
        }

        private static void InvokeInstanceMethod(object instance, string methodName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (methodInfo == null)
            {
                throw new ArgumentException($"Method '{methodName}' not found in type '{type.FullName}'.");
            }

            methodInfo.Invoke(instance, null);
        }

        private static T InvokeInstanceFunction<T>(object instance, string methodName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (methodInfo == null)
            {
                throw new ArgumentException($"Method '{methodName}' not found in type '{type.FullName}'.");
            }

            if (!typeof(T).IsAssignableFrom(methodInfo.ReturnType))
            {
                throw new ArgumentException($"Method '{methodName}' is not {typeof(T)}.");
            }

            return (T)methodInfo.Invoke(instance, null);
        }

        private static async Task<T> InvokeInstanceAsyncFunction<T>(object instance, string methodName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (methodInfo == null)
            {
                throw new ArgumentException($"Method '{methodName}' not found in type '{type.FullName}'.");
            }

            // 检查方法是否是异步方法 (返回Task或Task<T>)
            if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                throw new ArgumentException($"Method '{methodName}' is not asynchronous.");
            }

            return await (Task<T>)methodInfo.Invoke(instance, null);
        }

        private static TResult InvokeStaticFunction<TResult>(Type staticType, string methodName)
        {
            if (staticType == null)
            {
                throw new ArgumentNullException(nameof(staticType));
            }

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            MethodInfo methodInfo = staticType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);

            if (methodInfo == null)
            {
                throw new ArgumentException($"Method '{methodName}' not found in type '{staticType.FullName}'.");
            }

            return (TResult)methodInfo.Invoke(null, null);
        }

        #endregion

        #region JobLoops

        private async Task PollJobTaskLoop()
        {
            var endpoint = Instances.SettingsViewModel.RemoteControlGetTaskEndpointUri;

            if (!IsEndpointValid(endpoint))
            {
                return;
            }

            var uid = Instances.SettingsViewModel.RemoteControlUserIdentity;
            var did = Instances.SettingsViewModel.RemoteControlDeviceIdentity;
            var response = await Instances.HttpService.PostAsJsonAsync(new Uri(endpoint), new { user = uid, device = did });
            if (response == null)
            {
                Log.Logger.Error("RemoteControlService endpoint failed.");
                return;
            }

            var jsonObject = JsonConvert.DeserializeObject<JObject>(response);

            // A list of task
            if (jsonObject?.GetValue("tasks") is JArray tasks)
            {
                foreach (var task in tasks.OfType<JObject>())
                {
                    var type = task.GetValue("type")?.Value<string>();
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    // It is a valid task
                    var id = task.GetValue("id")?.Value<string>();
                    if (_enqueueTaskIds.Contains(id))
                    {
                        continue;
                    }

                    _enqueueTaskIds.Add(id);

                    switch (type)
                    {
                        case "LinkStart":
                        case "LinkStart-Base":
                        case "LinkStart-WakeUp":
                        case "LinkStart-Combat":
                        case "LinkStart-Recruiting":
                        case "LinkStart-Mall":
                        case "LinkStart-Mission":
                        case "LinkStart-AutoRoguelike":
                        case "LinkStart-Reclamation":
                        case "Toolbox-GachaOnce":
                        case "Toolbox-GachaTenTimes":
                        case "CaptureImage":
                        case "Settings-ConnectAddress":
                        case "Settings-Stage1":
                            _sequentialTaskQueue.Enqueue(task);
                            break;
                        case "CaptureImageNow":
                        case "HeartBeat":
                        case "StopTask":
                            _instantTaskQueue.Enqueue(task);
                            break;
                    }
                }
            }
        }

        private async Task ExecuteSequentialJobLoop()
        {
            if (_sequentialTaskQueue.TryDequeue(out var task))
            {
                var type = task.GetValue("type")?.Value<string>();
                var id = task.GetValue("id")?.Value<string>();
                var data = task.GetValue("params")?.Value<string>();
                var payload = string.Empty;
                var status = "SUCCESS";

                _currentSequentialTaskId = id;

                switch (type)
                {
                    case "LinkStart":
                        {
                            // 一键长草特殊任务
                            await _runningState.UntilIdleAsync();
                            var startLogStr = string.Format(LocalizationHelper.GetString("RemoteControlReceivedTask"), type, id);

                            Instances.TaskQueueViewModel.AddLog(startLogStr);
                            await Execute.OnUIThreadAsync(() =>
                            {
                                Instances.TaskQueueViewModel.LinkStart();
                            });
                            await _runningState.UntilIdleAsync();

                            var stopLogStr = string.Format(LocalizationHelper.GetString("RemoteControlCompletedTask"), type, id);
                            Instances.TaskQueueViewModel.AddLog(stopLogStr);
                            break;
                        }

                    case "LinkStart-Base":
                    case "LinkStart-WakeUp":
                    case "LinkStart-Combat":
                    case "LinkStart-Recruiting":
                    case "LinkStart-Mall":
                    case "LinkStart-Mission":
                    case "LinkStart-AutoRoguelike":
                    case "LinkStart-Reclamation":
                        {
                            await LinkStart([type.Split('-')[1]]);
                            break;
                        }

                    case "Toolbox-GachaOnce":
                        {
                            await _runningState.UntilIdleAsync();
                            Instances.RecognizerViewModel.GachaOnce();
                            while (!Instances.RecognizerViewModel.GachaDone)
                            {
                                await Task.Delay(100); // 暂停100毫秒以避免密集循环
                            }

                            break;
                        }

                    case "Toolbox-GachaTenTimes":
                        {
                            await _runningState.UntilIdleAsync();
                            Instances.RecognizerViewModel.GachaTenTimes();
                            while (!Instances.RecognizerViewModel.GachaDone)
                            {
                                await Task.Delay(100); // 暂停100毫秒以避免密集循环
                            }

                            break;
                        }

                    case "CaptureImage":
                        {
                            string errMsg = string.Empty;
                            bool connected = await Task.Run(() => Instances.AsstProxy.AsstConnect(ref errMsg));
                            if (connected)
                            {
                                var image = Instances.AsstProxy.AsstGetImage();
                                if (image == null)
                                {
                                    status = "FAILED";
                                    break;
                                }

                                byte[] bytes;
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(image));
                                    encoder.Save(stream);
                                    bytes = stream.ToArray();
                                }

                                payload = Convert.ToBase64String(bytes);
                                break;
                            }

                            status = "FAILED";
                            break;
                        }

                    case "Settings-ConnectAddress":
                        // ConfigurationHelper.SetValue(type.Split('-')[1], data);
                        await Execute.OnUIThreadAsync(() =>
                        {
                            Instances.SettingsViewModel.ConnectAddress = data;
                        });
                        break;
                    case "Settings-Stage1":
                        await Execute.OnUIThreadAsync(() =>
                        {
                            Instances.TaskQueueViewModel.Stage1 = data;
                        });
                        break;

                    // ReSharper disable once RedundantEmptySwitchSection
                    default:
                        // 未知的Type统一直接发给MAACore
                        // No! 未知的任务一概不处理
                        break;
                }

                var endpoint = Instances.SettingsViewModel.RemoteControlReportStatusUri;
                if (IsEndpointValid(endpoint))
                {
                    var uid = Instances.SettingsViewModel.RemoteControlUserIdentity;
                    var did = Instances.SettingsViewModel.RemoteControlDeviceIdentity;
                    var response = await Instances.HttpService.PostAsJsonAsync(new Uri(endpoint), new
                    {
                        user = uid,
                        device = did,
                        status,
                        task = id,
                        payload,
                    });
                    if (response == null)
                    {
                        Log.Logger.Error("RemoteControlService report task failed.");
                    }
                }

                _currentSequentialTaskId = string.Empty;
            }
        }

        private async Task ExecuteInstantJobLoop()
        {
            if (_instantTaskQueue.TryDequeue(out var task))
            {
                var type = task.GetValue("type")?.Value<string>();
                var id = task.GetValue("id")?.Value<string>();

                // 当前没有任务需要读取params,未来需要时可以取消注释
                // var data = task.GetValue("params")?.Value<string>();
                var payload = string.Empty;
                var status = "SUCCESS";

                switch (type)
                {
                    case "HeartBeat":
                        {
                            payload = _currentSequentialTaskId;
                            break;
                        }

                    case "StopTask":
                        {
                            await Task.Run(() =>
                            {
                                if (!Instances.AsstProxy.AsstStop())
                                {
                                    // 无法确定当前的界面，找不到借用的UI位置，因此只能Log
                                    Log.Logger.Error("Failed to stop Asst.");
                                }
                            });

                            // 无需等待，甩出任务即可返回，远端应该用心跳来确认界面卡死和取消是否成功。
                            break;
                        }

                    case "CaptureImageNow":
                        {
                            string errMsg = string.Empty;
                            bool connected = await Task.Run(() => Instances.AsstProxy.AsstConnect(ref errMsg));
                            if (connected)
                            {
                                var image = Instances.AsstProxy.AsstGetImage();
                                if (image == null)
                                {
                                    status = "FAILED";
                                    break;
                                }

                                byte[] bytes;
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(image));
                                    encoder.Save(stream);
                                    bytes = stream.ToArray();
                                }

                                payload = Convert.ToBase64String(bytes);
                                break;
                            }

                            status = "FAILED";
                            break;
                        }

                    // ReSharper disable once RedundantEmptySwitchSection
                    default:
                        // 未知的Type统一直接发给MAACore
                        // No! 未知的任务一概不处理
                        break;
                }

                var endpoint = Instances.SettingsViewModel.RemoteControlReportStatusUri;
                if (IsEndpointValid(endpoint))
                {
                    var uid = Instances.SettingsViewModel.RemoteControlUserIdentity;
                    var did = Instances.SettingsViewModel.RemoteControlDeviceIdentity;
                    var response = await Instances.HttpService.PostAsJsonAsync(new Uri(endpoint), new
                    {
                        user = uid,
                        device = did,
                        status,
                        task = id,
                        payload,
                    });
                    if (response == null)
                    {
                        Log.Logger.Error("RemoteControlService report task failed.");
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 根据"一键长草"功能进行修改的方法。
        /// </summary>
        /// <remarks>
        /// <para>注意以下特点:</para>
        /// <para>- 可以在非UI线程执行。</para>
        /// <para>- 不调用StartScript。</para>
        /// <para>- 不使用Model里的列表。</para>
        /// <para>- 在结尾添加对RunningStatus的等待。</para>
        /// <para>若"一键长草"功能在未来有更新，此方法也应进行相应的同步更新，但需确保上述特点保持不变。</para>
        /// </remarks>
        /// <param name="originalNames">指定的任务列表。</param>
        /// <returns>异步任务，无返回结果。</returns>
        // 这个是不是可以直接做到 TaskQueueViewModel 里面去？
        private async Task LinkStart(IEnumerable<string> originalNames)
        {
            await _runningState.UntilIdleAsync();

            _runningState.SetIdle(false);

            await Execute.OnUIThreadAsync(async () =>
            {
                // 虽然更改时已经保存过了，不过保险起见还是在点击开始之后再保存一次任务及基建列表
                Instances.TaskQueueViewModel.TaskItemSelectionChanged();
                Instances.SettingsViewModel.InfrastOrderSelectionChanged();

                InvokeInstanceMethod(Instances.TaskQueueViewModel, "ClearLog");

                /*await Task.Run(() => Instances.SettingsViewModel.RunScript("StartsWithScript"));*/

                Instances.TaskQueueViewModel.AddLog(LocalizationHelper.GetString("ConnectingToEmulator"));

                // 一般是点了“停止”按钮了
                if (Instances.TaskQueueViewModel.Stopping)
                {
                    Instances.TaskQueueViewModel.SetStopped();
                    return;
                }

                if (!await InvokeInstanceAsyncFunction<bool>(Instances.TaskQueueViewModel, "ConnectToEmulator"))
                {
                    return;
                }

                // 一般是点了“停止”按钮了
                if (Instances.TaskQueueViewModel.Stopping)
                {
                    Instances.TaskQueueViewModel.SetStopped();
                    return;
                }

                bool taskRet = true;

                // 直接遍历TaskItemViewModels里面的内容，是排序后的
                int count = 0;
                foreach (var item in originalNames)
                {
                    ++count;
                    switch (item)
                    {
                        case "Base":
                            taskRet &= InvokeInstanceFunction<bool>(Instances.TaskQueueViewModel, "AppendInfrast");
                            break;

                        case "WakeUp":
                            taskRet &= InvokeStaticFunction<bool>(Instances.TaskQueueViewModel.GetType(), "AppendStart");
                            break;

                        case "Combat":
                            taskRet &= InvokeInstanceFunction<bool>(Instances.TaskQueueViewModel, "AppendFight");
                            break;

                        case "Recruiting":
                            taskRet &= InvokeStaticFunction<bool>(Instances.TaskQueueViewModel.GetType(), "AppendRecruit");
                            break;

                        case "Mall":
                            taskRet &= InvokeInstanceFunction<bool>(Instances.TaskQueueViewModel, "AppendMall");
                            break;

                        case "Mission":
                            taskRet &= InvokeStaticFunction<bool>(Instances.TaskQueueViewModel.GetType(), "AppendAward");
                            break;

                        case "AutoRoguelike":
                            taskRet &= InvokeStaticFunction<bool>(Instances.TaskQueueViewModel.GetType(), "AppendRoguelike");
                            break;

                        case "Reclamation":
                            taskRet &= InvokeStaticFunction<bool>(Instances.TaskQueueViewModel.GetType(), "AppendReclamation");
                            break;

                        default:
                            --count;

                            // Instances.TaskQueueViewModel._logger.Error("Unknown task: " + item);
                            break;
                    }

                    if (taskRet)
                    {
                        continue;
                    }

                    Instances.TaskQueueViewModel.AddLog(item + "Error", UiLogColor.Error);
                    taskRet = true;
                    --count;
                }

                if (count == 0)
                {
                    Instances.TaskQueueViewModel.AddLog(LocalizationHelper.GetString("UnselectedTask"));
                    _runningState.SetIdle(true);
                    Instances.TaskQueueViewModel.SetStopped();
                    return;
                }

                taskRet &= Instances.AsstProxy.AsstStart();

                if (taskRet)
                {
                    Instances.TaskQueueViewModel.AddLog(LocalizationHelper.GetString("Running"));
                }
                else
                {
                    Instances.TaskQueueViewModel.AddLog(LocalizationHelper.GetString("UnknownErrorOccurs"));
                    await Instances.TaskQueueViewModel.Stop();
                    Instances.TaskQueueViewModel.SetStopped();
                }
            });

            await _runningState.UntilIdleAsync();
        }

        public static async Task ConnectionTest()
        {
            var endpoint = Instances.SettingsViewModel.RemoteControlGetTaskEndpointUri;

            if (!IsEndpointValid(endpoint, alarm: true))
            {
                return;
            }

            var uid = Instances.SettingsViewModel.RemoteControlUserIdentity;
            var did = Instances.SettingsViewModel.RemoteControlDeviceIdentity;

            try
            {
                var body = System.Text.Json.JsonSerializer.Serialize(new { user = uid, device = did });
                var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
                message.Headers.Accept.ParseAdd("application/json");
                message.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var client = GetPrivateFieldValue<HttpClient>(Instances.HttpService, "_client");
                var response = await client.SendAsync(message);

                if (response != null)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMsg = string.Format(LocalizationHelper.GetString("RemoteControlConnectionTestFail"), response.StatusCode);
                        using var toastFail = new ToastNotification(errorMsg);
                        toastFail.Show();
                        return;
                    }
                }
                else
                {
                    // 一般来说不会走到这里，因为null response一定会报错
                    var errorMsg = string.Format(LocalizationHelper.GetString("RemoteControlConnectionTestFail"), "Unknown");
                    using var toastFail = new ToastNotification(errorMsg);
                    toastFail.Show();
                    return;
                }

                using var toastSuccess = new ToastNotification(
                    LocalizationHelper.GetString("RemoteControlConnectionTestSuccess"));
                toastSuccess.Show();
            }
            catch (Exception e)
            {
                var error = e.Message;
                if (e.InnerException != null)
                {
                    error = e.InnerException.Message;
                }

                var errorMsg = string.Format(LocalizationHelper.GetString("RemoteControlConnectionTestFail"), error);
                using var toastErr = new ToastNotification(errorMsg);

                toastErr.Show();
            }
        }

        public static void RegenerateDeviceIdentity()
        {
            Instances.SettingsViewModel.RemoteControlDeviceIdentity = Guid.NewGuid().ToString("N");
        }

        public static bool IsEndpointValid(string endpoint, bool alarm = false)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                ShowToast("RemoteControlConnectionTestFailEmpty", alarm);
                return false;
            }

            string lowerEndpoint = endpoint.ToLower();

            if (lowerEndpoint.StartsWith("https://"))
            {
                return true;
            }
            else if (lowerEndpoint.StartsWith("http://"))
            {
                ShowToast("RemoteControlConnectionTestWarningHttpUnsafe", alarm);
                return true;
            }
            else
            {
                ShowToast("RemoteControlConnectionTestFailNotHttpOrHttps", alarm);
                return false;
            }
        }

        public static void ShowToast(string message, bool alarm)
        {
            if (alarm)
            {
                using var toast = new ToastNotification(LocalizationHelper.GetString(message));
                toast.Show();
            }
        }
    }
}
