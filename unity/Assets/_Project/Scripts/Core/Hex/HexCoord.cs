using System;

namespace FlyingFox.Core
{
    /// <summary>Axial hex coordinate. Parity with game.js key(q,r) and EDGE_DELTA.</summary>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public readonly int Q;
        public readonly int R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public string Key => $"{Q},{R}";

        public static readonly HexCoord Origin = new HexCoord(0, 0);

        /// <summary>
        /// Flat-top edges clockwise: 0 E, 1 SE, 2 SW, 3 W, 4 NW, 5 NE.
        /// Matches game.js EDGE_DELTA.
        /// </summary>
        public static readonly (int dq, int dr)[] EdgeDelta =
        {
            (+1, 0),  // 0 E
            (0, +1),  // 1 SE
            (-1, +1), // 2 SW
            (-1, 0),  // 3 W
            (0, -1),  // 4 NW
            (+1, -1), // 5 NE
        };

        public static readonly int[] OppositeEdge = { 3, 4, 5, 0, 1, 2 };

        public HexCoord Neighbor(int edge)
        {
            if ((uint)edge >= 6) throw new ArgumentOutOfRangeException(nameof(edge));
            var (dq, dr) = EdgeDelta[edge];
            return new HexCoord(Q + dq, R + dr);
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoord h && Equals(h);
        public override int GetHashCode() => (Q * 397) ^ R;
        public override string ToString() => Key;
        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);
    }

    public static class HexMath
    {
        /// <summary>Flat-top pixel position (world units scale by size).</summary>
        public static (float x, float y) ToPixel(HexCoord c, float size)
        {
            float x = size * (1.5f * c.Q);
            float y = size * ((MathF.Sqrt(3f) / 2f) * c.Q + MathF.Sqrt(3f) * c.R);
            return (x, y);
        }

        public static HexCoord FromPixel(float px, float py, float size)
        {
            float q = (2f / 3f * px) / size;
            float r = ((-1f / 3f) * px + (MathF.Sqrt(3f) / 3f) * py) / size;
            return AxialRound(q, r);
        }

        public static HexCoord AxialRound(float q, float r)
        {
            float x = q;
            float z = r;
            float y = -x - z;
            int rx = (int)MathF.Round(x);
            int ry = (int)MathF.Round(y);
            int rz = (int)MathF.Round(z);
            float xDiff = MathF.Abs(rx - x);
            float yDiff = MathF.Abs(ry - y);
            float zDiff = MathF.Abs(rz - z);
            if (xDiff > yDiff && xDiff > zDiff) rx = -ry - rz;
            else if (yDiff > zDiff) ry = -rx - rz;
            else rz = -rx - ry;
            return new HexCoord(rx, rz);
        }
    }
}
