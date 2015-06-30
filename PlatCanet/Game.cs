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
            Terrain = 0,
            Height,
            Land,
            VoronoiRegions,
            Moisture,
            Temperature,
            Biomes,
            BiomeTerrain
        }
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Run();
        }

        private readonly static Color[] terrainColours = ColorHelper.GenerateColorMap(new Color[]
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

        private static readonly Dictionary<Biome, Tuple<float, float, Color>> biomeColours = new Dictionary<Biome, Tuple<float, float, Color>>()
        {
            {Biome.AlpineTundra, new Tuple<float, float, Color>(0.25f, 0.5f, Color.FromArgb(97, 131, 106))},
            {Biome.Arctic, new Tuple<float, float, Color>(0.1f, 0.5f, Color.FromArgb(224, 224, 224))},
            {Biome.BorealForest, new Tuple<float, float, Color>(0.4f, 0.5f, Color.FromArgb(23, 95, 73))},
            {Biome.Desert, new Tuple<float, float, Color>(0.8f, 0.15f, Color.FromArgb(239, 217, 132))},
            {Biome.Glacier, new Tuple<float, float, Color>(0.0f, 0.0f, Color.FromArgb(137, 207, 246))},
            {Biome.Grasslands, new Tuple<float, float, Color>(0.55f, 0.1f, Color.FromArgb(164, 224, 98))},
            {Biome.Lake, new Tuple<float, float, Color>(0.0f, 0.0f, Color.FromArgb(137, 200, 246))},
            {Biome.Mountain, new Tuple<float, float, Color>(0.0f, 0.0f, Color.FromArgb(104, 124, 104))},
            {Biome.Ocean, new Tuple<float, float, Color>(0.0f, 0.0f, Color.FromArgb(0, 36, 72))},
            {Biome.Rainforest, new Tuple<float, float, Color>(0.65f, 0.75f, Color.FromArgb(0x32, 0xCD, 0x32))},
            {Biome.Savanna, new Tuple<float, float, Color>(0.6f, 0.4f, Color.FromArgb(219, 224, 154))},
            {Biome.Shrubland, new Tuple<float, float, Color>(0.55f, 0.2f, Color.FromArgb(210, 196, 134))},
            {Biome.TemperateForest, new Tuple<float, float, Color>(0.55f, 0.4f,Color.FromArgb(71, 194, 0))},
            {Biome.TropicalForest, new Tuple<float, float, Color>(0.75f, 0.5f, Color.FromArgb(23, 145, 73))},
            {Biome.TropicalRainforest, new Tuple<float, float, Color>(0.75f, 0.75f, Color.FromArgb(95, 124, 23))},
            {Biome.Tundra, new Tuple<float, float, Color>(0.25f, 0.15f, Color.FromArgb(114, 155, 121))},
            {Biome.Beach, new Tuple<float, float, Color>(0.25f, 0.15f, Color.FromArgb(220, 200, 120))}
        };

        private Dictionary<Biome, int> biomeCount = new Dictionary<Biome, int>();

        private static readonly Color[] temperatureColours = ColorHelper.GenerateColorMap(new Color[]
            {
                Color.FromArgb(0, 0, 255),
                Color.FromArgb(0, 255, 0),
                Color.FromArgb(200, 50, 0),
                Color.FromArgb(255, 0, 0),
                
            }, new int[] {0, 80, 161, 181});

        public World World { get; set; }

        public bool IsRunning { get; set; }

        public DisplayType CurrentDisplayMode { get; set; }

        public Game()
        {
            IsRunning = true;
            this.CurrentDisplayMode = DisplayType.BiomeTerrain;
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
                        Color col = Color.Black;
                        int val = 0;
                        Biome b;
                        switch(CurrentDisplayMode)
                        {
                            case DisplayType.Height:
                                val = (int)World.Altitude[x, y];
                                col = Color.FromArgb(val, val, val);
                                break;
                            case DisplayType.Land:
                            case DisplayType.VoronoiRegions:
                                val = (int)World.Altitude[x, y];
                                col = val > World.SeaLevel ? Color.White : Color.Black;
                                break;
                            case DisplayType.Biomes:
                                b = World.Classify(x, y);
                                biomeCount[b] = biomeCount[b]+1;
                                col = biomeColours[b].Item3;//GetBiomeColour(x, y);
                                break;
                            case DisplayType.Moisture:
                                val = (int)(255*(World.Moisture[x, y]));
                                col = Color.FromArgb(val, val, val);
                                break;
                            case DisplayType.Terrain:
                                val = (int)World.Altitude[x, y];
                                col = terrainColours[val];
                                break;
                            case DisplayType.Temperature:
                                val = (int)(255*(World.Temperature[x, y]));
                                col = Color.FromArgb(val, val, val);
                                break;
                            case DisplayType.BiomeTerrain:
                                b = World.Classify(x, y);
                                biomeCount[b] = biomeCount[b]+1;
                                if (b == Biome.Lake || b== Biome.Glacier)
                                {
                                    col =
                                        ColorHelper.Lerp(Color.FromArgb((int)World.Altitude[x, y], (int)World.Altitude[x, y], (int)World.Altitude[x, y]),
                                        biomeColours[Biome.Lake].Item3, 0.6f);
                                }
                                else
                                {
                                    col = biomeColours[b].Item3; //ColorHelper.Lerp(terrainColours[(int)World.Altitude[x, y]], , 0.6f);
                                    int totR = 0, totG = 0, totB = 0, tot = 0;

                                    for (int x2 = x - 1; x2 < x + 1; ++x2)
                                    {
                                        for (int y2 = y - 1; y2 < y + 1; ++y2)
                                        {
                                            Biome b2 = World.Classify(x2, y2);
                                            if (!(x2 < 0 || x2 >= World.Width || y2 < 0 || y2 >= World.Height 
                                                || b == b2 || b2 == Biome.Ocean || b2 == Biome.Lake))
                                            {
                                                Color surround = biomeColours[World.Classify(x2, y2)].Item3;
                                                totR += surround.R;
                                                totG += surround.G;
                                                totB += surround.B;
                                                tot++;
                                            }
                                        }
                                    }
                                    if (tot > 0)
                                        col = ColorHelper.Lerp(col, Color.FromArgb(totR / tot, totG / tot, totB / tot), 0.3f);
                                    col = ColorHelper.Lerp(terrainColours[(int)World.Altitude[x, y]], col, b == Biome.Arctic ? 0.7f : 0.37f);
                                }
                                    
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
                int totalLand = biomeCount.Values.Aggregate(0, (land, moreLand) => land += moreLand);
                totalLand -= biomeCount[Biome.Ocean] + biomeCount[Biome.Lake] + biomeCount[Biome.Glacier];
                foreach(var pair in biomeCount.OrderBy<KeyValuePair<Biome, int>, int>((pair) => pair.Value))
                {
                    Console.WriteLine("{0} : {1}, {2}%", pair.Key.ToString(), pair.Value, pair.Value*100f/totalLand);
                }
                Console.WriteLine("Seed: {0}", World.Seed);
                ReadCommand();
            }
        }

        private void ReadCommand()
        {
            string[] input = Console.ReadLine().Split(' ');
            if (input.Length < 2)
                return;
            switch(input[0])
            {
                case "d":
                    CurrentDisplayMode = (DisplayType)Enum.Parse(typeof(DisplayType), input[1]);
                    break;
                default:
                    break;
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
            Terminal.Set("window: size=160x100, cellsize=8x8, title='MICK IS A LETTUCE-LOVING SHITBAG'; font=default");
            Terminal.BkColor(Color.Black);
            Terminal.Clear();
            Terminal.Refresh();
            this.World = new World(320, 200);
            foreach (Biome b in biomeColours.Keys)
                biomeCount[b] = 0;
        }
    }
}
