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

        private static readonly Dictionary<Biome, Color> biomeColours = new Dictionary<Biome, Color>()
        {
            {Biome.AlpineTundra, Color.FromArgb(97, 131, 106)},
            {Biome.Arctic, Color.FromArgb(224, 224, 224)},
            {Biome.BorealForest, Color.FromArgb(23, 95, 73)},
            {Biome.Desert, Color.FromArgb(239, 217, 132)},
            {Biome.Glacier, Color.FromArgb(137, 207, 246)},
            {Biome.Grasslands, Color.FromArgb(164, 224, 98)},
            {Biome.Lake, Color.FromArgb(137, 200, 246)},
            {Biome.Mountain, Color.FromArgb(104, 124, 104)},
            {Biome.Ocean, Color.FromArgb(0, 36, 72)},
            {Biome.Rainforest, Color.FromArgb(0x32, 0xCD, 0x32)},
            {Biome.Savanna, Color.FromArgb(219, 224, 154)},
            {Biome.Shrubland, Color.FromArgb(210, 196, 134)},
            {Biome.TemperateForest, Color.FromArgb(71, 194, 0)},
            {Biome.TropicalForest, Color.FromArgb(23, 145, 73)},
            {Biome.TropicalRainforest, Color.FromArgb(95, 124, 23)},
            {Biome.Tundra, Color.FromArgb(114, 155, 121)},
            {Biome.Beach, Color.FromArgb(220, 200, 120)}
        };

        private static readonly Color[] temperatureColours = ColorHelper.GenerateColorMap(new Color[]
        {
            Color.FromArgb(0, 0, 255),
            Color.FromArgb(255, 0, 0)
        }, new int[] { 0, 8 });

        private static readonly Color[] moistureColours = ColorHelper.GenerateColorMap(new Color[]
        {
            Color.FromArgb(255, 0, 0),
            Color.FromArgb(255, 255, 0),
            Color.FromArgb(0, 255, 255),
            Color.FromArgb(0, 0, 255)
        }, new int[] { 0, 3, 6, 8 });

        private readonly static System.Drawing.Color[] terrainColours = ColorHelper.GenerateColorMap(new Color[]
        {
            Color.FromArgb(0, 0, 50), 
		    Color.FromArgb(200, 190, 40),
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
            {RenderType.Moisture, MoistureRender}
        };

        public static Color GetColour(int x, int y, World world, RenderType renderType)
        {
            return renders[renderType](x, y, world);
        }

        private static Color TemperatureRender(int x, int y, World world)
        {
            int quan = 0;
            for (; quan < 7; ++quan)
            {
                if (world.Temperature[x, y] < world.TemperatureQuantiles[quan])
                    break;
            }

            return world.Altitude[x, y] < World.SeaLevel ? Color.Aquamarine : temperatureColours[quan];
        }

        private static Color AltitudeRender(int x, int y, World world)
        {
            int quan = 0;
            for (; quan < 7; ++quan)
            {
                if (world.Temperature[x, y] < world.TemperatureQuantiles[quan])
                    break;
            }

            return world.Altitude[x, y] < World.SeaLevel ? Color.Aquamarine : temperatureColours[quan];
        }

        private static Color MoistureRender(int x, int y, World world)
        {
            int quan = 0;
            for (; quan < 7; ++quan)
            {
                if (world.Moisture[x, y] < world.MoistureQuantiles[quan])
                    break;
            }

            return world.Altitude[x, y] < World.SeaLevel ? Color.Navy : moistureColours[quan];
        }

        private static Color BiomeRender(int x, int y, World world)
        {
            int height = (int)world.Altitude[x, y];
            Biome biome = world.Classify(x, y);
            return biomeColours[biome];
        }

        private static Color BiomeBlendRender(int x, int y, World world)
        {
            Color col;
            int height = (int)world.Altitude[x, y];
            Biome biome = world.Classify(x, y);
            if (biome == Biome.Lake || biome == Biome.Glacier)
                col = ColorHelper.Lerp(Color.FromArgb(height, height, height), biomeColours[Biome.Lake], 0.6f);
            else
            {
                col = biomeColours[biome];
                int totR = 0, totG = 0, totB = 0, tot = 0;

                for (int x2 = x - 1; x2 < x + 1; ++x2)
                {
                    for (int y2 = y - 1; y2 < y + 1; ++y2)
                    {
                        Biome b2 = world.Classify(x2, y2);
                        if (!(x2 < 0 || x2 >= world.Width || y2 < 0 || y2 >= world.Height
                            || biome == b2 || b2 == Biome.Ocean || b2 == Biome.Lake))
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
                col = ColorHelper.Lerp(terrainColours[height], col, biome == Biome.Arctic ? 0.7f : 0.37f);
            }
            return col;
        }
    }
}
