// <copyright file="AppIcon.cs" company="MaaAssistantArknights">
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
using System.Windows;
using System.Windows.Media.Imaging;

namespace MaaWpfGui.Helper;

public class AppIcon
{
    private static readonly Lazy<BitmapSource> lazyIcon = new(ExtractIcon);

    private static BitmapSource ExtractIcon()
    {
        try
        {
            var sri = Application.GetResourceStream(new Uri("pack://application:,,,/newlogo.ico"));
            if (sri == null)
            {
                return new BitmapImage();
            }

            using var s = sri.Stream;
            var decoder = BitmapDecoder.Create(s, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var imageSource = decoder.Frames[0];

            return imageSource;
        }
        catch (Exception)
        {
            throw;
            return new BitmapImage();
        }
    }

    public static BitmapSource GetIcon() => lazyIcon.Value;
}
