namespace BlazorTradingApp.Models
{
    public class ForecastResult
    {
        /// <summary>Last N closing prices used as the historical anchor on the chart.</summary>
        public List<ForecastPoint> History { get; set; } = new();

        /// <summary>Projected price paths as percentile bands starting from the last known price.</summary>
        public List<ForecastPoint> Projection { get; set; } = new();

        /// <summary>Average log-return per candle (annualised drift estimate).</summary>
        public double DriftPerPeriod { get; set; }

        /// <summary>Standard deviation of log-returns per candle (volatility estimate).</summary>
        public double VolatilityPerPeriod { get; set; }

        /// <summary>Last known closing price — the starting point of the projection.</summary>
        public decimal LastPrice { get; set; }
    }

    public class ForecastPoint
    {
        public DateTime Date  { get; set; }

        /// <summary>Used for history: actual close price. Used for projection: median (P50) path.</summary>
        public double Price   { get; set; }

        // Confidence-band percentiles (only populated on projection points)
        public double? P10    { get; set; }
        public double? P25    { get; set; }
        public double? P75    { get; set; }
        public double? P90    { get; set; }
    }
}
