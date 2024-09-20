// <copyright file="NotificationHint.cs" company="MaaAssistantArknights">
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

namespace MaaWpfGui.Helper.Notification;

public class NotificationHint
{
    public static NotificationHint Expandable { get; } = new NotificationHint();

    public static NotificationHint RecruitHighRarity { get; } = new NotificationHint();

    public static NotificationHint RecruitRobot { get; } = new NotificationHint();

    public static NotificationHint NewVersion { get; } = new NotificationHint();

    internal class NotificationHintRowCount : NotificationHint
    {
        public int Value { get; }

        public NotificationHintRowCount(int rowCount)
        {
            Value = rowCount;
        }
    }

    internal class NotificationHintExpirationTime : NotificationHint
    {
        public TimeSpan Value { get; }

        public NotificationHintExpirationTime(TimeSpan value)
        {
            Value = value;
        }
    }

    public static NotificationHint RowCount(int rowCount) => new NotificationHintRowCount(rowCount);

    public static NotificationHint ExpirationTime(TimeSpan value) => new NotificationHintExpirationTime(value);
}
