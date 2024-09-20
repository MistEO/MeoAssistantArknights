// <copyright file="BitmapImageExtensions.cs" company="MaaAssistantArknights">
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

using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace MaaWpfGui.Helper
{
    public static class BitmapImageExtensions
    {
        public static bool IsEqual(this BitmapImage image1, BitmapImage image2)
        {
            if (image1 == null || image2 == null)
            {
                return false;
            }

            return image1.ToBytes().SequenceEqual(image2.ToBytes());
        }

        public static byte[] ToBytes(this BitmapImage image)
        {
            byte[] data = [];
            if (image == null)
            {
                return data;
            }

            try
            {
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using MemoryStream ms = new MemoryStream();
                encoder.Save(ms);
                data = ms.ToArray();

                return data;
            }
            catch
            {
                // ignored
            }

            return data;
        }
    }
}
