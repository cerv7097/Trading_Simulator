using BlazorTradingApp.Models;

namespace BlazorTradingApp.Services
{
    public class TradingSimulator
    {
        // ── Simple Moving Averages ───────────────────────────────────────────────
        public void CalculateSMAs(List<StockPrice> prices, int shortPeriod = 20, int longPeriod = 50)
        {
            for (int i = 0; i < prices.Count; i++)
            {
                if (i >= shortPeriod - 1)
                    prices[i].SMA20 = prices.Skip(i - shortPeriod + 1).Take(shortPeriod).Average(p => p.Close);

                if (i >= longPeriod - 1)
                    prices[i].SMA50 = prices.Skip(i - longPeriod + 1).Take(longPeriod).Average(p => p.Close);
            }
        }

        // ── RSI (Wilder smoothing) ───────────────────────────────────────────────
        public void CalculateRSI(List<StockPrice> prices, int period = 14)
        {
            if (prices.Count <= period) return;

            // Seed with simple averages for the first period
            decimal avgGain = 0, avgLoss = 0;
            for (int i = 1; i <= period; i++)
            {
                decimal change = prices[i].Close - prices[i - 1].Close;
                if (change > 0) avgGain += change;
                else avgLoss += Math.Abs(change);
            }
            avgGain /= period;
            avgLoss /= period;

            prices[period].RSI = avgLoss == 0 ? 100 : 100 - (100 / (1 + avgGain / avgLoss));

            // Wilder smoothing for subsequent bars
            for (int i = period + 1; i < prices.Count; i++)
            {
                decimal change = prices[i].Close - prices[i - 1].Close;
                decimal gain   = change > 0 ? change : 0;
                decimal loss   = change < 0 ? Math.Abs(change) : 0;

                avgGain = (avgGain * (period - 1) + gain) / period;
                avgLoss = (avgLoss * (period - 1) + loss) / period;

                prices[i].RSI = avgLoss == 0 ? 100 : 100 - (100 / (1 + avgGain / avgLoss));
            }
        }

        // ── MACD (EMA12 − EMA26, Signal EMA9) ───────────────────────────────────
        public void CalculateMACD(List<StockPrice> prices, int fast = 12, int slow = 26, int signal = 9)
        {
            if (prices.Count < slow) return;

            var emaFast = CalculateEMA(prices.Select(p => p.Close).ToList(), fast);
            var emaSlow = CalculateEMA(prices.Select(p => p.Close).ToList(), slow);

            // MACD line starts where the slow EMA has a value (index slow-1)
            var macdLine = new List<decimal?>(new decimal?[prices.Count]);
            for (int i = slow - 1; i < prices.Count; i++)
                macdLine[i] = emaFast[i] - emaSlow[i];

            // Signal line: EMA of MACD values
            var macdValues = macdLine.Where(m => m.HasValue).Select(m => m!.Value).ToList();
            var signalLine = CalculateEMA(macdValues, signal);

            int signalOffset = slow - 1; // index where MACD starts
            for (int i = 0; i < prices.Count; i++)
            {
                prices[i].MACD = macdLine[i];

                int sigIdx = i - signalOffset - (signal - 1);
                if (sigIdx >= 0 && sigIdx < signalLine.Count)
                {
                    prices[i].MACDSignal    = signalLine[sigIdx];
                    prices[i].MACDHistogram = macdLine[i] - signalLine[sigIdx];
                }
            }
        }

        private static List<decimal> CalculateEMA(List<decimal> values, int period)
        {
            var ema   = new List<decimal>(new decimal[values.Count]);
            decimal k = 2m / (period + 1);

            // Seed with simple average of first `period` values
            ema[period - 1] = values.Take(period).Average();

            for (int i = period; i < values.Count; i++)
                ema[i] = values[i] * k + ema[i - 1] * (1 - k);

            return ema;
        }

        // ── Strategy ─────────────────────────────────────────────────────────────
        public SimulationResult RunStrategy(List<StockPrice> prices, SimulationConfig config)
        {
            var result = new SimulationResult();
            var logs   = result.Logs;

            decimal portfolio  = config.PortfolioBalance;
            decimal peakValue  = portfolio;
            decimal maxDD      = 0;
            decimal maxDDPct   = 0;

            bool    holding    = false;
            decimal buyPrice   = 0;
            decimal shares     = 0;
            decimal stopPrice  = 0;
            decimal targetPrice = 0;

            for (int i = 1; i < prices.Count; i++)
            {
                var prev = prices[i - 1];
                var curr = prices[i];

                // ── Entry: SMA20 crosses above SMA50 ─────────────────────────────
                bool crossoverBuy = !holding
                    && prev.SMA20.HasValue && prev.SMA50.HasValue
                    && curr.SMA20.HasValue && curr.SMA50.HasValue
                    && prev.SMA20 < prev.SMA50
                    && curr.SMA20 >= curr.SMA50;

                if (crossoverBuy)
                {
                    // Apply buy slippage (fill slightly above market)
                    buyPrice = curr.Close * (1 + config.SlippagePercent / 100);
                    decimal tradeValue = portfolio * (config.RiskPerTradePercent / 100);
                    shares = Math.Floor(tradeValue / buyPrice);

                    if (shares <= 0)
                    {
                        logs.Add($"[SKIP BUY] {curr.Date:MM/dd HH:mm} — insufficient funds");
                        continue;
                    }

                    portfolio -= shares * buyPrice + config.CommissionPerTrade;
                    stopPrice  = buyPrice * (1 - config.StopLossPercent / 100);
                    targetPrice = buyPrice * (1 + config.TakeProfitPercent / 100);
                    holding    = true;

                    result.Trades.Add(new Trade
                    {
                        Date       = curr.Date,
                        Action     = "BUY",
                        Price      = buyPrice,
                        Shares     = shares,
                        Commission = config.CommissionPerTrade
                    });
                    logs.Add($"[BUY]  {curr.Date:MM/dd HH:mm} | {shares} shares @ {buyPrice:C} | SL {stopPrice:C} | TP {targetPrice:C}");
                }
                else if (holding)
                {
                    // ── Check stop-loss ───────────────────────────────────────────
                    bool hitStopLoss   = curr.Low  <= stopPrice;
                    bool hitTakeProfit = curr.High >= targetPrice;

                    // ── Check signal exit: SMA20 crosses below SMA50 ─────────────
                    bool crossoverSell = prev.SMA20.HasValue && prev.SMA50.HasValue
                        && curr.SMA20.HasValue && curr.SMA50.HasValue
                        && prev.SMA20 > prev.SMA50
                        && curr.SMA20 <= curr.SMA50;

                    string? exitReason = null;
                    decimal sellPrice  = 0;

                    if (hitStopLoss)
                    {
                        exitReason = "StopLoss";
                        sellPrice  = stopPrice * (1 - config.SlippagePercent / 100);
                    }
                    else if (hitTakeProfit)
                    {
                        exitReason = "TakeProfit";
                        sellPrice  = targetPrice * (1 - config.SlippagePercent / 100);
                    }
                    else if (crossoverSell)
                    {
                        exitReason = "Signal";
                        sellPrice  = curr.Close * (1 - config.SlippagePercent / 100);
                    }

                    if (exitReason != null)
                    {
                        decimal proceeds = shares * sellPrice - config.CommissionPerTrade;
                        decimal profit   = proceeds - (shares * buyPrice + config.CommissionPerTrade);
                        portfolio += proceeds;
                        holding   = false;

                        result.Trades.Add(new Trade
                        {
                            Date       = curr.Date,
                            Action     = "SELL",
                            Price      = sellPrice,
                            Shares     = shares,
                            Commission = config.CommissionPerTrade,
                            Profit     = profit,
                            ExitReason = exitReason
                        });
                        logs.Add($"[SELL] {curr.Date:MM/dd HH:mm} | {shares} shares @ {sellPrice:C} | Profit {profit:C} ({exitReason})");
                        result.TotalProfit += profit;
                    }
                }

                // ── Mark-to-market portfolio value ───────────────────────────────
                decimal mtmValue = portfolio + (holding ? shares * curr.Close : 0);
                result.EquityCurve.Add(new EquityPoint { Date = curr.Date, PortfolioValue = mtmValue });

                // ── Drawdown ──────────────────────────────────────────────────────
                if (mtmValue > peakValue) peakValue = mtmValue;
                decimal dd    = peakValue - mtmValue;
                decimal ddPct = peakValue > 0 ? dd / peakValue * 100 : 0;
                if (dd > maxDD)    { maxDD    = dd;    }
                if (ddPct > maxDDPct) { maxDDPct = ddPct; }
            }

            // ── Auto-close open position at end of data ──────────────────────────
            if (holding)
            {
                decimal sellPrice = prices.Last().Close * (1 - config.SlippagePercent / 100);
                decimal proceeds  = shares * sellPrice - config.CommissionPerTrade;
                decimal profit    = proceeds - (shares * buyPrice + config.CommissionPerTrade);
                result.TotalProfit += profit;

                result.Trades.Add(new Trade
                {
                    Date       = prices.Last().Date,
                    Action     = "SELL",
                    Price      = sellPrice,
                    Shares     = shares,
                    Commission = config.CommissionPerTrade,
                    Profit     = profit,
                    ExitReason = "AutoSell"
                });
                logs.Add($"[AUTO-SELL] {prices.Last().Date:MM/dd HH:mm} | {shares} shares @ {sellPrice:C} | Profit {profit:C}");
            }

            result.MaxDrawdown        = maxDD;
            result.MaxDrawdownPercent = maxDDPct;
            logs.Add($"Total Profit: {result.TotalProfit:C} | Max Drawdown: {maxDD:C} ({maxDDPct:F1}%)");

            return result;
        }
    }
}
