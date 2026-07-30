using FlyingFox.Core;
using UnityEngine;

namespace FlyingFox.Presentation
{
    /// <summary>Flat-top hex geometry. Edge 0 = +X (East), clockwise — matches Core.HexCoord.</summary>
    public static class HexMeshUtil
    {
        public const float DefaultSize = 1f;

        public static Vector3 ToWorld(HexCoord c, float size = DefaultSize)
        {
            var (x, y) = HexMath.ToPixel(c, size);
            return new Vector3(x, y, 0f);
        }

        public static HexCoord FromWorld(Vector3 world, float size = DefaultSize) =>
            HexMath.FromPixel(world.x, world.y, size);

        /// <summary>Six flat-top corners, edge i spans corners[i] → corners[(i+1)%6].</summary>
        public static Vector3[] Corners(float size = DefaultSize)
        {
            var pts = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i);
                pts[i] = new Vector3(size * Mathf.Cos(angle), size * Mathf.Sin(angle), 0f);
            }
            return pts;
        }

        /// <summary>
        /// Mesh with 6 wedge triangles colored via vertex colors (one biome per edge sector).
        /// </summary>
        public static Mesh BuildWedgeMesh(BiomeId[] edges, float size = DefaultSize)
        {
            if (edges == null || edges.Length != 6)
                throw new System.ArgumentException("Need 6 edges");

            var corners = Corners(size);
            var mesh = new Mesh { name = "HexWedges" };

            // 6 wedges × 3 verts
            var verts = new Vector3[18];
            var colors = new Color[18];
            var tris = new int[18];

            for (int e = 0; e < 6; e++)
            {
                int b = e * 3;
                verts[b] = Vector3.zero;
                verts[b + 1] = corners[e];
                verts[b + 2] = corners[(e + 1) % 6];
                var col = BiomePalette.Fill(edges[e]);
                colors[b] = col;
                colors[b + 1] = col;
                colors[b + 2] = col;
                tris[b] = b;
                tris[b + 1] = b + 1;
                tris[b + 2] = b + 2;
            }

            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>Outline line mesh for empty slots / rings (LineRenderer alternative).</summary>
        public static void SetOutlinePositions(LineRenderer lr, float size, float z = -0.01f)
        {
            var c = Corners(size);
            lr.positionCount = 7;
            lr.useWorldSpace = false;
            for (int i = 0; i < 6; i++)
                lr.SetPosition(i, c[i] + new Vector3(0, 0, z));
            lr.SetPosition(6, c[0] + new Vector3(0, 0, z));
            lr.loop = false;
        }
    }
}
