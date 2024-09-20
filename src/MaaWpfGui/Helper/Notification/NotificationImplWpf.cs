// <copyright file="NotificationImplWpf.cs" company="MaaAssistantArknights">
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
using System.Windows.Media;
using Notification.Wpf;
using Notification.Wpf.Base;
using Notification.Wpf.Constants;
using Notification.Wpf.Controls;
using Stylet;

namespace MaaWpfGui.Helper.Notification;

internal class NotificationImplWpf : INotificationPoster
{
    private readonly NotificationManager _notificationManager = new();
    private readonly BrushConverter _brushConverter = new();

    public event EventHandler<string> ActionActivated;

    public NotificationImplWpf()
    {
        // 同时显示最大数量
        NotificationConstants.NotificationsOverlayWindowMaxCount = 5;

        // 默认显示位置
        NotificationConstants.MessagePosition = NotificationPosition.BottomRight;

        // 最小显示宽度
        NotificationConstants.MinWidth = 400d;

        // 最大显示宽度
        NotificationConstants.MaxWidth = 460d;
    }

    #region 通知基本字体样式和内容模板

    /// <summary>
    /// Gets basic text styles.
    /// </summary>
    /// <remarks>创建一个基本文本字体样式。</remarks>
    public TextContentSettings BaseTextSettings => new TextContentSettings
    {
        FontStyle = FontStyles.Normal,
        FontFamily = new FontFamily("Microsoft Yahei"),
        FontSize = 14,
        FontWeight = FontWeights.Normal,
        TextAlignment = TextAlignment.Left,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalTextAlignment = VerticalAlignment.Stretch,
        Opacity = 1d,
    };

    #endregion 通知基本字体样式和内容模板

    public void ShowNotification(NotificationContent content)
    {
        var wpfcontent = new global::Notification.Wpf.NotificationContent
        {
            Title = content.Summary,
            Message = content.Body,

            Type = NotificationType.None,
            Icon = AppIcon.GetIcon(),
            CloseOnClick = true,

            RowsCount = 1,
            TrimType = NotificationTextTrimType.AttachIfMoreRows,

            Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF1F3550"),
        };

        // 默认的标题文本样式
        var titleTextSettings = BaseTextSettings;
        titleTextSettings.FontSize = 18d;
        wpfcontent.TitleTextSettings = titleTextSettings;

        // 默认的内容文本样式
        var messageTextSettings = BaseTextSettings;
        wpfcontent.MessageTextSettings = messageTextSettings;

        var expirationTime = TimeSpan.FromSeconds(3);

        foreach (var hint in content.Hints)
        {
            if (hint == NotificationHint.Expandable)
            {
                wpfcontent.TrimType = NotificationTextTrimType.Attach;
            }
            else if (hint == NotificationHint.RecruitHighRarity)
            {
                wpfcontent.Background = (SolidColorBrush)_brushConverter.ConvertFrom("#FF401804");
                wpfcontent.Foreground = (SolidColorBrush)_brushConverter.ConvertFrom("#FFFFC800");
            }
            else if (hint == NotificationHint.RecruitRobot)
            {
                // 给通知染上小车相似的颜色
                wpfcontent.Background = (SolidColorBrush)_brushConverter.ConvertFrom("#FFFFFFF4");
                wpfcontent.Foreground = (SolidColorBrush)_brushConverter.ConvertFrom("#FF111111");
            }
            else if (hint == NotificationHint.NewVersion)
            {
                wpfcontent.Background = (SolidColorBrush)_brushConverter.ConvertFrom("#FF007280");
            }
            else if (hint is NotificationHint.NotificationHintRowCount rowCount)
            {
                wpfcontent.RowsCount = (uint)rowCount.Value;
            }
            else if (hint is NotificationHint.NotificationHintExpirationTime expirationTimeHint)
            {
                expirationTime = expirationTimeHint.Value;
            }
        }

        if (content.Actions.Count >= 1)
        {
            var action = content.Actions[0];
            wpfcontent.LeftButtonContent = action.Label;
            wpfcontent.LeftButtonAction = () => ActionActivated?.Invoke(this, action.Tag);
            if (expirationTime < TimeSpan.FromSeconds(10))
            {
                expirationTime = TimeSpan.FromSeconds(10);
            }
        }

        if (content.Actions.Count >= 2)
        {
            var action = content.Actions[1];
            wpfcontent.RightButtonContent = action.Label;
            wpfcontent.RightButtonAction = () => ActionActivated?.Invoke(this, action.Tag);
        }

        try
        {
            Execute.OnUIThread(() =>
            {
                _notificationManager.Show(wpfcontent, expirationTime: expirationTime, ShowXbtn: false);
            });
        }
        catch
        {
            // ignored
        }
    }
}
