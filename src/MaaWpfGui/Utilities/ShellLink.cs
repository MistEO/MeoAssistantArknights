// <copyright file="ShellLink.cs" company="MaaAssistantArknights">
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

namespace MaaWpfGui.Utilities
{
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal unsafe interface IShellLink
    {
        void GetPath(char* pszFile, int cchMaxPath, IntPtr pfd, int fFlags);

        void GetIDList(out IntPtr ppidl);

        void SetIDList(IntPtr pidl);

        void GetDescription(char* pszName, int cchMaxName);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory(char* pszDir, int cchMaxPath);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments(char* pszArgs, int cchMaxPath);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation(char* pszIconPath, int cchIconPath, out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

        void Resolve(IntPtr hwnd, int fFlags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
