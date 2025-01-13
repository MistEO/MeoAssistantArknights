// <copyright file="ConnectSettingsUserControlModel.cs" company="MaaAssistantArknights">
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
using MaaWpfGui.Constants;
using MaaWpfGui.Helper;
using Stylet;

namespace MaaWpfGui.ViewModels.UserControl.TaskQueue;

public class StartUpTaskUserControlModel : PropertyChangedBase
{
    private string _accountName = ConfigurationHelper.GetValue(ConfigurationKeys.AccountName, string.Empty);

    public string AccountName
    {
        get => _accountName;
        set
        {
            SetAndNotify(ref _accountName, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.AccountName, value);
        }
    }

    // UI 绑定的方法
    // ReSharper disable once UnusedMember.Global
    public void AccountSwitchManualRun()
    {
        Instances.TaskQueueViewModel.QuickSwitchAccount();
    }
}
