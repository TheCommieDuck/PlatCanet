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
        
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Run();
        }

        private const int TileSize = 10;

        private const int ScaleFactor = 4;

        public World World { get; set; }

        public Window Window { get; set; }

        public bool IsRunning { get; set; }

        private Panel mapPanel;

        private Panel infoPanel;
        public MapRender.RenderType CurrentDisplayMode { get; set; }

        public Game()
        {
            IsRunning = true;
            CurrentDisplayMode = MapRender.RenderType.BlendedBiomeTerrain;
        }

        public void Run()
        {
            Init();
            while (IsRunning)
            {
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
                        Color col = MapRender.GetColour(x, y, World, CurrentDisplayMode);
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
            Window.AddOnClick(mapPanel, new Rectangle(0, 0, 120, 60), (wx, wy) => 
                {
                    int x = 2*wx/Window.CellSize, y = 2*wy/Window.CellSize;
                    DisplayDetails(x, y);
                    //CurrentDisplayMode = MapRender.RenderType.Altitude;
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
            Window.Print(infoPanel, x, y + 16, "Biome: {0}", World.Classify(mapX, mapY).ToString());
            Window.Print(infoPanel, x, y + 20, "Altitude: {0:0.0}", World.Altitude[mapX, mapY]);
            
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
                    Window.Put(mapPanel, (int)((x * (TileSize / 2))), (int)((y * (TileSize / 2))), t);
                    t++;
                }
            }
            Window.DrawBorders(mapPanel);
            Window.Refresh();
        }
    }
}
