// <copyright file="InfrastSettingsUserControl.xaml.cs" company="MaaAssistantArknights">
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MaaWpfGui.Views.UserControl
{
    /// <summary>
    /// InfrastSettingsUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class InfrastSettingsUserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InfrastSettingsUserControl"/> class.
        /// </summary>
        public InfrastSettingsUserControl()
        {
            InitializeComponent();
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
            };
            var parent = ((Control)sender).Parent as UIElement;
            parent?.RaiseEvent(eventArg);
        }
    }
}
