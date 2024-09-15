// <copyright file="DataHelper.cs" company="MaaAssistantArknights">
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaaWpfGui.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MaaWpfGui.Helper
{
    public static class DataHelper
    {
        // 储存角色信息的字典
        public static Dictionary<string, CharacterInfo> Characters { get; } = new();

        public static HashSet<string> CharacterNames { get; } = [];

        static DataHelper()
        {
            const string FilePath = "resource/battle_data.json";
            if (!File.Exists(FilePath))
            {
                return;
            }

            string jsonText = File.ReadAllText(FilePath);
            var characterData = JsonConvert.DeserializeObject<Dictionary<string, CharacterInfo>>(JObject.Parse(jsonText)["chars"]?.ToString() ?? string.Empty) ?? new Dictionary<string, CharacterInfo>();

            var characterNamesLangAdd = GetCharacterNamesAddAction(ConfigurationHelper.GetValue(ConfigurationKeys.Localization, LocalizationHelper.DefaultLanguage));
            var characterNamesClientAdd = GetCharacterNamesAddAction(ConfigurationHelper.GetValue(ConfigurationKeys.ClientType, string.Empty));
            foreach (var (key, value) in characterData)
            {
                Characters.Add(key, value);
                if (!key.StartsWith("char_"))
                {
                    continue;
                }

                characterNamesLangAdd.Invoke(value);
                characterNamesClientAdd.Invoke(value);
            }
        }

        private static Action<CharacterInfo> GetCharacterNamesAddAction(string str)
        {
            return str switch
            {
                "zh-cn" or "Official" or "Bilibili" =>
                    v => CharacterNames.Add(v.Name ?? string.Empty),
                "zh-tw" or "txwy" =>
                    v => CharacterNames.Add(v.NameTw ?? string.Empty),
                "en-us" or "YoStarEN" =>
                    v => CharacterNames.Add(v.NameEn ?? string.Empty),
                "ja-jp" or "YoStarJP" =>
                    v => CharacterNames.Add(v.NameJp ?? string.Empty),
                "ko-kr" or "YoStarKR" =>
                    v => CharacterNames.Add(v.NameKr ?? string.Empty),
                _ =>
                    v => CharacterNames.Add(v.Name ?? string.Empty),
            };
        }

        public static CharacterInfo? GetCharacterByNameOrAlias(string characterName)
        {
            return Characters.Values.FirstOrDefault(
                character => new[]
                    {
                        character.Name,
                        character.NameEn,
                        character.NameJp,
                        character.NameKr,
                        character.NameTw,
                    }.Any(name => name?.Equals(characterName, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        public static string? GetLocalizedCharacterName(string? characterName, string? language = null)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                return null;
            }

            var characterInfo = GetCharacterByNameOrAlias(characterName);
            if (characterInfo?.Name == null)
            {
                return null;
            }

            language ??= Instances.SettingsViewModel.OperNameLocalization;

            return language switch
            {
                "zh-cn" => characterInfo.Name,
                "zh-tw" => characterInfo.NameTw ?? characterInfo.Name,
                "en-us" => characterInfo.NameEn ?? characterInfo.Name,
                "ja-jp" => characterInfo.NameJp ?? characterInfo.Name,
                "ko-kr" => characterInfo.NameKr ?? characterInfo.Name,
                _ => characterInfo.Name,
            };
        }

        public class CharacterInfo
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("name_en")]
            public string? NameEn { get; set; }

            [JsonProperty("name_jp")]
            public string? NameJp { get; set; }

            [JsonProperty("name_kr")]
            public string? NameKr { get; set; }

            [JsonProperty("name_tw")]
            public string? NameTw { get; set; }

            [JsonProperty("position")]
            public string? Position { get; set; }

            [JsonProperty("profession")]
            public string? Profession { get; set; }

            [JsonProperty("rangeId")]
            public List<string>? RangeId { get; set; }

            [JsonProperty("rarity")]
            public int Rarity { get; set; }
        }
    }
}
