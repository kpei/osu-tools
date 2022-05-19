// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using Newtonsoft.Json;

namespace PerformanceCalculator.Difficulty.ExtendedSkills
{
    public class JsonDifficultyHitObject {
        [JsonProperty("deltaTime")]
        public double DeltaTime { get; set; }
        [JsonProperty("startTime")]
        public double StartTime { get; set; }
        [JsonProperty("endTime")]
        public double EndTime { get; set; }
    }
    public class ObjectWithStrain {

        [JsonProperty("hitObject")]
        public JsonDifficultyHitObject hitObject { get; set; }

        [JsonProperty("strain")]
        public double strain { get; set; }

        public ObjectWithStrain(DifficultyHitObject hitObject, double strain) {
            this.hitObject = new JsonDifficultyHitObject { DeltaTime = hitObject.DeltaTime, StartTime = hitObject.StartTime, EndTime = hitObject.EndTime};
            this.strain = strain;
        }

    }

}
