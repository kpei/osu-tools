// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculatorGUI.Online.API.Huismetbenen
{
    [Serializable]
    public class APIScore
    {
        [JsonProperty("uid")]
        public long Uid { get; set; }

        [JsonProperty("score_id")]
        public long ScoreId { get; set; }

        [JsonProperty("user_id")]
        public int UserId { get; set; }

        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }

        [JsonProperty("live_pp")]
        public double LivePp { get; set; }

        [JsonProperty("local_pp")]
        public double LocalPp { get; set; }

        [JsonProperty("pp_diff")]
        public double PpDiff { get; set; }

        [JsonProperty("pp_diff_relative")]
        public double PpDiffRelative { get; set; }

        [JsonProperty("aim_pp")]
        public double AimPp { get; set; }

        [JsonProperty("tap_pp")]
        public double TapPp { get; set; }

        [JsonProperty("acc_pp")]
        public double AccPp { get; set; }

        [JsonProperty("fl_pp")]
        public double FlPp { get; set; }

        [JsonConverter(typeof(APIModsConverter))]
        [JsonProperty("mods")]
        public APIMod[] Mods { get; set; }

        [JsonProperty("great")]
        public int Great { get; set; }

        [JsonProperty("good")]
        public int Good { get; set; }

        [JsonProperty("meh")]
        public int Meh { get; set; }

        [JsonProperty("miss")]
        public int Miss { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }

        [JsonProperty("score_date")]
        public DateTimeOffset ScoreDate { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("score_rank")]
        public ScoreRank ScoreRank { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("diff_name")]
        public string DiffName { get; set; }

        [JsonProperty("creator_name")]
        public string CreatorName { get; set; }

        [JsonProperty("old_rank")]
        public int? OldRank { get; set; }

        [JsonProperty("new_rank")]
        public int? NewRank { get; set; }

        public SoloScoreInfo ToSoloScoreInfo(int rulesetId)  {
            Dictionary<HitResult, int> hitStatistics = new Dictionary<HitResult, int>();
            hitStatistics.Add(HitResult.Great, Great);
            hitStatistics.Add(HitResult.Ok, Good);
            hitStatistics.Add(HitResult.Meh, Meh);
            hitStatistics.Add(HitResult.Miss, Miss);

            APIBeatmap beatmap = new APIBeatmap() {
                OnlineID = BeatmapId,
                BeatmapSet = new APIBeatmapSet() {
                    Title = Title,
                    TitleUnicode = Title,
                    Artist = Artist,
                    ArtistUnicode = Artist,
                }
            };

            return new SoloScoreInfo() {
                Beatmap = beatmap,
                BeatmapID = BeatmapId,
                RulesetID = rulesetId,
                Accuracy = Accuracy / 100d,
                UserID = UserId,
                MaxCombo = MaxCombo,
                Rank = ScoreRank, 
                Mods = Mods,
                PP = LocalPp,
                Statistics = hitStatistics,
                EndedAt = ScoreDate,
            };
        }

    }

    public class APIModsConverter : JsonConverter<APIMod[]>
    {
        public override APIMod[] ReadJson(JsonReader reader, Type objectType, APIMod[] existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.Null)
                return Array.Empty<APIMod>();
        
            string modsList = (string)reader.Value;
            
            if (modsList == "None")
                return Array.Empty<APIMod>();

            return modsList.Split(", ").Select(mod => new APIMod(){ Acronym = mod }).ToArray();
        }

        public override void WriteJson(JsonWriter writer, APIMod[] value, JsonSerializer serializer) {}

    }
}
