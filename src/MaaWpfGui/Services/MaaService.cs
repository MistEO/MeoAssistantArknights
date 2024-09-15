// <copyright file="MaaService.cs" company="MaaAssistantArknights">
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
using MaaWpfGui.Main;
using Newtonsoft.Json.Linq;
using AsstHandle = System.IntPtr;
using AsstInstanceOptionKey = System.Int32;
using AsstTaskId = System.Int32;

namespace MaaWpfGui.Services
{
    public static class MaaService
    {
        public delegate void CallbackDelegate(int msg, IntPtr jsonBuffer, IntPtr customArg);

        public delegate void ProcCallbackMsg(AsstMsg msg, JObject details);

        [DllImport("MaaCore.dll")]
        public static extern AsstHandle AsstCreateEx(CallbackDelegate callback, IntPtr customArg);

        [DllImport("MaaCore.dll")]
        public static extern void AsstDestroy(AsstHandle handle);

        [DllImport("MaaCore.dll")]
        public static extern unsafe bool AsstSetInstanceOption(AsstHandle handle, AsstInstanceOptionKey key, byte* value);

        [DllImport("MaaCore.dll")]
        public static extern bool AsstSetStaticOption(AsstStaticOptionKey key, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport("MaaCore.dll")]
        public static extern unsafe bool AsstLoadResource(byte* dirname);

        [DllImport("MaaCore.dll")]
        public static extern unsafe bool AsstConnect(AsstHandle handle, byte* adbPath, byte* address, byte* config);

        [DllImport("MaaCore.dll")]
        public static extern unsafe AsstTaskId AsstAppendTask(AsstHandle handle, byte* type, byte* taskParams);

        [DllImport("MaaCore.dll")]
        public static extern unsafe bool AsstSetTaskParams(AsstHandle handle, AsstTaskId id, byte* taskParams);

        [DllImport("MaaCore.dll")]
        public static extern bool AsstStart(AsstHandle handle);

        [DllImport("MaaCore.dll")]
        public static extern bool AsstRunning(AsstHandle handle);

        [DllImport("MaaCore.dll")]
        public static extern bool AsstStop(AsstHandle handle);

        [DllImport("MaaCore.dll")]
        public static extern unsafe ulong AsstGetImage(AsstHandle handle, byte* buff, ulong buffSize);

        [DllImport("MaaCore.dll")]
        public static extern ulong AsstGetNullSize();

        [DllImport("MaaCore.dll")]
        public static extern IntPtr AsstGetVersion();

        [DllImport("MaaCore.dll")]
        public static extern bool AsstBackToHome(AsstHandle handle);

        [DllImport("MaaCore.dll")]
        public static extern unsafe void AsstSetConnectionExtras(byte* name, byte* extras);
    }
}
