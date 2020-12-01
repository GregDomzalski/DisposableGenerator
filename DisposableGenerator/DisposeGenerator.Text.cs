using System.Text;
using Microsoft.CodeAnalysis;

namespace DisposableGenerator
{
    public partial class DisposeGenerator
    {
        private const string Indent = "    ";

        private static string EmitSource(GeneratorExecutionContext context, DisposeWork work)
        {
            StringBuilder sb = new StringBuilder();
            const string indentLevel = Indent;

            sb.AppendLine($"using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace {work.Symbol.ContainingNamespace}");
            sb.AppendLine($"{{");

            EmitClass(context, work, sb, indentLevel + Indent);

            sb.AppendLine($"}}");

            return sb.ToString();
        }

        private static void EmitClass(
            GeneratorExecutionContext context,
            DisposeWork work,
            StringBuilder sb,
            string indentLevel
        )
        {
            sb.AppendLine($"{indentLevel}{work.Symbol.DeclaredAccessibility} partial class {work.Symbol.Name}");
            sb.AppendLine($"{indentLevel}{{");
            sb.AppendLine($"{indentLevel}{Indent}private bool _isDisposed = false");

            EmitPublicDispose(context, work, sb, indentLevel + Indent);

            EmitPrivateDispose(context, work, sb, indentLevel + Indent);

            if (work.ImplementUnmanaged)
            {
                EmitFinalizer(context, work, sb, indentLevel + Indent);
            }

            sb.AppendLine($"{indentLevel}}}");
        }

        private static void EmitPublicDispose(
            GeneratorExecutionContext context,
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}public void Dispose()");
            sb.AppendLine($"{indentLevel}{{");

            EmitPublicDisposeImpl(context, work, sb, indentLevel + Indent);

            sb.AppendLine($"{indentLevel}}}");
        }

        private static void EmitPublicDisposeImpl(
            GeneratorExecutionContext context,
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}Dispose(true);");
            sb.AppendLine($"{indentLevel}GC.SuppressFinalize(this);");
        }

        private static void EmitPrivateDispose(
            GeneratorExecutionContext context,
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}private void Dispose(bool isDisposing)");
            sb.AppendLine($"{indentLevel}{{");

            EmitPrivateDisposeImpl(context, work, sb, indentLevel);

            sb.AppendLine($"{indentLevel}}}");
        }

        private static void EmitPrivateDisposeImpl(
            GeneratorExecutionContext context,
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
                foreach (var memberToDispose in work.DisposableMembers)
                {
                    sb.AppendLine($"{indentLevel}{Indent}{memberToDispose.Name}.Dispose();");
                }
            }

            sb.AppendLine($"{indentLevel}}}");

            sb.AppendLine();

            if (work.ImplementUnmanaged)
            {
                sb.AppendLine($"{indentLevel}DisposeUnmanaged();");
            }

            sb.AppendLine();

            sb.AppendLine($"{indentLevel}_isDisposed = true;");
        }

        private static void EmitFinalizer(
            GeneratorExecutionContext context,
            DisposeWork work,
            StringBuilder sb,
            string indentLevel)
        {
            sb.AppendLine($"{indentLevel}~{work.Symbol.Name}() => Dispose(false);");
        }
    }
}
