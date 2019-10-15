using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.SignalR
{
    class HubObservable<T> : IObservable<T>
    {
        string id;
        private Action<string, Type[], Delegate> addOrCreate;
        private Action<string, Type[], Delegate> removeOrDelete;

        public HubObservable(string id, Action<string, Type[], Delegate> addOrCreate, Action<string, Type[], Delegate> removeOrDelete)
        {
            this.id = id;
            this.addOrCreate = addOrCreate;
            this.removeOrDelete = removeOrDelete;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var func = (Func<T, Task>)((o) => { observer.OnNext(o); return Task.CompletedTask; });
            addOrCreate(id, new Type[] { typeof(T) }, func);
            return new Unsubscriber(() => UnSub(observer, func));
        }

        void UnSub(IObserver<T> observer, Delegate d)
        {
            removeOrDelete(id, new Type[] { typeof(T) }, d);
            observer.OnCompleted();
        }

        private class Unsubscriber : IDisposable
        {
            private Action _unsub;

            public Unsubscriber(Action unsub)
            {
                this._unsub = unsub;
            }

            public void Dispose()
            {
                _unsub?.Invoke();
            }
        }
    }
}
