using csDelaunay;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace PlatCanet
{
    class RegionCell
    {
        public Site Site { get; set; }

        public Biome Classification { get; set; }
        
        public RegionCell(Site s, Biome c)
        {
            Site = s;
            Classification = c;
        }
    }

    class VoronoiMap
    {
        public Voronoi Voronoi { get; set; }

        public VoronoiMap(List<Vector2f> points, int width, int height)
        {
            Voronoi = new Voronoi(points, new Rectf(0, 0, width, height), 1);
        }
    }
}
