namespace BlazorTradingApp.Models
{
    public class Trade
    {
        public DateTime Date { get; set; }
        public string Action { get; set; } = "";   // "BUY" or "SELL"
        public decimal Price { get; set; }          // fill price (after slippage)
        public decimal Shares { get; set; }
        public decimal Commission { get; set; }
        public decimal? Profit { get; set; }        // net profit on SELL (after commission)
        public string? ExitReason { get; set; }     // "Signal", "StopLoss", "TakeProfit", "AutoSell"
    }
}
