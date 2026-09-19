using System;

namespace Assets._Project.Develop.Runtime.Utilities.Reactive
{
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
