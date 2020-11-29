using System.Text;

namespace DisposableGenerator
{
    public partial class DisposeGenerator
    {
        private static string EmitDisposeImpl(DisposeWork work)
        {
            StringBuilder sb = new StringBuilder();

            if (work.ImplementUnmanaged) sb.Append(EmitFinalizer(work.ClassName));

            sb.Append(EmitDisposeBegin());

            if (work.ImplementManaged) sb.Append(EmitManagedCall());
            if (work.ImplementUnmanaged) sb.Append(EmitUnmanagedCall());

            sb.Append(EmitDisposeEnd());

            return sb.ToString();
        }

        //
        // using System;
        // namespace {namespaceName}
        // public partial class {ClassName}
        //
        public static string EmitHeader(string namespaceName, string className) =>
            $@"
using System;

namespace {namespaceName}
{{
    public partial class {className}
    {{
";

        //
        // close class
        // close namespace
        //
        private static string EmitFooter() =>
            @"
    }
}
";

        //
        // public void Dispose()
        //
        private static string EmitDisposeStub() =>
            @"
        public void Dispose()
        {
            // Nothing to dispose. Implement a DisposeManaged and/or DisposeUnmanaged method.
        }
";

        //
        // ~{ClassName}
        //
        private static string EmitFinalizer(string className) =>
            $@"
        ~{className}() => Dispose(false);
";

        //
        // private bool _disposed
        // public void Dispose()
        // private void Dispose(bool)
        //
        private static string EmitDisposeBegin() =>
            @"
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
";

        private static string EmitManagedCall() => @"
            if (disposing)
            {
                DisposeManaged();
            }
";

        private static string EmitUnmanagedCall() => @"
            DisposeUnmanaged();
";

        //
        // close Dispose()
        //
        private static string EmitDisposeEnd() => @"
            _disposed = true;
        }
";
    }
}
