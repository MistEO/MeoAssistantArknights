// <copyright file="GObject.cs" company="MaaAssistantArknights">
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

using static MaaWpfGui.WineCompat.MaaDesktopIntegration;

namespace MaaWpfGui.WineCompat
{
    internal class GObject
    {
        public nint Handle { get; }

        protected GObject(nint handle)
        {
            Handle = handle;
        }

        ~GObject()
        {
            g_object_unref(Handle);
        }
    }
}
