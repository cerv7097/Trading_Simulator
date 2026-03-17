namespace BlazorTradingApp.Models
{
    public class SimulationResult
    {
        public List<string> Logs { get; set; } = new();
        public List<Trade> Trades { get; set; } = new();
        public decimal TotalProfit { get; set; }
        public decimal MaxDrawdown { get; set; }         // peak-to-trough $ drop
        public decimal MaxDrawdownPercent { get; set; }  // peak-to-trough %
        public List<EquityPoint> EquityCurve { get; set; } = new();
    }

    public class EquityPoint
    {
        public DateTime Date { get; set; }
        public decimal PortfolioValue { get; set; }
    }
}
