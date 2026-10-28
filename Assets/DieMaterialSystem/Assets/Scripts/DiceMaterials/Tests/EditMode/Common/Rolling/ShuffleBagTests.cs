using System.Collections.Generic;
using NUnit.Framework;

namespace DiceMaterials.Tests
{
    public class ShuffleBagTests
    {
        [Test]
        public void EachCycle_ContainsEveryItemExactlyOnce()
        {
            var bag = new ShuffleBag<int>(new[] { 1, 2, 3, 4, 5 }, 7);
            for (int cycle = 0; cycle < 4; cycle++)
            {
                var seen = new HashSet<int>();
                for (int i = 0; i < 5; i++) seen.Add(bag.Next());
                Assert.AreEqual(5, seen.Count);
            }
        }
    }
}
