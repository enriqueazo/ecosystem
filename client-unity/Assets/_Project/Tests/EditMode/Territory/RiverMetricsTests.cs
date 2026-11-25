using System;
using Ecosystem.Territory;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → RiverMetrics: valores de partida y restricciones respecto a la pieza
    /// (0 &lt; hw &lt; h, 0 &lt; w &lt; W/2 y w/2 &lt; rp &lt; W/2).
    /// </summary>
    public sealed class RiverMetricsTests
    {
        private static readonly HexMetrics Hex = HexMetrics.Default;

        [Test]
        public void Default_HasTheApprovedStartingValues()
        {
            var metrics = RiverMetrics.Default;

            Assert.That(metrics.WaterLevel, Is.EqualTo(0.25f));
            Assert.That(metrics.ChannelWidth, Is.EqualTo(0.26f));
            Assert.That(metrics.PoolRadius, Is.EqualTo(0.17f));
            Assert.That(metrics.ArcSegments, Is.EqualTo(8));
            Assert.That(metrics.PoolSegments, Is.EqualTo(16));
        }

        [Test]
        public void Default_RespectsThePieceConstraints()
        {
            var metrics = RiverMetrics.Default;

            Assert.That(metrics.WaterLevel, Is.GreaterThan(0f).And.LessThan(Hex.Height));
            Assert.That(metrics.ChannelWidth, Is.GreaterThan(0f).And.LessThan(Hex.Width / 2f));
            Assert.That(metrics.PoolRadius, Is.GreaterThan(metrics.ChannelWidth / 2f).And.LessThan(Hex.Width / 2f));
        }

        [TestCase(0f, 0.26f, 0.17f)]
        [TestCase(0.35f, 0.26f, 0.17f)]
        [TestCase(0.25f, 0f, 0.17f)]
        [TestCase(0.25f, 0.44f, 0.30f)]
        [TestCase(0.25f, 0.26f, 0.13f)]
        [TestCase(0.25f, 0.26f, 0.44f)]
        public void Constructor_RejectsValuesOutOfRange(float waterLevel, float channelWidth, float poolRadius)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RiverMetrics(Hex, waterLevel, channelWidth, poolRadius, 8, 16));
        }

        [TestCase(1, 16)]
        [TestCase(8, 2)]
        public void Constructor_RejectsTooFewSegments(int arcSegments, int poolSegments)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RiverMetrics(Hex, 0.25f, 0.26f, 0.17f, arcSegments, poolSegments));
        }
    }
}
