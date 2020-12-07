using Xunit;

namespace DisposableGenerator.UnitTests
{
    public class WriterTests
    {
        [Fact]
        public void Emit_NoWork_EmitsDisposeStub()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        public void Dispose()
        {
        }
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass"
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("")]
        public void Emit_ClassAccessibility_EmitsDisposeStub(string declaredAccessibility)
        {
            // Arrange
            var space = declaredAccessibility.Length > 0 ? " " : "";
            var expectedText =
                $@"using System;

namespace TestNamespace
{{
    {declaredAccessibility}{space}partial class TestClass
    {{
        public void Dispose()
        {{
        }}
    }}
}}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                DeclaredAccessibility = declaredAccessibility
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void Emit_OneAutoDisposedMember_EmitsManagedDisposeOfMembers()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                Member1.Dispose();
            }

            _isDisposed = true;
        }
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                DisposableMemberNames = new [] { "Member1" }
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void Emit_MultipleAutoDisposedMembers_EmitsManagedDisposeOfMembers()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                Member1.Dispose();
                Member2.Dispose();
                Member3.Dispose();
            }

            _isDisposed = true;
        }
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                DisposableMemberNames = new[] { "Member1", "Member2", "Member3" }
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void Emit_ImplementedDisposeManaged_CallsDisposeManaged()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                DisposeManaged();
            }

            _isDisposed = true;
        }
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                ImplementManaged = true
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void Emit_ExplicitDisposeManaged_SuppressesAutoDisposal()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                DisposeManaged();
            }

            _isDisposed = true;
        }
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                DisposableMemberNames = new[] { "Member1", "Member2", "Member3" },
                ImplementManaged = true
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void Emit_ExplicitDisposeUnmanaged_CallsDisposeUnmanaged()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
            }

            DisposeUnmanaged();

            _isDisposed = true;
        }

        ~TestClass() => Dispose(false);
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                ImplementUnmanaged = true
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void Emit_ExplicitDisposeUnmanaged_ImplementsFinalizer()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
            }

            DisposeUnmanaged();

            _isDisposed = true;
        }

        ~TestClass() => Dispose(false);
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                ImplementUnmanaged = true
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void Emit_BothExplicitDisposers_CallsBothDisposers()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                DisposeManaged();
            }

            DisposeUnmanaged();

            _isDisposed = true;
        }

        ~TestClass() => Dispose(false);
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                ImplementManaged = true,
                ImplementUnmanaged = true
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void Emit_DisposableMembersWithUnmanaged_AutoDisposesAndCallsUnmanaged()
        {
            // Arrange
            var expectedText =
                @"using System;

namespace TestNamespace
{
    partial class TestClass
    {
        private bool _isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                Disposable1.Dispose();
                Disposable2.Dispose();
            }

            DisposeUnmanaged();

            _isDisposed = true;
        }

        ~TestClass() => Dispose(false);
    }
}
";

            DisposeWork work = new DisposeWork
            {
                NamespaceName = "TestNamespace",
                ClassName = "TestClass",
                ImplementUnmanaged = true,
                DisposableMemberNames = new [] { "Disposable1", "Disposable2" }
            };

            var writer = new Writer(work);

            // Act
            var actualText = writer.Emit();

            // Assert
            Assert.Equal(expectedText, actualText);
        }
    }
}
