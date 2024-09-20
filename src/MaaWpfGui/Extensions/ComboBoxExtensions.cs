// <copyright file="ComboBoxExtensions.cs" company="MaaAssistantArknights">
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

using System.Windows.Controls;
using System.Windows.Input;
using MaaWpfGui.Helper;

namespace MaaWpfGui.Extensions
{
    /// <summary>
    /// <seealso cref="ComboBox"/> Extensions
    /// </summary>
    public static class ComboBoxExtensions
    {
        private const string InputTag = "TextInput";
        private const string SelectionTag = "Selection";

        /// <summary>
        /// Make <seealso cref="ComboBox"/> searchable
        /// </summary>
        /// <param name="targetComboBox">Target <seealso cref="ComboBox"/></param>
        public static void MakeComboBoxSearchable(this ComboBox targetComboBox)
        {
            if (targetComboBox?.Template.FindName("PART_EditableTextBox", targetComboBox) is not TextBox targetTextBox)
            {
                return;
            }

            targetComboBox.Items.IsLiveFiltering = true;
            targetComboBox.StaysOpenOnEdit = true;
            targetComboBox.IsEditable = true;
            targetComboBox.IsTextSearchEnabled = false;

            // enter input mode by default
            targetComboBox.Tag = InputTag;

            targetTextBox.PreviewKeyDown += (se, ev) =>
            {
                if (ev.Key is Key.Enter or Key.Return or Key.Tab)
                {
                    return;
                }

                // switch to input mode
                targetComboBox.Tag = InputTag;

                // reset selection
                if (targetComboBox.SelectedItem != null)
                {
                    targetComboBox.SelectedItem = null;
                    targetComboBox.Text = string.Empty;
                }

                targetComboBox.IsDropDownOpen = true;
            };

            targetTextBox.TextChanged += (o, args) =>
            {
                if (targetComboBox.Tag is SelectionTag)
                {
                    // text changed in selection mode, switch to input mode
                    targetComboBox.Tag = InputTag;
                }
                else
                {
                    // input mode
                    var searchTerm = targetTextBox.Text;

                    // reset selection
                    if (targetComboBox.SelectionBoxItem != null)
                    {
                        targetComboBox.SelectedItem = null;
                        targetTextBox.Text = searchTerm;
                        targetTextBox.SelectionStart = targetTextBox.Text.Length;
                    }

                    // filter items
                    if (string.IsNullOrEmpty(searchTerm) || searchTerm == LocalizationHelper.GetString("NotSelected"))
                    {
                        targetComboBox.Items.Filter = item => true;
                        targetComboBox.SelectedItem = default;
                    }
                    else
                    {
                        targetComboBox.Items.Filter = item => item.ToString().Contains(searchTerm);
                    }

                    targetTextBox.SelectionStart = targetTextBox.Text.Length;
                }
            };

            targetComboBox.SelectionChanged += (o, args) =>
            {
                // selection changed, switch to selection mode
                if (targetComboBox.SelectedItem != null)
                {
                    targetComboBox.Tag = SelectionTag;
                }
            };
        }
    }
}
