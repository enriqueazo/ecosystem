using System;

namespace Ecosystem.Territory
{
    /// <summary>Rectángulo en píxeles con origen abajo a la izquierda (como Screen.safeArea).</summary>
    public readonly struct ScreenRect
    {
        public ScreenRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
    }

    /// <summary>Resultado del encuadre (data-model.md → TerritoryFraming).</summary>
    public readonly struct TerritoryFramingResult
    {
        public TerritoryFramingResult(float orthographicSize, float cameraOffsetU, float cameraOffsetV)
        {
            OrthographicSize = orthographicSize;
            CameraOffsetU = cameraOffsetU;
            CameraOffsetV = cameraOffsetV;
        }

        /// <summary>Semialtura visible en unidades de mundo.</summary>
        public float OrthographicSize { get; }

        /// <summary>Desplazamiento de la cámara en su plano (eje derecho) respecto al centro del tablero.</summary>
        public float CameraOffsetU { get; }

        /// <summary>Desplazamiento de la cámara en su plano (eje arriba) respecto al centro del tablero.</summary>
        public float CameraOffsetV { get; }
    }

    /// <summary>
    /// Encuadre de una cámara ortográfica inclinada <c>tilt</c> grados hacia abajo y sin giro: el
    /// tablero entero, centrado en el área segura y con un margen por lado (research.md R-6).
    /// Función pura: se prueba sin escena.
    /// </summary>
    public static class TerritoryFraming
    {
        /// <summary>Coordenadas del punto en el plano de la cámara: u a la derecha, v hacia arriba.</summary>
        public static (float U, float V) Project(Point3 point, float tiltDegrees)
        {
            var radians = tiltDegrees * Math.PI / 180.0;
            return (point.X, (float)(point.Y * Math.Cos(radians) + point.Z * Math.Sin(radians)));
        }

        /// <summary>Centro de la proyección del tablero en el plano de la cámara.</summary>
        public static (float U, float V) ProjectedCenter(Point3[] boardCorners, float tiltDegrees)
        {
            var (minU, maxU, minV, maxV) = Extents(boardCorners, tiltDegrees);
            return ((minU + maxU) / 2f, (minV + maxV) / 2f);
        }

        public static TerritoryFramingResult Compute(
            Point3[] boardCorners, float tiltDegrees, float screenWidth, float screenHeight, ScreenRect safeArea, float marginFraction)
        {
            if (boardCorners == null || boardCorners.Length == 0) throw new ArgumentException("Sin esquinas.", nameof(boardCorners));
            if (screenWidth <= 0f || screenHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(screenWidth), "Pantalla sin tamaño.");
            if (marginFraction < 0f || marginFraction >= 0.5f) throw new ArgumentOutOfRangeException(nameof(marginFraction));

            var safe = safeArea.Width > 0f && safeArea.Height > 0f ? safeArea : new ScreenRect(0f, 0f, screenWidth, screenHeight);

            // Zona disponible en fracciones de la pantalla: el área segura menos el margen por lado.
            var safeX0 = safe.X / screenWidth;
            var safeX1 = (safe.X + safe.Width) / screenWidth;
            var safeY0 = safe.Y / screenHeight;
            var safeY1 = (safe.Y + safe.Height) / screenHeight;
            var availableWidth = (safeX1 - safeX0) * (1f - 2f * marginFraction);
            var availableHeight = (safeY1 - safeY0) * (1f - 2f * marginFraction);
            var availableCenterX = (safeX0 + safeX1) / 2f;
            var availableCenterY = (safeY0 + safeY1) / 2f;

            var (minU, maxU, minV, maxV) = Extents(boardCorners, tiltDegrees);
            var halfU = (maxU - minU) / 2f;
            var halfV = (maxV - minV) / 2f;
            var aspect = screenWidth / screenHeight;

            // Semialtura S: el alto visible es 2S y el ancho 2S·aspect.
            var size = Math.Max(halfV / availableHeight, halfU / (aspect * availableWidth));

            // El centro del tablero cae en el centro de la zona disponible.
            var offsetU = -(availableCenterX - 0.5f) * 2f * size * aspect;
            var offsetV = -(availableCenterY - 0.5f) * 2f * size;
            return new TerritoryFramingResult(size, offsetU, offsetV);
        }

        private static (float MinU, float MaxU, float MinV, float MaxV) Extents(Point3[] corners, float tiltDegrees)
        {
            float minU = float.MaxValue, maxU = float.MinValue, minV = float.MaxValue, maxV = float.MinValue;
            foreach (var corner in corners)
            {
                var (u, v) = Project(corner, tiltDegrees);
                minU = Math.Min(minU, u);
                maxU = Math.Max(maxU, u);
                minV = Math.Min(minV, v);
                maxV = Math.Max(maxV, v);
            }

            return (minU, maxU, minV, maxV);
        }
    }
}
