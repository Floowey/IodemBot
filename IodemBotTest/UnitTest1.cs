using Xunit;

namespace IodemBotTest
{
    public class UnitTest1
    {
        [Fact]
        public void TestWorks()
        {
            Assert.Equal(1, 1);
        }

        [Fact]
        public void TestFails()
        {
            Assert.Equal(1, 2);
        }
    }
}