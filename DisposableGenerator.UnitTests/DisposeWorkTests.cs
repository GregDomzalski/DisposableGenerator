using Xunit;

namespace DisposableGenerator.UnitTests
{
    public class DisposeWorkTests
    {
        [Fact]
        public void HasWork_DefaultClass_ReturnsFalse()
        {
            var work = new DisposeWork();

            Assert.False(work.HasWork);
        }

        [Fact]
        public void HasWork_ImplementManagedTrue_ReturnsTrue()
        {
            var work = new DisposeWork()
            {
                ImplementManaged = true
            };

            Assert.True(work.HasWork);
        }

        [Fact]
        public void HasWork_ImplementsUnmanagedTrue_ReturnsTrue()
        {
            var work = new DisposeWork()
            {
                ImplementUnmanaged = true
            };

            Assert.True(work.HasWork);
        }

        [Fact]
        public void HasWork_NonZeroDisposableMembers_ReturnsTrue()
        {
            var work = new DisposeWork()
            {
                DisposableMemberNames = new [] { "Test1", "Test2" }
            };

            Assert.True(work.HasWork);
        }
    }
}