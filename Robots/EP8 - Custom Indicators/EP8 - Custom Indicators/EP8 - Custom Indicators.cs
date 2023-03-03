using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EP8CustomIndicators : Robot
    {
        [Parameter(DefaultValue = 0.0)]
        public double Parameter { get; set; }

        //Create indicator variables
        private AverageTrueRange atr;
        private SSLChannel ssl;

        protected override void OnStart()
        {
            //Load indicators on start up
            atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            ssl = Indicators.GetIndicator<SSLChannel>(10, MovingAverageType.Simple);
        }

        protected override void OnBar()
        {
            //Calculate Trade amount based on ATR
            var PrevATR = Math.Round(atr.Result.Last(1) / Symbol.PipSize);
            var TradeAmount = (Account.Equity * 0.02) / (1.5 * PrevATR * Symbol.PipValue);
            TradeAmount = Symbol.NormalizeVolumeInUnits(TradeAmount, RoundingMode.Down);

            //Get Current Positions
            var LongPosition = Positions.Find("SSL Long");
            var ShortPosition = Positions.Find("SSL Short");

            //Two Line Cross Example
            var SSLUp = ssl._sslUp.Last(1);
            var PrevSSLUp = ssl._sslUp.Last(2);
            var SSLDown = ssl._sslDown.Last(1);
            var PrevSSLDown = ssl._sslDown.Last(2);

            //Check for trade signal
            if (SSLUp > SSLDown && PrevSSLUp < PrevSSLDown)
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, TradeAmount, "SSL Long", 1.5 * PrevATR, PrevATR);
                if (ShortPosition != null)
                {
                    ClosePosition(ShortPosition);
                }
            }
            else if (SSLUp < SSLDown && PrevSSLUp > PrevSSLDown)
            {
                ExecuteMarketOrder(TradeType.Sell, SymbolName, TradeAmount, "SSL Short", 1.5 * PrevATR, PrevATR);
                if (LongPosition != null)
                {
                    ClosePosition(LongPosition);
                }
            }

        }




        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}

