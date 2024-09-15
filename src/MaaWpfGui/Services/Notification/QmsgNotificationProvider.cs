// <copyright file="QmsgNotificationProvider.cs" company="MaaAssistantArknights">
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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MaaWpfGui.Constants;
using MaaWpfGui.Helper;
using MaaWpfGui.Services.Web;
using Newtonsoft.Json;
using Serilog;

namespace MaaWpfGui.Services.Notification
{
    public class QmsgNotificationProvider : IExternalNotificationProvider
    {
        private readonly IHttpService _httpService;

        private readonly ILogger _logger = Log.ForContext<QmsgNotificationProvider>();

        public QmsgNotificationProvider(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<bool> SendAsync(string title, string content)
        {
            var server = ConfigurationHelper.GetValue(ConfigurationKeys.ExternalNotificationQmsgServer, string.Empty);
            var key = ConfigurationHelper.GetValue(ConfigurationKeys.ExternalNotificationQmsgKey, string.Empty);
            var receiveUser = ConfigurationHelper.GetValue(ConfigurationKeys.ExternalNotificationQmsgUser, string.Empty);
            var sendBot = ConfigurationHelper.GetValue(ConfigurationKeys.ExternalNotificationQmsgBot, string.Empty);

            var uri = $"{server}/jsend/{key}";

            var response = await _httpService.PostAsJsonAsync(
                new Uri(uri),
                new QmsgContent { Msg = content, Qq = receiveUser, Bot = sendBot, });

            if (String.Empty.Equals(response))
            {
                _logger.Warning("Failed to send Qmsg notification");
                return false;
            }

            var responseRoot = JsonDocument.Parse(response).RootElement;
            var hasCodeProperty = responseRoot.TryGetProperty("success", out var codeElement);
            var reasonProperty = responseRoot.TryGetProperty("reason", out var reasonElement);
            if (hasCodeProperty is false)
            {
                _logger.Warning("Failed to send Qmsg notification, unknown response, {Response}", response);
                return false;
            }

            var success = codeElement.GetBoolean();
            if (success is false)
            {
                _logger.Warning("Failed to send Qmsg notification, unknown response {Response}", response);
                return false;
            }

            if (success)
            {
                return true;
            }

            var reason = reasonElement.GetRawText();
            _logger.Warning("Failed to send Qmsg notification, reason {Reason}", reason);
            return false;
        }

        private class QmsgContent
        {
            // 消息内容
            [JsonPropertyName("msg")]

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Msg { get; set; }

            [JsonPropertyName("qq")]

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Qq { get; set; }

            [JsonPropertyName("bot")]

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Bot { get; set; }

            /// <summary>
            /// 转换为Dictionary
            /// </summary>
            public Dictionary<string, string> ToDictionary()
            {
                Dictionary<string, string> map = new Dictionary<string, string>();
                if (this == null)
                {
                    return map;
                }

                var objstr = JsonConvert.SerializeObject(this);

                map = JsonConvert.DeserializeObject<Dictionary<string, string>>(objstr);
                return map;
            }
        }
    }
}
