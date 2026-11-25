using System;

namespace Ecosystem.Territory
{
    /// <summary>
    /// Medidas del agua y del cauce del río (data-model.md → RiverMetrics). <see cref="Default"/> es
    /// la única fuente de estos valores en el código; los aprobados están en
    /// contracts/visual-direction.md §2 de la spec 005.
    /// </summary>
    public sealed class RiverMetrics
    {
        /// <summary>Valores de partida (research.md R-7), sobre <see cref="HexMetrics.Default"/>.</summary>
        public static readonly RiverMetrics Default = new RiverMetrics(HexMetrics.Default, 0.25f, 0.26f, 0.17f, 8, 16);

        public RiverMetrics(HexMetrics hex, float waterLevel, float channelWidth, float poolRadius, int arcSegments, int poolSegments)
        {
            if (hex == null) throw new ArgumentNullException(nameof(hex));
            if (waterLevel <= 0f || waterLevel >= hex.Height) throw new ArgumentOutOfRangeException(nameof(waterLevel));
            if (channelWidth <= 0f || channelWidth >= hex.Width / 2f) throw new ArgumentOutOfRangeException(nameof(channelWidth));
            if (poolRadius <= channelWidth / 2f || poolRadius >= hex.Width / 2f) throw new ArgumentOutOfRangeException(nameof(poolRadius));
            if (arcSegments < 2) throw new ArgumentOutOfRangeException(nameof(arcSegments));
            if (poolSegments < 6) throw new ArgumentOutOfRangeException(nameof(poolSegments));

            WaterLevel = waterLevel;
            ChannelWidth = channelWidth;
            PoolRadius = poolRadius;
            ArcSegments = arcSegments;
            PoolSegments = poolSegments;
        }

        /// <summary>Altura hw de la lámina de agua de las celdas de agua y del cauce.</summary>
        public float WaterLevel { get; }

        /// <summary>Anchura w del cauce, igual en todas las piezas y giros.</summary>
        public float ChannelWidth { get; }

        /// <summary>Radio rp de la poza del manantial.</summary>
        public float PoolRadius { get; }

        /// <summary>Segmentos con que se discretiza el arco de 60° de la curva.</summary>
        public int ArcSegments { get; }

        /// <summary>Segmentos con que se discretiza la poza del manantial.</summary>
        public int PoolSegments { get; }
    }
}
