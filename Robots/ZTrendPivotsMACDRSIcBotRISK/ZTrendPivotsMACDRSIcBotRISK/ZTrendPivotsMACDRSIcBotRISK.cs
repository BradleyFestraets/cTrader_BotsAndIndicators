// -------------------------------------------------------------------------------------------------
//
// This is the code for a cAlgo robot, named ZTrendPivotsMACDRSIcBotRISK, used for automated trading in the
// foreign exchange market. The robot uses moving averages as a basis for making buy/sell decisions
// with a momentum confirm with the Relative Strenght Index Oscillator.
// The robot has several parameters that can be adjusted, such as the quantity of trade, the type 
// moving average, the source for the moving average calculation, the slow and fast periods for 
// moving average calculation, stop loss and trailing stop in pips. The robot opens long positions
// when the fast moving average crosses above the slow moving average and opens short positions 
// the fast moving average crosses below the slow moving average. If a position is open, the robot
// will close it before opening a new one in the opposite direction.
// Trade Timeframes -  m30
//
// -------------------------------------------------------------------------------------------------
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;
using System.Linq;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ZTrendPivotsMACDRSIcBotRISK : Robot
    {


        [Parameter("MA Type", Group = "Moving Average")]
        public MovingAverageType MAType { get; set; }

        [Parameter("Source", Group = "Moving Average")]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow Periods", Group = "Moving Average", DefaultValue = 200)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", Group = "Moving Average", DefaultValue = 42)]
        public int FastPeriods { get; set; }


        [Parameter("Source", Group = "RSI")]
        public DataSeries Source { get; set; }

        [Parameter("Periods", Group = "RSI", DefaultValue = 14)]
        public int Periods { get; set; }

        [Parameter("Signal Periods", Group = "MACD", DefaultValue = 21)]
        public int SignalPeriods { get; set; }

        [Parameter("ATR Periods", Group = "ATR", DefaultValue = 14)]
        public int ATRPeriods { get; set; }

        [Parameter("ATR Multiplier", Group = "ATR", DefaultValue = 1.2)]
        public double ATRMultiplier { get; set; }


        private AverageTrueRange atr;
        private ExponentialMovingAverage ema242;
        private MacdCrossOver macd;
        private RelativeStrengthIndex rsi;
        private MovingAverage slowMa;
        private MovingAverage fastMa;
        private const string label = "ZTrendPivotsMACDRSIcBotRISK";
        private double positionSize;

        protected override void OnStart()
        {
            atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            ema242 = Indicators.ExponentialMovingAverage(Bars.HighPrices, 242);
            ema242 = Indicators.ExponentialMovingAverage(Bars.ClosePrices, 242);
            ema242 = Indicators.ExponentialMovingAverage(Bars.LowPrices, 242);
            macd = Indicators.MacdCrossOver(SourceSeries, 12, 26, 9);
            rsi = Indicators.RelativeStrengthIndex(Source, Periods);
            fastMa = Indicators.MovingAverage(SourceSeries, FastPeriods, MAType);
            slowMa = Indicators.MovingAverage(SourceSeries, SlowPeriods, MAType);
        }

        protected override void OnTick()
        {
            var longPosition = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(label, SymbolName, TradeType.Sell);
            var currentSlowMa = slowMa.Result.Last(0);
            var currentFastMa = fastMa.Result.Last(0);
            var previousSlowMa = slowMa.Result.Last(1);
            var previousFastMa = fastMa.Result.Last(1);
            var currentRsi = rsi.Result.LastValue;
            var currentMacdLine = macd.MACD.LastValue;
            var currentSignalLine = macd.Signal.LastValue;

            var atrPipValue = Math.Round(atr.Result.Last(0) / Symbol.PipSize) * ATRMultiplier;
            var stopLoss = Math.Round(atrPipValue * 1.2);
            var takeProfit = Math.Round(atrPipValue * 2);
            var closeprice = Bars.ClosePrices.LastValue;
            
            //Money management
            double accountEquity = Account.Equity;
            double riskPercentage = 0.02; // 2% risk
            double riskAmount = (accountEquity * riskPercentage)/atrPipValue;
            double pipValue = Symbol.PipValue;
            positionSize = (riskAmount / stopLoss) / pipValue;
            
            // Round the position size down to the nearest lot size
            positionSize = Symbol.NormalizeVolumeInUnits(positionSize);

            if (Bars.ClosePrices.LastValue > ema242.Result.LastValue)
            {
                if (previousSlowMa > previousFastMa && currentSlowMa <= currentFastMa &&
                longPosition == null && currentRsi > 50 && currentMacdLine > 0 && currentSignalLine > 0)
                {
                    if (shortPosition != null)
                        ClosePosition(shortPosition);
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, positionSize, label, stopLoss, takeProfit, "trendpivots", true);
                }
            }
            
            if (Bars.ClosePrices.LastValue < ema242.Result.LastValue)
            {
                if (previousSlowMa < previousFastMa && currentSlowMa >= currentFastMa &&
                shortPosition == null && currentRsi < 50 && currentMacdLine < 0 && currentSignalLine < 0)
                {
                    if (longPosition != null)
                        ClosePosition(longPosition);
                        
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, positionSize, label, stopLoss, takeProfit, "trendpivots", true);
                }
            }
        }
        
        private double VolumeInUnits
        {
            get { return Symbol.QuantityToVolumeInUnits(positionSize); }
        }
    }
}