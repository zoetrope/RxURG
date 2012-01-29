using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace RxURGViewer
{
    class ObservableSerilaPort : IObservable<string>, IDisposable
    {
        private readonly SerialPort _serialPort;

        public ObservableSerilaPort(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8,StopBits stopBits = StopBits.One)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.Open();
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            if (observer == null) throw new ArgumentNullException("observer");

            // 受信イベントが発生したときの処理
            var rcvEvent = Observable.FromEventPattern<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                h => h.Invoke, h => _serialPort.DataReceived += h, h => _serialPort.DataReceived -= h)
                .Select(e =>
                {
                    if (e.EventArgs.EventType == SerialData.Eof)
                    {
                        observer.OnCompleted();
                        return string.Empty;
                    }
                    // 受信データを文字列に変換
                    var buf = new byte[_serialPort.BytesToRead];
                    _serialPort.Read(buf, 0, buf.Length);
                    return Encoding.ASCII.GetString(buf);
                })
                .Scan(Tuple.Create(new List<string>(), ""),
                      (t, s) =>
                      {
                          // 前回の残り t.Item2 と 今回の受信データ s を連結する。
                          var source = String.Concat(t.Item2, s);
                          
                          // 改行コードがついている分は Item1 に入れて、Observerに通知する。
                          // 改行コードがついていない分は Item2 に入れ、次回のデータ受信時に処理する。
                          var items = source.Split('\n');
                          return Tuple.Create(items.Take(items.Length - 1).ToList(), items.Last());
                      })
                .SelectMany(x => x.Item1) // Item1だけをObserverに通知する。
                .Subscribe(observer);

            // エラーイベントが発生したときの処理
            var errEvent = Observable.FromEventPattern<SerialErrorReceivedEventHandler, SerialErrorReceivedEventArgs>
                (h => _serialPort.ErrorReceived += h, h => _serialPort.ErrorReceived -= h)
                .Subscribe(e => observer.OnError(new Exception(e.EventArgs.EventType.ToString())));

            // Disposeが呼ばれたらイベント登録を解除する
            return Disposable.Create(() =>
            {
                rcvEvent.Dispose();
                errEvent.Dispose();
            });
        }

        public void Send(string text)
        {
            _serialPort.Write(text);
        }

        public void Dispose()
        {
            _serialPort.Close();
        }
    }
}
