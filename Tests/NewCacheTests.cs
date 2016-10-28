using Xunit;
using FluentAssertions;

namespace VerySimple
{
    public class NewCacheTests
    {
        [Fact]
        public void GivenNoSessionExistsInDbOrCacheThenNullIsReturned()
        {
            var cache = new NewCache();

            cache
                .Get("foo")
                .Should()
                .BeNull();
        }
    }
}