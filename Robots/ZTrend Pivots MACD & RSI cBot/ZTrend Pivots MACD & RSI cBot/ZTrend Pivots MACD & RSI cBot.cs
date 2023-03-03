// -------------------------------------------------------------------------------------------------
//
// This is the code for a cAlgo robot, named ZTrendPivotsMACDRSIcBot, used for automated trading in the
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
using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ZTrendPivotsMACDRSIcBot : Robot
{
    [Parameter("Quantity (Lots)", Group = "Volume", DefaultValue = 0.01, MinValue = 0.01, MaxValue = 0.8, Step = 0.01)]
    public double Quantity { get; set; }
    
    [Parameter("MA Type", Group = "Moving Average")]
    public MovingAverageType MAType { get; set; }
    
    [Parameter("Source", Group = "Moving Average")]
    public DataSeries SourceSeries { get; set; }
    
    [Parameter("Slow Periods", Group = "Moving Average", DefaultValue = 32)]
    public int SlowPeriods { get; set; }
    
    [Parameter("Fast Periods", Group = "Moving Average", DefaultValue = 8)]
    public int FastPeriods { get; set; }
    
    [Parameter("Stop Loss (pips)", DefaultValue = 20)]
    public int StopLoss { get; set; }
    
    [Parameter("Trailing Stop (pips)", DefaultValue = 50)]
    public int TrailingStop { get; set; }
    
    [Parameter("Source", Group = "RSI")]
    public DataSeries Source { get; set; }
    
    [Parameter("Periods", Group = "RSI", DefaultValue = 14, MinValue = 8, MaxValue = 30)]
    public int Periods { get; set; }
    
    [Parameter("Signal Periods", Group = "MACD", DefaultValue = 14, MinValue = 8, MaxValue = 30)]
    public int SignalPeriods { get; set; }

    
    private ExponentialMovingAverage ema242;
    private MacdCrossOver macd;
    private RelativeStrengthIndex rsi;
    private MovingAverage slowMa;
    private MovingAverage fastMa;
    private const string label = "ZTrendPivotsMACDRSIcBot";
   
   
   protected override void OnStart()
{
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

    
    if (Bars.ClosePrices.LastValue > ema242.Result.LastValue) 
{    
    if (previousSlowMa > previousFastMa && currentSlowMa <= currentFastMa &&
    longPosition == null && currentRsi > 50 && currentMacdLine > 0 && currentSignalLine > 0)
{
    if (shortPosition != null)
    ClosePosition(shortPosition);
    ExecuteMarketOrder(TradeType.Buy, SymbolName, VolumeInUnits, label, TrailingStop, 150, "trendpivots", true);
}
    
    
    else if (Bars.ClosePrices.LastValue < ema242.Result.LastValue) 
   {
    if (previousSlowMa < previousFastMa && currentSlowMa >= currentFastMa &&
    shortPosition == null && currentRsi < 50  && currentMacdLine < 0 && currentSignalLine < 0)
   {
    if (longPosition != null)
    ClosePosition(longPosition);
    ExecuteMarketOrder(TradeType.Sell, SymbolName, VolumeInUnits, label, TrailingStop, 150, "trendpivots", true);
  }
    }  
        }
            }
    private double VolumeInUnits
    {
    get { return Symbol.QuantityToVolumeInUnits(Quantity); }
        }
    }
}

