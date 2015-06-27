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
        

        public const int SeaLevel = 60;

        public const int GlacialTemp = -10;

        public const int MountainLevel = 180;

        public int Height { get; private set; }
        public int Width { get; private set; }

        public Heightmap Altitude { get; private set; }
        public Heightmap Temperature { get; private set; }
        public Heightmap Moisture { get; private set; }

        public VoronoiMap VoronoiMap { get; private set; }

        public HashSet<HashSet<RegionCell>> Regions { get; private set; }

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
            terrType.Seed = 5;
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
            
            builder.SourceModule = select;
            builder.DestNoiseMap = Altitude;
            builder.SetDestSize(Width, Height);
            builder.SetBounds(6, 10, 1, 5);
            builder.Build();
            Altitude.Normalize(0f, 1f);
            Altitude.ScaleByGradient(1f);
            Altitude.Normalize(0, 255);

            terrType.Seed++;
            builder.DestNoiseMap = Moisture;
            builder.SetDestSize(Width, Height);
            builder.SetBounds(6, 10, 1, 5);
            builder.Build();
            Moisture.Normalize(0, 40);

            terrType.Seed++;
            builder.DestNoiseMap = Temperature;
            builder.SetDestSize(Width, Height);
            builder.SetBounds(6, 10, 1, 5);
            builder.Build();
            Temperature.Normalize(-30, 50);
        }

        public void GenerateVoronoiMap()
        {
            List<Vector2f> vertices = new List<Vector2f>();
            Random r = new Random();
            for (int i = 0; i < 20000; ++i)
                vertices.Add(new Vector2f(r.NextDouble() * Width, r.NextDouble() * Height));
            VoronoiMap = new VoronoiMap(vertices, Width, Height);
            CreateRegions();
        }

        private void CreateRegions()
        {
            List<HashSet<RegionCell>> regions = new List<HashSet<RegionCell>>();
            HashSet<Site> unprocessedSites = new HashSet<Site>(VoronoiMap.Voronoi.SitesIndexedByLocation.Values);
            Queue<RegionCell> processQueue = new Queue<RegionCell>();

            while (unprocessedSites.Count > 0)
            {
                Site first = unprocessedSites.First();
                processQueue.Enqueue(new RegionCell(first, Classify(first.Coord)));
                unprocessedSites.Remove(first);
                HashSet<RegionCell> currentRegion = new HashSet<RegionCell>();
                regions.Add(currentRegion);

                while (processQueue.Count > 0)
                {
                    RegionCell current = processQueue.Dequeue();
                    currentRegion.Add(current);
                    foreach (Site s in current.Site.NeighborSites())
                    {
                        Biome b = Classify(s.Coord);
                        if (b != current.Classification || !unprocessedSites.Contains(s))
                            continue;
                        processQueue.Enqueue(new RegionCell(s, b));
                        unprocessedSites.Remove(s);
                    }
                }
            }
            Regions = new HashSet<HashSet<RegionCell>>(regions);
        }

        public Biome Classify(Vector2f s)
        {
            return Classify((int)s.X, (int)s.Y);
        }
        public Biome Classify(int x, int y)
        {
            float temp = Temperature[x, y], height = Altitude[x, y], moisture = Moisture[x, y];
            if(height < World.SeaLevel)
                return temp < World.GlacialTemp ? Biome.Glacier : Biome.Lake; //make it all lakes, then flood-fill the oceanic region after.

            if (height > World.MountainLevel)
                return Biome.Mountain;

            if (temp < -10)
                return Biome.Arctic;
            if (temp < 0)
                return moisture > 10 ? Biome.AlpineTundra : Biome.Tundra;
            if(temp < 20)
            {
                if (moisture < 10)
                    return moisture > 5 ? Biome.Shrubland : Biome.Grasslands;
                if (temp < 10)
                    return Biome.BorealForest;
                return moisture < 13 ? Biome.Savanna : moisture < 20 ? Biome.TemperateForest : Biome.Rainforest;
            }
            return moisture < 8 ? Biome.Desert : moisture < 20 ? Biome.TropicalForest : Biome.TropicalRainforest;
        }
    }
}
