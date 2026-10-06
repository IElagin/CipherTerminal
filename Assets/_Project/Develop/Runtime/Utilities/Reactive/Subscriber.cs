using System;

namespace Assets._Project.Develop.Runtime.Utilities.Reactive
{
    public class Subscriber : IDisposable
    {
        private readonly Action _action;
        private readonly Action<Subscriber> _onDispose;

        public Subscriber(Action action, Action<Subscriber> onDispose)
        {
            _action = action;
            _onDispose = onDispose;
        }

        public void Invoke() => _action.Invoke();

        public void Dispose() => _onDispose.Invoke(this);
    }

    public class Subscriber<T> : IDisposable
    {
        private readonly Action<T> _action;
        private readonly Action<Subscriber<T>> _onDispose;

        public Subscriber(Action<T> action, Action<Subscriber<T>> onDispose)
        {
            _action = action;
            _onDispose = onDispose;
        }

        public void Invoke(T value) => _action.Invoke(value);

        public void Dispose() => _onDispose.Invoke(this);
    }

    public class Subscriber<TPrevious, TCurrent> : IDisposable
    {
        private readonly Action<TPrevious, TCurrent> _action;
        private readonly Action<Subscriber<TPrevious, TCurrent>> _onDispose;

        public Subscriber(Action<TPrevious, TCurrent> action, Action<Subscriber<TPrevious, TCurrent>> onDispose)
        {
            _action = action;
            _onDispose = onDispose;
        }

        public void Invoke(TPrevious previousValue, TCurrent currentValue) => _action.Invoke(previousValue, currentValue);

        public void Dispose() => _onDispose.Invoke(this);
    }
}
