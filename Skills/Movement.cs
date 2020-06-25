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

            // Reduced this
            double distanceAddition = (Math.Pow(Math.Abs(distanceMoved), 1.15) / 1100);
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
                distanceAddition += 9 * Math.Min(Math.Abs(distanceMoved), normalized_hitobject_radius * 2) / (normalized_hitobject_radius * 6) / sqrtStrain;



            }
            // Bonus for edge dashes.
            if (catchCurrent.LastObject.DistanceToHyperDash <= 20.0f / CatchPlayfield.BASE_WIDTH)
            {
                // Bonus increased
                if (!catchCurrent.LastObject.HyperDash)
                    edgeDashBonus += 13;
                else
                {
                    // After a hyperdash we ARE in the correct position. Always!
                    playerPosition = catchCurrent.NormalizedPosition;
                }

                distanceAddition *= 1.0 + edgeDashBonus * ((20 - catchCurrent.LastObject.DistanceToHyperDash * CatchPlayfield.BASE_WIDTH) / 20) * Math.Pow((Math.Min(catchCurrent.StrainTime * catchCurrent.ClockRate, 265) / 265), 1.5); // Edge Dashes are easier at lower ms values

            }

            double distanceRatioBonus;
            // Gives weight to non-hyperdashes
            if (!catchCurrent.LastObject.HyperDash)
            {
                // Speed is the ratio between "1/strain time" and the distance moved
                // So the larger and shorter a movement will be, the more it will be valued

                //Give value to long and fast movements
                distanceRatioBonus = Math.Abs(distanceMoved) / weightedStrainTime;

                // Give value to short movements if direction change
                if (distanceMoved > 0.1 && distanceMoved < 40 && Math.Sign(distanceMoved) != Math.Sign(lastDistanceMoved))
                {
                    distanceRatioBonus += Math.Log(100 / Math.Abs(distanceMoved), 1.2) * 1.2;
                }
            }
            else // Hyperdashes calculation
            {
                distanceRatioBonus = Math.Abs(distanceMoved) / (2.5 * weightedStrainTime);

            }
            distanceAddition *= 0.8 + distanceRatioBonus;


            lastPlayerPosition = playerPosition;
            lastDistanceMoved = distanceMoved;
            lastStrainTime = catchCurrent.StrainTime;


            return distanceAddition / weightedStrainTime;
        }
    }
}
