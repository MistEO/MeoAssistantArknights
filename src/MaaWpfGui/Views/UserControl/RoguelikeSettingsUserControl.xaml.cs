// <copyright file="RoguelikeSettingsUserControl.xaml.cs" company="MaaAssistantArknights">
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
#pragma warning disable SA1402

using System;
using System.Globalization;
using System.Windows.Controls;
using MaaWpfGui.Helper;

namespace MaaWpfGui.Views.UserControl
{
    /// <summary>
    /// RoguelikeSettingsUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class RoguelikeSettingsUserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoguelikeSettingsUserControl"/> class.
        /// </summary>
        public RoguelikeSettingsUserControl()
        {
            InitializeComponent();
            _current = this;
        }

        private static RoguelikeSettingsUserControl _current;
        private static bool _isValidResult;

        internal static bool IsValidResult
        {
            get => _isValidResult;
            set
            {
                _isValidResult = value;
                if (!IsValidResult)
                {
                    _current.StartingCoreCharComboBox.ItemsSource = DataHelper.CharacterNames;
                }
            }
        }

        private void StartingCoreCharComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (!IsValidResult)
            {
                return;
            }

            var name = StartingCoreCharComboBox.Text;
            StartingCoreCharComboBox.ItemsSource = Instances.SettingsViewModel.RoguelikeCoreCharList;
            StartingCoreCharComboBox.Text = name;
        }
    }

    public class StartingCoreCharRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is not string stringValue)
            {
                return new ValidationResult(false, HandyControl.Properties.Langs.Lang.FormatError);
            }

            if (!string.IsNullOrEmpty(stringValue) && DataHelper.GetCharacterByNameOrAlias(stringValue) is null)
            {
                RoguelikeSettingsUserControl.IsValidResult = false;
                return new ValidationResult(false, LocalizationHelper.GetString("RoguelikeStartingCoreCharNotFound"));
            }

            RoguelikeSettingsUserControl.IsValidResult = true;
            return ValidationResult.ValidResult;
        }
    }
}
