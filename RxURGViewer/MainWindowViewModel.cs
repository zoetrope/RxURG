//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using System.Text;
//using System.Threading;
//using System.Windows;
//using Codeplex.Reactive;
//
//namespace RxURGViewer
//{
//    public class MainWindowViewModel
//    {
//        public ReactiveProperty<Point[]> Points { get; set; }
//        public ReactiveCommand StartCommand { get; set; }
//
//        public MainWindowViewModel()
//        {
//            var sensor = new ObservableSerilaPort("COM16");
//
//            var messageObserver =
//                Observable.Defer(() =>
//                    // 空文字が現れるまで1つのリストにまとめる
//                    sensor.TakeWhile(s => s != string.Empty)
//                        .Aggregate(new List<string>(), (l, s) =>
//                        {
//                            l.Add(s);
//                            return l;
//                        })
//                ).Repeat();
//
//
//            Points =
//                messageObserver
//                    .Where(xs => xs[0].StartsWith("MD"))
//                    .Select(Decode) // 受信データをデコード
//                    .Select(xs => xs
//                        .Select(PolarToCartesian) // 極座標から直交座標に変換
//                        .Select(p => new Point(400.0 - (p.Y / 10.0), 300.0 - (p.X / 10.0))) // 描画用に座標変換
//                        .ToArray())
//                    .ToReactiveProperty();
//
//            StartCommand = new ReactiveCommand();
//            StartCommand.Subscribe(_ => sensor.Send("MD0044072501000\n"));
//
//        }
//
//        /// <summary>
//        /// 計測データをデコードして距離データのリストに変換する
//        /// </summary>
//        /// <param name="message">計測データ</param>
//        /// <returns>距離データのリスト[mm]</returns>
//        public IEnumerable<int> Decode(List<string> message)
//        {
//            // 最初の3行はエコーバック、エラーコード、タイムスタンプなので飛ばす。
//            // 4行目以降は、チェックサム(最後の1文字)を取り除いて結合する。
//            var data = string.Join("", message.Skip(3).Select(x => x.Remove(x.Length - 1)));
//
//            var distance = Encoding.ASCII.GetBytes(data)
//                .Buffer(3) // 3キャラエンコーディング方式なので、3つずつまとめる。
//                .Where(xs=>xs.Count == 3)
//                .Select(xs => (xs[0] - 0x30) * 4096 + (xs[1] - 0x30) * 64 + (xs[2] - 0x30)); // 距離データに変換
//
//            return distance;
//        }
//
//        const double StartAngle = -120.0; // センサの計測開始角度[deg]
//        const double EndAngle = 120.0; // センサの計測終了角度[deg]
//        const double Resolution = 240.0 / 682.0; // センサの角度分解能[deg]
//
//        /// <summary>
//        /// 極座標を直交座標に変換する
//        /// </summary>
//        /// <param name="r">距離[mm]</param>
//        /// <param name="index">方向のインデックス</param>
//        /// <returns>計測データの直交座標表現(x[mm],y[mm])</returns>
//        public Point PolarToCartesian(int r, int index)
//        {
//            var degree = index * Resolution + StartAngle;
//
//            var theta = degree * Math.PI / 180.0;
//
//            var x = r * Math.Cos(theta);
//            var y = r * Math.Sin(theta);
//
//            return new Point(x, y);
//        }
//    }
//}
