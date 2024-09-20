// <copyright file="Root.cs" company="MaaAssistantArknights">
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
using System.Text.Json.Serialization;
using ObservableCollections;

namespace MaaWpfGui.Configuration
{
    public class Root : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [JsonInclude]
        public ObservableDictionary<string, SpecificConfig> Configurations { get; private set; } = new ObservableDictionary<string, SpecificConfig>();

        [JsonInclude]
        public ObservableDictionary<int, Timer> Timers { get; private set; } = new ObservableDictionary<int, Timer>();

        public string Current { get; set; } = "Default";

        [JsonInclude]
        public VersionUpdate VersionUpdate { get; private set; } = new VersionUpdate();

        [JsonInclude]
        public AnnouncementInfo AnnouncementInfo { get; private set; } = new AnnouncementInfo();

        [JsonIgnore]
        public SpecificConfig CurrentConfig
        {
            get
            {
                Configurations.TryGetValue(Current, out var result);
                return result;
            }

            set => Configurations[Current] = value;
        }

        // ReSharper disable once UnusedMember.Global
        public void OnPropertyChanged(string propertyName, object before, object after)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventDetailArgs(propertyName, before, after));
        }
    }
}
