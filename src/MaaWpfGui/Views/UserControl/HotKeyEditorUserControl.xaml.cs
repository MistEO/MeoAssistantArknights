// <copyright file="HotKeyEditorUserControl.xaml.cs" company="MaaAssistantArknights">
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
#nullable enable

using System.Windows;
using System.Windows.Input;
using MaaWpfGui.Services.HotKeys;

namespace MaaWpfGui.Views.UserControl
{
    public partial class HotKeyEditorUserControl
    {
        public static readonly DependencyProperty HotKeyProperty =
            DependencyProperty.Register(nameof(HotKey), typeof(MaaHotKey),
                typeof(HotKeyEditorUserControl),
                new FrameworkPropertyMetadata(default(MaaHotKey),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public MaaHotKey? HotKey
        {
            get => (MaaHotKey)GetValue(HotKeyProperty);
            set => SetValue(HotKeyProperty, value);
        }

        public HotKeyEditorUserControl()
        {
            InitializeComponent();
        }

        private void HotKeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Don't let the event pass further
            // because we don't want standard textBox shortcuts working
            e.Handled = true;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Remove hotKey if no modifiers
            if (modifiers == ModifierKeys.None)
            {
                HotKey = null;
                return;
            }

            // If no actual key was pressed - return
            if (key is Key.LeftCtrl
                    or Key.RightCtrl
                    or Key.LeftAlt
                    or Key.RightAlt
                    or Key.LeftShift
                    or Key.RightShift
                    or Key.LWin
                    or Key.RWin
                    or Key.Clear
                    or Key.OemClear
                    or Key.Apps)
            {
                return;
            }

            // Update the value
            HotKey = new MaaHotKey(key, modifiers);
        }
    }
}
