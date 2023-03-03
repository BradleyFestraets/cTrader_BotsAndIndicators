using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    //Set time zone to Eastern Standard Time EP9-Best time to trade
    [Robot(TimeZone = TimeZones.EasternStandardTime, AccessRights = AccessRights.None)]
    public class EP12NNFXBot : Robot
    {
        //Create Parameters EP10-Functions and Parameters
        [Parameter("Risk %", DefaultValue = 0.02)]
        public double RiskPct { get; set; }
        [Parameter("Baseline period", DefaultValue = 20)]
        public int BaselinePeriod { get; set; }
        [Parameter("Baseline MA type", DefaultValue = MovingAverageType.Hull)]
        public MovingAverageType BaselineMAType{ get; set; }

        //Create indicator variables EP5-ATR
        private AverageTrueRange atr;
        private MovingAverage hma;
        private Aroon aroon;
        private SSLChannel ssl;
        private ChaikinMoneyFlow chaikin;
        private MovingAverage chaikinMA;

        protected override void OnStart()
        {
            //Load indicators on start up EP5-ATR
            atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            hma = Indicators.MovingAverage(Bars.ClosePrices, BaselinePeriod, BaselineMAType);
            aroon = Indicators.Aroon(25);
            ssl = Indicators.GetIndicator<SSLChannel>(10, MovingAverageType.Simple);
            chaikin = Indicators.ChaikinMoneyFlow(14);
            chaikinMA = Indicators.MovingAverage(chaikin.Result, 3, MovingAverageType.Exponential);

        }

        protected override void OnTick()
        {
            // Put your core logic here EP7-MACD and EP8-Custom Indicators
            var C1 = aroon.Up.Last(0) - aroon.Down.Last(0);
            var PrevC1 = aroon.Up.Last(1) - aroon.Down.Last(1);
            var C2 = ssl._sslUp.Last(0) - ssl._sslDown.Last(0);
            var Volume = chaikinMA.Result.Last(0);
            var Baseline = hma.Result.Last(0);

            //Check Entry Signal
            if (C1 > 0 && PrevC1 < 0 && C2 > 0 && Volume > 0 && Symbol.Bid > Baseline)
            {
                Open(TradeType.Buy, "NNFX Long");
            }
            else if (C1 < 0 && PrevC1 > 0 && C2 < 0 && Volume < 0 && Symbol.Bid < Baseline)
            {
                Open(TradeType.Sell, "NNFX Short");
            }

            //Check Exit Indicator
            if (C2 > 0)
            {
                Close(TradeType.Sell, "NNFX Short");
            }
            else if (C2 < 0)
            {
                Close(TradeType.Buy, "NNFX Long");
            }

        }
        //Function for opening a new trade - EP10-Functions and Parameters
        private void Open(TradeType tradeType, string Label)
        {
            //Calculate trade amount based on ATR - EP6-Money Management
            var ATR = Math.Round(atr.Result.Last(0) / Symbol.PipSize, 0);
            var TradeAmount = (Account.Equity * RiskPct) / (1.5 * ATR * Symbol.PipValue);
            TradeAmount = Symbol.NormalizeVolumeInUnits(TradeAmount, RoundingMode.Down);

            //Check there's no existing position before entering a trade
            var position = Positions.Find(Label, SymbolName);
            //Set trade entry time - EP9-Best time to trade
            if (position == null & Server.Time.Hour == 16 & Server.Time.Minute == 58)
                ExecuteMarketOrder(tradeType, SymbolName, TradeAmount, Label, 1.5 * ATR, ATR);
        }

        //Function for closing trades - EP10-Functions and Parameters
        private void Close(TradeType tradeType, string Label)
        {
            foreach (var position in Positions.FindAll(Label, SymbolName, tradeType))
                ClosePosition(position);
        }
    }
}
