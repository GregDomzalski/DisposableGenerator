using System;

namespace DisposableGenerator.Examples
{
    public partial class DisposableObserver : IDisposable
    {
        public string Name { get; set; }

        public DisposableObserver(string name)
        {
            Name = name;
        }

        private void DisposeManaged()
        {
            Console.WriteLine($"DisposableObserver[{Name}].DisposeManaged() called!");
        }

        private void DisposeUnmanaged()
        {
            Console.WriteLine($"DisposableObserver[{Name}].DisposeUnmanaged() called!");
        }
    }

    public partial class Example1 : IDisposable
    {
        private DisposableObserver _disposableMember1;
        private DisposableObserver _disposableMember2;

        public Example1()
        {
            _disposableMember1 = new DisposableObserver(nameof(_disposableMember1));
            _disposableMember2 = new DisposableObserver(nameof(_disposableMember2));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (var observer = new DisposableObserver("TestObserver"))
            {
                Console.WriteLine("TestObserver:");
            }

            using (var example1 = new Example1())
            {
                Console.WriteLine("Example1:");
            }
        }
    }
}
