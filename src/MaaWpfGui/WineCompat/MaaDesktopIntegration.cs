// <copyright file="MaaDesktopIntegration.cs" company="MaaAssistantArknights">
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
using System.Runtime.InteropServices;

namespace MaaWpfGui.WineCompat;

internal unsafe class MaaDesktopIntegration
{
    public static bool Availabile { get; }

    static MaaDesktopIntegration()
    {
        if (WineRuntimeInformation.IsRunningUnderWine)
        {
            try
            {
                glib_default_main_loop_start();
                AppDomain.CurrentDomain.ProcessExit += (sender, args) => glib_default_main_loop_stop();
                Availabile = true;
            }
            catch
            {
                // Ignore
            }
        }
    }

    public const string DllName = "MaaDesktopIntegration.so";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int glib_default_main_loop_start();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void glib_default_main_loop_stop();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int notify_init([MarshalAs(UnmanagedType.LPUTF8Str)] string app_name);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_object_unref(IntPtr ptr);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr notify_notification_new([MarshalAs(UnmanagedType.LPUTF8Str)] string summary, [MarshalAs(UnmanagedType.LPUTF8Str)] string body, [MarshalAs(UnmanagedType.LPUTF8Str)] string icon);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_add_action(IntPtr notification, [MarshalAs(UnmanagedType.LPUTF8Str)] string action, [MarshalAs(UnmanagedType.LPUTF8Str)] string label, delegate* unmanaged[Stdcall]<nint, nint, nint, void> callback, IntPtr user_data, delegate* unmanaged[Stdcall]<nint, void> free_func);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_clear_actions(IntPtr notification);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_clear_hints(IntPtr notification);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_close(IntPtr notification, out IntPtr error);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr notify_notification_get_activation_token(IntPtr notification);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int notify_notification_get_closed_reason(IntPtr notification);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_set_app_icon(IntPtr notification, [MarshalAs(UnmanagedType.LPUTF8Str)] string app_icon);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_set_app_name(IntPtr notification, [MarshalAs(UnmanagedType.LPUTF8Str)] string app_name);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_set_category(IntPtr notification, [MarshalAs(UnmanagedType.LPUTF8Str)] string category);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_set_hint(IntPtr notification, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, IntPtr value);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_set_image_from_pixbuf(IntPtr notification, IntPtr pixbuf);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_set_timeout(IntPtr notification, int timeout);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_set_urgency(IntPtr notification, int urgency);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void notify_notification_show(IntPtr notification, out IntPtr error);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int notify_notification_update(IntPtr notification, [MarshalAs(UnmanagedType.LPUTF8Str)] string summary, [MarshalAs(UnmanagedType.LPUTF8Str)] string body, [MarshalAs(UnmanagedType.LPUTF8Str)] string icon);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr gdk_pixbuf_new_from_data(IntPtr data, int colorspace, int has_alpha, int bits_per_sample, int width, int height, int rowstride, delegate* unmanaged[Stdcall]<nint, nint, void> destroy_fn, IntPtr destroy_fn_data);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr g_signal_connect_data(nint instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string detailed_signal, delegate* unmanaged[Stdcall]<nint, nint, void> c_handler, nint data, delegate* unmanaged[Stdcall]<nint, nint, void> destroy_data, uint connect_flags);
}
