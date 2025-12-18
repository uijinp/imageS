using System;

namespace WpfClient
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

        // We can make these dynamic or property-based for WPF
        public static double CanvasWidth { get; set; } = 1920.0;
        public static double CanvasHeight { get; set; } = 1080.0;

        public static PointF Normalize(double x, double y)
        {
            return new PointF(
                (float)Math.Max(0.0, Math.Min(x / CanvasWidth, 1.0)),
                (float)Math.Max(0.0, Math.Min(y / CanvasHeight, 1.0))
            );
        }

        public static (double X, double Y) Denormalize(PointF p)
        {
            return (
                p.X * CanvasWidth,
                p.Y * CanvasHeight
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
