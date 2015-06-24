using BearLib;
using csDelaunay;
using SharpNoise;
using SharpNoise.Builders;
using SharpNoise.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatCanet
{
    class World
    {
        public int Height { get; private set; }
        public int Width { get; private set; }

        public Heightmap Altitude { get; private set; }
        public Heightmap Temperature { get; private set; }
        public Heightmap Moisture { get; private set; }

        public VoronoiMap VoronoiMap { get; private set; }

        public HashSet<HashSet<MapCell>> Regions { get; private set; }

        public World(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Altitude = new Heightmap(width, height);
            this.Temperature = new Heightmap(width, height);
            this.Moisture = new Heightmap(width, height);
            GenerateNoiseMap();
            GenerateVoronoiMap();
        }

        public void GenerateNoiseMap()
        {
            Perlin terrType = new Perlin();
            terrType.Persistence = 0.6;
            terrType.Frequency = 0.8;
            RidgedMulti ridge = new RidgedMulti();
            ScaleBias scale = new ScaleBias();
            scale.Source0 = ridge;
            scale.Scale = 0.5f;
            scale.Bias = 0.3f;
            Simplex sim = new Simplex();
            Add select = new Add();
            select.Source0 = terrType;
            select.Source1 = scale;

            PlaneNoiseMapBuilder builder = new PlaneNoiseMapBuilder();
            terrType.Seed = 5;
            builder.SourceModule = select;// terrType;//select;
            builder.DestNoiseMap = Altitude;
            builder.SetDestSize(Width, Height);
            builder.SetBounds(6, 10, 1, 5);
            builder.Build();
            Altitude.Normalize(0f, 1f);
            Altitude.ScaleByGradient(1f);
            Altitude.Normalize(0, 1);
        }

        public void GenerateVoronoiMap()
        {
            List<Vector2f> vertices = new List<Vector2f>();
            Random r = new Random();
            for (int i = 0; i < 10000; ++i)
                vertices.Add(new Vector2f(r.NextDouble() * Width, r.NextDouble() * Height));
            VoronoiMap = new VoronoiMap(vertices, Width, Height);
            CreateRegions();
        }

        private void CreateRegions()
        {
            List<HashSet<MapCell>> regions = new List<HashSet<MapCell>>();
            HashSet<Site> unprocessedSites = new HashSet<Site>(VoronoiMap.Voronoi.SitesIndexedByLocation.Values);
            Queue<MapCell> processQueue = new Queue<MapCell>();

            while (unprocessedSites.Count > 0)
            {
                Site first = unprocessedSites.First();
                processQueue.Enqueue(new MapCell(first, Classify(first)));
                unprocessedSites.Remove(first);

                HashSet<MapCell> currentRegion = new HashSet<MapCell>();
                regions.Add(currentRegion);

                while (processQueue.Count > 0)
                {
                    MapCell current = processQueue.Dequeue();
                    currentRegion.Add(current);
                    foreach (Site s in current.Site.NeighborSites().Where(t => Classify(t) == current.Classification && unprocessedSites.Contains(t)))
                    {
                        processQueue.Enqueue(new MapCell(s, Classify(s)));
                        unprocessedSites.Remove(s);
                    }
                }
            }
            Regions = new HashSet<HashSet<MapCell>>(regions);
        }

        public int Classify(Site s)
        {
            return Altitude[(int)s.Coord.X, (int)s.Coord.Y] > (60f / 255f) ? 1 : 0;
        }
    }
}
