using cAlgo.API;
using cAlgo.API.Indicators;
using System;
 
namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BbTradeSystemB : Robot
    {
        public enum ENUM_TP_TYPE
        {
            Fixed = 0,
            RiskRatio = 1
        }
        public enum ENUM_RISK_SOURCE
        {
            Equity = 0,
            Balance = 1
        }
 
        public enum ENUM_LOT_TYPE
        {
            Fixed_Lot = 0,
            Percent = 1
        }
        public enum ENUM_CROSS_TYPE
        {
            Current_Breakout = 0,
            //Current breakout
            Close_Above_Below = 1,
            //Close Above/Below
            Bar_Cross = 2
            //Bar Cross
        }
        public enum ENUM_CROSS_LINE
        {
            Top_Line = 0,
            // Top Line
            Bottom_Line = 1
            // Bottom Line
        }
        public enum ENUM_CROSS_DIRECTION
        {
            Above = 0,
            // Above
            Below = 1
            // Below
        }
 
        #region Input BB Parameters
 
        [Parameter("BB Source", Group = "BB Parameters")]
        public DataSeries BBSeries { get; set; }
 
        [Parameter("BB Period", Group = "BB Parameters", DefaultValue = 20)]
        public int BBPeriods { get; set; }
 
        [Parameter("Bands Deviation", Group = "BB Parameters", DefaultValue = 2)]
        public double deviation { get; set; }
 
        [Parameter("MA Type", Group = "BB Parameters")]
        public MovingAverageType maType { get; set; }
        #endregion
 
 
        #region Input Trade Parameters
 
        [Parameter("Label", Group = "Trade Parameters", DefaultValue = "BB Trade System")]
        public string Label { get; set; }
 
        [Parameter("Cross Type", Group = "Trade Parameters", DefaultValue = ENUM_CROSS_TYPE.Current_Breakout)]
        public ENUM_CROSS_TYPE crossType { get; set; }
        [Parameter("Buy Cross Line", Group = "Trade Parameters", DefaultValue = ENUM_CROSS_LINE.Bottom_Line)]
        public ENUM_CROSS_LINE crossLineBuy { get; set; }
        [Parameter("Buy Cross Direction", Group = "Trade Parameters", DefaultValue = ENUM_CROSS_DIRECTION.Below)]
        public ENUM_CROSS_DIRECTION crossDirectBuy { get; set; }
        [Parameter("Sell Cross Line", Group = "Trade Parameters", DefaultValue = ENUM_CROSS_LINE.Top_Line)]
        public ENUM_CROSS_LINE crossLineSell { get; set; }
        [Parameter("Sell Cross Direction", Group = "Trade Parameters", DefaultValue = ENUM_CROSS_DIRECTION.Above)]
        public ENUM_CROSS_DIRECTION crossDirectSell { get; set; }
 
        [Parameter("Take Profit type", Group = "Trade Parameters", DefaultValue = ENUM_TP_TYPE.Fixed)]
        public ENUM_TP_TYPE tpType { get; set; }
 
        [Parameter("Stop Loss in pips", Group = "Trade Parameters", DefaultValue = 20)]
        public double SL { get; set; }
 
        [Parameter("Take Profit value", Group = "Trade Parameters", DefaultValue = 35)]
        public double TP { get; set; }
 
        [Parameter("Close on the opposite signal", Group = "Trade Parameters", DefaultValue = true)]
        public bool oppositeClose { get; set; }
 
        [Parameter("Max Orders", Group = "Trade Parameters", DefaultValue = 1)]
        public int maxOrders { get; set; }
 
        [Parameter("Use Reverse Trade", Group = "Trade Parameters", DefaultValue = true)]
        public bool reverseTrade { get; set; }
 
        #endregion
        #region Input Lot Size Parameters
        [Parameter("Lot Type", Group = "Lot Size", DefaultValue = ENUM_LOT_TYPE.Fixed_Lot)]
        public ENUM_LOT_TYPE lotType { get; set; }
 
        [Parameter("Risk Source", Group = "Lot Size", DefaultValue = ENUM_RISK_SOURCE.Balance)]
        public ENUM_RISK_SOURCE riskSource { get; set; }
 
        [Parameter("Risk/Lot Value", Group = "Lot Size", DefaultValue = 0.1)]
        public double risk { get; set; }
        #endregion
        #region Input Break Even Parameters
        [Parameter("Use BreakEven", Group = "BreakEven", DefaultValue = false)]
        public bool UseBE { get; set; }
        [Parameter("BreakEven Start(pips)", Group = "BreakEven", DefaultValue = 10)]
        public double BEStart { get; set; }
 
        [Parameter("BreakEven Profit(pips)", Group = "BreakEven", DefaultValue = 0)]
        public double BEProfit { get; set; }
        #endregion
 
        private BollingerBands BB;
 
        protected override void OnStart()
        {
            BB = Indicators.BollingerBands(BBSeries, BBPeriods, deviation, maType);
            // Put your initialization logic here
        }
 
        double bbPrevTop;
        double bbPrevBott;
        double pricePrev;
        DateTime lastTrade;
 
 
        protected override void OnTick()
        {
            if (UseBE)
                BreakEven();
 
            if (lastTrade != Bars.OpenTimes.Last(0))
            {
                if (CheckCondition(TradeType.Buy))
                {
                    if (oppositeClose)
                        CloseOrders(TradeType.Sell);
                    if(CalculateOrders() < maxOrders)
                    {
                        lastTrade = Bars.OpenTimes.Last(0);
                        OpenOrder(TradeType.Buy);
                    }
                }
                if (CheckCondition(TradeType.Sell))
                {
                    if (oppositeClose)
                        CloseOrders(TradeType.Buy);
                    if(CalculateOrders() < maxOrders)
                    {
                        lastTrade = Bars.OpenTimes.Last(0);
                        OpenOrder(TradeType.Sell);
                    }
                }
            }
            bbPrevTop = BB.Top.Last(0);
            bbPrevBott = BB.Bottom.Last(0);
            pricePrev = Bars.ClosePrices.Last(0);
 
        }
 
        bool CheckOrder(TradeType type)
        {
            if (Positions.FindAll(Label, Symbol.Name, type) != null)
                return false;
            return true;
        }
 
        int CalculateOrders()
        {
            return Positions.FindAll(Label, Symbol.Name).Length;
        }
 
        void CloseOrders(TradeType type)
        {
            if (reverseTrade)
                type = type == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
 
            foreach (var pos in Positions.FindAll(Label, Symbol.Name, type))
            {
                ClosePosition(pos);
            }
        }
 
        void OpenOrder(TradeType type)
        {
            if (reverseTrade)
                type = type == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
 
            double op;
            double tp = tpType == ENUM_TP_TYPE.Fixed ? TP : SL * TP;
            double sl;
 
            double source = riskSource == ENUM_RISK_SOURCE.Balance ? Account.Balance : Account.Equity;
 
            double volumeInUnits = 0;
            if (lotType == ENUM_LOT_TYPE.Fixed_Lot)
                volumeInUnits = Symbol.QuantityToVolumeInUnits(risk);
            else
                volumeInUnits = CalculateVolume(SL, risk, source);
 
            if (volumeInUnits == -1)
                return;
            ExecuteMarketOrder(type, SymbolName, volumeInUnits, Label, SL, TP);
        }
 
 
        private void BreakEven()
        {
            if (!UseBE)
                return;
 
            foreach (var pos in Positions.FindAll(Label, SymbolName))
            {
                if (pos.TradeType == TradeType.Buy)
                {
                    if (Symbol.Ask >= pos.EntryPrice + BEStart * Symbol.PipSize && (pos.StopLoss < pos.EntryPrice + BEProfit * Symbol.PipSize || pos.StopLoss == null))
                    {
                        ModifyPosition(pos, pos.EntryPrice + BEProfit * Symbol.PipSize, pos.TakeProfit);
                    }
                }
                if (pos.TradeType == TradeType.Sell)
                {
                    if (Symbol.Bid <= pos.EntryPrice - BEStart * Symbol.PipSize && (pos.StopLoss > pos.EntryPrice - BEProfit * Symbol.PipSize || pos.StopLoss == null))
                    {
                        ModifyPosition(pos, pos.EntryPrice + BEProfit * Symbol.PipSize, pos.TakeProfit);
                    }
                }
            }
        }
 
        bool CheckCondition(TradeType trade)
        {
            int shift = crossType == ENUM_CROSS_TYPE.Current_Breakout ? 0 : 1;
 
            double bb;
            double bbPrev;
 
            if (trade == TradeType.Buy)
            {
                if (crossLineBuy == ENUM_CROSS_LINE.Top_Line)
                    bb = BB.Top.Last(shift);
                else
                    bb = BB.Bottom.Last(shift);
                if (crossType == ENUM_CROSS_TYPE.Current_Breakout)
                {
                    if (crossLineBuy == ENUM_CROSS_LINE.Top_Line)
                        bbPrev = bbPrevTop;
                    else
                        bbPrev = bbPrevBott;
                    if (crossDirectBuy == ENUM_CROSS_DIRECTION.Above)
                    {
                        if (Bars.ClosePrices.Last(0) < bb && pricePrev > bbPrev)
                            return true;
                        else
                            return false;
                    }
                    if (crossDirectBuy == ENUM_CROSS_DIRECTION.Below)
                    {
                        if (Bars.ClosePrices.Last(0) > bb && pricePrev < bbPrev)
                            return true;
                        else
                            return false;
                    }
                }
                if (crossType == ENUM_CROSS_TYPE.Close_Above_Below)
                {
                    if (crossDirectBuy == ENUM_CROSS_DIRECTION.Above)
                    {
                        if (Bars.ClosePrices.Last(1) > bb)
                            return true;
                        else
                            return false;
                    }
                    if (crossDirectBuy == ENUM_CROSS_DIRECTION.Below)
                    {
                        if (Bars.ClosePrices.Last(1) < bb)
                            return true;
                        else
                            return false;
                    }
                }
                if (crossType == ENUM_CROSS_TYPE.Bar_Cross)
                {
                    if (Bars.LowPrices.Last(1) < bb && Bars.HighPrices.Last(1) > bb)
                        return true;
                    else
                        return false;
                }
            }
 
 
            if (trade == TradeType.Sell)
            {
                if (crossLineSell == ENUM_CROSS_LINE.Top_Line)
                    bb = BB.Top.Last(shift);
                else
                    bb = BB.Bottom.Last(shift);
                if (crossType == ENUM_CROSS_TYPE.Current_Breakout)
                {
                    if (crossLineSell == ENUM_CROSS_LINE.Top_Line)
                        bbPrev = bbPrevTop;
                    else
                        bbPrev = bbPrevBott;
                    if (crossDirectSell == ENUM_CROSS_DIRECTION.Above)
                    {
                        if (Bars.ClosePrices.Last(0) < bb && pricePrev > bbPrev)
                            return true;
                        else
                            return false;
                    }
                    if (crossDirectSell == ENUM_CROSS_DIRECTION.Below)
                    {
                        if (Bars.ClosePrices.Last(0) > bb && pricePrev < bbPrev)
                            return true;
                        else
                            return false;
                    }
                }
                if (crossType == ENUM_CROSS_TYPE.Close_Above_Below)
                {
                    if (crossDirectSell == ENUM_CROSS_DIRECTION.Above)
                    {
                        if (Bars.ClosePrices.Last(1) > bb)
                            return true;
                        else
                            return false;
                    }
                    if (crossDirectSell == ENUM_CROSS_DIRECTION.Below)
                    {
                        if (Bars.ClosePrices.Last(1) < bb)
                            return true;
                        else
                            return false;
                    }
                }
                if (crossType == ENUM_CROSS_TYPE.Bar_Cross)
                {
                    if (Bars.LowPrices.Last(1) < bb && Bars.HighPrices.Last(1) > bb)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }
        private double CalculateVolume(double stopLossPips, double riskSize, double source)
        {
            // source = Account.Balance or Account.Equity
            double riskPerTrade = source * riskSize / 100;
            double totalPips = stopLossPips;
 
            double _volume;
            double exactVolume = riskPerTrade / (Symbol.PipValue * totalPips);
            if (exactVolume >= Symbol.VolumeInUnitsMin)
            {
                _volume = Symbol.NormalizeVolumeInUnits(exactVolume);
            }
            else
            {
                _volume = -1;
                Print("Not enough Equity to place minimum trade, exactVolume " + exactVolume + " is not >= Symbol.VolumeInUnitsMin " + Symbol.VolumeInUnitsMin);
            }
            return _volume;
        }
        
        protected override void OnStop()
        {
            // Put your deinitialization logic here
            foreach(Position position in Positions)
                ClosePosition(position);
        }
    }
}