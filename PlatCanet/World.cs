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

        public const int BeachEnd = 65;

        public const float GlacialTemp = 0.06f;

        public const int MountainLevel = 235;

        public const int BeachThreshold = 4;

        public int Seed { get; private set; }
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
            Random r = new Random();
            Seed = r.Next(300);
            GenerateAltitudeMap();
            GenerateVoronoiMap();
        }

        public void GenerateAltitudeMap()
        {
            PlaneNoiseMapBuilder builder = new PlaneNoiseMapBuilder();
            
            Perlin terrainNoiseBase = new Perlin();
            terrainNoiseBase.Persistence = 0.6;
            terrainNoiseBase.Frequency = 0.8;
            terrainNoiseBase.Seed = Seed;

            RidgedMulti terrainRidge = new RidgedMulti();

            ScaleBias terrainScale = new ScaleBias();
            terrainScale.Source0 = terrainRidge;
            terrainScale.Scale = 1.2f;
            terrainScale.Bias = 0.3f;

            Add terrainAdd = new Add();
            terrainAdd.Source0 = terrainNoiseBase;
            terrainAdd.Source1 = terrainScale;
            
            builder.SourceModule = terrainAdd;
            builder.DestNoiseMap = Altitude;
            builder.SetDestSize(Width, Height);
            builder.SetBounds(6, 10, 1, 5);
            builder.Build();

            Altitude.Normalize(0f, 1f);
            Altitude.ScaleByGradient(1f);
            Altitude.Normalize(0, 1);

            Perlin moistureNoiseBase = new Perlin();
            moistureNoiseBase.Persistence = 0.03;
            moistureNoiseBase.Frequency = 0.6;
            moistureNoiseBase.OctaveCount = 8;
            moistureNoiseBase.Seed = Seed;

            Exponent exp = new Exponent();
            exp.Exp = 1.3f;
            exp.Source0 = moistureNoiseBase;

            builder.SourceModule = exp;// moistureNoiseBase;
            builder.DestNoiseMap = Moisture;
            builder.SetDestSize(Width, Height);
            builder.SetBounds(6, 10, 1, 5);
            builder.Build();
            Moisture.Normalize(0, 1);
            Moisture.LerpByInverse(Altitude, 0.4f);
            Moisture.Normalize(0, 1);

            Perlin tempNoiseBase = new Perlin();
            tempNoiseBase.Persistence = 0.03;
            tempNoiseBase.Frequency = 0.6;
            tempNoiseBase.OctaveCount = 8;
            tempNoiseBase.Seed = Seed;
            exp.Source0 = tempNoiseBase;

            builder.SourceModule = exp;
            builder.DestNoiseMap = Temperature;
            builder.SetDestSize(Width, Height);
            builder.SetBounds(0, 10, 0, 5);
            builder.Build();
            Temperature.Normalize(0, 1);
            Temperature.LerpByInverse(Altitude, 0.35f);
            Temperature.ScaleByCentre();
            Temperature.Normalize(0, 1);

            Altitude.Normalize(0, 255);
        }

        public void GenerateVoronoiMap()
        {
            List<Vector2f> vertices = new List<Vector2f>();
            Random r = new Random(Seed);
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

            if (height < World.SeaLevel)
                return temp < World.GlacialTemp ? Biome.Glacier : Biome.Lake; //make it all lakes, then flood-fill the oceanic region after.

            if (height > World.SeaLevel && height < BeachEnd)
                return Biome.Beach;
            if (height > World.MountainLevel)
                return Biome.Mountain;

            if (temp < 0.2f)
                return Biome.Arctic;
            if (temp < 0.30f)
                return moisture > 0.45f ? Biome.AlpineTundra : Biome.Tundra;
            if(temp < 0.60f)
            {
                if (moisture < 0.25f)
                    return moisture > 5 ? Biome.Shrubland : Biome.Grasslands;
                if (temp < 0.4f)
                    return Biome.BorealForest;
                return moisture < 0.35f ? Biome.Savanna : moisture < 0.5f ? Biome.TemperateForest : Biome.Rainforest;
            }
            return moisture < 0.25f ? Biome.Desert : moisture < 0.5f ? Biome.TropicalForest : Biome.TropicalRainforest;
        }
    }
}
