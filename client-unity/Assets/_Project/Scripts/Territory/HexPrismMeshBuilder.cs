namespace Ecosystem.Territory
{
    /// <summary>Geometría del prisma en arrays planos, sin tipos de Unity.</summary>
    public sealed class HexPrismMeshData
    {
        public HexPrismMeshData(Point3[] vertices, Point3[] normals, Point2[] uvs, int[] topIndices, int[] sideIndices)
        {
            Vertices = vertices;
            Normals = normals;
            Uvs = uvs;
            TopIndices = topIndices;
            SideIndices = sideIndices;
        }

        public Point3[] Vertices { get; }
        public Point3[] Normals { get; }
        public Point2[] Uvs { get; }

        /// <summary>Submalla 0: cara superior (césped).</summary>
        public int[] TopIndices { get; }

        /// <summary>Submalla 1: los seis laterales (tierra).</summary>
        public int[] SideIndices { get; }
    }

    /// <summary>Mapeo de la cara superior (data-model.md → Mallas generadas).</summary>
    public enum TopUv
    {
        /// <summary>El de la spec 004: el hexágono inscrito en el cuadrado de la textura.</summary>
        Inscribed,

        /// <summary>
        /// En red: u = x/W y v = z/(3R), para texturas periódicas en la red hexagonal que no tienen
        /// costura entre piezas (research.md R-4 de la spec 005).
        /// </summary>
        Lattice,
    }

    /// <summary>
    /// Genera el prisma hexagonal con vértice arriba (data-model.md → HexPrismMesh; research.md R-3):
    /// 30 vértices y 16 triángulos con sombreado plano, sin cara inferior. Los triángulos van en
    /// sentido horario vistos desde fuera, que es la cara frontal en Unity.
    /// </summary>
    public static class HexPrismMeshBuilder
    {
        private const int TopVertexCount = 6;

        public static HexPrismMeshData Build(HexMetrics metrics, TopUv topUv = TopUv.Inscribed)
        {
            var vertices = new Point3[30];
            var normals = new Point3[30];
            var uvs = new Point2[30];
            var corners = new Point3[6];
            for (var i = 0; i < 6; i++) corners[i] = metrics.Corner(i);

            // Cara superior: las seis esquinas, mapeo planar sobre el cuadrado que la contiene.
            var up = new Point3(0f, 1f, 0f);
            for (var i = 0; i < TopVertexCount; i++)
            {
                var corner = corners[i];
                vertices[i] = corner;
                normals[i] = up;
                uvs[i] = topUv == TopUv.Lattice
                    ? new Point2(corner.X / metrics.Width, corner.Z / (3f * metrics.OuterRadius))
                    : new Point2(corner.X / metrics.Width + 0.5f, corner.Z / (2f * metrics.OuterRadius) + 0.5f);
            }

            var topIndices = new int[12];
            for (var t = 0; t < 4; t++)
            {
                topIndices[t * 3] = 0;
                topIndices[t * 3 + 1] = t + 2;
                topIndices[t * 3 + 2] = t + 1;
            }

            // Laterales: la cara k une las esquinas k y k+1. Vértices B_k, B_k+1, T_k+1, T_k.
            var sideIndices = new int[36];
            for (var k = 0; k < 6; k++)
            {
                var top0 = corners[k];
                var top1 = corners[(k + 1) % 6];
                var bottom0 = new Point3(top0.X, 0f, top0.Z);
                var bottom1 = new Point3(top1.X, 0f, top1.Z);
                var normal = Normalize(new Point3((top0.X + top1.X) / 2f, 0f, (top0.Z + top1.Z) / 2f));

                var b = TopVertexCount + 4 * k;
                vertices[b] = bottom0;
                vertices[b + 1] = bottom1;
                vertices[b + 2] = top1;
                vertices[b + 3] = top0;
                uvs[b] = new Point2(0f, 0f);
                uvs[b + 1] = new Point2(1f, 0f);
                uvs[b + 2] = new Point2(1f, 1f);
                uvs[b + 3] = new Point2(0f, 1f);
                for (var v = 0; v < 4; v++) normals[b + v] = normal;

                var s = k * 6;
                sideIndices[s] = b;
                sideIndices[s + 1] = b + 3;
                sideIndices[s + 2] = b + 2;
                sideIndices[s + 3] = b;
                sideIndices[s + 4] = b + 2;
                sideIndices[s + 5] = b + 1;
            }

            return new HexPrismMeshData(vertices, normals, uvs, topIndices, sideIndices);
        }

        private static Point3 Normalize(Point3 v)
        {
            var length = (float)System.Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            return new Point3(v.X / length, v.Y / length, v.Z / length);
        }
    }
}
