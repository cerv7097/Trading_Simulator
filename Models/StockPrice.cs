namespace BlazorTradingApp.Models
{
    public class StockPrice
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }

        // Indicators
        public decimal? SMA20 { get; set; }
        public decimal? SMA50 { get; set; }
        public decimal? RSI { get; set; }
        public decimal? MACD { get; set; }
        public decimal? MACDSignal { get; set; }
        public decimal? MACDHistogram { get; set; }
    }
}
