using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.EasternStandardTime, AccessRights = AccessRights.None)]
    public class EP11Baseline : Robot
    {
        [Parameter("Risk %", DefaultValue = 0.02)]
        public double RiskPct { get; set; }

        [Parameter("Baseline Period", DefaultValue = 28)]
        public int BaselinePeriod { get; set; }

        [Parameter("Baseline MAType", DefaultValue = MovingAverageType.TimeSeries)]
        public MovingAverageType BaselineMAType { get; set; }

        //Create indicator variables
        private AverageTrueRange atr;
        private MovingAverage baseline;

        protected override void OnStart()
        {
            //Load indicators on start up
            atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            baseline = Indicators.MovingAverage(Bars.ClosePrices, BaselinePeriod, BaselineMAType);
        }

        protected override void OnTick()
        {

            var Baseline = baseline.Result.Last(0);
            var Last_Price = Bars.ClosePrices.Last(0);

            if (Last_Price > Baseline)
            {
                Open("Baseline", TradeType.Buy);

            }
            else if (Last_Price < Baseline)
            {
                Open("Baseline", TradeType.Sell);
            }


        }

        private void Open(String Label, TradeType TradeDirection)
        {
            //Calculate Trade amount based on ATR
            var PrevATR = Math.Round(atr.Result.Last(1) / Symbol.PipSize);
            var TradeAmount = (Account.Equity * RiskPct) / (1.5 * PrevATR * Symbol.PipValue);
            TradeAmount = Symbol.NormalizeVolumeInUnits(TradeAmount, RoundingMode.Down);
            if (Server.Time.Hour == 16 && Server.Time.Minute == 29 && Positions.Count == 0)
            {
                ExecuteMarketOrder(TradeDirection, SymbolName, TradeAmount, Label, 1.5 * PrevATR, PrevATR);
            }
        }

        private void Close(String Label, TradeType TradeDirection)
        {
            foreach (var position in Positions.FindAll(Label, SymbolName, TradeDirection))
            {
                ClosePosition(position);
            }
        }



        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}