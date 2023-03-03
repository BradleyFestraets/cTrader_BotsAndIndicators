using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo.Indicators
{
    [Cloud("SSLDown", "SSLUp")]
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class SSLChannel : Indicator
    {
        //////////////////////////////////////////////////////////////////////// PARAMETERS
        [Parameter("Length", DefaultValue = 10)]
        public int _length { get; set; }
        [Parameter("MA Type", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType _MAType { get; set; }

        //////////////////////////////////////////////////////////////////////// OUTPUTS
        [Output("SSLDown", LineColor = "Red")]
        public IndicatorDataSeries _sslDown { get; set; }
        [Output("SSLUp", LineColor = "Green")]
        public IndicatorDataSeries _sslUp { get; set; }

        private MovingAverage _maHigh, _maLow;
        private IndicatorDataSeries _hlv;
        //////////////////////////////////////////////////////////////////////// INITIALIZE
        protected override void Initialize()
        {
            _maHigh = Indicators.MovingAverage(Bars.HighPrices, _length, _MAType);
            _maLow = Indicators.MovingAverage(Bars.LowPrices, _length, _MAType);
            _hlv = CreateDataSeries();
        }
        //////////////////////////////////////////////////////////////////////// CALCULATE
        public override void Calculate(int index)
        {
            _hlv[index] = Bars.ClosePrices[index] > _maHigh.Result[index] ? 1 : Bars.ClosePrices[index] < _maLow.Result[index] ? -1 : _hlv[index - 1];
            _sslDown[index] = _hlv[index] < 0 ? _maHigh.Result[index] : _maLow.Result[index];
            _sslUp[index] = _hlv[index] < 0 ? _maLow.Result[index] : _maHigh.Result[index];
        }
    }
}
