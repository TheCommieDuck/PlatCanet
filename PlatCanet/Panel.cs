using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatCanet
{
    struct Panel
    {
        public Rectangle Area;

        public bool HasBorders;

        public int ID;

        public int Width
        {
            get
            {
                return Area.Width;
            }
        }

        public int Height
        {
            get
            {
                return Area.Height;
            }
        }
        public int X
        {
            get
            {
                return Area.X;
            }
        }

        public int Y
        {
            get
            {
                return Area.Y;
            }
        }

    }
}
