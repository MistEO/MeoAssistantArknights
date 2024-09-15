// <copyright file="CheckBoxHelper.cs" company="MaaAssistantArknights">
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

namespace MaaWpfGui.Helper
{
    public static class CheckBoxHelper
    {
        public static void ToggleCheckBoxNullOnRightClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
            {
                return;
            }

            CheckBox checkBox = (CheckBox)sender;
            checkBox.IsChecked = checkBox.IsChecked == null ? false : null;
        }

        public static void ToggleCheckBoxNullOnLeftClick(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            checkBox.IsChecked = checkBox.IsChecked == true ? null : false;
        }
    }
}
