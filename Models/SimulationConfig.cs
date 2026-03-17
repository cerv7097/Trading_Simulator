namespace BlazorTradingApp.Models
{
    public class SimulationConfig
    {
        public decimal PortfolioBalance { get; set; } = 10000m;
        public decimal RiskPerTradePercent { get; set; } = 10m;   // % of portfolio to deploy per trade
        public decimal StopLossPercent { get; set; } = 2m;        // exit if price drops X% below buy
        public decimal TakeProfitPercent { get; set; } = 5m;      // exit if price rises X% above buy
        public decimal CommissionPerTrade { get; set; } = 1m;     // flat fee per trade in $
        public decimal SlippagePercent { get; set; } = 0.1m;      // % slippage on fills
    }
}
