using System.Collections.Generic;

namespace Ecosystem.Territory
{
    /// <summary>
    /// Triangula un polígono simple (convexo o no) por recorte de orejas (research.md R-2 de la spec
    /// 005). Devuelve índices al polígono de entrada en triángulos antihorarios en el plano (X, Y), sea
    /// cual sea la orientación de entrada. Con datos degenerados (menos de 3 puntos, área nula o un
    /// polígono que no es simple) devuelve lo triangulado hasta ese punto, nunca una excepción.
    /// </summary>
    public static class PolygonTriangulator
    {
        private const float Epsilon = 1e-9f;

        public static int[] Triangulate(IReadOnlyList<Point2> polygon)
        {
            if (polygon == null || polygon.Count < 3) return new int[0];

            var area = SignedArea(polygon);
            if (System.Math.Abs(area) < Epsilon) return new int[0];

            var remaining = new List<int>(polygon.Count);
            for (var i = 0; i < polygon.Count; i++) remaining.Add(area > 0 ? i : polygon.Count - 1 - i);

            var triangles = new List<int>((polygon.Count - 2) * 3);
            var guard = 0;
            var current = 0;
            while (remaining.Count > 3)
            {
                if (guard++ > remaining.Count)
                {
                    break; // Sin orejas: el polígono no es simple.
                }

                var n = remaining.Count;
                var prev = remaining[(current + n - 1) % n];
                var curr = remaining[current % n];
                var next = remaining[(current + 1) % n];
                if (IsEar(polygon, remaining, prev, curr, next))
                {
                    triangles.Add(prev);
                    triangles.Add(curr);
                    triangles.Add(next);
                    remaining.RemoveAt(current % n);
                    guard = 0;
                    current = (current + n - 2) % (n - 1);
                }
                else
                {
                    current = (current + 1) % n;
                }
            }

            if (remaining.Count == 3 && Cross(polygon[remaining[0]], polygon[remaining[1]], polygon[remaining[2]]) > Epsilon)
            {
                triangles.AddRange(remaining);
            }

            return triangles.ToArray();
        }

        private static bool IsEar(IReadOnlyList<Point2> polygon, List<int> remaining, int prev, int curr, int next)
        {
            var a = polygon[prev];
            var b = polygon[curr];
            var c = polygon[next];
            if (Cross(a, b, c) <= Epsilon) return false; // Reflejo o colineal.

            foreach (var index in remaining)
            {
                if (index == prev || index == curr || index == next) continue;
                var p = polygon[index];
                if (SamePoint(p, a) || SamePoint(p, b) || SamePoint(p, c)) continue;
                if (InsideOrOnTriangle(p, a, b, c)) return false;
            }

            return true;
        }

        private static float SignedArea(IReadOnlyList<Point2> polygon)
        {
            var area = 0f;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                area += a.X * b.Y - b.X * a.Y;
            }

            return area / 2f;
        }

        private static float Cross(Point2 a, Point2 b, Point2 c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        private static bool SamePoint(Point2 a, Point2 b) => System.Math.Abs(a.X - b.X) < 1e-7f && System.Math.Abs(a.Y - b.Y) < 1e-7f;

        private static bool InsideOrOnTriangle(Point2 p, Point2 a, Point2 b, Point2 c) =>
            Cross(a, b, p) >= -Epsilon && Cross(b, c, p) >= -Epsilon && Cross(c, a, p) >= -Epsilon;
    }
}
