using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Linq;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class BollingerBandsBot : Robot
    {
        private BollingerBands _bollingerBands;
        //private Position _position;
        
        protected override void OnStart()
        {
            var period = 41;
            var standardDeviations = 2.0;
            _bollingerBands = Indicators.BollingerBands(Bars.ClosePrices, period, 
                standardDeviations, MovingAverageType.Simple);
        }

        protected override void OnBar()
        {
           
            var topBand = _bollingerBands.Top.LastValue;
            var bottomBand = _bollingerBands.Bottom.LastValue;
            var currentPrice = Bars.ClosePrices.LastValue;
            const double volume = 1000;
            //const double stopLossPips = 53;
            //const double takeProfitPips = 89;
            
            var long_entry = currentPrice > topBand;
            var short_entry = currentPrice < bottomBand;
            
            var bbBuyPositions = from position in Positions
                     where position.Label == "BB Buy"
                     select position;

            var bbSellPositions = from position in Positions
                     where position.Label == "BB Sell"
                     select position;

            
            if(short_entry)
            {
                //Close long orders
                foreach (var position in bbBuyPositions)
                {
                    ClosePosition(position);
                }
                
                if(bbSellPositions.Count() == 0)
                    ExecuteMarketOrder(TradeType.Sell, Symbol.Name, volume, "BB Sell");//, stopLossPips, takeProfitPips, "BB Buy", true);
            }
            
            if(long_entry)
            {
                //Close short orders
                foreach (var position in bbSellPositions)
                {
                    ClosePosition(position);
                } 
                
                if(bbBuyPositions.Count() == 0)
                    ExecuteMarketOrder(TradeType.Buy, Symbol.Name, volume, "BB Buy");//, stopLossPips, takeProfitPips, "BB Sell", true);
            }
        }
    }
}