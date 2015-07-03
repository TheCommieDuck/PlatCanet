using BearLib;
using csDelaunay;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        private readonly static System.Drawing.Color[] terrainColours = ColorHelper.GenerateColorMap(new Color[]
                {
                    System.Drawing.Color.FromArgb(0, 0, 50), 
		            System.Drawing.Color.FromArgb(200, 190, 40),
		            System.Drawing.Color.FromArgb(114, 150, 71),
		            System.Drawing.Color.FromArgb(80, 120, 10),
		            System.Drawing.Color.FromArgb(17, 109, 7),
		            System.Drawing.Color.FromArgb(120, 220, 120),
		            System.Drawing.Color.FromArgb(208, 168, 139),
		            System.Drawing.Color.FromArgb(230, 210, 185)
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

        private static readonly System.Drawing.Color[] temperatureColours = ColorHelper.GenerateColorMap(new Color[]
            {
                Color.FromArgb(0, 0, 255),
                Color.FromArgb(0, 255, 0),
                Color.FromArgb(200, 50, 0),
                Color.FromArgb(255, 0, 0),
                
            }, new int[] {0, 80, 161, 181});

        public World World { get; set; }

        public Window Window { get; set; }

        private Color[,] renderMap;

        public bool IsRunning { get; set; }

        private Panel mapPanel;

        private Vector2f currPos;

        public DisplayType CurrentDisplayMode { get; set; }

        public Game()
        {
            IsRunning = true;
            this.CurrentDisplayMode = DisplayType.BiomeTerrain;
        }

        public void Run()
        {
            Stopwatch timer = Stopwatch.StartNew();
            Init();
            Console.WriteLine("Init time: {0}", timer.ElapsedMilliseconds);
            timer.Restart();
            int iter = 0;
            while (IsRunning)
            {
                Window.ClearArea(mapPanel);
                DrawMap();
                Window.Refresh();
                Thread.Sleep(1);
                iter++;
                if(iter >= 10)
                {
                    Console.WriteLine("Time for 10 iterations: {0}", timer.ElapsedMilliseconds);
                    iter = 0;
                    timer.Restart();
                }
                HandleInput();
            }
        }

        private void GenerateMapTextures()
        {
            Bitmap[] mapTiles = new Bitmap[World.Width / 40 * World.Height / 40];
            Parallel.For(0, mapTiles.Length, i =>
            {
                mapTiles[i] = new Bitmap(40, 40);
                for (int bx = 0; bx < 40; bx++)
                {
                    for (int by = 0; by < 40; by++)
                    {
                        int x = bx + ((i % 6) * 40), y = by + ((i / 6) * 40);
                        Biome biome = World.Classify(x, y);
                        Color col = Color.Empty;
                        biomeCount[biome] = biomeCount[biome] + 1;
                        int height = (int)World.Altitude[x, y];
                        if (biome == Biome.Lake || biome == Biome.Glacier)
                            col = ColorHelper.Lerp(Color.FromArgb(height, height, height), biomeColours[Biome.Lake].Item3, 0.6f);
                        else
                        {
                            col = biomeColours[biome].Item3;
                            int totR = 0, totG = 0, totB = 0, tot = 0;

                            for (int x2 = x - 1; x2 < x + 1; ++x2)
                            {
                                for (int y2 = y - 1; y2 < y + 1; ++y2)
                                {
                                    Biome b2 = World.Classify(x2, y2);
                                    if (!(x2 < 0 || x2 >= World.Width || y2 < 0 || y2 >= World.Height
                                        || biome == b2 || b2 == Biome.Ocean || b2 == Biome.Lake))
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
                            col = ColorHelper.Lerp(terrainColours[height], col, biome == Biome.Arctic ? 0.7f : 0.37f);
                        }
                        mapTiles[i].SetPixel(bx, by, col);
                    }
                }
            });

            /*for(int x = 0; x < World.Width; x++)
            {
                for(int y = 0; y < World.Height; y++)
                {
                    Biome biome = World.Classify(x, y);
                    Color col = Color.Empty;
                    biomeCount[biome] = biomeCount[biome] + 1;
                    int height = (int)World.Altitude[x, y];
                    if (biome == Biome.Lake || biome == Biome.Glacier)
                        col = ColorHelper.Lerp(Color.FromArgb(height, height, height), biomeColours[Biome.Lake].Item3, 0.6f);
                    else
                    {
                        col = biomeColours[biome].Item3; 
                        int totR = 0, totG = 0, totB = 0, tot = 0;

                        for (int x2 = x - 1; x2 < x + 1; ++x2)
                        {
                            for (int y2 = y - 1; y2 < y + 1; ++y2)
                            {
                                Biome b2 = World.Classify(x2, y2);
                                if (!(x2 < 0 || x2 >= World.Width || y2 < 0 || y2 >= World.Height
                                    || biome == b2 || b2 == Biome.Ocean || b2 == Biome.Lake))
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
                        col = ColorHelper.Lerp(terrainColours[height], col, biome == Biome.Arctic ? 0.7f : 0.37f);
                    }

                    int tileID = (int)(Math.Floor(x / 40f) + Math.Floor(y / 40f));
                    int indexX = x % 40, indexY = y % 40;
                    mapTiles[tileID].SetPixel(indexX, indexY, col);
                    /*for(int bx = 0; bx < 4; ++bx)
                    {
                        for(int by = 0; by < 4; ++by)
                        {
                            mapTiles[tileID].SetPixel(indexX + bx, indexY + by, col);
                        }
                    }*/
                    /*Color col = Color.Empty;
                    int val = 0;
                    Biome b;
                    switch (CurrentDisplayMode)
                    {
                        case DisplayType.Land:
                        case DisplayType.VoronoiRegions:
                            val = (int)World.Altitude[x, y];
                            col = val > World.SeaLevel ? Color.White : Color.Black;
                            break;
                        case DisplayType.Biomes:
                            b = World.Classify(x, y);
                            biomeCount[b] = biomeCount[b] + 1;
                            col = biomeColours[b].Item3;
                            break;
                        case DisplayType.Moisture:
                        case DisplayType.Height:
                        case DisplayType.Temperature:
                            Heightmap h = (CurrentDisplayMode == DisplayType.Moisture ? World.Moisture :
                                CurrentDisplayMode == DisplayType.Height ? World.Altitude : World.Temperature);
                            val = (int)(255 * (h[x, y]));
                            col = Color.FromArgb(val, val, val);
                            break;
                        case DisplayType.Terrain:
                            val = (int)World.Altitude[x, y];
                            col = terrainColours[val];
                            break;
                        case DisplayType.BiomeTerrain:
                            b = World.Classify(x, y);
                            biomeCount[b] = biomeCount[b] + 1;
                            if (b == Biome.Lake || b == Biome.Glacier)
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
                    //Window.SetColour(col);
                    renderMap[x, y] = col;
                    //SubcellRender(x, y);
                }
            }*/
            int t = 0xE000;

            foreach(Bitmap map in mapTiles)
            {
                if (!Terminal.Set("0x{0:X}: {1}, raw-size=40x40, resize=160x160, resize-filter=nearest", t, map))
                    System.Diagnostics.Debugger.Break();
                t++;
            }
            /*if (CurrentDisplayMode == DisplayType.VoronoiRegions)
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
                        Window.SetColour(col);
                        SubcellRender(x, y);
                    }
                }
            }*/


            /*Bitmap bi = new Bitmap(World.Width * Window.CellSize / 2, World.Height * Window.CellSize / 2);
            for (int x = 0; x < World.Width * Window.CellSize / 2; x++)
            {
                for (int y = 0; y < World.Height * Window.CellSize / 2; y++)
                {
                    bi.SetPixel(x, y, renderMap[(int)Math.Floor(x / 4f), (int)Math.Floor(y / 4f)]);
                }
            }
            Terminal.Set("0xE000: {0}, raw-size=960x480", bi);*/
        }

        private void HandleInput()
        {
            int read = Int32.MaxValue;
            while(true)
            {
                while (!Terminal.HasInput())
                {
                    Thread.Sleep(1);
                }
                read = Terminal.Read();
                if (read >= Terminal.TK_MOUSE_X1 && read <= Terminal.TK_MOUSE_CLICKS) //mouse
                    continue;
                break;
            }
            if (read == Terminal.TK_RIGHT)
                currPos.X += 1;
            else if(read == Terminal.TK_LEFT)
                currPos.X -= 1;
            else if (read == Terminal.TK_UP)
                currPos.Y -= 1;
            else if (read == Terminal.TK_DOWN)
                currPos.Y += 1;
            //Console.WriteLine(read);
        }
        private void ReadCommand()
        {
            string[] input = Console.ReadLine().Split(' ');
            switch(input[0])
            {
                case "d":
                    if (input.Length < 2)
                        return;
                    CurrentDisplayMode = (DisplayType)Enum.Parse(typeof(DisplayType), input[1]);
                    break;
                case "i":
                    int totalLand = biomeCount.Values.Aggregate(0, (land, moreLand) => land += moreLand);
                totalLand -= biomeCount[Biome.Ocean] + biomeCount[Biome.Lake] + biomeCount[Biome.Glacier];
                foreach(var pair in biomeCount.OrderBy<KeyValuePair<Biome, int>, int>((pair) => pair.Value))
                {
                    Console.WriteLine("{0} : {1}, {2}%", pair.Key.ToString(), pair.Value, pair.Value*100f/totalLand);
                }
                Console.WriteLine("Seed: {0}", World.Seed);
                break;
                default:
                    break;
            }
        }

        private void SubcellRender(int x, int y)
        {
            int half = (Window.CellSize / 2);
            int dx = (x % 2) * half;
            int dy = (y % 2) * half;
            Terminal.Layer((int)((dx / half) + ((half/2) * dy)));
            Window.PutExt(mapPanel, (int)Math.Floor(x / 2f), (int)Math.Floor(y / 2f), dx, dy, 9624);
        }

        public void Init()
        {
            Window = new Window(161, 101, "Faux is definitely not a shitbag");
            mapPanel = Window.CreatePanel(new Rectangle(40, 0, 120, 60));
            Panel otherPanel = Window.CreatePanel(new Rectangle(5, 5, 10, 10));
            Panel thirdPanel = Window.CreatePanel(new Rectangle(40, 60, 120, 40));
            Panel forthPanel = Window.CreatePanel(new Rectangle(0, 0, 40, 100));

            for (int x = 0; x < 40; ++x)
            {
                for(int y = 0; y < 10; ++y)
                {
                    Window.Put(otherPanel, x, y, x > y ? x.ToString()[0] : y.ToString()[0]);
                }
            }
            Window.DrawBorders(otherPanel);
            Window.DrawBorders(mapPanel);
            Window.DrawBorders(thirdPanel);
            Window.DrawBorders(forthPanel);
            Window.Refresh();
            int w = 240, h = 120;
            this.World = new World(w, h);
            this.renderMap = new Color[w, h];
            foreach (Biome b in biomeColours.Keys)
                biomeCount[b] = 0;
            GenerateMapTextures();
            currPos = Vector2f.zero;
        }

        private void DrawMap()
        {
            int t = 0xE000;
            Stopwatch w = Stopwatch.StartNew();
            for (int y = 0; y < 3; y++)
            {
                for(int x = 0; x < 6; ++x)
                {
                    Window.Put(mapPanel, (int)((x*20)+currPos.X), (int)((y*20)+currPos.Y), t);
                    t++;
                }
            }
                //Window.Put(mapPanel, 0, 0, 0xE000);
            /*for (int x = 0; x < (mapPanel.Width * 2); x++)
            {
                for (int y = 0; y < (mapPanel.Height * 2); y++)
                {
                    Window.SetColour(renderMap[(int)currPos.X + x, (int)currPos.Y + y]);
                    SubcellRender(x, y);
                }
            }*/
            Terminal.Refresh();
            Console.WriteLine("Second time: {0}", w.ElapsedMilliseconds);
            w.Stop();
        }
    }
}
