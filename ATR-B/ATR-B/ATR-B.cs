using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class ATRB : Robot
    {
        [Parameter(DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType atr_MovingAverageType { get; set; }
        [Parameter(DefaultValue = 14)]
        public int atr_Periods { get; set; }
        
        private AverageTrueRange atr;

        protected override void OnStart()
        {
            atr = Indicators.AverageTrueRange(atr_Periods, atr_MovingAverageType);
        }

        protected override void OnTick()
        {
            Print("Previous ATRB [0]", atr.Result.Last(1));
        }

        protected override void OnStop()
        {
        }
    }
}