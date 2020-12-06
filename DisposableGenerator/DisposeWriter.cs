using System.Text;

namespace DisposableGenerator
{
    public class DisposeWriter
    {
        private const string Indent = "    ";
        private readonly DisposeWork _work;

        public DisposeWriter(DisposeWork work)
        {
            _work = work;
        }

        public string Emit() => EmitSource(_work);

        private static string EmitSource(DisposeWork work)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace {work.NamespaceName}");
            sb.AppendLine($"{{");

            EmitClass(work, sb, Indent);

            sb.AppendLine($"}}");

            return sb.ToString();
        }

        private static void EmitClass(
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            var declaredAccessibility = work.DeclaredAccessibility;
            declaredAccessibility += work.DeclaredAccessibility.Length > 0 ? " " : "";

            sb.AppendLine($"{indentLevel}{declaredAccessibility}partial class {work.ClassName}");
            sb.AppendLine($"{indentLevel}{{");

            if (work.HasWork)
            {
                sb.AppendLine($"{indentLevel}{Indent}private bool _isDisposed = false;");
                sb.AppendLine();
            }

            EmitPublicDispose(work, sb, indentLevel + Indent);

            if (work.HasWork)
            {
                sb.AppendLine();
                EmitPrivateDispose(work, sb, indentLevel + Indent);
            }

            if (work.ImplementUnmanaged)
            {
                sb.AppendLine();
                EmitFinalizer(work, sb, indentLevel + Indent);
            }

            sb.AppendLine($"{indentLevel}}}");
        }

        private static void EmitPublicDispose(
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}public void Dispose()");
            sb.AppendLine($"{indentLevel}{{");

            if (work.HasWork)
            {
                EmitPublicDisposeImpl(work, sb, indentLevel + Indent);
            }

            sb.AppendLine($"{indentLevel}}}");
        }

        private static void EmitPublicDisposeImpl(
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}Dispose(true);");
            sb.AppendLine($"{indentLevel}GC.SuppressFinalize(this);");
        }

        private static void EmitPrivateDispose(
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}private void Dispose(bool isDisposing)");
            sb.AppendLine($"{indentLevel}{{");

            EmitPrivateDisposeImpl(work, sb, indentLevel + Indent);

            sb.AppendLine($"{indentLevel}}}");
        }

        private static void EmitPrivateDisposeImpl(
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}if (_isDisposed)");
            sb.AppendLine($"{indentLevel}{{");
            sb.AppendLine($"{indentLevel}{Indent}return;");
            sb.AppendLine($"{indentLevel}}}");

            sb.AppendLine();

            sb.AppendLine($"{indentLevel}if (disposing)");
            sb.AppendLine($"{indentLevel}{{");

            if (work.ImplementManaged)
            {
                sb.AppendLine($"{indentLevel}{Indent}DisposeManaged();");
            }
            else
            {
                // Dispose each disposable
                foreach (var memberToDispose in work.DisposableMemberNames)
                {
                    sb.AppendLine($"{indentLevel}{Indent}{memberToDispose}.Dispose();");
                }
            }

            sb.AppendLine($"{indentLevel}}}");
            sb.AppendLine();

            if (work.ImplementUnmanaged)
            {
                sb.AppendLine($"{indentLevel}DisposeUnmanaged();");
                sb.AppendLine();
            }

            sb.AppendLine($"{indentLevel}_isDisposed = true;");
        }

        private static void EmitFinalizer(
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}~{work.ClassName}() => Dispose(false);");
        }
    }
}
