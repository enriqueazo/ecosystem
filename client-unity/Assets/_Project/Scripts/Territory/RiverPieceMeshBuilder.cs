using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecosystem.Territory
{
    /// <summary>Geometría de una pieza de río en arrays planos, sin tipos de Unity.</summary>
    public sealed class RiverPieceMeshData
    {
        public RiverPieceMeshData(Point3[] vertices, Point3[] normals, Point2[] uvs, int[] grassIndices, int[] sideIndices, int[] waterIndices)
        {
            Vertices = vertices;
            Normals = normals;
            Uvs = uvs;
            GrassIndices = grassIndices;
            SideIndices = sideIndices;
            WaterIndices = waterIndices;
        }

        public Point3[] Vertices { get; }
        public Point3[] Normals { get; }
        public Point2[] Uvs { get; }

        /// <summary>Submalla 0: orillas de hierba a la altura h.</summary>
        public int[] GrassIndices { get; }

        /// <summary>Submalla 1: laterales (con la muesca del cauce) y paredes del cauce.</summary>
        public int[] SideIndices { get; }

        /// <summary>Submalla 2: agua del cauce a la altura hw.</summary>
        public int[] WaterIndices { get; }

        public int[][] Submeshes => new[] { GrassIndices, SideIndices, WaterIndices };
    }

    /// <summary>
    /// Genera las piezas de río con cauce hundido (data-model.md → Mallas generadas; research.md R-2
    /// de la spec 005). La pieza es el prisma de hierba con un cauce abierto por arriba: su fondo
    /// visible es la lámina de agua a hw y sus paredes van de hw a h. El cauce corta cada arista
    /// abierta en su punto medio con anchura w. La malla de giro r es la de giro 0 girada 60°·r en
    /// sentido antihorario visto desde arriba; las coordenadas de textura planares se calculan después
    /// de girar, para que casen con las piezas vecinas.
    /// </summary>
    public static class RiverPieceMeshBuilder
    {
        private enum Part
        {
            Grass,
            Side,
            Water,
        }

        public static RiverPieceMeshData Build(HexMetrics hex, RiverMetrics river, RiverShape shape, int rotation)
        {
            if (hex == null) throw new ArgumentNullException(nameof(hex));
            if (river == null) throw new ArgumentNullException(nameof(river));
            if (shape == RiverShape.None || !RiverPiece.IsDefined(shape)) throw new ArgumentOutOfRangeException(nameof(shape));
            if (rotation < 0 || rotation > 5) throw new ArgumentOutOfRangeException(nameof(rotation));

            var outline = new Outline(hex, river);
            List<Point2[]> grass;
            Point2[] water;
            switch (shape)
            {
                case RiverShape.Straight:
                    outline.Straight(out grass, out water);
                    break;
                case RiverShape.Curve:
                    outline.Curve(out grass, out water);
                    break;
                default:
                    outline.Spring(out grass, out water);
                    break;
            }

            var mesh = new MeshAccumulator(hex);
            foreach (var polygon in grass) mesh.AddTopPolygon(polygon, hex.Height, Part.Grass);
            mesh.AddTopPolygon(water, river.WaterLevel, Part.Water);
            mesh.AddWalls(water, outline, river.WaterLevel, hex.Height);
            var open = RiverPiece.OpenEdges(shape, 0);
            foreach (var edge in HexEdges.All) mesh.AddLateral(edge, open.Contains(edge), outline, river.WaterLevel);

            return mesh.ToData(rotation);
        }

        /// <summary>El contorno de la pieza en el plano XZ (Point2.X = x, Point2.Y = z), con giro 0.</summary>
        private sealed class Outline
        {
            private readonly HexMetrics _hex;
            private readonly RiverMetrics _river;

            public Outline(HexMetrics hex, RiverMetrics river)
            {
                _hex = hex;
                _river = river;
                Apothem = hex.Width / 2f;
            }

            public float Apothem { get; }

            /// <summary>Vértice k del hexágono, a 30° + 60°·k.</summary>
            public Point2 Corner(int k)
            {
                var c = _hex.Corner(((k % 6) + 6) % 6);
                return new Point2(c.X, c.Z);
            }

            /// <summary>La arista e va del vértice e − 1 al vértice e.</summary>
            public Point2 EdgeStart(HexEdge edge) => Corner((int)edge - 1);

            public Point2 EdgeEnd(HexEdge edge) => Corner((int)edge);

            /// <summary>Corte del cauce en la arista: hacia su inicio (signo −1) o hacia su final (+1).</summary>
            public Point2 Cut(HexEdge edge, int sign)
            {
                var n = edge.Normal();
                var half = _river.ChannelWidth / 2f * sign;
                return new Point2(n.X * Apothem - n.Z * half, n.Z * Apothem + n.X * half);
            }

            public bool OnEdge(Point2 p, HexEdge edge)
            {
                var n = edge.Normal();
                return Math.Abs(p.X * n.X + p.Y * n.Z - Apothem) < 1e-5f;
            }

            public void Straight(out List<Point2[]> grass, out Point2[] water)
            {
                water = new[] { Cut(HexEdge.E, -1), Cut(HexEdge.E, +1), Cut(HexEdge.W, -1), Cut(HexEdge.W, +1) };
                grass = new List<Point2[]>
                {
                    new[] { Cut(HexEdge.E, +1), Corner(0), Corner(1), Corner(2), Cut(HexEdge.W, -1) },
                    new[] { Cut(HexEdge.W, +1), Corner(3), Corner(4), Corner(5), Cut(HexEdge.E, -1) },
                };
            }

            /// <summary>
            /// Arco de 60° entre las aristas E y NO con centro en la intersección de sus rectas (a W del
            /// centro en la dirección de NE) y radio 1,5·R: entra y sale perpendicular a cada arista.
            /// </summary>
            public void Curve(out List<Point2[]> grass, out Point2[] water)
            {
                var ne = HexEdge.NE.Normal();
                var center = new Point2(ne.X * _hex.Width, ne.Z * _hex.Width);
                var radius = 1.5f * _hex.OuterRadius;
                var half = _river.ChannelWidth / 2f;
                var segments = _river.ArcSegments;

                Point2[] Arc(float r, Point2 first, Point2 last)
                {
                    var points = new Point2[segments + 1];
                    for (var k = 0; k <= segments; k++)
                    {
                        var angle = Math.PI / 180.0 * (270.0 - 60.0 * k / segments);
                        points[k] = new Point2(center.X + r * (float)Math.Cos(angle), center.Y + r * (float)Math.Sin(angle));
                    }

                    // Extremos exactos: los mismos cortes que calcula la pieza vecina.
                    points[0] = first;
                    points[segments] = last;
                    return points;
                }

                var inner = Arc(radius - half, Cut(HexEdge.E, +1), Cut(HexEdge.NW, -1));
                var outer = Arc(radius + half, Cut(HexEdge.E, -1), Cut(HexEdge.NW, +1));

                water = outer.Concat(inner.Reverse()).ToArray();
                grass = new List<Point2[]>
                {
                    new[] { Cut(HexEdge.E, +1), Corner(0), Corner(1) }.Concat(inner.Reverse().Take(segments)).ToArray(),
                    new[] { Cut(HexEdge.NW, +1), Corner(2), Corner(3), Corner(4), Corner(5) }.Concat(outer.Take(segments)).ToArray(),
                };
            }

            /// <summary>Cauce desde la arista E hasta una poza circular de radio rp en el centro.</summary>
            public void Spring(out List<Point2[]> grass, out Point2[] water)
            {
                var half = _river.ChannelWidth / 2f;
                var pool = _river.PoolRadius;
                var theta = Math.Asin(half / pool);
                var xp = (float)Math.Sqrt(pool * pool - half * half);
                var segments = _river.PoolSegments;
                var arc = new Point2[segments - 1];
                for (var k = 1; k < segments; k++)
                {
                    var angle = theta + (2 * Math.PI - 2 * theta) * k / segments;
                    arc[k - 1] = new Point2(pool * (float)Math.Cos(angle), pool * (float)Math.Sin(angle));
                }

                var upper = new Point2(xp, half);
                var lower = new Point2(xp, -half);
                water = new[] { Cut(HexEdge.E, -1), Cut(HexEdge.E, +1), upper }.Concat(arc).Concat(new[] { lower }).ToArray();
                grass = new List<Point2[]>
                {
                    new[] { Cut(HexEdge.E, +1), Corner(0), Corner(1), Corner(2), Corner(3), Corner(4), Corner(5), Cut(HexEdge.E, -1), lower }
                        .Concat(arc.Reverse()).Concat(new[] { upper }).ToArray(),
                };
            }
        }

        private sealed class MeshAccumulator
        {
            private static readonly Point3 Up = new Point3(0f, 1f, 0f);

            private readonly HexMetrics _hex;
            private readonly List<Point3> _vertices = new List<Point3>();
            private readonly List<Point3> _normals = new List<Point3>();
            private readonly List<Part> _parts = new List<Part>();
            private readonly List<float> _sideU = new List<float>();
            private readonly Dictionary<Part, List<int>> _indices = new Dictionary<Part, List<int>>
            {
                { Part.Grass, new List<int>() },
                { Part.Side, new List<int>() },
                { Part.Water, new List<int>() },
            };

            public MeshAccumulator(HexMetrics hex)
            {
                _hex = hex;
            }

            public void AddTopPolygon(Point2[] polygon, float y, Part part)
            {
                var first = _vertices.Count;
                foreach (var p in polygon) AddVertex(new Point3(p.X, y, p.Y), Up, part, 0f);

                var triangles = PolygonTriangulator.Triangulate(polygon);
                for (var t = 0; t < triangles.Length; t += 3)
                {
                    AddTriangle(first + triangles[t], first + triangles[t + 1], first + triangles[t + 2], part);
                }
            }

            /// <summary>Paredes del cauce: los lados del agua que no están en una arista, de hw a h, mirando al agua.</summary>
            public void AddWalls(Point2[] water, Outline outline, float bottom, float top)
            {
                var counterClockwise = SignedArea(water) > 0f;
                var u = 0f;
                for (var i = 0; i < water.Length; i++)
                {
                    var p = water[i];
                    var q = water[(i + 1) % water.Length];
                    var length = (float)Math.Sqrt((q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y));
                    if (HexEdges.All.Any(edge => outline.OnEdge(p, edge) && outline.OnEdge(q, edge))) continue;

                    // Normal horizontal hacia el interior del agua (a la izquierda si el contorno es antihorario).
                    var sign = counterClockwise ? 1f : -1f;
                    var normal = new Point3(-(q.Y - p.Y) / length * sign, 0f, (q.X - p.X) / length * sign);
                    var u0 = u / _hex.OuterRadius;
                    var u1 = (u + length) / _hex.OuterRadius;
                    AddQuad(new Point3(p.X, bottom, p.Y), new Point3(q.X, bottom, q.Y), new Point3(q.X, top, q.Y), new Point3(p.X, top, p.Y), normal, u0, u1);
                    u += length;
                }
            }

            /// <summary>Lateral de una arista: completo de 0 a h, o con la muesca del cauce hasta hw.</summary>
            public void AddLateral(HexEdge edge, bool open, Outline outline, float waterLevel)
            {
                var normal = edge.Normal();
                var start = outline.EdgeStart(edge);
                var end = outline.EdgeEnd(edge);
                if (!open)
                {
                    AddSideQuad(start, end, 0f, _hex.Height, normal, start);
                    return;
                }

                var lowCut = outline.Cut(edge, -1);
                var highCut = outline.Cut(edge, +1);
                AddSideQuad(start, lowCut, 0f, _hex.Height, normal, start);
                AddSideQuad(lowCut, highCut, 0f, waterLevel, normal, start);
                AddSideQuad(highCut, end, 0f, _hex.Height, normal, start);
            }

            private void AddSideQuad(Point2 a, Point2 b, float bottom, float top, Point3 normal, Point2 origin)
            {
                float U(Point2 p) => (float)Math.Sqrt((p.X - origin.X) * (p.X - origin.X) + (p.Y - origin.Y) * (p.Y - origin.Y)) / _hex.OuterRadius;
                AddQuad(new Point3(a.X, bottom, a.Y), new Point3(b.X, bottom, b.Y), new Point3(b.X, top, b.Y), new Point3(a.X, top, a.Y), normal, U(a), U(b));
            }

            private void AddQuad(Point3 a0, Point3 b0, Point3 b1, Point3 a1, Point3 normal, float ua, float ub)
            {
                var i = _vertices.Count;
                AddVertex(a0, normal, Part.Side, ua);
                AddVertex(b0, normal, Part.Side, ub);
                AddVertex(b1, normal, Part.Side, ub);
                AddVertex(a1, normal, Part.Side, ua);
                AddTriangle(i, i + 1, i + 2, Part.Side);
                AddTriangle(i, i + 2, i + 3, Part.Side);
            }

            private void AddVertex(Point3 position, Point3 normal, Part part, float sideU)
            {
                _vertices.Add(position);
                _normals.Add(normal);
                _parts.Add(part);
                _sideU.Add(sideU);
            }

            /// <summary>Añade el triángulo con la cara frontal hacia su normal (la de Unity).</summary>
            private void AddTriangle(int a, int b, int c, Part part)
            {
                var pa = _vertices[a];
                var u = new Point3(_vertices[b].X - pa.X, _vertices[b].Y - pa.Y, _vertices[b].Z - pa.Z);
                var v = new Point3(_vertices[c].X - pa.X, _vertices[c].Y - pa.Y, _vertices[c].Z - pa.Z);
                var cross = new Point3(u.Y * v.Z - u.Z * v.Y, u.Z * v.X - u.X * v.Z, u.X * v.Y - u.Y * v.X);
                var n = _normals[a];
                var list = _indices[part];
                if (cross.X * n.X + cross.Y * n.Y + cross.Z * n.Z >= 0f)
                {
                    list.Add(a);
                    list.Add(b);
                    list.Add(c);
                }
                else
                {
                    list.Add(a);
                    list.Add(c);
                    list.Add(b);
                }
            }

            public RiverPieceMeshData ToData(int rotation)
            {
                var angle = Math.PI / 3.0 * rotation;
                var cos = (float)Math.Cos(angle);
                var sin = (float)Math.Sin(angle);
                Point3 Rotate(Point3 p) => new Point3(p.X * cos - p.Z * sin, p.Y, p.X * sin + p.Z * cos);

                var count = _vertices.Count;
                var vertices = new Point3[count];
                var normals = new Point3[count];
                var uvs = new Point2[count];
                for (var i = 0; i < count; i++)
                {
                    var v = Rotate(_vertices[i]);
                    vertices[i] = v;
                    normals[i] = Rotate(_normals[i]);
                    switch (_parts[i])
                    {
                        case Part.Grass:
                            uvs[i] = new Point2(v.X / _hex.Width + 0.5f, v.Z / (2f * _hex.OuterRadius) + 0.5f);
                            break;
                        case Part.Water:
                            uvs[i] = new Point2(v.X / _hex.Width, v.Z / (3f * _hex.OuterRadius));
                            break;
                        default:
                            uvs[i] = new Point2(_sideU[i], v.Y / _hex.Height);
                            break;
                    }
                }

                return new RiverPieceMeshData(
                    vertices,
                    normals,
                    uvs,
                    _indices[Part.Grass].ToArray(),
                    _indices[Part.Side].ToArray(),
                    _indices[Part.Water].ToArray());
            }

            private static float SignedArea(Point2[] polygon)
            {
                var area = 0f;
                for (var i = 0; i < polygon.Length; i++)
                {
                    var a = polygon[i];
                    var b = polygon[(i + 1) % polygon.Length];
                    area += a.X * b.Y - b.X * a.Y;
                }

                return area / 2f;
            }
        }
    }
}
