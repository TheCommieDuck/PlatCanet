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

                    float gradVal = Math.Max(0.2f, (float)(dist > r2 ? (1f - (Math.Pow(radius, 2.5))) : (1f - (Math.Pow(Math.Sqrt(dist), 2.5)))));
                    this.SetValue(x, y, this.GetValue(x, y) * gradVal);
                }
            }
        }

        public void ScaleByCentre()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; ++y)
                {
                    float ny = Math.Abs((2f * y / (float)Height) - 1);
                    float dist = (ny * ny);

                    float gradVal = dist * 1.1f;
                    this.SetValue(x, y, this.GetValue(x, y) * Math.Max(0.1f, (1-gradVal)));
                }
            }
        }

        public void LerpByInverse(Heightmap other, float coef)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; ++y)
                {
                    this.SetValue(x, y, this.GetValue(x, y) + ((1.01f - other[x, y] - this.GetValue(x, y)) * coef));
                }
                    
            }
        }
    }
}
