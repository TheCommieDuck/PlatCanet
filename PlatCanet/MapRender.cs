using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatCanet
{
    static class MapRender
    {
        public enum RenderType
        {
            FancyAltitude = 0,
            Altitude,
            Land,
            Moisture,
            Temperature,
            Biomes,
            BlendedBiomeTerrain
        }

        private static readonly Dictionary<BiomeType, Color> biomeColours = new Dictionary<BiomeType, Color>()
        {
            {BiomeType.TropicalDryForest, Color.FromArgb(190, 190, 70)},
            {BiomeType.ColdDesert, Color.FromArgb(97, 131, 106)},
            {BiomeType.Arctic, Color.FromArgb(224, 224, 224)},
            {BiomeType.BorealForest, Color.FromArgb(23, 95, 73)},
            {BiomeType.Desert, Color.FromArgb(239, 217, 132)},
            //{BiomeType.Glacier, Color.FromArgb(137, 207, 246)},
            {BiomeType.Steppe, Color.FromArgb(164, 224, 98)},
            {BiomeType.Lake, Color.FromArgb(137, 200, 246)},
            {BiomeType.Mountain, Color.FromArgb(104, 124, 104)},
            {BiomeType.Ocean, Color.FromArgb(0x87, 0xCE, 0xFA)},
            {BiomeType.Rainforest, Color.FromArgb(0x32, 0xCD, 0x32)},
            {BiomeType.Savanna, Color.FromArgb(219, 224, 154)},
            {BiomeType.Heathland, Color.FromArgb(210, 196, 134)},
            {BiomeType.CoolTemperateForest, Color.FromArgb(50, 194, 40)},
            {BiomeType.WarmTemperateForest, Color.FromArgb(81, 190, 0)},
            {BiomeType.Tundra, Color.FromArgb(114, 155, 121)},
            {BiomeType.Beach, Color.FromArgb(220, 200, 120)}
        };

        private static readonly Color[] temperatureColours = new Color[]
        {
            Color.FromArgb(0, 0, 255),
            Color.FromArgb(42, 0, 212),
            Color.FromArgb(85, 0, 170),
            Color.FromArgb(127, 0, 127),
            Color.FromArgb(170, 0, 85),
            Color.FromArgb(212, 0, 42),
            Color.FromArgb(255, 0, 0)
        };

        private static readonly Color[] moistureColours = ColorHelper.GenerateColorMap(new Color[]
        {
            Color.FromArgb(255, 0, 0),
            Color.FromArgb(255, 255, 0),
            Color.FromArgb(0, 255, 255),
            Color.FromArgb(0, 0, 255)
        }, new int[] { 0, 3, 6, 8 });

        private readonly static System.Drawing.Color[] terrainColours = ColorHelper.GenerateColorMap(new Color[]
        {
            Color.FromArgb(0x2E, 0x58, 0x94), 
		    Color.FromArgb(0x69, 0xAF, 0xD0),
		    Color.FromArgb(114, 150, 71),
		    Color.FromArgb(80, 120, 10),
		    Color.FromArgb(17, 109, 7),
		    Color.FromArgb(120, 220, 120),
		    Color.FromArgb(208, 168, 139),
		    Color.FromArgb(230, 210, 185)
        }, new int[] { 0, World.SeaLevel, World.BeachEnd, 100, 140, 210, 220, 256 });


        private static readonly Dictionary<RenderType, Func<int, int, World, Color>> renders = new Dictionary<RenderType, Func<int, int, World, Color>>()
        {
            {RenderType.Temperature, TemperatureRender},
            {RenderType.Biomes, BiomeRender},
            {RenderType.Moisture, MoistureRender},
            {RenderType.FancyAltitude, FancyAltitudeRender},
            {RenderType.BlendedBiomeTerrain, BiomeBlendRender}
        };

        public static Color GetColour(int x, int y, World world, RenderType renderType)
        {
            return renders[renderType](x, y, world);
        }

        private static Color TemperatureRender(int x, int y, World world)
        {
            int quan = world.GetQuantile(world.TemperatureQuantiles, x, y, world.Temperature);

            return world.Altitude[x, y] < World.SeaLevel ? Color.Navy : temperatureColours[quan];
        }

        private static Color FancyAltitudeRender(int x, int y, World world)
        {
            return terrainColours[(int)world.Altitude[x, y]];
        }

        private static Color MoistureRender(int x, int y, World world)
        {
            int quan = world.GetQuantile(world.MoistureQuantiles, x, y, world.Moisture);
            return world.Altitude[x, y] < World.SeaLevel ? Color.Navy : moistureColours[quan];
        }

        private static Color BiomeRender(int x, int y, World world)
        {
            int height = (int)world.Altitude[x, y];
            Biome biome = world.Classify(x, y);
            return biomeColours[biome.MainBiome];
        }

        private static Color BiomeBlendRender(int x, int y, World world)
        {
            Color col = Color.Black;
            int height = (int)world.Altitude[x, y];
            BiomeType biome = world.Classify(x, y).MainBiome;
            if (biome == BiomeType.Ocean || biome == BiomeType.Beach)
                col = terrainColours[height];
            else
            {
                col = biomeColours[biome];
                int totR = 0, totG = 0, totB = 0, tot = 0;

                for (int x2 = x - 1; x2 < x + 1; ++x2)
                {
                    for (int y2 = y - 1; y2 < y + 1; ++y2)
                    {
                        BiomeType b2 = world.Classify(x2, y2).MainBiome;
                        if (!(x2 < 0 || x2 >= world.Width || y2 < 0 || y2 >= world.Height
                            || biome == b2 || b2 == BiomeType.Ocean || b2 == BiomeType.Beach))
                        {
                            Color surround = biomeColours[b2];
                            totR += surround.R;
                            totG += surround.G;
                            totB += surround.B;
                            tot++;
                        }
                    }
                }
                if (tot > 0)
                    col = ColorHelper.Lerp(col, Color.FromArgb(totR / tot, totG / tot, totB / tot), 0.3f);
                col = ColorHelper.Lerp(terrainColours[height], col, biome == BiomeType.Arctic ? 0.7f : 0.4f);
            }
            return col;
        }
    }
}
