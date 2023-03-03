using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using System.Threading;
 
namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SimulatorB : Robot
    {
        [Parameter("Volume", DefaultValue = 50000)]
        public int vol { get; set; }
        [Parameter("Slo-Mo Step (ms)", DefaultValue = 200)]
        public int slomoStep { get; set; }
        [Parameter("TP", DefaultValue = 50)]
        public double tp { get; set; }
        [Parameter("SL", DefaultValue = 75)]
        public double sl { get; set; }
        private int baseVol;
        public int mul = 0;
 
        protected override void OnStart()
        {
            baseVol = vol;
            Chart.MouseDown += OnChartMouseDown;
            Chart.MouseWheel += OnChartMouseWheel;
        }
 
        void OnChartMouseWheel(ChartMouseWheelEventArgs obj)
        {
            if (obj.CtrlKey)
            {
                if (obj.Delta > 0)
                    vol += baseVol;
                if (obj.Delta < 0)
                    vol -= baseVol;
                if (vol < baseVol)
                    vol = baseVol;
            }
 
            if (obj.AltKey)
            {
                if (obj.Delta > 0)
                    mul++;
                if (obj.Delta < 0)
                    mul--;
                if (mul < 0)
                    mul = 0;
                if (mul > 5)
                    mul = 5;
            }
        }
 
        void OnChartMouseDown(ChartMouseEventArgs obj)
        {
            if (obj.AltKey && !obj.CtrlKey && !obj.ShiftKey)
                ExecuteMarketOrder(TradeType.Buy, Symbol, vol, "", sl, tp);
            if (obj.CtrlKey && !obj.AltKey && !obj.ShiftKey)
                ExecuteMarketOrder(TradeType.Sell, Symbol, vol, "", sl, tp);
            if (obj.AltKey && obj.CtrlKey && !obj.ShiftKey)
                closeClosestPosition(obj.YValue);
 
            if (obj.ShiftKey && !obj.CtrlKey && obj.AltKey)
            {
                if (obj.YValue > Symbol.Bid)
                    PlaceStopOrder(TradeType.Buy, Symbol, vol, obj.YValue, "", sl, tp);
                else
                    PlaceLimitOrder(TradeType.Buy, Symbol, vol, obj.YValue, "", sl, tp);
            }
            if (obj.ShiftKey && obj.CtrlKey && !obj.AltKey)
            {
                if (obj.YValue > Symbol.Bid)
                    PlaceLimitOrder(TradeType.Sell, Symbol, vol, obj.YValue, "", sl, tp);
                else
                    PlaceStopOrder(TradeType.Sell, Symbol, vol, obj.YValue, "", sl, tp);
            }
            if (!obj.AltKey && !obj.CtrlKey && obj.ShiftKey)
                cancelClosestOrder(obj.YValue);
        }
 
        protected override void OnTick()
        {
            Chart.DrawStaticText("Dashboard", "Tot. Pips: " + pips() + "\tCurrent Volume: " + vol, VerticalAlignment.Top, HorizontalAlignment.Left, Color.White);
            Chart.DrawStaticText("slomo", "Current Slow Motion Level: " + mul + "\n" + ((slomoStep * mul != 0) ? ((double)slomoStep * mul / 1000).ToString() + " Second(s) per tick" : "No Slo-Mo"), VerticalAlignment.Top, HorizontalAlignment.Right, Color.White);
            Thread.Sleep(slomoStep * mul);
        }
 
        protected override void OnStop()
        {
 
        }
 
        private void closeClosestPosition(double price)
        {
            if (Positions.Count == 0)
                return;
            Position pos = Positions[0];
            foreach (var _pos in Positions)
            {
                if (Math.Abs(price - _pos.EntryPrice) < Math.Abs(price - pos.EntryPrice))
                    pos = _pos;
            }
            ClosePosition(pos);
        }
 
        private void cancelClosestOrder(double price)
        {
            if (PendingOrders.Count == 0)
                return;
            PendingOrder ord = PendingOrders[0];
            foreach (var _ord in PendingOrders)
            {
                if (Math.Abs(price - _ord.TargetPrice) < Math.Abs(price - ord.TargetPrice))
                    ord = _ord;
            }
            CancelPendingOrder(ord);
        }
 
        private double pips()
        {
            double pips = 0;
 
            foreach (var pos in Positions)
            {
                pips += pos.Pips;
            }
            foreach (var pos in History)
            {
                pips += pos.Pips;
            }
 
            return pips;
        }
 
    }
}