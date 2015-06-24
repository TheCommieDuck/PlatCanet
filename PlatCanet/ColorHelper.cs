using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatCanet
{
    class ColorHelper
    {
        public static Color[] GenerateColorMap(Color[] points, int[] indices)
        {
            Color[] array = new Color[indices.Last()+1];
            for(int i = 0; i < points.Length-1; ++i)
            {
                int start = indices[i];
                int end = indices[i + 1];
                for(int x = start; x < end; x++)
                    array[x] = ColorHelper.Lerp(points[i], points[i+1], (float)(x - start)/(end - start));
            }
            return array;
        }

        public static Color Lerp(Color a, Color b, float coefficient)
        {
            return Color.FromArgb(
                (byte)(a.A + ((b.A - a.A) * coefficient)),
                (byte)(a.R + ((b.R - a.R) * coefficient)),
                (byte)(a.G + ((b.G - a.G) * coefficient)),
                (byte)(a.B + ((b.B - a.B) * coefficient))
                );
        }
    }
}
