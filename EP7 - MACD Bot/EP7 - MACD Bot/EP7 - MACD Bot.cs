using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EP7MACDBot : Robot
    {
        [Parameter(DefaultValue = 0.0)]
        public double Parameter { get; set; }

        //Create indicator variables
        private AverageTrueRange atr;
        private MacdCrossOver macd;

        protected override void OnStart()
        {
            //Load indicators on start up
            atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            macd = Indicators.MacdCrossOver(26, 7, 9);
        }

        protected override void OnBar()
        {
            //Calculate Trade amount based on ATR
            var PrevATR = Math.Round(atr.Result.Last(1) / Symbol.PipSize);
            var TradeAmount = (Account.Equity * 0.02) / (1.5 * PrevATR * Symbol.PipValue);
            TradeAmount = Symbol.NormalizeVolumeInUnits(TradeAmount, RoundingMode.Down);

            //Two line cross example
            var MACDline = macd.MACD.Last(1);
            var PrevMACDline = macd.MACD.Last(2);
            var Signal = macd.Signal.Last(1);
            var PrevSignal = macd.Signal.Last(2);

            //Check for trade signal
            if (MACDline > Signal && PrevMACDline < PrevSignal) // && MACDline > 0 && Signal > 0)
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, TradeAmount, "MACD Cross", 1.5 * PrevATR, PrevATR);
            }
            else if (MACDline < Signal && PrevMACDline > PrevSignal) // && MACDline < 0 && Signal < 0)
            {
                ExecuteMarketOrder(TradeType.Sell, SymbolName, TradeAmount, "MACD Cross", 1.5 * PrevATR, PrevATR);
            }

            /*
            //Zero line cross example
            var Histogram = macd.Histogram.Last(1);
            var PrevHistogram = macd.Histogram.Last(2);
            if (Histogram > 0 && PrevHistogram < 0)
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, TradeAmount, "MACD Histogram", 1.5 * PrevATR, PrevATR);
            }
            else if (Histogram < 0 && PrevHistogram > 0)
            {
                ExecuteMarketOrder(TradeType.Sell, SymbolName, TradeAmount, "MACD Histogram", 1.5 * PrevATR, PrevATR);
            }
            */

        }




        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
    
    public enum StrategyType{
        
        
    }
}
