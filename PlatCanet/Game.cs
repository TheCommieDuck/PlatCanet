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
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Run();

        }

        private static Color[] colourMap = ColorHelper.GenerateColorMap(new Color[]
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

        public World World { get; set; }

        public bool IsRunning { get; set; }

        public Game()
        {
            IsRunning = true;
        }
        public void Run()
        {
            Init();
            while(IsRunning)
            {
                /*foreach (Site s in World.VoronoiMap.Voronoi.SitesIndexedByLocation.Values)
                {
                    float tot = 0;
                    float cnt = 0;
                    foreach (Edge e in s.Edges)
                    {
                        if (e.ClippedEnds == null)
                            continue;
                        tot += World.Altitude.GetValue((int)e.ClippedEnds[0].X, (int)e.ClippedEnds[0].Y);
                        cnt++;
                    }
                    int x = (int)s.Coord.X;
                    int y = (int)s.Coord.Y;
                    float val = 255* (tot / cnt);//255 * (World.Altitude.GetValue(x, y));
                    Terminal.Color(colourMap[(int)val]);
                    int dx = (x % 2) * 4;
                    int dy = (y % 2) * 4;
                    Terminal.Layer((int)((dx / 4) + (0.5 * dy)));
                    Terminal.PutExt((int)Math.Floor(x / 2f), (int)Math.Floor(y / 2f), dx, dy, 9624);
                }*/

                for (int x = 0; x < World.Width; x++)
                {
                    for (int y = 0; y < World.Height; y++)
                    {
                        float val = 255 * (World.Altitude.GetValue(x, y));
                        Terminal.Color(val > 60f ? Color.Wheat : Color.Black);
                        int dx = (x % 2) * 4;
                        int dy = (y % 2) * 4;
                        Terminal.Layer((int)((dx / 4) + (0.5 * dy)));
                        Terminal.PutExt((int)Math.Floor(x / 2f), (int)Math.Floor(y / 2f), dx, dy, 9624);
                    }
                }
                Random r = new Random();
                foreach(HashSet<MapCell> region in World.Regions)
                {
                    Color col = Color.FromArgb((int)Math.Floor(r.NextDouble() * 255), (int)Math.Floor(r.NextDouble() * 255), (int)Math.Floor(r.NextDouble() * 255));
                    foreach(Vector2f point in region.Select(cell => cell.Site.Coord))
                    {
                        int x = (int)point.X;
                        int y = (int)point.Y;
                        float val = 255 * (World.Altitude.GetValue(x, y));
                        Terminal.Color(col);
                        int dx = (x % 2) * 4;
                        int dy = (y % 2) * 4;
                        Terminal.Layer((int)((dx / 4) + (0.5 * dy)));
                        Terminal.PutExt((int)Math.Floor(x / 2f), (int)Math.Floor(y / 2f), dx, dy, 9624);
                    }
                }

                Terminal.Refresh();
                Console.ReadLine();
            }
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
