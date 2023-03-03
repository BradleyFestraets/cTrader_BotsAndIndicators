using System;
using System.Linq;
using System.IO;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
 
// -------------------------------------------------------------------------------------------------
//     Ctrader Market Data Extractor v0.1
//     Inspired from https://ctrader.com/algos/cbots/show/588
//     Ahmed Ben Hamouda - XtendPlex 
//     https://www.xtendplex.com
// -------------------------------------------------------------------------------------------------
 
namespace cAlgo
{
    [Robot(TimeZone = TimeZones.WEuropeStandardTime, AccessRights = AccessRights.FileSystem)]
    public class MarketDataExtractorB : Robot
    {
 
 
        public enum enumModeType
        {
            DOMLive,
            BarChartHist,
            TickHist
        }
        // [Parameter("TypeMode", Group = "TypeMode Settings", DefaultValue = enumModeType.DOMLive)]
        //public enumModeType modeType { get; set; }
 
        [Parameter("Data Dir", DefaultValue = "C:\\Users\\BradleyF\\Documents\\Data extractor")]
        public string DataDir { get; set; }
 
 
        private string fiName;
        private System.IO.FileStream fstream;
        private System.IO.StreamWriter fwriter;
        private enumModeType extractMode = enumModeType.DOMLive;
 
 
        protected override void OnStart()
        {
 
            var ticktype = Bars.TimeFrame.ToString();
            extractMode = (IsBacktesting == false ? enumModeType.DOMLive : (ticktype.Contains("Tick") ? enumModeType.TickHist : enumModeType.BarChartHist));
            string csvhead = (extractMode.Equals(enumModeType.BarChartHist) ? "date,open,high,low,close,volume\n" : (extractMode.Equals(enumModeType.TickHist) ? "date,ask,bid\n" : "date,askPrice,bidPrice,Pricespread,askCount,bidCount,VwapAsk,VwapBid,AdjPriceSpread,volumeAsk,volumeBid,volumeSpread\n"));
            fiName = DataDir + "\\" + "export-" + Symbol.Name + "-" + ticktype + "-" + extractMode.ToString() + ".csv";
 
            if (System.IO.File.Exists(fiName) == false)
                System.IO.File.WriteAllText(fiName, csvhead);
 
            Print("fiName=" + fiName);
 
 
 
            fstream = File.Open(fiName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            Print("File is Open");
            fstream.Seek(0, SeekOrigin.End);
            fwriter = new System.IO.StreamWriter(fstream, System.Text.Encoding.UTF8, 1);
            Print("Fwriter is created");
            fwriter.AutoFlush = true;
            Print("done onStart()");
        }
 
        protected Tuple<double, double> vwap_price(cAlgo.API.Collections.IReadonlyList<cAlgo.API.MarketDepthEntry> mkentries)
        {
            double volumeSum = 0.0;
            double pvSum = 0.0;
            for (int i = 0; i < mkentries.Count; i++)
            {
                var domEntries = mkentries[i];
                pvSum += (double)domEntries.Price * (double)domEntries.Volume;
                volumeSum += (double)domEntries.Volume;
            }
            if (volumeSum == 0)
            {
                return Tuple.Create(0.0, 0.0);
            }
            else
            {
                return Tuple.Create((pvSum / volumeSum), volumeSum);
            }
        }
 
 
 
 
        protected override void OnTick()
        {
 
 
            if (extractMode.Equals(enumModeType.TickHist))
            {
                var sa = new System.Collections.Generic.List<string>();
                var barTime = Bars.OpenTimes.LastValue;
                //var barTime = Server.Time;
                var timestr = barTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
 
                sa.Add(timestr);
                sa.Add(Symbol.Ask.ToString("F6"));
                sa.Add(Symbol.Bid.ToString("F6"));
 
                var sout = string.Join(";", sa);
                fwriter.WriteLine(sout);
                fwriter.Flush();
            }
 
            else if (extractMode.Equals(enumModeType.DOMLive))
            {
 
                var sa = new System.Collections.Generic.List<string>();
                // var barTime = MarketSeries.OpenTime.LastValue;
                var barTime = Server.Time;
                var timestr = barTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
 
                MarketDepth mkdepth = MarketData.GetMarketDepth(Symbol.Name);
                var priceVolAdjAsk = vwap_price(mkdepth.AskEntries);
                var priceVolAdjBid = vwap_price(mkdepth.BidEntries);
                var priceAdjAsk = priceVolAdjAsk.Item1;
                var priceAdjBid = priceVolAdjBid.Item1;
                var priceAdjSpread = priceAdjAsk - priceAdjBid;
                var volAdjAsk = priceVolAdjAsk.Item2;
                var volAdjBid = priceVolAdjBid.Item2;
                var volAdjSpread = volAdjBid - volAdjAsk;
 
                if ((priceAdjAsk == 0.0) && (priceAdjBid == 0.0))
                    return;
 
 
                sa.Add(timestr);
                sa.Add(Symbol.Ask.ToString("F6"));
                sa.Add(Symbol.Bid.ToString("F6"));
                sa.Add(Symbol.Spread.ToString("F6"));
                sa.Add(mkdepth.AskEntries.Count.ToString("F2"));
                sa.Add(mkdepth.BidEntries.Count.ToString("F2"));
                sa.Add(priceAdjAsk.ToString("F6"));
                sa.Add(priceAdjBid.ToString("F6"));
                sa.Add(priceAdjSpread.ToString("F6"));
                sa.Add(volAdjAsk.ToString("F2"));
                sa.Add(volAdjBid.ToString("F2"));
                sa.Add(volAdjSpread.ToString("F2"));
 
                var sout = string.Join(";", sa);
                fwriter.WriteLine(sout);
                fwriter.Flush();
 
            }
 
        }
 
        protected override void OnBar()
        {
            if (extractMode.Equals(enumModeType.BarChartHist))
            {
                var sa = new System.Collections.Generic.List<string>();
                var barTime = Bars.OpenTimes.Last(1);
                // var barTime = Server.Time;
                var timestr = barTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                sa.Add(timestr);
                sa.Add(Bars.OpenPrices.Last(1).ToString("F6"));
                sa.Add(Bars.HighPrices.Last(1).ToString("F6"));
                sa.Add(Bars.LowPrices.Last(1).ToString("F6"));
                sa.Add(Bars.ClosePrices.Last(1).ToString("F6"));
                sa.Add(Bars.TickVolumes.Last(1).ToString("F6"));
 
                var sout = string.Join(",", sa);
                fwriter.WriteLine(sout);
                fwriter.Flush();
            }
        }
 
 
 
        protected override void OnStop()
        {
            Print("OnStop()");
            fwriter.Close();
            fstream.Close();
 
        }
    }
}