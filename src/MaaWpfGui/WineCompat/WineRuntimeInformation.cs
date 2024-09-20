// <copyright file="WineRuntimeInformation.cs" company="MaaAssistantArknights">
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
using System.Runtime.InteropServices;

namespace MaaWpfGui.WineCompat
{
    internal class WineRuntimeInformation
    {
        public static bool IsRunningUnderWine { get; }

        public static string? WineVersion { get; }

        public static string? HostSystemName { get; }

        static unsafe WineRuntimeInformation()
        {
            var ntdll = NativeLibrary.Load("ntdll.dll");
            try
            {
                if (NativeLibrary.TryGetExport(ntdll, "wine_get_version", out var fn))
                {
                    var wine_get_version = (delegate* unmanaged[Cdecl]<nint>)fn;
                    var str = wine_get_version();
                    if (str != IntPtr.Zero)
                    {
                        IsRunningUnderWine = true;
                    }

                    WineVersion = Marshal.PtrToStringUTF8(str);

                    if (NativeLibrary.TryGetExport(ntdll, "wine_get_host_version", out fn))
                    {
                        var wine_get_host_version = (delegate* unmanaged[Cdecl]<nint*, nint*, void>)fn;
                        nint sysname = 0, release = 0;
                        wine_get_host_version(&sysname, &release);
                        HostSystemName = Marshal.PtrToStringUTF8(sysname);
                    }
                }
                else
                {
                    IsRunningUnderWine = false;
                }
            }
            finally
            {
                NativeLibrary.Free(ntdll);
            }
        }
    }
}
