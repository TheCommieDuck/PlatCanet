using BearLib;
using csDelaunay;
using SharpNoise;
using SharpNoise.Builders;
using SharpNoise.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PlatCanet
{
    class World
    {

        public const int SeaLevel = 60;

        public const int BeachEnd = 65;

        public const float GlacialTemp = 0.06f;

        public const int MountainLevel = 200;

        public int Seed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        public Heightmap Altitude { get; private set; }
        public Heightmap Temperature { get; private set; }
        public Heightmap Moisture { get; private set; }

        public float[] TemperatureQuantiles { get; private set; }

        public float[] MoistureQuantiles { get; private set; }

        public HashSet<HashSet<RegionCell>> Regions { get; private set; }

        public World(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Altitude = new Heightmap(width, height);
            this.Temperature = new Heightmap(width, height);
            this.Moisture = new Heightmap(width, height);
            Random r = new Random();
            Seed = r.Next(1000000);
            GenerateAltitudeMap();
            GetQuantiles();
            GenerateVoronoiMap();
        }

        private void GetQuantiles()
        {
            TemperatureQuantiles = new float[6];
            List<float> temps = new List<float>();
            for(int x = 0; x < Width; ++x)
            {
                for(int y = 0; y < Height; ++y)
                {
                    if(Altitude[x, y] >= SeaLevel)
                        temps.Add(Temperature[x, y]);
                }
            }
            float[] sortedTemps = temps.OrderBy(t => t).ToArray();
            int len = sortedTemps.Length;

            TemperatureQuantiles[0] = sortedTemps[(5 * sortedTemps.Length / 100) - 1];
            TemperatureQuantiles[1] = sortedTemps[(20 * sortedTemps.Length / 100) - 1];
            TemperatureQuantiles[2] = sortedTemps[(32 * sortedTemps.Length / 100) - 1];
            TemperatureQuantiles[3] = sortedTemps[(53 * sortedTemps.Length / 100) - 1];
            TemperatureQuantiles[4] = sortedTemps[(68 * sortedTemps.Length / 100) - 1];
            TemperatureQuantiles[5] = sortedTemps[(90 * sortedTemps.Length / 100) - 1];
            //TemperatureQuantiles[6] = sortedTemps[(97 * sortedTemps.Length / 100) - 1];

            MoistureQuantiles = new float[7];
            List<float> moists = new List<float>();
            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    if (Altitude[x, y] >= SeaLevel)
                        moists.Add(Moisture[x, y]);
                }
            }
            float[] sortedmoist = moists.OrderBy(t => t).ToArray();
            len = sortedmoist.Length;
            //1/8, 2/8, 3/8, 4/8, 5/8, 6/8, 7/8th quantiles
            MoistureQuantiles[0] = sortedmoist[(5 * sortedmoist.Length / 100) - 1];
            MoistureQuantiles[1] = sortedmoist[(12 * sortedmoist.Length / 100) - 1];
            MoistureQuantiles[2] = sortedmoist[(30 * sortedmoist.Length / 100) - 1];
            MoistureQuantiles[3] = sortedmoist[(50 * sortedmoist.Length / 100) - 1];
            MoistureQuantiles[4] = sortedmoist[(72 * sortedmoist.Length / 100) - 1];
            MoistureQuantiles[5] = sortedmoist[(89 * sortedmoist.Length / 100) - 1];
            MoistureQuantiles[6] = sortedmoist[(97 * sortedmoist.Length / 100) - 1];
        }

        public int GetQuantile(float[] quantiles, int x, int y, Heightmap map)
        {
            int quan = 0;
            for (; quan < quantiles.Length; ++quan)
            {
                if (map[x, y] < quantiles[quan])
                    break;
            }
            return quan;
        }

        public void GenerateAltitudeMap()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
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
            Altitude.Normalize(0, 255);

            Perlin moistureNoiseBase = new Perlin();
            moistureNoiseBase.Persistence = 0.2;
            moistureNoiseBase.Frequency = 1;
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
            Moisture.DistanceFromWater(Altitude);
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

            //add some height factor
            Temperature.Foreach((x, y) =>
                {
                    float height = Altitude[x, y];
                    float factor = 1f;
                    if (height > 160)
                    {
                        if (height > 210)
                            factor = 0.03f;
                        else
                            factor = 1f - ((height - 160) / 50);
                    }
                    Temperature[x, y] *= factor;
                });
            //and latitudinal stuff
            Temperature.Foreach((x, y) =>
                {
                    float grad = 1f - (Math.Abs(((float)y / Height) - 0.5f) * 2);
                    Temperature[x, y] = (grad * 5f + Temperature[x, y] * 4f) / 9f;
                });

            Temperature.Normalize(0, 1);
        }

        public void GenerateVoronoiMap()
        {
            List<Vector2f> vertices = new List<Vector2f>();
            Random r = new Random(Seed);
            for (int i = 0; i < 2000; ++i)
                vertices.Add(new Vector2f(r.NextDouble() * Width, r.NextDouble() * Height));
            CreateRegions(new csDelaunay.Voronoi(vertices, new Rectf(0, 0, Width, Height), 1));
        }

        private void CreateRegions(csDelaunay.Voronoi v)
        {
            /*List<HashSet<RegionCell>> regions = new List<HashSet<RegionCell>>();
            HashSet<Site> unprocessedSites = new HashSet<Site>(v.SitesIndexedByLocation.Values);
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
            }*/
            //Regions = new HashSet<HashSet<RegionCell>>(regions);
        }

        public Biome Classify(Vector2f s)
        {
            return Classify((int)s.X, (int)s.Y);
        }
        public Biome Classify(int x, int y)
        {
            if (Altitude[x, y] < World.SeaLevel)
                return new Biome(SubBiomeType.Ocean); //make it all lakes, then flood-fill the oceanic region after.

            if (Altitude[x, y] > World.SeaLevel && Altitude[x, y] < BeachEnd)
                return new Biome(SubBiomeType.Beach);
            if (Altitude[x, y] > World.MountainLevel)
                return new Biome(SubBiomeType.Mountain);

            return Biomes.Classify(GetQuantile(TemperatureQuantiles, x, y, Temperature), GetQuantile(MoistureQuantiles, x, y, Moisture));
        }
    }
}
