// <copyright file="GdkPixBuf.cs" company="MaaAssistantArknights">
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
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static MaaWpfGui.WineCompat.MaaDesktopIntegration;

namespace MaaWpfGui.WineCompat;

internal class GdkPixBuf : GObject
{
    public const int GDK_COLORSPACE_RGB = 0;

    private class PinnedPixels : IDisposable
    {
        public MemoryHandle Handle { get; }

        public PinnedPixels(Memory<byte> pixels)
        {
            Handle = pixels.Pin();
        }

        public void Dispose()
        {
            Handle.Dispose();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        public static void DestroyCallback(IntPtr pixels, IntPtr data)
        {
            var handle = GCHandle.FromIntPtr(data);
            handle.Free();
        }
    }

    private GdkPixBuf(nint handle)
        : base(handle)
    {
    }

    public static unsafe GdkPixBuf CreateFromPixels(int width, int height, int rowStride, bool has_alpha, Memory<byte> pixels)
    {
        var pin = new PinnedPixels(pixels);
        var gch = GCHandle.Alloc(pin);
        var obj = gdk_pixbuf_new_from_data((IntPtr)pin.Handle.Pointer, GDK_COLORSPACE_RGB, has_alpha ? 1 : 0, 8, width, height, rowStride, &PinnedPixels.DestroyCallback, GCHandle.ToIntPtr(gch));
        return new GdkPixBuf(obj);
    }
}
