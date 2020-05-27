// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Humanizer;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;

namespace osu.Game.Rulesets.Catch.Difficulty.Skills
{
    public class Movement : Skill
    {
        private const float absolute_player_positioning_error = 16f;
        private const float normalized_hitobject_radius = 41.0f;
        private const double direction_change_bonus = 21.0;

        protected override double SkillMultiplier => 900;
        protected override double StrainDecayBase => 0.2;

        protected override double DecayWeight => 0.94;

        protected readonly float HalfCatcherWidth;

        private float? lastPlayerPosition;
        private float lastDistanceMoved;
        private double lastStrainTime;
        private double lastStrainTimeVariation;
        //private double totalDistanceTraveled;

        public Movement(float halfCatcherWidth)
        {
            HalfCatcherWidth = halfCatcherWidth;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var catchCurrent = (CatchDifficultyHitObject)current;

            if (lastPlayerPosition == null)
                lastPlayerPosition = catchCurrent.LastNormalizedPosition;

            float playerPosition = Math.Clamp(
                lastPlayerPosition.Value,
                catchCurrent.NormalizedPosition - (normalized_hitobject_radius - absolute_player_positioning_error),
                catchCurrent.NormalizedPosition + (normalized_hitobject_radius - absolute_player_positioning_error)
            );

            float distanceMoved = playerPosition - lastPlayerPosition.Value;

            double weightedStrainTime = catchCurrent.StrainTime + 13 + (3 / catchCurrent.ClockRate);

            double distanceAddition = (Math.Pow(Math.Abs(distanceMoved), 1.3) / 510);
            double sqrtStrain = Math.Sqrt(weightedStrainTime);

            double edgeDashBonus = 0;

            // Direction change bonus.
            if (Math.Abs(distanceMoved) > 0.1)
            {
                if (Math.Abs(lastDistanceMoved) > 0.1 && Math.Sign(distanceMoved) != Math.Sign(lastDistanceMoved))
                {
                    double bonusFactor = Math.Min(50, Math.Abs(distanceMoved)) / 50;
                    double antiflowFactor = Math.Max(Math.Min(70, Math.Abs(lastDistanceMoved)) / 70, 0.38);

                    distanceAddition += direction_change_bonus / Math.Sqrt(lastStrainTime + 16) * bonusFactor * antiflowFactor * Math.Max(1 - Math.Pow(weightedStrainTime / 1000, 3), 0);
                }

                // Base bonus for every movement, giving some weight to streams.
                // I increased this bonus to give more weight to every object
                distanceAddition += 16 * Math.Min(Math.Abs(distanceMoved), normalized_hitobject_radius * 2) / (normalized_hitobject_radius * 6) / sqrtStrain;


                // New change
                // Bonus for BPM changes
                // This one is a little bit hacky but works well on many tech maps
                // Every value is a "placeholder" and is likely to be changed to improve the calculation
                double strainTimeDifference = Math.Abs(lastStrainTime - catchCurrent.StrainTime);
                
                double STVariationbonus = 0.95f; // Nerfes a little bit when there is no BPM change
                // Gives a bonus if there is a significant straintime change
                // The range is a placeholder
                if (strainTimeDifference > 10f)
                {
                    STVariationbonus = 1.25f; // The values are placeholders and definitely improvable
                    // A higher bonus bonus is given if the BPM change is not approximately the same as the previous
                    // The range is also a placeholder
                    if (strainTimeDifference < lastStrainTimeVariation - 10f || strainTimeDifference > lastStrainTimeVariation + 10f)
                    {
                        STVariationbonus += 0.30;
                        lastStrainTimeVariation = strainTimeDifference; // Keeps track of the last variation
                    }
                }
                
                distanceAddition *= STVariationbonus;
                
            }
            // Bonus for edge dashes.
            if (catchCurrent.LastObject.DistanceToHyperDash <= 20.0f / CatchPlayfield.BASE_WIDTH)
            {
                // I nerfed edge dash bonus because of the buff to non-hyperdashes
                if (!catchCurrent.LastObject.HyperDash)
                    edgeDashBonus += 2.3;
                else
                {
                    // After a hyperdash we ARE in the correct position. Always!
                    playerPosition = catchCurrent.NormalizedPosition;
                }

                distanceAddition *= 1.0 + edgeDashBonus * ((20 - catchCurrent.LastObject.DistanceToHyperDash * CatchPlayfield.BASE_WIDTH) / 20) * Math.Pow((Math.Min(catchCurrent.StrainTime * catchCurrent.ClockRate, 265) / 265), 1.5); // Edge Dashes are easier at lower ms values

            }

            // New change
            double distanceRatioBonus = 0;
            // Gives weight to non-hyperdashes
            if (!catchCurrent.LastObject.HyperDash)
            {
                // Speed is the ratio between "1/strain time" and the distance moved
                // So the larger and shorter a movement will be, the more it will be valued
                distanceRatioBonus  = ((2000/ weightedStrainTime) * Math.Abs(distanceMoved*3)) / 3300;
            }
            distanceAddition *= 0.85 + distanceRatioBonus; // This mostly nerfes HDashes
            

            lastPlayerPosition = playerPosition;
            lastDistanceMoved = distanceMoved;
            lastStrainTime = catchCurrent.StrainTime;
            

            return distanceAddition / weightedStrainTime;
        }
    }
}
