using SharpNoise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlatCanet
{
    class Heightmap : NoiseMap
    {
        public Heightmap(int width, int height)
            :base(width, height)
        {

        }

        public void Normalize(float min, float max)
        {
            float currMin = Data.Min();
            float a = max - min;
            float b = Data.Max() - currMin;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                    this.SetValue(x, y, min + (((GetValue(x, y)-currMin) * a)/b));
            }
        }

        public void ScaleByGradient(float radius)
        {
            double r2 = radius * radius;
            for(int x = 0; x < Width; x++)
            {
                for(int y = 0; y < Height; ++y)
                {
                    float nx = (2f * x / (float)Width) - 1;
                    float ny = (2f * y / (float)Height) - 1;
                    float dist = (nx * nx) + (ny * ny);

                    float gradVal = Math.Max(0.15f, (float)(dist > r2 ? (1f - (Math.Pow(radius, 2.5))) : (1f - (Math.Pow(Math.Sqrt(dist), 2.5)))));
                    this.SetValue(x, y, this.GetValue(x, y) * gradVal);
                }
            }
        }


        public void Foreach(Action<int, int> doThing)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; ++y)
                {
                    doThing(x, y);
                }
            }
        }

        internal void DistanceFromWater(Heightmap height)
        {
            //First, find the coast - any square where we are next to water.
            //Then, queue these up and find the nearest tile
            float[,] distances = new float[Width, Height];
            float maxDist = 0;

            Queue<Tuple<int, int>> currentPoints = new Queue<Tuple<int,int>>();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; ++y)
                {
                    if(IsSea(x, y, height))
                    {
                        distances[x, y] = 0;
                        continue;
                    }
                    distances[x, y] = -1;
                    for (int dx = -1; dx < 2; ++dx)
                    {
                        for (int dy = -1; dy < 2; ++dy)
                        {
                            if (IsSea(x + dx, y + dy, height))
                            {
                                currentPoints.Enqueue(new Tuple<int, int>(x, y));
                                distances[x, y] =  (dy == 0 || dx == 0) ? 1f : 1.33f;
                                maxDist = Math.Max(distances[x, y], maxDist);
                            }                                
                        }
                    } 
                }
            }

            while(currentPoints.Count > 0)
            {
                Tuple<int, int> point = currentPoints.Dequeue();
                int x = point.Item1, y = point.Item2;

                for (int dx = -1; dx < 2; ++dx)
                {
                    for(int dy = -1; dy < 2; ++dy)
                    {
                        float neighbour = distances[x, y] + ((dy == 0 || dx == 0) ? 1f : 1.33f);

                        if (distances[x + dx, y + dy] == -1 || distances[x + dx, y + dy] > neighbour)
                        {
                            distances[x + dx, y + dy] = neighbour;
                            currentPoints.Enqueue(new Tuple<int, int>(x + dx, y + dy));
                            maxDist = Math.Max(distances[x, y], maxDist);
                        }
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; ++y)
                {
                    this[x, y] += ((maxDist - distances[x, y])*2f/maxDist);
                }
            }
        }

        private bool IsSea(int x, int y, Heightmap map)
        {
            return map[x, y] < World.SeaLevel;
        }
    }
}
