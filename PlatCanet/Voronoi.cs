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
    
    /*class MapCorner
    {
        public Vector2f Position { get; set; }

        public List<MapCorner> AdjacentCorners { get; set; }

        public MapCorner(Vertex v)
        {
            Position = v.Coord;
            AdjacentCorners = new List<MapCorner>();
        }

        public void AddAdjacentCorner(MapCorner c)
        {
            AdjacentCorners.Add(c);
        }
    }

    class MapCentre
    {
        public MapCentre(Site s)
        {

        }
    }

    class MapEdge
    {
        private MapCorner c1;
        private MapCorner c2;
        private MapCentre left;
        private MapCentre right;

        public MapEdge(MapCorner c1, MapCorner c2, MapCentre left, MapCentre right)
        {
            // TODO: Complete member initialization
            this.c1 = c1;
            this.c2 = c2;
            this.left = left;
            this.right = right;
        }
    }*/

    class MapCell
    {
        public static int NoClassification = -1;
        public Site Site { get; set; }

        public int Classification { get; set; }
        
        public MapCell(Site s, int c)
        {
            Site = s;
            Classification = c;
        }
    }

    class VoronoiMap
    {
        /*private Dictionary<int, MapCorner> corners = new Dictionary<int, MapCorner>();
        private Dictionary<int, MapCentre> centres = new Dictionary<int, MapCentre>();
        private Dictionary<int, MapEdge> edges = new Dictionary<int, MapEdge>();*/

        public Voronoi Voronoi { get; set; }

        public VoronoiMap(List<Vector2f> points, int width, int height)
        {
            Voronoi = new Voronoi(points, new Rectf(0, 0, width, height), 1);
        }

        /*public MapCorner GetCorner(Vertex v)
        {
            MapCorner c;
            int hash = v.Coord.GetHashCode();
            corners.TryGetValue(hash, out c);
            if (c == null)
            {
                c = new MapCorner(v);
                corners.Add(v.GetHashCode(), c);
            }
            return c;
        }

        public MapCentre GetCentre(Site s)
        {
            MapCentre c;
            int hash = s.Coord.GetHashCode();
            centres.TryGetValue(hash.GetHashCode(), out c);
            if (c == null)
            {
                c = new MapCentre(s);
                centres.Add(hash, c);
            }
            return c;
        }

        public MapEdge GetEdge(MapCorner c1, MapCorner c2, MapCentre left, MapCentre right)
        {
            Vector2f mid = new Vector2f((c1.Position.X + c2.Position.X) / 2f, (c1.Position.Y + c1.Position.Y) / 2f);
            int hash = mid.GetHashCode();
            MapEdge e;
            edges.TryGetValue(mid.GetHashCode(), out e);
            if (e == null)
            {
                e = new MapEdge(c1, c2, left, right);
                edges.Add(hash, e);
            }
            return e;
        }*/
    }
}
