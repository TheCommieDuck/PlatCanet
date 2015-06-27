using BearLib;
using csDelaunay;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;


//useful characters
//█
namespace PlatCanet
{
    class Game
    {
        public enum DisplayType
        {
            Terrain,
            Height,
            Land,
            VoronoiRegions,
            Moisture,
            Temperature,
            Biomes
        }
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Run();

        }

        private readonly static Color[] terrainColours = ColorHelper.GenerateColorMap(new Color[]
                {
                    Color.FromArgb(0, 0, 50), 
		            Color.FromArgb(30, 30, 170),
		            Color.FromArgb(114, 150, 71),
		            Color.FromArgb(80, 120, 10),
		            Color.FromArgb(17, 109, 7),
		            Color.FromArgb(120, 220, 120),
		            Color.FromArgb(208, 208, 239),
		            Color.FromArgb(255, 255, 255)
                }, new int[] { 0, 60, 68, 100, 140, 210, 220, 256 });

        private static readonly Dictionary<Biome, Color> biomeColours = new Dictionary<Biome,Color>()
        {
            {Biome.AlpineTundra, Color.FromArgb(97, 131, 106)},
            {Biome.Arctic, Color.FromArgb(224, 224, 224)},
            {Biome.BorealForest, Color.FromArgb(23, 95, 73)},
            {Biome.Desert, Color.FromArgb(239, 217, 132)},
            {Biome.Glacier, Color.FromArgb(240, 240, 240)},
            {Biome.Grasslands, Color.FromArgb(164, 224, 98)},
            {Biome.Lake, Color.FromArgb(0, 64, 128)},
            {Biome.Mountain, Color.FromArgb(104, 124, 104)},
            {Biome.Ocean, Color.FromArgb(0, 36, 72)},
            {Biome.Rainforest, Color.FromArgb(88, 131, 88)},
            {Biome.Savanna, Color.FromArgb(219, 224, 154)},
            {Biome.Shrubland, Color.FromArgb(210, 196, 134)},
            {Biome.TemperateForest, Color.FromArgb(71, 194, 0)},
            {Biome.TropicalForest, Color.FromArgb(224, 224, 224)},
            {Biome.TropicalRainforest, Color.FromArgb(95, 124, 23)},
            {Biome.Tundra, Color.FromArgb(114, 155, 121)}
        };

        public World World { get; set; }

        public bool IsRunning { get; set; }

        public DisplayType CurrentDisplayMode { get; set; }

        public Game()
        {
            IsRunning = true;
            this.CurrentDisplayMode = DisplayType.Biomes;
        }
        public void Run()
        {
            Init();
            while (IsRunning)
            {
                for (int x = 0; x < World.Width; x++)
                {
                    for (int y = 0; y < World.Height; y++)
                    {
                        int val = (int)World.Altitude[x, y];
                        Color col = Color.Black;
                        switch(CurrentDisplayMode)
                        {
                            case DisplayType.Height:
                                col = Color.FromArgb((int)val, (int)val, (int)val);
                                break;
                            case DisplayType.Land:
                            case DisplayType.VoronoiRegions:
                                col = val > World.SeaLevel ? Color.White : Color.Black;
                                break;
                            case DisplayType.Biomes:
                                col = biomeColours[World.Classify(x, y)];
                                break;
                            case DisplayType.Terrain:
                                col = terrainColours[val];
                                break;
                        }
                        Terminal.Color(col);
                        SubcellRender(x, y);
                    }
                }

                if (CurrentDisplayMode == DisplayType.VoronoiRegions)
                {
                    Random r = new Random();
                    foreach (HashSet<RegionCell> region in World.Regions)
                    {
                        Color col = Color.FromArgb((int)(r.NextDouble() * 255), (int)(r.NextDouble() * 255), (int)(r.NextDouble() * 255));
                        foreach (Vector2f point in region.Select(cell => cell.Site.Coord))
                        {
                            int x = (int)point.X;
                            int y = (int)point.Y;
                            float val = (World.Altitude.GetValue(x, y));
                            Terminal.Color(col);
                            SubcellRender(x, y);
                        }
                    }
                }
                Terminal.Refresh();
                Console.ReadLine();
            }
        }

        private void SubcellRender(int x, int y)
        {
            int dx = (x % 2) * 4;
            int dy = (y % 2) * 4;
            Terminal.Layer((int)((dx / 4) + (0.5 * dy)));
            Terminal.PutExt((int)Math.Floor(x / 2f), (int)Math.Floor(y / 2f), dx, dy, 9624);
        }

        public void Init()
        {
            Terminal.Open();
            Terminal.Set("window: size=160x100, cellsize=8x8, title='PLAT CANET'; font=default");
            Terminal.BkColor(Color.ForestGreen);
            Terminal.Clear();
            Terminal.Refresh();
            this.World = new World(320, 200);
        }
    }
}
