// <copyright file="TimerSettingsUserControl.xaml.cs" company="MaaAssistantArknights">
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

using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MaaWpfGui.Views.UserControl
{
    /// <summary>
    /// TimerSettingsUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class TimerSettingsUserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimerSettingsUserControl"/> class.
        /// </summary>
        public TimerSettingsUserControl()
        {
            InitializeComponent();
        }

        [GeneratedRegex("^[0-9]{0,2}$")]
        private static partial Regex NumbersRegex();

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            string currentText = textBox.Text;

            // Combine the current text with the new input
            string newText = currentText.Insert(textBox.SelectionStart, e.Text);

            // Validate the combined text
            Regex regex = NumbersRegex();
            e.Handled = !regex.IsMatch(newText);
        }
    }
}
