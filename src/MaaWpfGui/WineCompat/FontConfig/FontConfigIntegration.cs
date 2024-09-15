// <copyright file="FontConfigIntegration.cs" company="MaaAssistantArknights">
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
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media;
using Serilog;
using static MaaWpfGui.WineCompat.FontConfig.Native;

namespace MaaWpfGui.WineCompat.FontConfig;

public unsafe class FontConfigIntegration
{
    private readonly ref struct Defer(Action action)
    {
        public void Dispose() => action();
    }

    public static void Install(string wpf, string fontconfig)
    {
        var wpffont = new FontFamily(wpf);
        var fceq = GetFontFamilyFromPattern(fontconfig);
        if (fceq == null)
        {
            return;
        }

        var map = wpffont.FamilyMaps;
        map.Clear();
        int i = 0;
        foreach (var item in fceq.FamilyMaps)
        {
            map.Insert(i++, item);
        }
    }

    public static void Install()
    {
        Debug.WriteLine($"FontConfigIntegration Install");
        // GC.KeepAlive(DefaultFont.UserInterface);
        // DefaultFont.UserInterface = GetFontFamilyFromPattern("system-ui");
        // DefaultFont.Monospace = GetFontFamilyFromPattern("monospace");
        // DefaultFont.SansSerif = GetFontFamilyFromPattern("sans-serif");
        // DefaultFont.Serif = GetFontFamilyFromPattern("serif");

        Install("Global User Interface", "system-ui");
        Install("Global Monospace", "monospace");
        Install("Global Sans Serif", "sans-serif");
        Install("Global Serif", "serif");
    }

    public static FontFamily GetFontFamilyFromPattern(string pattern)
    {
        var logger = Log.ForContext<FontConfigIntegration>();
        logger.Information($"GetFontFamilyFromPattern: {pattern}");
        var config = FcInitLoadConfigAndFonts();
        using var _dtor_config = new Defer(() => FcConfigDestroy(config));

        var fcPattern = FcNameParse(pattern);
        using var _dtor_pattern = new Defer(() => FcPatternDestroy(fcPattern));

        FcConfigSubstitute(config, fcPattern, FcMatchKind.Pattern);
        FcDefaultSubstitute(fcPattern);

        var fs = FcFontSort(config, fcPattern, FcBool.True, null, out var result);
        if (fs == null)
        {
            logger.Error("FcFontSort failed");
            return null;
        }

        var wpffont = new FontFamily();
        wpffont.FamilyNames.Add(XmlLanguage.GetLanguage("en-us"), "FontConfig " + pattern);

        var resolved = FcCharSetCreate();
        using var _dtor_resolved = new Defer(() => FcCharSetDestroy(resolved));

        for (var i = 0; i < fs->nfont; i++)
        {
            var item = new FontFamilyMap();
            result = FcPatternGetString(fs->fonts[i], "family", 0, out var pfamily);
            if (result == FcResult.Match)
            {
                item.Target = Marshal.PtrToStringUTF8(pfamily);
                logger.Information(item.Target);
            }
            else
            {
                logger.Error("FcPatternGetString failed: {0}", result);
                continue;
            }

            result = FcPatternGetCharSet(fs->fonts[i], "charset", 0, out var s);
            if (result == FcResult.Match)
            {
                var fallback = FcCharSetSubtract(s, resolved);
                using var _dtor_fallback = new Defer(() => FcCharSetDestroy(fallback));
                FcCharSetMerge(resolved, s, out _);
                var range = CharSetToUnicodeRange(fallback);
                item.Unicode = range;
                logger.Information($"{item.Target} {range}");
                wpffont.FamilyMaps.Add(item);
            }
            else
            {
                logger.Error("FcPatternGetCharSet failed: {0}", result);
            }
        }

        return wpffont;
    }

    private static string CharSetToUnicodeRange(FcCharSet* cs)
    {
        // return "0020-10FFFF";
        Span<uint> bitmap = stackalloc uint[FC_CHARSET_MAP_SIZE];
        var sb = new StringBuilder();

        var offset = FcCharSetFirstPage(cs, ref bitmap[0], out var next);

        var range_start = 0u;
        var range_end = 0u;
        bool in_range = false;

        if ((bitmap[0] & 1) != 0)
        {
            BeginRange(offset);
        }

        void BeginRange(uint start)
        {
            range_start = start;
            range_end = start;
            in_range = true;
        }

        void AdvanceRange(uint length)
        {
            if (!in_range)
            {
                throw new InvalidOperationException("range not started");
            }

            range_end += length;
        }

        void EndRange()
        {
            if (!in_range)
            {
                throw new InvalidOperationException("range not started");
            }

            if (sb.Length > 0)
            {
                sb.Append(",");
            }

            if (range_end - range_start == 1)
            {
                sb.Append($"{range_start:X4}");
            }
            else
            {
                sb.Append($"{range_start:X4}-{range_end - 1:X4}");
            }

            in_range = false;
            range_start = 0;
            range_end = 0;
        }

        while (offset != FC_CHARSET_DONE)
        {
            for (var i = 0; i < bitmap.Length; i++)
            {
                var field = bitmap[i];
                var bit_offset = 0u;

                // happy path: no bits unset
                if (field == 0xFFFFFFFF)
                {
                    if (!in_range)
                    {
                        BeginRange(offset + ((uint)i * 32));
                    }

                    AdvanceRange(32);
                    continue;
                }

                if (field == 0)
                {
                    if (in_range)
                    {
                        EndRange();
                    }

                    continue;
                }

                while (field != 0 && bit_offset < 32)
                {
                    var trailing_0 = BitOperations.TrailingZeroCount(field);
                    if (trailing_0 > 0)
                    {
                        if (in_range)
                        {
                            EndRange();
                        }

                        field >>= trailing_0;
                        bit_offset += (uint)trailing_0;
                        if (field != 0)
                        {
                            BeginRange(offset + ((uint)i * 32) + bit_offset);
                        }
                    }

                    var trailing_1 = BitOperations.TrailingZeroCount(~field);
                    if (trailing_1 > 0)
                    {
                        if (!in_range)
                        {
                            BeginRange(offset + ((uint)i * 32) + bit_offset);
                        }

                        AdvanceRange((uint)trailing_1);
                        field >>= trailing_1;
                        bit_offset += (uint)trailing_1;
                        if (field == 0 && bit_offset < 32)
                        {
                            EndRange();
                        }
                    }
                }
            }

            if (next == FC_CHARSET_DONE)
            {
                break;
            }

            if (in_range && next != offset + (bitmap.Length * 32))
            {
                EndRange();
            }

            offset = FcCharSetNextPage(cs, ref bitmap[0], ref next);
        }

        return sb.ToString();
    }
}
