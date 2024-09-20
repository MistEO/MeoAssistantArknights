// <copyright file="LibNotifyNotification.cs" company="MaaAssistantArknights">
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static MaaWpfGui.WineCompat.MaaDesktopIntegration;

namespace MaaWpfGui.WineCompat;

internal class LibNotifyNotification : GObject
{
    protected GCHandle WeakGCHandle { get; }

    public LibNotifyNotification(string title, string message, string iconPath)
        : base(CreateObject(title, message, iconPath))
    {
        WeakGCHandle = GCHandle.Alloc(this, GCHandleType.Weak);
        unsafe
        {
            g_signal_connect_data(Handle, "closed", &CloseSignalCallback, GCHandle.ToIntPtr(WeakGCHandle), &CloseSignalFreeCallback, 0);
        }
    }

    ~LibNotifyNotification()
    {
        WeakGCHandle.Free();
    }

    private static IntPtr CreateObject(string title, string message, string iconPath)
    {
        return notify_notification_new(title, message, iconPath);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void ActionCallback(nint notification, nint action, nint userData)
    {
        var instance = GCHandle.FromIntPtr(userData).Target as LibNotifyNotification;
        instance?.ActionActivated?.Invoke(instance, Marshal.PtrToStringUTF8(action)!);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void ActionFreeCallback(nint userData)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void CloseSignalCallback(nint notification, nint userData)
    {
        var instance = GCHandle.FromIntPtr(userData).Target as LibNotifyNotification;
        instance?.Closed?.Invoke(instance, EventArgs.Empty);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void CloseSignalFreeCallback(nint userData, nint closure)
    {
    }

    public event EventHandler<string> ActionActivated;

    public event EventHandler Closed;

    public unsafe void AddAction(string tag, string label)
    {
        notify_notification_add_action(Handle, tag, label, &ActionCallback, GCHandle.ToIntPtr(WeakGCHandle), &ActionFreeCallback);
    }

    public void ClearActions()
    {
        notify_notification_clear_actions(Handle);
    }

    public void ClearHints()
    {
        notify_notification_clear_hints(Handle);
    }

    public void Close()
    {
        notify_notification_close(Handle, out var error);
    }

    public void SetAppIcon(string appIcon)
    {
        notify_notification_set_app_icon(Handle, appIcon);
    }

    public void SetAppName(string appName)
    {
        notify_notification_set_app_name(Handle, appName);
    }

    public void SetCategory(string category)
    {
        notify_notification_set_category(Handle, category);
    }

    public void SetHint(string key, nint value)
    {
        notify_notification_set_hint(Handle, key, value);
    }

    public void SetImageFromPixbuf(GdkPixBuf pixbuf)
    {
        notify_notification_set_image_from_pixbuf(Handle, pixbuf.Handle);
    }

    public void SetTimeout(int timeout)
    {
        notify_notification_set_timeout(Handle, timeout);
    }

    public void SetUrgency(int urgency)
    {
        notify_notification_set_urgency(Handle, urgency);
    }

    public void Show()
    {
        notify_notification_show(Handle, out var error);
    }

    public bool Update(string title, string message, string iconPath)
    {
        return notify_notification_update(Handle, title, message, iconPath) != 0;
    }
}
