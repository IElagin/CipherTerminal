using System;

namespace Assets._Project.Develop.Runtime.Utilities.Reactive
{
    public interface IReadOnlyVariable<T>
    {
        public T Value { get; }

        public IDisposable Subscribe(Action<T, T> action);
    }
}
