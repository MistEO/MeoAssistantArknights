// <copyright file="MessageBoxHelper.cs" company="MaaAssistantArknights">
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
#pragma warning disable CS0618
#pragma warning disable SA1401

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using HandyControl.Data;
using Windows.Win32;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;

[assembly: SecurityCritical]
[assembly: SecurityTreatAsSafe]

namespace MaaWpfGui.Helper
{
    /// <summary>
    /// 消息框弹窗格式管理
    /// </summary>
    public static class MessageBoxHelper
    {
        /// <summary>
        /// OK text
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static string OK = "OK";

        /// <summary>
        /// Cancel text
        /// </summary>
        public static string Cancel = "Cancel";

        /// <summary>
        /// Abort text
        /// </summary>
        public static string Abort = "Abort";

        /// <summary>
        /// Retry text
        /// </summary>
        public static string Retry = "Retry";

        /// <summary>
        /// Ignore text
        /// </summary>
        public static string Ignore = "Ignore";

        /// <summary>
        /// Yes text
        /// </summary>
        public static string Yes = "Yes";

        /// <summary>
        /// No text
        /// </summary>
        public static string No = "No";

        private static void SetImage(MessageBoxImage messageBoxImage, ref string iconKey, ref string iconBrushKey)
        {
            var key = string.Empty;
            var brushKey = string.Empty;

            switch (messageBoxImage)
            {
                case MessageBoxImage.Question:
                    key = ResourceToken.AskGeometry;
                    brushKey = ResourceToken.AccentBrush;
                    break;

                case MessageBoxImage.Error:
                    key = ResourceToken.ErrorGeometry;
                    brushKey = ResourceToken.DangerBrush;
                    break;

                case MessageBoxImage.Warning:
                    key = ResourceToken.WarningGeometry;
                    brushKey = ResourceToken.WarningBrush;
                    break;

                case MessageBoxImage.Information:
                    key = ResourceToken.InfoGeometry;
                    brushKey = ResourceToken.InfoBrush;
                    break;
            }

            iconKey = string.IsNullOrEmpty(iconKey) ? key : iconKey;
            iconBrushKey = string.IsNullOrEmpty(iconBrushKey) ? brushKey : iconBrushKey;
        }

        public static MessageBoxResult Show(MessageBoxInfo info) => HandyControl.Controls.MessageBox.Show(info);

        public static MessageBoxResult Show(
            string messageBoxText,
            string caption = "",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            string iconKey = "",
            string iconBrushKey = "",
            string ok = "",
            string cancel = "",
            string yes = "",
            string no = "",
            bool useNativeMethod = false) => Show(WindowHandle.None, messageBoxText, caption, buttons, icon, iconKey, iconBrushKey, ok, cancel, yes, no, useNativeMethod);

        public static MessageBoxResult Show(
            WindowHandle ownerWindow,
            string messageBoxText,
            string caption = "",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            string iconKey = "",
            string iconBrushKey = "",
            string ok = "",
            string cancel = "",
            string yes = "",
            string no = "",
            bool useNativeMethod = false)
        {
            caption = string.IsNullOrEmpty(caption) ? LocalizationHelper.GetString("Tip") : caption;
            ok = string.IsNullOrEmpty(ok) ? LocalizationHelper.GetString("Ok") : ok;
            cancel = string.IsNullOrEmpty(cancel) ? LocalizationHelper.GetString("ManualRestart") : cancel;
            yes = string.IsNullOrEmpty(yes) ? LocalizationHelper.GetString("Yes") : yes;
            no = string.IsNullOrEmpty(no) ? LocalizationHelper.GetString("No") : no;

            if (useNativeMethod)
            {
                return ShowNative(ownerWindow, messageBoxText, null, caption, buttons, icon, MessageBoxResult.None, false, ok, cancel, yes, no);
            }

            SetImage(icon, ref iconKey, ref iconBrushKey);
            var info = new MessageBoxInfo
            {
                Message = messageBoxText,
                Caption = caption,
                Button = buttons,
                IconKey = iconKey,
                IconBrushKey = iconBrushKey,
                ConfirmContent = ok,
                CancelContent = cancel,
                YesContent = yes,
                NoContent = no,
            };

            return HandyControl.Controls.MessageBox.Show(info);
        }

        private readonly unsafe ref struct DisposablePin<T>
            where T : unmanaged
        {
            private readonly GCHandle _handle;

            public DisposablePin(object obj)
            {
                _handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            }

            public T* AddrOfPinnedObject => (T*)_handle.AddrOfPinnedObject();

            public void Dispose()
            {
                _handle.Free();
            }
        }

        public static unsafe MessageBoxResult ShowNative(
           WindowHandle ownerWindow,
           string messageBoxText,
           string mainInstruction = "",
           string windowTitle = "",
           MessageBoxButton buttons = MessageBoxButton.OK,
           MessageBoxImage icon = MessageBoxImage.None,
           MessageBoxResult defaultButton = MessageBoxResult.None,
           bool alwaysAllowCancel = false,
           string ok = "",
           string cancel = "",
           string yes = "",
           string no = "")
        {
            using var contentPin = new DisposablePin<char>(messageBoxText);
            using var instructionPin = new DisposablePin<char>(mainInstruction);
            using var titlePin = new DisposablePin<char>(windowTitle);

            var config = new TASKDIALOGCONFIG
            {
                pszContent = contentPin.AddrOfPinnedObject,
                pszMainInstruction = instructionPin.AddrOfPinnedObject,
                pszWindowTitle = titlePin.AddrOfPinnedObject,
                hwndParent = (Windows.Win32.Foundation.HWND)ownerWindow.Handle,
                nDefaultButton = (int)defaultButton,
                dwFlags = TASKDIALOG_FLAGS.TDF_POSITION_RELATIVE_TO_WINDOW | TASKDIALOG_FLAGS.TDF_SIZE_TO_CONTENT,
            };

            if (alwaysAllowCancel)
            {
                config.dwFlags |= TASKDIALOG_FLAGS.TDF_ALLOW_DIALOG_CANCELLATION;
            }

            switch (icon)
            {
                case MessageBoxImage.Information:
                    // case MessageBoxImage.Asterisk:
                    config.Anonymous1.pszMainIcon = PInvoke.TD_INFORMATION_ICON;
                    break;
                case MessageBoxImage.Hand:
                    // case MessageBoxImage.Stop:
                    // case MessageBoxImage.Error
                    config.Anonymous1.pszMainIcon = PInvoke.TD_ERROR_ICON;
                    break;
                case MessageBoxImage.Exclamation:
                    // case MessageBoxImage.Warning:
                    config.Anonymous1.pszMainIcon = PInvoke.TD_WARNING_ICON;
                    break;
                case MessageBoxImage.Question:
                    var iconInfo = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>() };
                    PInvoke.SHGetStockIconInfo(SHSTOCKICONID.SIID_HELP, SHGSI_FLAGS.SHGSI_ICON, ref iconInfo).ThrowOnFailure();
                    config.Anonymous1.hMainIcon = iconInfo.hIcon;
                    config.dwFlags |= TASKDIALOG_FLAGS.TDF_USE_HICON_MAIN;
                    break;
                case MessageBoxImage.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(icon), icon, null);
            }

            bool hasOk = false, hasCancel = false, hasYes = false, hasNo = false;
            var customButtons = new List<TASKDIALOG_BUTTON>();
            var gcHandles = new List<GCHandle>();
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    hasOk = true;
                    break;
                case MessageBoxButton.OKCancel:
                    hasOk = true;
                    hasCancel = true;
                    break;
                case MessageBoxButton.YesNoCancel:
                    hasYes = true;
                    hasNo = true;
                    hasCancel = true;
                    break;
                case MessageBoxButton.YesNo:
                    hasYes = true;
                    hasNo = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
            }

            if (hasOk)
            {
                if (string.IsNullOrEmpty(ok))
                {
                    config.dwCommonButtons |= TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON;
                }
                else
                {
                    var gch = GCHandle.Alloc(ok, GCHandleType.Pinned);
                    gcHandles.Add(gch);
                    customButtons.Add(new TASKDIALOG_BUTTON { nButtonID = (int)MessageBoxResult.OK, pszButtonText = (char*)gch.AddrOfPinnedObject() });
                }
            }

            if (hasCancel)
            {
                if (string.IsNullOrEmpty(cancel))
                {
                    config.dwCommonButtons |= TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CANCEL_BUTTON;
                }
                else
                {
                    var gch = GCHandle.Alloc(cancel, GCHandleType.Pinned);
                    gcHandles.Add(gch);
                    customButtons.Add(new TASKDIALOG_BUTTON { nButtonID = (int)MessageBoxResult.Cancel, pszButtonText = (char*)gch.AddrOfPinnedObject() });
                }
            }

            if (hasYes)
            {
                if (string.IsNullOrEmpty(yes))
                {
                    config.dwCommonButtons |= TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_YES_BUTTON;
                }
                else
                {
                    var gch = GCHandle.Alloc(yes, GCHandleType.Pinned);
                    gcHandles.Add(gch);
                    customButtons.Add(new TASKDIALOG_BUTTON { nButtonID = (int)MessageBoxResult.Yes, pszButtonText = (char*)gch.AddrOfPinnedObject() });
                }
            }

            if (hasNo)
            {
                if (string.IsNullOrEmpty(no))
                {
                    config.dwCommonButtons |= TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_NO_BUTTON;
                }
                else
                {
                    var gch = GCHandle.Alloc(no, GCHandleType.Pinned);
                    gcHandles.Add(gch);
                    customButtons.Add(new TASKDIALOG_BUTTON { nButtonID = (int)MessageBoxResult.No, pszButtonText = (char*)gch.AddrOfPinnedObject() });
                }
            }

            if (customButtons.Count != 0)
            {
                var array = customButtons.ToArray();
                var gch = GCHandle.Alloc(array, GCHandleType.Pinned);
                gcHandles.Add(gch);
                config.pButtons = (TASKDIALOG_BUTTON*)gch.AddrOfPinnedObject();
                config.cButtons = (uint)customButtons.Count;
            }

            int button = 0;
            PInvoke.TaskDialogIndirect(config, &button, null, null).ThrowOnFailure();

            foreach (var h in gcHandles)
            {
                h.Free();
            }

            return (MessageBoxResult)button;
        }
    }
}
