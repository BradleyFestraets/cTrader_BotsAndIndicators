// -------------------------------------------------------------------------------------------------
//
// "Zulfikar RSIcBot". It uses the Relative Strength Index (RSI) and Exponential Moving Average 
// indicators to determine whether to buy or sell a currency pair. If the current close price 
// 200-day EMA and the RSI is below 50, it will close any open sell positions
// and open a buy position. If the current close price is below the 200-day EMA and the
// RSI is above 50, it will close any open buy positions and open a sell position. Use on the 30
// minute chart on major pair.
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
    public class ZScalpingRSIMACDEMAcBot : Robot
    {
    [Parameter("Quantity (Lots)", Group = "Volume", DefaultValue = 0.01, MinValue = 0.01, MaxValue = 0.08 , Step = 0.01)]
    public double Quantity { get; set; }
    
    [Parameter("Stop Loss (pips)", DefaultValue = 15)]
    public int StopLoss { get; set; }
    
    [Parameter("Trailing Stop (pips)", DefaultValue = 20)]
    public int TrailingStop { get; set; }
    [Parameter("Source", Group = "RSI")]
    public DataSeries Source { get; set; }
    
    [Parameter("Periods", Group = "RSI", DefaultValue = 14, MinValue = 8, MaxValue = 30)]
    public int Periods { get; set; }
    
    [Parameter("Signal Periods", Group = "MACD", DefaultValue = 14, MinValue = 8, MaxValue = 30)]
    public int SignalPeriods { get; set; }

    private MacdCrossOver macd;
    private RelativeStrengthIndex rsi;
    private ExponentialMovingAverage ema200;
    private ExponentialMovingAverage ema50;
    private ExponentialMovingAverage ema21;
    
    protected override void OnStart()
    {
    
    rsi = Indicators.RelativeStrengthIndex(Source, Periods);
    macd = Indicators.MacdCrossOver(Source, 12, 26, 9);
    ema21 = Indicators.ExponentialMovingAverage(Bars.ClosePrices, 21);
    ema50 = Indicators.ExponentialMovingAverage(Bars.ClosePrices, 50);
    ema200 = Indicators.ExponentialMovingAverage(Bars.ClosePrices, 200);
    
    }
    
    
    protected override void OnTick()
    {
    var currentMacdLine = macd.MACD.LastValue;
    var currentSignalLine = macd.Signal.LastValue;
    
    if (Bars.ClosePrices.LastValue > ema200.Result.LastValue && Bars.ClosePrices.LastValue > ema50.Result.LastValue && Bars.ClosePrices.LastValue > ema21.Result.LastValue)
    {
    if (rsi.Result.LastValue > 50 && rsi.Result.LastValue < 65)
    {
    if (currentMacdLine > 0 && currentSignalLine > 0)
    Close(TradeType.Sell);
    Open(TradeType.Buy);
    }   
    }
    else if (Bars.ClosePrices.LastValue < ema200.Result.LastValue && Bars.ClosePrices.LastValue < ema50.Result.LastValue && Bars.ClosePrices.LastValue < ema21.Result.LastValue)
    {
    if (rsi.Result.LastValue < 50 && rsi.Result.LastValue > 35)
    {
    if (currentMacdLine < 0 && currentSignalLine < 0)
    Close(TradeType.Buy);
    Open(TradeType.Sell);
    }
    }
   
   // This is the trailing stop logic.
    }
    private void Close(TradeType tradeType)
    {

    foreach (var position in Positions.FindAll("ZScalpingRSIMACDEMAcBot", SymbolName, tradeType))
    ClosePosition(position);
    }
    private void Open(TradeType tradeType)
    {
    var position = Positions.Find("ZScalpingRSIMACDEMAcBot", SymbolName, tradeType);
    var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);
    if (position == null)
    ExecuteMarketOrder(tradeType, SymbolName, volumeInUnits, "ZScalpingRSIMACDEMAcBot", 20 , null, "RSI Trailing", true);
        }
    }
}
