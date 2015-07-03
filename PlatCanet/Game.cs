using BearLib;
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
                    Color.FromArgb(0, 0, 50), 
		            Color.FromArgb(200, 190, 40),
		            Color.FromArgb(114, 150, 71),
		            Color.FromArgb(80, 120, 10),
		            Color.FromArgb(17, 109, 7),
		            Color.FromArgb(120, 220, 120),
		            Color.FromArgb(208, 168, 139),
		            Color.FromArgb(230, 210, 185)
                }, new int[] { 0, World.SeaLevel, World.BeachEnd, 100, 140, 210, 220, 256 });

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

        private const int TileSize = 10;

        public World World { get; set; }

        public Window Window { get; set; }

        public bool IsRunning { get; set; }

        private Panel mapPanel;

        private Panel infoPanel;

        private Vector2f currPos;

        public Game()
        {
            IsRunning = true;
        }

        public void Run()
        {
            Init();
            while (IsRunning)
            {
                //Window.ClearArea(mapPanel);
                Draw();
                HandleInput();
            }
            Terminal.Close();
        }

        private void GenerateMapTextures()
        {
            int tileWidth = World.Width / TileSize, tileHeight = World.Height / TileSize;
            Bitmap[] mapTiles = new Bitmap[tileWidth * tileHeight];
            Parallel.For(0, mapTiles.Length, i =>
            {
                mapTiles[i] = new Bitmap(TileSize, TileSize);
                for (int bx = 0; bx < TileSize; bx++)
                {
                    for (int by = 0; by < TileSize; by++)
                    {
                        int x = bx + ((i % tileWidth) * TileSize), y = by + ((int)Math.Floor((float)(i / tileWidth)) * TileSize);
                        Biome biome = World.Classify(x, y);
                        Color col = Color.Empty;
                        int height = (int)World.Altitude[x, y];
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
                                    Biome b2 = World.Classify(x2, y2);
                                    if (!(x2 < 0 || x2 >= World.Width || y2 < 0 || y2 >= World.Height
                                        || biome == b2 || b2 == Biome.Ocean || b2 == Biome.Lake))
                                    {
                                        Color surround = biomeColours[World.Classify(x2, y2)];
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

            int tileStart = 0xE000;

            foreach(Bitmap map in mapTiles)
            {
                Terminal.Set("0x{0:X}: {1}, raw-size={3}x{3}, resize={2}x{2}, resize-filter=nearest", tileStart, map, TileSize * Window.CellSize / 2, TileSize);
                tileStart++;
            }
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
                if (read == Terminal.TK_MOUSE_MOVE)
                    continue;
                break;
            }
            if (read >= Terminal.TK_MOUSE_LEFT && read <= Terminal.TK_MOUSE_SCROLL) //mouse
                Window.HandleMouse(read);
            if (read == Terminal.TK_CLOSE)
            {
                IsRunning = false;
                return;
            }
            
            if (read == Terminal.TK_RIGHT)
                currPos.X += 1;
            if(read == Terminal.TK_LEFT)
                currPos.X -= 1;
            if (read == Terminal.TK_UP)
                currPos.Y -= 1;
            if (read == Terminal.TK_DOWN)
                currPos.Y += 1;
        }

        public void Init()
        {
            Window = new Window(162, 102, "PLAT CANET v0.0.0.0.0.0.0.0.0.1");
            mapPanel = Window.CreatePanel(new Rectangle(40, 0, 121, 61));
            infoPanel = Window.CreatePanel(new Rectangle(40, 61, 121, 40));
            infoPanel.MinLayer = 5;
            infoPanel.MaxLayer = 5;
            Window.DrawBorders(mapPanel);
            Window.DrawBorders(infoPanel);
            Window.Refresh();
            int w = 240, h = 120;
            this.World = new World(w, h);
            GenerateMapTextures();
            currPos = Vector2f.zero;
            Terminal.Set("0xF000: rainforest.png");
            Terminal.Set("0xF001: alpinetundra.png");
            Window.AddOnClick(mapPanel, new Rectangle(0, 0, 120, 60), (wx, wy) => 
                {
                    int x = 2*wx/Window.CellSize, y = 2*wy/Window.CellSize;
                    DisplayDetails(x, y);
                    /*Console.WriteLine(@"Point: {0}, {1}. 
Altitude: {2}.
Moisture: {3}.
Biome: {4}.", x, y, World.Altitude[x, y], World.Moisture[x, y], World.Classify(x, y));*/
                });
        }

        private void DisplayDetails(int mapX, int mapY)
        {
            Window.ClearArea(infoPanel);
            int x = 3, y = 3;

            
            Window.Print(infoPanel, x, y+1, "Selected Area:");
            Window.Print(infoPanel, x, y + 4, "X: {0}, Y: {1}", mapX, mapY);
            Window.Print(infoPanel, x, y + 8, "Temperature: {0:0.0}°C", (World.Temperature[mapX, mapY] * 70) - 20);
            Window.Print(infoPanel, x, y + 12, "Rainfall: Approx {0:0.00}mm/year", (World.Moisture[mapX, mapY] * 3500));
            Window.Print(infoPanel, x, y + 16, "Biome: {0}", 
                String.Concat(World.Classify(mapX, mapY).ToString().Select(c => Char.IsUpper(c) ? " " + c : c.ToString())).TrimStart(' '));
            if (World.Classify(mapX, mapY) == Biome.AlpineTundra)
            {
                Window.Put(infoPanel, x, y + 20, 0xF001);
            }
            else if (World.Classify(mapX, mapY) == Biome.Rainforest)
            {
                Window.Put(infoPanel, x, y + 20, 0xF000);
            }

            Window.DrawBorders(infoPanel);
            Window.Refresh();
        }

        private void Draw()
        {
            int t = 0xE000;
            for (int y = 0; y < World.Height/TileSize; y++)
            {
                for(int x = 0; x < World.Width/TileSize; ++x)
                {
                    Window.Put(mapPanel, (int)((x * (TileSize / 2)) + currPos.X), (int)((y * (TileSize / 2)) + currPos.Y), t);
                    t++;
                }
            }
            Window.DrawBorders(mapPanel);
            Window.Refresh();
        }
    }
}
