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

        private const int TileSize = 40;

        public World World { get; set; }

        public Window Window { get; set; }

        public bool IsRunning { get; set; }

        private Panel mapPanel;

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
                Window.ClearArea(mapPanel);
                DrawMap();
                HandleInput();
            }
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
                        int x = bx + ((i % tileWidth) * 40), y = by + ((int)Math.Floor((float)(i / tileWidth)) * 40);
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
                Terminal.Set("0x{0:X}: {1}, raw-size={3}x{3}, resize={2}x{2}, resize-filter=nearest", tileStart, map, TileSize * 4, TileSize);
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
                if (read >= Terminal.TK_MOUSE_X1 && read <= Terminal.TK_MOUSE_CLICKS) //mouse
                    continue;
                break;
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
            Window = new Window(162, 102, "Faux is definitely not a shitbag");
            mapPanel = Window.CreatePanel(new Rectangle(40, 0, 121, 61));
            Window.DrawBorders(mapPanel);
            Window.Refresh();
            int w = 240, h = 120;
            this.World = new World(w, h);
            GenerateMapTextures();
            currPos = Vector2f.zero;
        }

        private void DrawMap()
        {
            int t = 0xE000;
            for (int y = 0; y < 3; y++)
            {
                for(int x = 0; x < 6; ++x)
                {
                    Window.Put(mapPanel, (int)((x*20)+currPos.X), (int)((y*20)+currPos.Y), t);
                    t++;
                }
            }
            Terminal.Refresh();
        }
    }
}
