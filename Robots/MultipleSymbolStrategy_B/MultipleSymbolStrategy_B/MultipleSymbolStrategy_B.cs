using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Linq;
 
namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MultipleSymbolStrategy : Robot
    {
        [Parameter("Fast MA Periods", DefaultValue = 50)]
        public int FastMAPeriods { get; set; }
 
        [Parameter("Slow MA Periods", DefaultValue = 200)]
        public int SlowMAPeriods { get; set; }
 
        private MovingAverage fastMA;
        private MovingAverage slowMA;
 
        protected override void OnStart()
        {
            // Set up the moving averages for each symbol
            foreach (var symbol in Symbols)
            {
                fastMA = Indicators.MovingAverage(Bars.ClosePrices, FastMAPeriods);
                slowMA = Indicators.MovingAverage(Bars.ClosePrices, SlowMAPeriods);
            }
        }
 
        protected override void OnBar()
        {
            // Check for crossovers on each symbol
            foreach (var symbol in Symbols)
            {
                if (fastMA.Result.Last(0) > slowMA.Result.Last(0) && fastMA.Result.Last(1) < slowMA.Result.Last(1))
                {
                    // Buy signal
                    ExecuteMarketOrder(TradeType.Buy, symbol, 10000, "MA crossover buy");
                }
                else if (fastMA.Result.Last(0) < slowMA.Result.Last(0) && fastMA.Result.Last(1) > slowMA.Result.Last(1))
                {
                    // Sell signal
                    ExecuteMarketOrder(TradeType.Sell, symbol, 10000, "MA crossover sell");
                }
            }
        }
    }
}
