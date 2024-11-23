namespace BmsDisplayConfig
{
    internal struct RectXYWH
    {
        public int x, y, width, height;

        public static RectXYWH FromXYWH(int x, int y, int width, int height)
        {
            RectXYWH @this = new RectXYWH();
            @this.x = x;
            @this.y = y;
            @this.width = width;
            @this.height = height;
            return @this;
        }

        public static RectXYWH FromLTRB(int left, int top, int right, int bottom)
        {
            RectXYWH @this = new RectXYWH();
            @this.x = left;
            @this.y = top;
            @this.width = (right-left);
            @this.height = (bottom-top);
            return @this;
        }
        public static RectXYWH FromLTRB(Win32.Rect r)
        {
            return FromLTRB(r.left, r.top, r.right, r.bottom);
        }

        public int GetRight() { return this.x + this.width; }
        public int GetBottom() { return this.y + this.height; }

        public bool Contains(RectXYWH that)
        {
            if (that.x < this.x) return false;
            if (that.y < this.y) return false;
            if (that.GetRight() > this.GetRight()) return false;
            if (that.GetBottom() > this.GetBottom()) return false;
            return true;
        }

        public override string ToString() => $"{{ X:{x},Y:{y},W:{width},H:{height} }}";
    }
}
