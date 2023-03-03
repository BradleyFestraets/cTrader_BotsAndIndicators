using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ZBreakoutScalpingStratBBRSIcBot : Robot
    {
        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Band Height (pips)", DefaultValue = 40.0, MinValue = 1, MaxValue = 50, Step = 1)]
        public double BandHeightPips { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 15)]
        public int StopLossInPips { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 20)]
        public int TakeProfitInPips { get; set; }

        [Parameter("Quantity (Lots)", DefaultValue = 0.01, MinValue = 0.01, MaxValue = 0.8, Step = 0.1)]
        public double Quantity { get; set; }

        [Parameter("Bollinger Bands Deviations", DefaultValue = 2, MinValue = 1, MaxValue = 3, Step = 1)]
        public double Deviations { get; set; }

        [Parameter("Bollinger Bands Periods", DefaultValue = 20, MinValue = 10, MaxValue = 30, Step = 1)]
        public int Periods { get; set; }

        [Parameter("Bollinger Bands MA Type")]
        public MovingAverageType MAType { get; set; }

        [Parameter("Consolidation Periods", DefaultValue = 0)]
        public int ConsolidationPeriods { get; set; }

        BollingerBands bollingerBands;
        private const string label = "ZBreakoutScalpingStratBBRSIcBot";
        int consolidation;

        protected override void OnStart()
        {
            bollingerBands = Indicators.BollingerBands(Source, Periods, Deviations, MAType);
        }

        protected override void OnBar()
        {

            var top = bollingerBands.Top.Last(1);
            var bottom = bollingerBands.Bottom.Last(1);

            if (top - bottom <= BandHeightPips * Symbol.PipSize)
            {
                consolidation = consolidation + 1;
            }
            else
            {
                consolidation = 0;
            }

            if (consolidation >= ConsolidationPeriods)
            {
                var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);

                // Get 200 EMA value
                var ema200 = Indicators.ExponentialMovingAverage(Source, 200);

                // Get RSI value
                var rsi = Indicators.RelativeStrengthIndex(Source, 14);

                if (Symbol.Ask > ema200.Result.Last(1) && rsi.Result.Last(1) < 35)
                {
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, "ZBreakoutScalpingStratBBRSIcBot", StopLossInPips, TakeProfitInPips);

                    consolidation = 0;
                }
                else if (Symbol.Bid < ema200.Result.Last(1) && rsi.Result.Last(1) > 65)
                {
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, "ZBreakoutScalpingStratBBRSIcBot", StopLossInPips, TakeProfitInPips);

                    consolidation = 0;
                }
            }
        }
    }
}
