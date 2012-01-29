using System;
using System.IO.Ports;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RxURG
{
    public class ObservableSerialPort : IObservable<byte[]>, IDisposable
    {
        private readonly SerialPort _serialPort;

        public ObservableSerialPort(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.Open();
        }

public IDisposable Subscribe(IObserver<byte[]> observer)
{
    if (observer == null) throw new ArgumentNullException("observer");

    var disposable = new CompositeDisposable();

    // 受信イベントが発生したときの処理
    var received = _serialPort.DataReceivedAsObservable()
        .Subscribe(e => {
            if (e.EventType == SerialData.Eof)
            {
                observer.OnCompleted();
                disposable.Dispose();
            }
            else
            {
                var buf = new byte[_serialPort.BytesToRead];
                var len = _serialPort.Read(buf, 0, buf.Length);
                observer.OnNext(buf);
            }
        });


    // エラーイベントが発生したときの処理
    var error = _serialPort.ErrorReceivedAsObservable()
        .Subscribe(e => {
            observer.OnError(new Exception(e.EventType.ToString()));
            disposable.Dispose();
        });

    disposable.Add(received);
    disposable.Add(error);

    // Disposeが呼ばれたらイベント登録を解除する
    return disposable;
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
    public static class SerialPortExtensions
    {
        // 面倒くさいけれど単純なFromEventでのイベントのRx化
        public static IObservable<SerialDataReceivedEventArgs> DataReceivedAsObservable(this SerialPort serialPort)
        {
            return Observable.FromEvent<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                h => (sender, e) => h(e), h => serialPort.DataReceived += h, h => serialPort.DataReceived -= h);
        }

        public static IObservable<SerialErrorReceivedEventArgs> ErrorReceivedAsObservable(this SerialPort serialPort)
        {
            return Observable.FromEvent<SerialErrorReceivedEventHandler, SerialErrorReceivedEventArgs>(
                h => (sender, e) => h(e), h => serialPort.ErrorReceived += h, h => serialPort.ErrorReceived -= h);
        }

        // DataReceived(プラスbyte[]化)とErrorReceivedを合成する
        public static IObservable<byte[]> ObserveReceiveBytes(this SerialPort serialPort)
        {
            var received = serialPort.DataReceivedAsObservable()
                .TakeWhile(e => e.EventType != SerialData.Eof) // これでOnCompletedを出す
                .Select(e =>
                {
                    var buf = new byte[serialPort.BytesToRead];
                    serialPort.Read(buf, 0, buf.Length);
                    return buf;
                });

            var error = serialPort.ErrorReceivedAsObservable()
                .Take(1) // 届いたらすぐに例外だすので長さ1として扱う（どうせthrowするなら関係ないけど一応）
                .Do(x => { throw new Exception(x.EventType.ToString()); });

            return received.TakeUntil(error); // receivedが完了した時に同時にerrorをデタッチする必要があるのでMergeではダメ
        }
    }

}
