// <copyright file="VersionUpdate.cs" company="MaaAssistantArknights">
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

using System.ComponentModel;

namespace MaaWpfGui.Configuration
{
    public class VersionUpdate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // The following should not be modified manually
        public string Name { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public bool IsFirstBoot { get; set; } = false;

        public string Package { get; set; } = string.Empty;

        public string Proxy { get; set; } = string.Empty;

        public UpdateVersionType VersionType { get; set; } = UpdateVersionType.Stable;

        public bool UpdateCheck { get; set; } = true;

        public bool UpdateAutoCheck { get; set; } = false;

        public bool AutoDownloadUpdatePackage { get; set; } = true;

        public bool AutoInstallUpdatePackage { get; set; } = false;

        public void OnPropertyChanged(string propertyName, object before, object after)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventDetailArgs(propertyName, before, after));
        }

        public enum UpdateVersionType
        {
            /// <summary>
            /// 稳定版
            /// </summary>
            Stable = 0,

            /// <summary>
            /// 测试版
            /// </summary>
            Nightly,

            /// <summary>
            /// 开发版
            /// </summary>
            Beta
        }
    }
}
