using BearLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatCanet
{
    class Window
    {
        public int Width { get; private set; }

        public int Height { get; private set; }

        public const int CellSize = 8;

        private Dictionary<int, Panel> panels;

        private int currentPanelID = 0;

        private bool isDirty = false;

        private Color currColor = Color.Black;

        private int currLayer = 0;

        private readonly int[] mouseEvents = new int[] { Terminal.TK_MOUSE_LEFT, Terminal.TK_MOUSE_RIGHT, Terminal.TK_MOUSE_MIDDLE, Terminal.TK_MOUSE_SCROLL };

        private Dictionary<Rectangle, Action<int, int>> buttons;

        public Window(int width, int height, string title)
        {
            Width = width;
            Height = height;
            panels = new Dictionary<int, Panel>();
            Terminal.Open();
            Terminal.Set("window: size={0}x{1}, cellsize={3}x{3}, title='{2}'; font=default", width, height, title, Window.CellSize);
            Terminal.BkColor(Color.Black);
            Terminal.Composition(Terminal.TK_COMPOSITION);
            Terminal.Clear();
            Terminal.Refresh();

            buttons = new Dictionary<Rectangle, Action<int, int>>();
        }

        public Panel CreatePanel(Rectangle area, bool hasBorders = true)
        {
            Panel p =new Panel();
            p.Area = area;
            p.MinLayer = 0;
            p.MaxLayer = 4;
            p.ID = currentPanelID++;
            p.HasBorders = hasBorders;
            panels.Add(p.ID, p);
            return p;
        }

        public void Put(int panel, int x, int y, int c, Color colour)
        {
            SetColour(colour);
            Put(panel, x, y, c);
        }

        public void Put(Panel panel, int x, int y, int c)
        {
            PutExt(panel.ID, x, y, 0, 0, c);
        }

        public void Put(int panel, int x, int y, int c)
        {
            PutExt(panel, x, y, 0, 0, c);
        }

        public void SetColour(Color colour)
        {
            if (colour != currColor)
            {
                Terminal.Color(colour);
                currColor = colour;
            }
        }

        public void PutExt(Panel panel, int x, int y, int dx, int dy, int c, Color colour)
        {
            PutExt(panel.ID, x, y, dx, dy, c, colour);
        }

        public void PutExt(Panel panel, int x, int y, int dx, int dy, int c)
        {
            PutExt(panel.ID, x, y, dx, dy, c);
        }

        public void PutExt(int panel, int x, int y, int dx, int dy, int c, Color colour)
        {
            SetColour(colour);
            PutExt(panel, x, y, dx, dy, c, colour);
        }

        public void PutExt(int panel, int x, int y, int dx, int dy, int c)
        {
            Panel p = panels[panel];
            if (p.HasBorders)
            {
                x++;
                y++;
            }

            if (x < 0 || y < 0 || x >= p.Width || y >= p.Height)
                return;

            if (currLayer < p.MinLayer)
            {
                Terminal.Layer(p.MinLayer);
                currLayer = p.MinLayer;
            }
            else if (currLayer > p.MaxLayer)
            {
                Terminal.Layer(p.MaxLayer);
                currLayer = p.MaxLayer;
            }

            Terminal.PutExt(x + p.X, y + p.Y, dx, dy, c);
            isDirty = true;
        }

        public void Refresh()
        {
            if(isDirty)
            {
                Terminal.Refresh();
                isDirty = false;
            }
        }

        public void DrawBorders(Panel p)
        {
            Terminal.Layer(p.MaxLayer + 1);
            currLayer = p.MaxLayer + 1;
            if (!p.HasBorders)
                return;
            //draw the 4 corners
            Terminal.Put(p.X, p.Y, '╔');
            Terminal.Put(p.X, p.Area.Bottom, '╚');
            Terminal.Put(p.Area.Right, p.Y, '╗');
            Terminal.Put(p.Area.Right, p.Area.Bottom, '╝');

            for(int x = p.X+1; x < p.Area.Right; ++x)
            {
                Terminal.Put(x, p.Y, '═');
                Terminal.Put(x, p.Area.Bottom, '═');
            }

            for (int y = p.Y + 1; y < p.Area.Bottom; ++y)
            {
                Terminal.Put(p.X, y, '║');
                Terminal.Put(p.Area.Right, y, '║');
            }
            isDirty = true;
        }

        public void ClearArea(Panel panel)
        {
            for (int l = panel.MinLayer; l <= panel.MaxLayer; ++l)
            {
                Terminal.Layer(l);
                Terminal.ClearArea(panel.X + 1, panel.Y + 1, panel.Width - 1, panel.Height - 1);
            }
            Terminal.Layer(currLayer);
        }

        public void HandleMouse(int e)
        {
            if (!mouseEvents.Contains(e))
                return;

            int x = Terminal.State(Terminal.TK_MOUSE_PIXEL_X), y = Terminal.State(Terminal.TK_MOUSE_PIXEL_Y);
            KeyValuePair<Rectangle, Action<int, int>> action = buttons.FirstOrDefault(b => b.Key.Contains(x, y));
            if (action.Value != null)
                action.Value(x-action.Key.X, y-action.Key.Y);
        }

        public void AddOnClick(Panel p, Rectangle area, Action<int, int> action)
        {
            buttons.Add(new Rectangle((area.X + p.X + 1)*CellSize, (area.Y + p.Y + 1)*CellSize, area.Width*CellSize, area.Height*CellSize), action);
        }

        internal static void Print(Panel panel, int x, int y, string str, params object[] args)
        {
            if (x < 0 || y < 0 || x >= panel.Width || y >= panel.Height)
                return;
            Terminal.Print(x + panel.X, y + panel.Y, String.Format(str, args));
        }
    }
}
