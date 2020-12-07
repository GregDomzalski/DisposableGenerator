using System.Collections.Generic;
using System.Linq;

namespace DisposableGenerator
{
    public class DisposeWork
    {
        public string NamespaceName;
        public string ClassName;
        public string DeclaredAccessibility;

        public IEnumerable<string> DisposableMemberNames;

        public bool ImplementManaged;
        public bool ImplementUnmanaged;

        public bool HasWork => ImplementUnmanaged || ImplementManaged || DisposableMemberNames.Any();

        public DisposeWork()
        {
            NamespaceName = string.Empty;
            ClassName = string.Empty;
            DeclaredAccessibility = string.Empty;
            DisposableMemberNames = Enumerable.Empty<string>();
        }
    }
}