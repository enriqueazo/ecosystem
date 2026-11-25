using System;

namespace Ecosystem.Territory
{
    /// <summary>Punto 2D inmutable (coordenadas de textura). El núcleo no depende de Unity.</summary>
    public readonly struct Point2 : IEquatable<Point2>
    {
        public Point2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }

        public bool Equals(Point2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Point2 other && Equals(other);
        public override int GetHashCode() => (X, Y).GetHashCode();
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>
    /// Punto o vector 3D inmutable en unidades de mundo de Unity: x a la derecha, y arriba y z
    /// hacia el fondo (data-model.md → Convenciones geométricas).
    /// </summary>
    public readonly struct Point3 : IEquatable<Point3>
    {
        public Point3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public bool Equals(Point3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is Point3 other && Equals(other);
        public override int GetHashCode() => (X, Y, Z).GetHashCode();
        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
