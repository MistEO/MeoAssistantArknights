// <copyright file="ConnectSettingsUserControl.xaml.cs" company="MaaAssistantArknights">
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
using MaaWpfGui.Helper;

namespace MaaWpfGui.Views.UserControl
{
    /// <summary>
    /// ConnectSettingsUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectSettingsUserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectSettingsUserControl"/> class.
        /// </summary>
        public ConnectSettingsUserControl()
        {
            InitializeComponent();
        }

        private void StartsWithScript_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Instances.SettingsViewModel.StartsWithScript = files?[0] ?? string.Empty;
        }

        private void EndsWithScript_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Instances.SettingsViewModel.EndsWithScript = files?[0] ?? string.Empty;
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
