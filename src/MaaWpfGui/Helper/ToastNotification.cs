// <copyright file="ToastNotification.cs" company="MaaAssistantArknights">
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
#pragma warning disable SA1307

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;
using MaaWpfGui.Configuration;
using MaaWpfGui.Helper.Notification;
using MaaWpfGui.WineCompat;
using Microsoft.Win32;

using Serilog;

namespace MaaWpfGui.Helper
{
    /// <summary>
    /// The toast notification.
    /// </summary>
    public class ToastNotification : IDisposable
    {
        private unsafe struct OSVERSIONINFOEXW
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            private fixed char _szCSDVersion[128];
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;

            public Span<char> szCSDVersion => MemoryMarshal.CreateSpan(ref _szCSDVersion[0], 128);
        }

        [DllImport("ntdll.dll", ExactSpelling = true)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEXW versionInfo);

        // 这玩意有用吗？
        // ReSharper disable once UnusedMember.Local
        private static INotificationPoster _notificationPoster;

        private static readonly string _openUrlPrefix;

        static ToastNotification()
        {
            _notificationPoster = GetNotificationPoster();
            _notificationPoster.ActionActivated += OnActionActivated;
            _openUrlPrefix = $"OpenUrl:{_notificationPoster.GetHashCode()}:";
        }

        private static readonly ILogger _logger = Log.ForContext<ToastNotification>();

        private static unsafe bool IsWindows10OrGreater()
        {
            var osVersionInfo = default(OSVERSIONINFOEXW);
            osVersionInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);
            RtlGetVersion(ref osVersionInfo);
            var version = new Version(osVersionInfo.dwMajorVersion, osVersionInfo.dwMinorVersion, osVersionInfo.dwBuildNumber);
            return version > new Version(10, 0, 10240);
        }

        /// <summary>
        /// Checks toast system.
        /// </summary>
        /// <returns>The toast system is initialized.</returns>
        private static INotificationPoster GetNotificationPoster()
        {
            if (WineRuntimeInformation.IsRunningUnderWine)
            {
                if (NotificationImplLibNotify.IsSupported)
                {
                    return new NotificationImplLibNotify();
                }
            }
            else if (IsWindows10OrGreater())
            {
                return new NotificationImplWinRT();
            }

            return new NotificationImplWpf();
        }

        /// <summary>
        /// 按钮激活后的事件，参数为按钮标签
        /// </summary>
        public static event EventHandler<string> ActionActivated;

        /// <summary>
        /// 通知标题
        /// </summary>
        private string _notificationTitle = string.Empty;

        /// <summary>
        /// 通知文本内容
        /// </summary>
        private StringBuilder _contentCollection = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="ToastNotification"/> class.
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <remarks>
        /// <para>Toast通知，基于 <see cref="NotificationConstants"/> 实现，方便管理通知样式。</para>
        /// <para>建议使用 <see langword="using"/> 调用，如果不使用 <see langword="using"/> 调用，建议手动调用 <see cref="Dispose()"/> 方法释放。</para>
        /// <para>在线程中必须使用 <c>Dispatcher.Invoke</c> 相关方法调用。</para>
        /// </remarks>
        public ToastNotification(string title = null)
        {
            // 初始化通知标题
            _notificationTitle = title ?? _notificationTitle;
        }

        private static void OnActionActivated(object sender, string tag)
        {
            if (tag.StartsWith(_openUrlPrefix))
            {
                var url = tag.Substring(_openUrlPrefix.Length);
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else
            {
                ActionActivated?.Invoke(sender, tag);
            }
        }

        /// <summary>
        /// 添加一行文本内容
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>返回本类，可继续调用其它方法</returns>
        public ToastNotification AppendContentText(string text = null)
        {
            _contentCollection.AppendLine(text ?? string.Empty);
            return this;
        }

        #region 通知提示音

        #region 通知提示音列表枚举

        /// <summary>
        /// 通知提示音列表枚举，
        /// </summary>
        public enum NotificationSounds
        {
            /// <summary>
            /// 默认响声。
            /// </summary>
            [Description("默认响声")]
            Beep,

            /// <summary>
            /// 感叹号。
            /// </summary>
            [Description("感叹号")]
            Exclamation,

            /// <summary>
            /// 星号。
            /// </summary>
            [Description("星号")]
            Asterisk,

            /// <summary>
            /// 问题。
            /// </summary>
            [Description("问题")]
            Question,

            /// <summary>
            /// 关键性停止。
            /// </summary>
            [Description("关键性停止")]
            Hand,

            /// <summary>
            /// 通知 (Windows 10 及以上特有，低版本系统直接用也可以)。
            /// </summary>
            [Description("通知 (Windows 10 及以上特有，低版本系统直接用也可以)")]
            Notification,

            /// <summary>
            /// 无声。
            /// </summary>
            [Description("无声")]
            None,
        }

        #endregion 通知提示音列表枚举

        /// <summary>
        /// 播放通知提示音
        /// 如果要播放音频文件，参考微软文档 <see cref="SoundPlayer"/> 类
        /// </summary>
        /// <param name="sounds">提示音类型</param>
        protected void PlayNotificationSound(NotificationSounds sounds = NotificationSounds.None)
        {
            try
            {
                switch (sounds)
                {
                    case NotificationSounds.Exclamation: SystemSounds.Exclamation.Play(); break;
                    case NotificationSounds.Asterisk: SystemSounds.Asterisk.Play(); break;
                    case NotificationSounds.Question: SystemSounds.Question.Play(); break;
                    case NotificationSounds.Beep: SystemSounds.Beep.Play(); break;
                    case NotificationSounds.Hand: SystemSounds.Hand.Play(); break;

                    case NotificationSounds.Notification:
                        using (var key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current"))
                        {
                            if (key != null)
                            {
                                // 获取 (Default) 项
                                var o = key.GetValue(null);
                                if (o != null)
                                {
                                    var theSound = new SoundPlayer((string)o);
                                    theSound.Play();
                                }
                            }
                            else
                            {
                                // 如果不支持就播放其它提示音
                                PlayNotificationSound(NotificationSounds.Asterisk);
                            }
                        }

                        break;

                    case NotificationSounds.None:
                    default: break;
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        #endregion 通知提示音

        #region 通知按钮设置

        #region 通知按钮变量

        private List<NotificationAction> _actions = [];

        #endregion 通知按钮变量

        /// <summary>
        /// 给通知添加一个按钮
        /// </summary>
        /// <param name="label">按钮标题</param>
        /// <param name="tag">事件标签</param>
        /// <returns>返回类本身，可继续调用其它方法</returns>
        /// <remarks>按钮按下时，触发 <see cref="ActionActivated"/> 事件</remarks>
        public ToastNotification AddButton(string label, string tag)
        {
            _actions.Add(new NotificationAction(label, tag));
            return this;
        }

        public static string GetActionTagForOpenWeb(string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                return _openUrlPrefix + url;
            }

            throw new ArgumentException("URL must start with http:// or https://");
        }

        #endregion 通知按钮设置

        #region 通知显示
        #region 显示通知方法

        /// <summary>
        /// 显示通知
        /// </summary>
        /// <param name="lifeTime">通知显示时间 (s)</param>
        /// <param name="row">内容显示行数，如果内容太多建议使用 <see cref="ShowMore(double, uint, NotificationSounds, NotificationContent)"/></param>
        /// <param name="sound">播放提示音</param>
        /// <param name="hints">通知额外元数据，可能影响通知展示方式</param>
        ///
        public void Show(double lifeTime = 10d, uint row = 1,
            NotificationSounds sound = NotificationSounds.Notification, params NotificationHint[] hints)
        {
            // TODO: 整理过时代码
            if (!ConfigFactory.CurrentConfig.GUI.UseNotify)
            {
                return;
            }

            var content = new NotificationContent
            {
                Summary = _notificationTitle,
                Body = _contentCollection.ToString(),
            };

            foreach (var action in _actions)
            {
                content.Actions.Add(action);
            }

            content.Hints.Add(NotificationHint.RowCount((int)row));

            // 调整显示时间，如果存在按钮的情况下显示时间将强制设为最大时间
            lifeTime = lifeTime < 3d ? 3d : lifeTime;

            var timeSpan = _actions.Count != 0
                ? TimeSpan.FromSeconds(lifeTime)
                : TimeSpan.MaxValue;

            content.Hints.Add(NotificationHint.ExpirationTime(timeSpan));

            foreach (var hint in hints)
            {
                content.Hints.Add(hint);
            }

            // 显示通知
            _notificationPoster.ShowNotification(content);

            // 播放通知提示音
            PlayNotificationSound(sound);

            // 任务栏闪烁
            FlashWindowEx();
        }

        /// <summary>
        /// 显示内容更多的通知
        /// </summary>
        /// <param name="lifeTime">通知显示时间 (s)</param>
        /// <param name="row">内容显示行数，只用于预览一部分通知，多出内容会放在附加按钮的窗口中</param>
        /// <param name="sound">播放提示音，不设置就没有声音</param>
        public void ShowMore(double lifeTime = 12d, uint row = 2,
            NotificationSounds sound = NotificationSounds.None, params NotificationHint[] hints)
        {
            var morehints = new NotificationHint[hints.Length + 1];
            hints.CopyTo(morehints, 0);
            morehints[hints.Length] = NotificationHint.Expandable;
            Show(lifeTime: lifeTime,
                 row: row,
                 sound: sound,
                 hints: morehints);
        }

        /// <summary>
        /// 显示公招特殊标签通知
        /// </summary>
        /// <param name="row">内容显示行数，比如第 2 行用来放星星</param>
        public void ShowRecruit(uint row = 1)
        {
            Show(row: row,
                 sound: NotificationSounds.Notification,
                 hints: NotificationHint.RecruitHighRarity);
        }

        /// <summary>
        /// 显示公招小车标签通知
        /// </summary>
        /// <param name="row">内容显示行数，比如第 2 行用来放星星</param>
        public void ShowRecruitRobot(uint row = 1)
        {
            Show(row: row,
                sound: NotificationSounds.Notification,
                hints: NotificationHint.RecruitRobot);
        }

        /// <summary>
        /// 显示更新版本的通知
        /// </summary>
        /// <param name="row">内容行数</param>
        public void ShowUpdateVersion(uint row = 3)
        {
            ShowMore(row: row,
                    sound: NotificationSounds.Notification,
                    hints: NotificationHint.NewVersion);
        }

        #endregion 显示通知方法

        #endregion 通知显示

        #region 任务栏闪烁

        /// <summary>
        /// 闪烁信息
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private struct FLASHWINFO
        {
            /// <summary>
            /// 结构大小
            /// </summary>
            public uint cbSize;

            /// <summary>
            /// 要闪烁或停止的窗口句柄
            /// </summary>
            public IntPtr hwnd;

            /// <summary>
            /// 闪烁的类型
            /// </summary>
            public uint dwFlags;

            /// <summary>
            /// 闪烁窗口的次数
            /// </summary>
            public uint uCount;

            /// <summary>
            /// 窗口闪烁的频率（毫秒）
            /// </summary>
            public uint dwTimeout;
        }

#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        /// <summary>
        /// 闪烁类型。
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public enum FlashType : uint
        {
            /// <summary>
            /// 停止闪烁。
            /// </summary>
            [Description("停止闪烁")]
            FLASHW_STOP = 0,

            /// <summary>
            /// 只闪烁标题。
            /// </summary>
            [Description("只闪烁标题")]
            FALSHW_CAPTION = 1,

            /// <summary>
            /// 只闪烁任务栏。
            /// </summary>
            [Description("只闪烁任务栏")]
            FLASHW_TRAY = 2,

            /// <summary>
            /// 标题和任务栏同时闪烁。
            /// </summary>
            [Description("标题和任务栏同时闪烁")]
            FLASHW_ALL = 3,

            /// <summary>
            /// 与 <see cref="FLASHW_TRAY"/> 配合使用。
            /// </summary>
            FLASHW_PARAM1 = 4,

            /// <summary>
            /// 与 <see cref="FLASHW_TRAY"/> 配合使用。
            /// </summary>
            FLASHW_PARAM2 = 12,

            /// <summary>
            /// 闪烁直到达到次数或收到停止。
            /// </summary>
            [Description("闪烁直到达到次数或收到停止")]
            FLASHW_TIMER = FLASHW_TRAY | FLASHW_PARAM1,

            /// <summary>
            /// 未激活时闪烁直到窗口被激活或收到停止。
            /// </summary>
            [Description("未激活时闪烁直到窗口被激活或收到停止")]
            FLASHW_TIMERNOFG = FLASHW_TRAY | FLASHW_PARAM2,
        }

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        /// <summary>
        /// 闪烁窗口任务栏
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="type">闪烁类型</param>
        /// <param name="count">闪烁次数</param>
        /// <returns>是否成功</returns>
        public static bool FlashWindowEx(IntPtr hWnd = default, FlashType type = FlashType.FLASHW_TIMERNOFG, uint count = 5)
        {
            var fInfo = default(FLASHWINFO);
            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = hWnd != default ? hWnd : new WindowInteropHelper(System.Windows.Application.Current.MainWindow!).Handle;
            fInfo.dwFlags = (uint)type;
            fInfo.uCount = count;
            fInfo.dwTimeout = 0;
            return FlashWindowEx(ref fInfo);
        }

        #endregion 任务栏闪烁

        /// <summary>
        /// 通知使用完后释放已使用的数据
        /// </summary>
        public void Dispose()
        {
            _contentCollection.Clear();
        }

        public static void Cleanup()
        {
            try
            {
                (_notificationPoster as IDisposable)?.Dispose();
            }
            catch (Exception e)
            {
                Log.ForContext<ToastNotification>().Error(e, "Cleanup error");
            }
        }
    }
}
