// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using System.Collections.Generic;

namespace PerformanceCalculator.Difficulty.ExtendedSkills
{
    public class ExtendedFlashlight : Flashlight, IExtendedSkill {

        public List<ObjectWithStrain> objectsWithStrain;
        public ExtendedFlashlight(Mod[] mods): base(mods)
        {
            this.objectsWithStrain = new List<ObjectWithStrain>();
        }

        public ObjectWithStrain[] GetObjectsWithStrain()
        {
            return this.objectsWithStrain.ToArray();
        }

        public override string ToString(){
            return "flashlight";
        }

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            var strainValue = base.StrainValueAt(current);
            this.objectsWithStrain.Add(new ObjectWithStrain(current, strainValue));

            return strainValue;
        }
    }
}
