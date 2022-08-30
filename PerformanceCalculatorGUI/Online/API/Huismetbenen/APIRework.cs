// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;

namespace PerformanceCalculatorGUI.Online.API.Huismetbenen
{
    [Serializable]
    public class APIRework
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rework_type_code")]
        public string ReworkTypeCode { get; set; }

        [JsonProperty("is_private")]
        public int IsPrivate { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("commit")]
        public string Commit { get; set; }

        [JsonProperty("gamemode")]
        public int Gamemode { get; set; }

        [JsonProperty("queue_enabled")]
        public int QueueEnabled { get; set; }

        [JsonProperty("algorithm_version")]
        public int AlgorithmVersion { get; set; }

        [JsonProperty("banner_text")]
        public string BannerText { get; set; }

        [JsonProperty("compare_with_master")]
        public int CompareWithMaster { get; set; }

        [JsonProperty("pp_skills")]
        public string PpSkills { get; set; }

        [JsonProperty("parsed_pp_skills")]
        public string[] ParsedPpSkills { get; set; }
    }
}
