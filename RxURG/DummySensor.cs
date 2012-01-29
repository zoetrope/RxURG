using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace RxURG
{
    class DummySensor : IObservable<byte>
    {
        
        public IDisposable Subscribe(IObserver<byte> observer)
        {
            var disposable = Observable.FromEvent<Action<byte>, byte>(
                h => DataReceived += h, h => DataReceived -= h)
                .Subscribe(observer.OnNext);

            return Disposable.Create(() => {
                //Console.WriteLine("DummySensor Disposable");
                disposable.Dispose();
            });
        }

        public event Action<byte> DataReceived;

        public void Notify(byte data)
        {
            var handler = DataReceived;

            if (handler != null)
            {
                handler(data);
            }
        }

    }
}
