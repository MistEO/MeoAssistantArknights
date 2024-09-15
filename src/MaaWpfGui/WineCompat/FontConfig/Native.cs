// <copyright file="Native.cs" company="MaaAssistantArknights">
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
using System.Runtime.InteropServices;

namespace MaaWpfGui.WineCompat.FontConfig;

internal unsafe class Native
{
    private const string DllName = MaaDesktopIntegration.DllName;

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern FcConfig* FcInitLoadConfigAndFonts();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern FcPattern* FcNameParse([MarshalAs(UnmanagedType.LPUTF8Str)] string pattern);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern FcBool FcConfigSubstitute(FcConfig* config, FcPattern* pattern, FcMatchKind kind);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern void FcDefaultSubstitute(FcPattern* pattern);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern FcResult FcPatternGetString(FcPattern* pattern, [MarshalAs(UnmanagedType.LPUTF8Str)] string obj, int n, out nint s);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern FcResult FcPatternGetCharSet(FcPattern* pattern, [MarshalAs(UnmanagedType.LPUTF8Str)] string obj, int n, out FcCharSet* s);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern FcFontSet* FcFontSort(FcConfig* config, FcPattern* pattern, FcBool trim, nint* blanks, out FcResult result);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern FcCharSet* FcCharSetCreate();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern FcCharSet* FcCharSetSubtract(FcCharSet* a, FcCharSet* b);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int FcCharSetMerge(FcCharSet* a, FcCharSet* b, out FcBool changed);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint FcCharSetFirstPage(FcCharSet* a, ref uint map, out uint next);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint FcCharSetNextPage(FcCharSet* a, ref uint map, ref uint next);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern void FcCharSetDestroy(FcCharSet* a);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern void FcPatternDestroy(FcPattern* pattern);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern void FcFontSetDestroy(FcFontSet* fs);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern void FcConfigDestroy(FcConfig* config);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int MaaFcCharsetMapSize();

    public static int FC_CHARSET_MAP_SIZE { get; } = MaaFcCharsetMapSize();

    public const uint FC_CHARSET_DONE = unchecked((uint)-1);
}
