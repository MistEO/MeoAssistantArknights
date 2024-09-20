// <copyright file="Types.cs" company="MaaAssistantArknights">
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
#pragma warning disable CS0649
namespace MaaWpfGui.WineCompat.FontConfig;

internal enum FcResult
{
    Match,
    NoMatch,
    TypeMismatch,
    NoId,
    OutOfMemory
}

internal enum FcMatchKind
{
    Pattern,
    Font,
    Scan,
    FcMatchKindEnd,
    FcMatchKindBegin = Pattern
}

internal unsafe struct FcFontSet
{
    public int nfont;
    public int sfont;
    public FcPattern** fonts;
}

internal struct FcConfig { }
internal struct FcPattern { }
internal struct FcCharSet { }

internal enum FcBool
{
    False,
    True,
    DontCare
}
