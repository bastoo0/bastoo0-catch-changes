## Star Rating

You can find the code for the Star Rating calculation in Skills -> Movement.cs

In a nutshell, my changes are the following :
- Separating hyperdashes and non-hyperdashes calculations
- Giving value to non-hyperdashes because they used to be greatly undervalued, this buffs everything but mostly technical and convert maps
- The base value of the SR is the distance, so I reduced that to nerf fullscreen jumps
- Nerfing hyper-dashes to reduce the importance of the distance bonus
- Giving a huge buff on short movements upon direction change
- Nerfing edge dashes bonus because it's less necessary now


## Performance Points

You can find the code for the Performance Points calculation in CatchPerformanceCalculator.cs

Performance Points are mostly based on the Star Rating but some other factors are added.

My changes are the following :

- The length bonus is now composed of 2 factors: the number of hitobjects (which was formerly the only factor) and the count of direction changes, it's a better way to evaluate the stamina. The length bonus has been greatly reduced on short maps.
- Low AR on Hidden has been greatly buffed below AR 9.8
- Low AR on NoMod has been slightly buffed below AR 9 (it will be mostly visible on really low AR and Easy plays)
- HalfTime Mod recieve a nerf on every AR because it makes the catcher slower and easier to control
- DoubleTime Mod recieve a buff on every AR because it makes the catcher faster and harder to control
- FlashLight Mod has been buffed over AR 8 because it's harder to read (I shoud probably nerf it under AR 6-7 because people use it to get a better reading but it won't follow the same logic as the score and it's easily replaceable with something to cover the upper part of the screen)
