using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Newtonsoft.Json;
using TradeLib;
using TradeLib.Entities;

[assembly: AllowPartiallyTrustedCallers()]
namespace cAlgo.Robots
{
    //Set time zone to Eastern Standard Time EP9-Best time to trade
    [Robot(TimeZone = TimeZones.EasternStandardTime)]
    public class MyNNFXTemplate28 : Robot
    {
        #region Framework parameters
        //Parameters for the Template Risk Management
        [Parameter("Risk %", Group = "Risk Management", DefaultValue = 2)]
        public int RiskPct { get; set; }

        [Parameter("SL Factor", Group = "Risk Management", DefaultValue = 1.5)]
        public double SlFactor { get; set; }

        [Parameter("TP Factor", Group = "Risk Management", DefaultValue = 1)]
        public double TpFactor { get; set; }

        //Parameters for the Template General Settings
        [Parameter("Trade On Time", Group = "General Settings", DefaultValue = false)]
        public bool TradeOnTime { get; set; }

        [Parameter("Trade On Multiple Instruments", Group = "General Settings", DefaultValue = true)]
        public bool TradeMultipleInstruments { get; set; }

        [Parameter("Name of WatchList", Group = "General Settings", DefaultValue = "Forex Majors")]
        public string WatchListName { get; set; }

        [Parameter("Trade Hour", Group = "General Settings", DefaultValue = "16")]
        public int TradeHour { get; set; }

        [Parameter("Trade Minute", Group = "General Settings", DefaultValue = "55")]
        public int TradeMinute { get; set; }
        [Parameter("File Path", Group = "General Settings", DefaultValue = "D:\\ForexData\\Save.json")]
        public string FilePath { get; set; }
        #endregion

        //indicator variables for the Template for multi symbols
        private readonly Dictionary<string, AverageTrueRange> AtrList = new Dictionary<string, AverageTrueRange>();
        private readonly Dictionary<string, MovingAverage> hmaList = new Dictionary<string, MovingAverage>();
        private readonly Dictionary<string, Aroon> aroonList = new Dictionary<string, Aroon>();
        private readonly Dictionary<string, ChaikinMoneyFlow> chaikinList = new Dictionary<string, ChaikinMoneyFlow>();
        private readonly Dictionary<string, MovingAverage> chaikinMAList = new Dictionary<string, MovingAverage>();
        private readonly Dictionary<string, MovingAverage> sslMA_LowList = new Dictionary<string, MovingAverage>();
        private readonly Dictionary<string, MovingAverage> sslMA_HighList = new Dictionary<string, MovingAverage>();

        private readonly Dictionary<string, TradeSymbolInfo> TradeSymbolInfoList = new Dictionary<string, TradeSymbolInfo>();
        private readonly List<TradeSymbolInfo> SymbolsToTradeList = new List<TradeSymbolInfo>();
        private readonly Dictionary<string, int> CorrelationTable = new Dictionary<string, int>();

        //indicator variables for the Template for single symbol
        private string _botName;
        private AverageTrueRange _atr;
        private int _barToCheck;
        private double riskPercentage;
        private bool _hadABigBar = false;
        private TimeFrame higherTimeframe;
        private int Day { get; set; }

        //indicator variables for the Imported indicators
        private MovingAverage _hma;
        private Aroon _aroon;
        private ChaikinMoneyFlow _chaikin;
        private MovingAverage _chaikinMA;
        private MovingAverage _sslMA_High;
        private MovingAverage _sslMA_Low;

        #region Ctrader EventHandlers
        protected override void OnStart()
        {
            _botName = GetType().ToString();
            _botName = _botName.Substring(_botName.LastIndexOf('.') + 1);

            // gets 1 timeframe higher then you use the bot on if you want to use that
            switch (TimeFrame.Name)
            {
                case ("Minute5"):
                    higherTimeframe = TimeFrame.Minute15;
                    break;
                case ("Minute15"):
                    higherTimeframe = TimeFrame.Hour;
                    break;
                case ("Hour"):
                    higherTimeframe = TimeFrame.Hour4;
                    break;
                case ("Hour4"):
                    higherTimeframe = TimeFrame.Daily;
                    break;
                case ("Daily"):
                    higherTimeframe = TimeFrame.Weekly;
                    break;
                default:
                    throw new Exception("Wrong Timeframe for this bot");
            }

            // If you trade on a specific time you need to check the current bar (0), but if you trade on the start of a new bar you need to check the previous bar (1)
            _barToCheck = TradeOnTime ? 0 : 1;

            // As you fill in a percentage number you need it to be devided by 100 to calculate with it
            riskPercentage = (double)RiskPct / 100;

            // sets an eventhandler on every closed position (Runs the method when a position closes)
            Positions.Closed += PositionsOnClosed;

            if (TradeMultipleInstruments)
            {
                // Creates a list of all the symbols in the specified watchlist
                foreach (string symbolName in Watchlists.FirstOrDefault(w => w.Name == WatchListName).SymbolNames.ToArray())
                {
                    TradeSymbolInfo tradeSymbolInfo = new TradeSymbolInfo
                    {
                        Symbol = Symbols.GetSymbol(symbolName)
                    };
                    TradeSymbolInfoList.Add(symbolName, tradeSymbolInfo);
                }

                foreach (KeyValuePair<string, TradeSymbolInfo> symbol in TradeSymbolInfoList)
                {
                    var bars = MarketData.GetBars(TimeFrame, symbol.Key);

                    // setting up a simple corralation table that lets you give a value to a individual currency
                    var cur1 = symbol.Key.Substring(0, 3);
                    var cur2 = symbol.Key.Substring(3, 3);
                    if (!CorrelationTable.ContainsKey(cur1))
                    {
                        CorrelationTable.Add(cur1, 0);
                    }
                    if (!CorrelationTable.ContainsKey(cur2))
                    {
                        CorrelationTable.Add(cur2, 0);
                    }

                    // if you do not trade on a specific time it sets an eventhandler on every bar open event (runs the method whenever a new bar opens)
                    if (!TradeOnTime)
                    {
                        bars.BarOpened += OnBarsBarOpened;
                    }
                    else
                    {
                        bars.Tick += OnBarTick;
                    }

                    AtrList.Add(symbol.Key, Indicators.AverageTrueRange(bars, 14, MovingAverageType.Exponential));

                    //Load here the specific indicators for this bot for multiple Instruments
                    hmaList.Add(symbol.Key, Indicators.MovingAverage(bars.ClosePrices, 20, MovingAverageType.Hull));
                    aroonList.Add(symbol.Key, Indicators.Aroon(bars, 25));
                    chaikinList.Add(symbol.Key, Indicators.ChaikinMoneyFlow(bars, 14));
                    chaikinMAList.Add(symbol.Key, Indicators.MovingAverage(Indicators.ChaikinMoneyFlow(bars, 14).Result, 3, MovingAverageType.Exponential));
                    sslMA_HighList.Add(symbol.Key, Indicators.MovingAverage(bars.HighPrices, 10, MovingAverageType.Simple));
                    sslMA_LowList.Add(symbol.Key, Indicators.MovingAverage(bars.LowPrices, 10, MovingAverageType.Simple));

                }
            }
            else
            {
                // if you do not trade on a specific time it sets an eventhandler on every bar open event (runs the method whenever a new bar opens)
                if (!TradeOnTime)
                {
                    Bars.BarOpened += OnBarsBarOpened;
                }
                else
                {
                    Bars.Tick += OnBarTick;
                }

                _atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);

                //Load here the specific indicators for this bot for a single instrument
                _hma = Indicators.MovingAverage(Bars.ClosePrices, 20, MovingAverageType.Hull);
                _aroon = Indicators.Aroon(25);
                _chaikin = Indicators.ChaikinMoneyFlow(14);
                _chaikinMA = Indicators.MovingAverage(_chaikin.Result, 3, MovingAverageType.Exponential);
                _sslMA_High = Indicators.MovingAverage(Bars.HighPrices, 10, MovingAverageType.Simple);
                _sslMA_Low = Indicators.MovingAverage(Bars.LowPrices, 10, MovingAverageType.Simple);

            }
        }

        protected override void OnStop()
        {

        }

        private void PositionsOnClosed(PositionClosedEventArgs obj)
        {
            if (obj.Reason == PositionCloseReason.TakeProfit)
            {
                Position position = Positions.Find(obj.Position.Label);
                if (position != null)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        ModifyPosition(position, position.EntryPrice, null, true);
                    }

                    if (position.TradeType == TradeType.Sell)
                    {
                        ModifyPosition(position, position.EntryPrice, null, true);
                    }
                }
            }
            if (obj.Reason == PositionCloseReason.StopLoss)
            {

            }
        }

        private void OnBarTick(BarsTickEventArgs obj)
        {
            if (TimeToTrade())
            {
                if (TradeMultipleInstruments)
                {
                    TradeController(obj.Bars, TradeSymbolInfoList[obj.Bars.SymbolName].Symbol, AtrList[obj.Bars.SymbolName]);
                }
                else
                {
                    TradeController(Bars, Symbol, _atr);
                }
            }
        }

        private void OnBarsBarOpened(BarOpenedEventArgs obj)
        {
            if (TradeMultipleInstruments)
            {
                TradeController(obj.Bars, TradeSymbolInfoList[obj.Bars.SymbolName].Symbol, AtrList[obj.Bars.SymbolName]);
            }
            else
            {
                TradeController(Bars, Symbol, _atr);
            }
        }

        #endregion
        private void TradeController(Bars bars, Symbol symbol, AverageTrueRange atr)
        {
            string label = string.Format("{0}_{1}", _botName, symbol.Name);
            CheckForTradesToClose(bars, symbol, label);

            // prevention for overleverage, first check all currencys pairs and execute trades only if te list of all pairs that give a signal is complete
            bool executeTrades = false;
            if (!TradeMultipleInstruments || bars.SymbolName == TradeSymbolInfoList.Last().Key)
            {
                executeTrades = true;
                Print("executeTrades == true");
            }

            ExtendedTradeType tradetype = CheckForTradesToOpen(bars, symbol, atr);

            if (tradetype != ExtendedTradeType.Nothing)
            {
                SymbolsToTradeList.Add(new TradeSymbolInfo
                {
                    Atr = atr,
                    Label = label,
                    Symbol = symbol,
                    TradeType = (TradeType)tradetype,
                    Risk = riskPercentage
                });

            }

            // Within the body of this if statement you need to go through the list and place the logic what trades to take what trades to split risk on
            if (executeTrades)
            {
                // this takes all trades and devides the risk over the concurrent currencies
                List<TradeSymbolInfo> trades = (SymbolsToTradeList.Count > 1) ? DevideRiskTradeList(SymbolsToTradeList) : SymbolsToTradeList;
                foreach (TradeSymbolInfo trade in trades)
                {
                    if (trade.TradeType == TradeType.Buy)
                    {
                        Close(TradeType.Sell, trade.Symbol, trade.Label);
                        Open(trade.TradeType, trade.Symbol, trade.Atr, trade.Label, trade.Risk);
                    }
                    else if (trade.TradeType == TradeType.Sell)
                    {
                        Close(TradeType.Buy, trade.Symbol, trade.Label);
                        Open(trade.TradeType, trade.Symbol, trade.Atr, trade.Label, trade.Risk);
                    }
                }
                SymbolsToTradeList.Clear();
            }
        }

        private List<TradeSymbolInfo> DevideRiskTradeList(List<TradeSymbolInfo> tradableSymbolList)
        {
            List<TradeSymbolInfo> exclude = new List<TradeSymbolInfo>();
            List<string> currencys = new List<string>();
            Dictionary<string, List<TradeSymbolInfo>> tradesPerCurrency = new Dictionary<string, List<TradeSymbolInfo>>();

            foreach (var item in tradableSymbolList)
            {
                var cur1 = item.Symbol.Name.Substring(0, 3);
                var cur2 = item.Symbol.Name.Substring(3, 3);
                if (!currencys.Contains(cur1))
                {
                    currencys.Add(cur1);
                }
                if (!currencys.Contains(cur2))
                {
                    currencys.Add(cur2);
                }
            }
            foreach (string currency in currencys)
            {
                tradesPerCurrency.Add(currency, tradableSymbolList.Where(t => t.Symbol.Name.Contains(currency)).ToList());
            }
            foreach (KeyValuePair<string, List<TradeSymbolInfo>> pair in tradesPerCurrency.OrderByDescending(key => key.Value.Count))
            {
                foreach (var item in pair.Value)
                {
                    if (currencys.Contains(item.Symbol.Name.Substring(0, 3)) || currencys.Contains(item.Symbol.Name.Substring(3, 3)))
                    {
                        var sym = tradableSymbolList.FirstOrDefault(t => t.Symbol.Name == item.Symbol.Name);
                        var newCalculatedRisk = Math.Round(riskPercentage / pair.Value.Count(), 4);
                        sym.Risk = sym.Risk < newCalculatedRisk ? sym.Risk : newCalculatedRisk;
                        if (currencys.Contains(item.Symbol.Name.Substring(0, 3)))
                        {
                            currencys.Remove(item.Symbol.Name.Substring(0, 3));
                        }
                        if (currencys.Contains(item.Symbol.Name.Substring(3, 3)))
                        {
                            currencys.Remove(item.Symbol.Name.Substring(3, 3));
                        }
                    }
                }
            }

            return tradableSymbolList;
        }
        
        private void CheckForTradesToClose(Bars bars, Symbol symbol, string label)
        {
            //SSLChannel ssl = TradeMultipleInstruments ? _sslList[bars.SymbolName] : _ssl;
            var ssl_High = TradeMultipleInstruments ? sslMA_HighList[bars.SymbolName] : _sslMA_High;
            var ssl_Low = TradeMultipleInstruments ? sslMA_LowList[bars.SymbolName] : _sslMA_Low;

            var C2 = ssl_High.Result.Last(0) - ssl_Low.Result.Last(0);

            if (C2 > 0)
            {
                if (Positions.FindAll(label, symbol.Name, TradeType.Sell) == null)
                {
                    return;
                }
                foreach (Position position in Positions.FindAll(label, symbol.Name, TradeType.Sell))
                {
                    ClosePosition(position);
                    Print("CheckForTradesToClose - {0} - Sell", position.Label);
                }
            }
            
            if (C2 < 0)
            {
                if (Positions.FindAll(label, symbol.Name, TradeType.Buy) == null)
                {
                    return;
                }
                foreach (Position position in Positions.FindAll(label, symbol.Name, TradeType.Buy))
                {
                    ClosePosition(position);
                    Print("CheckForTradesToClose - {0} - Buy", position.Label);
                }
            }

        }

        private ExtendedTradeType CheckForTradesToOpen(Bars bars, Symbol symbol, AverageTrueRange atr)
        {
            double barSize = Math.Round(Math.Abs((bars.HighPrices.Last(_barToCheck) - bars.LowPrices.Last(_barToCheck)) / symbol.PipSize), 0);
            double atrSize = Math.Round(atr.Result.Last(_barToCheck) / symbol.PipSize, 0);

            if (barSize > atrSize)
            {
                return ExtendedTradeType.Nothing;
            }

            _aroon = TradeMultipleInstruments ? aroonList[bars.SymbolName] : _aroon;
            var C1 = _aroon.Up.Last(0) - _aroon.Down.Last(0);
            var PrevC1 = _aroon.Up.Last(1) - _aroon.Down.Last(1);
            var ssl_High = TradeMultipleInstruments ? sslMA_HighList[bars.SymbolName] : _sslMA_High;
            var ssl_Low = TradeMultipleInstruments ? sslMA_LowList[bars.SymbolName] : _sslMA_Low;
            var C2 = ssl_High.Result.Last(0) - ssl_Low.Result.Last(0);
            var Volume = chaikinMAList[bars.SymbolName].Result.Last(0);
            var Baseline = hmaList[bars.SymbolName].Result.Last(0);

            if (C1 > 0 && PrevC1 < 0 && C2 > 0 && Volume > 0 && Symbol.Bid > Baseline)
            {
                Print("CheckForTradesToOpen - {0} - Buy", symbol.Name);
                return ExtendedTradeType.Buy;
            }

            if (C1 < 0 && PrevC1 > 0 && C2 < 0 && Volume < 0 && Symbol.Bid < Baseline)
            {
                Print("CheckForTradesToOpen - {0} - Sell", symbol.Name);
                return ExtendedTradeType.Sell;
            }

            return ExtendedTradeType.Nothing;
        }

        //Function for opening a new trade
        private void Open(TradeType tradeType, Symbol symbol, AverageTrueRange atr, string label, double risk)
        {
            List<string> list = new List<string>
            {
                symbol.Name
            };
            if (TradeMultipleInstruments)
            {
                list = Watchlists.FirstOrDefault(w => w.Name == WatchListName).SymbolNames.Where(s => s.Contains(symbol.Name.Substring(0, 3)) || s.Contains(symbol.Name.Substring(3, 3))).ToList();
            }

            //Check there's no existing position before entering a trade, label contains the Indicatorname and the currency
            foreach (var symbolname in list)
            {
                if (Positions.Find(label, symbolname, tradeType) != null)
                {
                    return;
                }
            }

            //Calculate trade amount based on ATR
            double atrSize = Math.Round(atr.Result.Last(_barToCheck) / symbol.PipSize, 0);
            double tradeAmount = Account.Equity * risk / (SlFactor * atrSize * symbol.PipValue);
            tradeAmount = symbol.NormalizeVolumeInUnits(tradeAmount / 2, RoundingMode.Down);

            Print("Open");
            ExecuteMarketOrder(tradeType, symbol.Name, tradeAmount, label, SlFactor * atrSize, TpFactor * atrSize);
            ExecuteMarketOrder(tradeType, symbol.Name, tradeAmount, label, SlFactor * atrSize, null);

            if (_hadABigBar)
            {
                _barToCheck = TradeOnTime ? 0 : 1;
                _hadABigBar = false;
            }
        }

        //Function for closing trades
        private void Close(TradeType tradeType, Symbol symbol, string label)
        {
            if (Positions.FindAll(label, symbol.Name, tradeType) == null)
            {
                return;
            }
            foreach (Position position in Positions.FindAll(label, symbol.Name, tradeType))
            {
                ClosePosition(position);
            }
        }

        private bool TimeToTrade()
        {
            return Server.Time.Hour == TradeHour && Server.Time.Minute == TradeMinute;
        }

        private void CalculateCorrelationTable(Bars bars)
        {
            // ***Example code for a real simple basic correlation***
            //var pSAR = parabolicSARList[bars.SymbolName];
            //if (pSAR.Result.Last() > bars.Last().Close)
            //{
            //    CorrelationTable[bars.SymbolName.Substring(0, 3)] += 1;
            //    CorrelationTable[bars.SymbolName.Substring(3, 3)] -= 1;
            //}
            //if (pSAR.Result.Last() < bars.Last().Close)
            //{
            //    CorrelationTable[bars.SymbolName.Substring(0, 3)] -= 1;
            //    CorrelationTable[bars.SymbolName.Substring(3, 3)] += 1;
            //}
        }
    }


    public class TradeSymbolInfo
    {
        public Symbol Symbol { get; set; }
        public AverageTrueRange Atr { get; set; }
        public TradeType TradeType { get; set; }
        public string Label { get; set; }
        public int Ranking { get; set; }
        public double Risk { get; set; }
    }

    //
    // Summary:
    //     The direction of a trade order.
    //
    // Remarks:
    //     Indicates the trade direction, whether it is a Buy or a Sell trade.
    public enum ExtendedTradeType
    {
        //
        // Summary:
        //     Represents a Buy order.
        Buy = 0,
        //
        // Summary:
        //     Represents a Sell order.
        Sell = 1,
        //
        // Summary:
        //     Represents a Sell order.
        Nothing = 2
    }
}