using System;

namespace DrawClient
{
    public static class CoordinateMapper
    {
        // Simple struct for Point
        public struct PointF
        {
            public float X;
            public float Y;
            public PointF(float x, float y) { X = x; Y = y; }
            public override string ToString() => $"({X:F4}, {Y:F4})";
        }

        // Mock screen size for Console App
        private static int ScreenWidth = 1920;
        private static int ScreenHeight = 1080;

        public static PointF Normalize(int x, int y)
        {
            return new PointF(
                Math.Max(0f, Math.Min((float)x / ScreenWidth, 1f)),
                Math.Max(0f, Math.Min((float)y / ScreenHeight, 1f))
            );
        }

        public static (int X, int Y) Denormalize(PointF p)
        {
            return (
                (int)(p.X * ScreenWidth),
                (int)(p.Y * ScreenHeight)
            );
        }
        
        // Simple serialization (X,Y as floats)
        // 8 bytes (4 for X, 4 for Y)
        public static byte[] ToBytes(PointF p)
        {
             byte[] buf = new byte[8];
            BitConverter.GetBytes(p.X).CopyTo(buf, 0);
            BitConverter.GetBytes(p.Y).CopyTo(buf, 4);
            return buf;
        }

        public static PointF FromBytes(byte[] data)
        {
            if (data.Length < 8) return new PointF(0,0);
            float x = BitConverter.ToSingle(data, 0);
            float y = BitConverter.ToSingle(data, 4);
            return new PointF(x, y);
        }
    }
}
