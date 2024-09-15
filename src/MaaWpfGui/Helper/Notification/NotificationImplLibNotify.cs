// <copyright file="NotificationImplLibNotify.cs" company="MaaAssistantArknights">
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
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaaWpfGui.WineCompat;
using Stylet;

namespace MaaWpfGui.Helper.Notification;

internal class NotificationImplLibNotify : INotificationPoster
{
    public event EventHandler<string> ActionActivated;

    public static bool IsSupported { get; } = CheckSupport();

    private static GdkPixBuf _icon;

    private static bool CheckSupport()
    {
        try
        {
            if (MaaDesktopIntegration.notify_init("MAA") == 0)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        Execute.OnUIThread(() =>
        {
            var bmp3 = new FormatConvertedBitmap(AppIcon.GetIcon(), PixelFormats.Bgra32, null, 0);
            var pixels = new byte[bmp3.PixelWidth * bmp3.PixelHeight * 4];
            bmp3.CopyPixels(pixels, bmp3.PixelWidth * 4, 0);

            // BGRA -> RGBA
            var u32view = MemoryMarshal.Cast<byte, uint>(pixels.AsSpan());
            for (int i = 0; i < u32view.Length; i++)
            {
                var bgra = u32view[i]; // BB GG RR AA -> 0xAARRGGBB
                var argb = BinaryPrimitives.ReverseEndianness(bgra); // AA RR GG BB 0xBBGGRRAA
                var rgba = BitOperations.RotateRight(argb, 8); // RR GG BB AA -> 0xAABBGGRR
                u32view[i] = rgba;
            }

            _icon = GdkPixBuf.CreateFromPixels(bmp3.PixelWidth, bmp3.PixelHeight, bmp3.PixelWidth * 4, true, pixels);
        });
        return true;
    }

    public void ShowNotification(NotificationContent content)
    {
        var notification = new LibNotifyNotification(content.Summary, content.Body, string.Empty);
        notification.SetImageFromPixbuf(_icon);
        foreach (var action in content.Actions)
        {
            notification.AddAction(action.Tag, action.Label);
        }

        notification.ActionActivated += (sender, args) => ActionActivated?.Invoke(this, args);
        notification.Show();
        var gch = GCHandle.Alloc(notification);
        notification.Closed += (sender, args) =>
        {
            gch.Free();
        };
    }
}
