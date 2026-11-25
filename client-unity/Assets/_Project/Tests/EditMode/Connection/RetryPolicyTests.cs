using System;
using System.Linq;
using Ecosystem.Connection;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Connection
{
    /// <summary>data-model.md → Política de reintentos (FR-015, SC-003).</summary>
    public sealed class RetryPolicyTests
    {
        [Test]
        public void ThreeRetries_WithBaseDelaysOf1_2_And4Seconds()
        {
            Assert.That(RetryPolicy.MaxRetries, Is.EqualTo(3));
            Assert.That(RetryPolicy.BaseDelays, Is.EqualTo(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) }));
        }

        [Test]
        public void Delays_StayWithinTwentyPercentOfTheBase()
        {
            var random = new Random(42);
            for (var sample = 0; sample < 500; sample++)
            {
                for (var retry = 0; retry < RetryPolicy.MaxRetries; retry++)
                {
                    var delay = RetryPolicy.DelayFor(retry, random).TotalSeconds;
                    var baseSeconds = RetryPolicy.BaseDelays[retry].TotalSeconds;
                    Assert.That(delay, Is.InRange(baseSeconds * 0.8, baseSeconds * 1.2));
                }
            }
        }

        [Test]
        public void WorstCase_UntilOffline_IsAtMostSixtySeconds()
        {
            var worst = RetryPolicy.WorstCase(TimeSpan.FromSeconds(10));

            Assert.That(worst.TotalSeconds, Is.LessThanOrEqualTo(60));
            Assert.That(worst.TotalSeconds, Is.EqualTo(4 * 10 + (1 + 2 + 4) * 1.2).Within(0.001));
        }

        [Test]
        public void OnlyTransientFailures_AreRetried()
        {
            var retryable = Enum.GetValues(typeof(FailureKind)).Cast<FailureKind>().Where(RetryPolicy.IsRetryable);

            Assert.That(retryable, Is.EqualTo(new[] { FailureKind.Transient }));
        }
    }
}
