using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>data-model.md → RiverShape, RiverPiece.OpenEdges y RiverPiece.Canonical (research.md R-3).</summary>
    public sealed class RiverPieceTests
    {
        private static int[] Open(RiverShape shape, int rotation) =>
            RiverPiece.OpenEdges(shape, rotation).Select(edge => (int)edge).OrderBy(edge => edge).ToArray();

        [Test]
        public void ShapesHaveTheirDefinedValues()
        {
            Assert.That((int)RiverShape.None, Is.EqualTo(0));
            Assert.That((int)RiverShape.Spring, Is.EqualTo(1));
            Assert.That((int)RiverShape.Straight, Is.EqualTo(2));
            Assert.That((int)RiverShape.Curve, Is.EqualTo(3));
        }

        [Test]
        public void RotationZero_OpensTheBaseEdges()
        {
            Assert.That(Open(RiverShape.Spring, 0), Is.EqualTo(new[] { 0 }));
            Assert.That(Open(RiverShape.Straight, 0), Is.EqualTo(new[] { 0, 3 }));
            Assert.That(Open(RiverShape.Curve, 0), Is.EqualTo(new[] { 0, 2 }));
        }

        [Test]
        public void EveryRotation_AddsItsStepsToEachBaseEdge([Values(0, 1, 2, 3, 4, 5)] int rotation)
        {
            Assert.That(Open(RiverShape.Spring, rotation), Is.EqualTo(new[] { rotation }));
            Assert.That(Open(RiverShape.Straight, rotation), Is.EqualTo(new[] { rotation % 3, rotation % 3 + 3 }));
            Assert.That(Open(RiverShape.Curve, rotation), Is.EqualTo(new[] { rotation, (rotation + 2) % 6 }.OrderBy(e => e).ToArray()));
        }

        [Test]
        public void Curve_OpensTwoEdgesOneApart([Values(0, 1, 2, 3, 4, 5)] int rotation)
        {
            var edges = Open(RiverShape.Curve, rotation);
            var gap = (edges[1] - edges[0] + 6) % 6;
            Assert.That(gap == 2 || gap == 4, Is.True, "Curva suave (120°), nunca contigua ni opuesta.");
        }

        [Test]
        public void Straight_WithRotationAndRotationPlusThree_OpensTheSameEdges([Values(0, 1, 2)] int rotation)
        {
            Assert.That(Open(RiverShape.Straight, rotation + 3), Is.EqualTo(Open(RiverShape.Straight, rotation)));
        }

        [Test]
        public void Canonical_ReducesOnlyTheStraight([Values(0, 1, 2, 3, 4, 5)] int rotation)
        {
            Assert.That(RiverPiece.Canonical(RiverShape.Straight, rotation), Is.EqualTo(rotation % 3));
            Assert.That(RiverPiece.Canonical(RiverShape.Spring, rotation), Is.EqualTo(rotation));
            Assert.That(RiverPiece.Canonical(RiverShape.Curve, rotation), Is.EqualTo(rotation));
        }

        [Test]
        public void None_OpensNoEdges()
        {
            Assert.That(RiverPiece.OpenEdges(RiverShape.None, 0), Is.Empty);
        }

        [Test]
        public void CanonicalKeys_AreFifteen()
        {
            Assert.That(RiverPiece.CanonicalKeys().Count(), Is.EqualTo(15));
        }
    }
}
