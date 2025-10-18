using NUnit.Framework;

namespace QaBugsForm.Tests
{
    [TestFixture]
    public class SmokeTests
    {
        [Test]
        public void OnePlusOneIsTwo()
        {
            Assert.That(1 + 1, Is.EqualTo(2));
        }
    }
}
