using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Codeplex.Reactive;

namespace RxURGViewer
{
    public class MainWindowViewModel
    {
        public ReactiveProperty<Point[]> Points { get; set; }
        public ReactiveCommand StartCommand { get; set; }

        public MainWindowViewModel()
        {
            var source = new Subject<string>();
            var sensor = GetSensor(source);

            var messageObserver =
                Observable.Defer(() =>
                    // 空文字が現れるまで1つのリストにまとめる
                    sensor.TakeWhile(s => s != string.Empty).ToList())
                .Repeat();
            
            Points =
                messageObserver
                    .Select(Decode) // 受信データをデコード
                    .Select(xs => xs
                        .Select(PolarToCartesian) // 極座標から直交座標に変換
                        .Select(p => new Point(400.0 - (p.Y / 10.0), 300.0 - (p.X / 10.0))) // 描画用に座標変換
                        .ToArray())
                    .ToReactiveProperty();

            StartCommand = new ReactiveCommand();
            StartCommand.Subscribe(_ => source.OnNext(_data));
            
        }

        /// <summary>
        /// 計測データをデコードして距離データのリストに変換する
        /// </summary>
        /// <param name="messageList">計測データ</param>
        /// <returns>距離データのリスト[mm]</returns>
        public IEnumerable<int> Decode(IList<string> messageList)
        {
            // 最初の3行はエコーバック、エラーコード、タイムスタンプなので飛ばす。
            // 4行目以降は、チェックサム(最後の1文字)を取り除いて結合する。
            var data = string.Join("", messageList.Skip(3).Select(x => x.Remove(x.Length - 1)));

            var distance = Encoding.ASCII.GetBytes(data)
                .Buffer(3) // 3キャラエンコーディング方式なので、3つずつまとめる。
                .Select(xs => (xs[0] - 0x30) * 4096 + (xs[1] - 0x30) * 64 + (xs[2] - 0x30)); // 距離データに変換

            return distance;
        }

        const double StartAngle = -120.0; // センサの計測開始角度[deg]
        const double EndAngle = 120.0; // センサの計測終了角度[deg]
        const double Resolution = 240.0 / 682.0; // センサの角度分解能[deg]

        /// <summary>
        /// 極座標を直交座標に変換する
        /// </summary>
        /// <param name="r">距離[mm]</param>
        /// <param name="index">方向のインデックス</param>
        /// <returns>計測データの直交座標表現(x[mm],y[mm])</returns>
        public Point PolarToCartesian(int r, int index)
        {
            var degree = index * Resolution + StartAngle;

            var theta = degree * Math.PI / 180.0;

            var x = r * Math.Cos(theta);
            var y = r * Math.Sin(theta);

            return new Point(x, y);
        }

        static string _data = @"GD0044072501" + "\n" +
            @"00P" + "\n" +
            @"0DKO>" + "\n" +
            @"00i00i00i00i00k00k00n01101101101101101101100o00m00o00o0130130140]" + "\n" +
            @"14012012014015017017017016017017016016015015015014014014014015010" + "\n" +
            @"501801<01<01?01D01D01D01F01F01L01O01R01T01V01W01X01X01X01Z01Z01Ze" + "\n" +
            @"01\01b01j02;02`09H09H09Z09Z09_0:90:90:@0:@0:@0:;0:@0:;0:;0:90:90]" + "\n" +
            @"9Z08X08408408408608608608408408408408908908908908808608308008008V" + "\n" +
            @"007m07m07j07h07h07h07d06E04D04>04=04=04>04C04H04H04I04J04K04U04Ue" + "\n" +
            @"04X04X04X04W04W04W04W04[04]04_04`04`04h04l04l04n05005005305;05>0N" + "\n" +
            @"5D05F05J05M05Q05T05W05[05]05^05`05f05f05m05n065065065068065065060" + "\n" +
            @"906:06:06;06<06>06A06L06L06N06S06T06d07S07[09D0hH0hH0hH0gO0fk0fDV" + "\n" +
            @"0eg0eU0e@0db0db0db0000000000000000000000000a40`N0_o0_`0_G0_=0^a0^" + "\n" +
            @"^I0^<0]h0]W0]@0]00\X0\L0[l0[f0[S0[?0[00Zi0ZJ0ZC0Z70Z70Z70Z90Z90Z2" + "\n" +
            @"90Z40Z00XR0XR0XR0XR0XR0XO0XD0W]0VT0VT0VM0V;0Um0Uc0U]0UQ0UJ0UC0U9Y" + "\n" +
            @"0Te0Tc0T^0TK0T=0T70T60Sm0Sf0Sf0SR0SO0SD0S?0S70Rn0Rh0Rh0Rd0R]0RK0Y" + "\n" +
            @"RD0RD0R70R60R20Qo0Qb0Q^0Q\0QV0QL0QI0QH0QC0Q50Q40Q30Po0Pk0Pi0Pg0PD" + "\n" +
            @"a0P[0PR0PR0PQ0PI0PI0PG0PB0PB0P@0P?0P:0P90P00P00Oh0Od0Oc0Oc0O`0O_[" + "\n" +
            @"0O]0O]0OZ0OZ0OZ0OZ0OZ0O[0O[0Og0PO0PO0PO0PL0PL0PI0P90P90O_0OP0OP0k" + "\n" +
            @"OP0Od0P50P50P>0PG0PG0PC0PC0PC0Oa0OH0OH0OH0OJ0OK0OL0OK0OK0OL0OP0O1" + "\n" +
            @"Q0OQ0OQ0OR0OR0OT0OT0OU0OZ0OZ0O[0O[0O\0O]0Oc0Oc0Oc0Od0On0On0Oo0OoY" + "\n" +
            @"0P40P40P80P=0PC0PE0PE0PE0PN0PN0PP0PX0P`0Pb0Pg0Ph0Pm0Q90Q90Q90Q?0?" + "\n" +
            @"QC0QF0QI0QM0Q[0Qa0Qc0Qi0R20R20R=0RA0RG0RO0RR0RX0R]0Rj0S10S20S90ST" + "\n" +
            @"@0SJ0SP0SS0Sa0Sk0T80T:0T>0TI0TN0T]0Ta0Tl0U40U;0UN0UR0UV0Ul0V20V?5" + "\n" +
            @"0VC0VQ0Va0W30W50WH0Xg0Xn0Xn0Xm0Xm0Xm0Xm0Z30Z<0Zb0Zb0Zb0ZW0ZW0ZW0E" + "\n" +
            @"ZX0[20[50[S0[a0\;0\G0\V0\c0]=0]T0]a0^00^E0^[0^k0_J0_Y0`30`E0`Y0`2" + "\n" +
            @"g0aE0aW0al0bK0b\0c10cH0ck0d;0dS0dg0eF0ek0fE0f_0g?0g]0h;0iV0j`0jaW" + "\n" +
            @"0jc0lY0l]0la0le0m>0mn0n[0oQ10110i11512W0000070000000000000000000L" + "\n" +
            @"000000071?d1?d1Af1Af1B800000000000000000000000000000000000000000j" + "\n" +
            @"00000000000000000000000000000000000000000000000000000000000000000" + "\n" +
            @"00000000000000005@05905905905805304m04N03P03F03@02n03202i02b02Y0U" + "\n" +
            @"2:02101h01h01h01d01m01n01o02002002001i01d01d01d01l01l01l01l01l01m" + "\n" +
            @"l01o01o01o01o02102102102101k01k01k01k01k01h01_01S01Q01P01P01P01PW" + "\n" +
            @"01O01Q01O01N01N01N01N01M01M01G01I01H01G01H01H01G01E01A01>01=01=0J" + "\n" +
            @"1=01:0180170170160170180180180190190190190170140140140140140139" + "\n\n";

        private IObservable<string> GetSensor(ISubject<string> sensor)
        {

            var observer = sensor.Scan(Tuple.Create(new List<string>(), ""),
                (t, s) =>
                {
                    var source = String.Concat(t.Item2, s);
                    var items = source.Split('\n');
                    return Tuple.Create(items.Take(items.Length - 1).ToList(), items.Last());
                })
                .SelectMany(x => x.Item1);

            return observer;
        }
    }

}
