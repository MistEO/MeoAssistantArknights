// <copyright file="TextDialogWithTimerUserControl.xaml.cs" company="MaaAssistantArknights">
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
using System.Windows.Controls.Primitives;
using HandyControl.Tools;

namespace MaaWpfGui.Views.UserControl
{
    /// <summary>
    /// TextDialogWithTimerUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class TextDialogWithTimerUserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextDialogWithTimerUserControl"/> class.
        /// </summary>
        /// <param name="content">主要内容</param>
        /// <param name="tipContent">提示内容</param>
        /// <param name="buttonContent">按钮内容</param>
        /// <param name="milliseconds">倒计时</param>
        public TextDialogWithTimerUserControl(string content, string tipContent, string buttonContent, double milliseconds)
        {
            InitializeComponent();

            var animation = AnimationHelper.CreateAnimation(100, milliseconds);
            animation.EasingFunction = null;
            animation.Completed += (s, e) => Completed?.Invoke(s, e);
            Button.Click += (s, e) => Click?.Invoke(s, e);
            Button.Content = buttonContent;
            Content.Text = content;
            Tip.Text = tipContent;
            ProgressBarTimer.BeginAnimation(RangeBase.ValueProperty, animation);
        }

        public event RoutedEventHandler Click;

        public event EventHandler Completed;
    }
}
