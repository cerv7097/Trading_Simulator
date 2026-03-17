using BlazorTradingApp.Models;

namespace BlazorTradingApp.Services
{
    /// <summary>
    /// Forecasts future price paths using Geometric Brownian Motion (GBM) — the same
    /// statistical process assumed by the Black-Scholes options-pricing model.
    ///
    /// How it works:
    ///   1. Computes log-returns from historical closing prices.
    ///   2. Estimates drift (μ) = mean log-return per period.
    ///   3. Estimates volatility (σ) = standard deviation of log-returns.
    ///   4. Simulates N independent price paths using:
    ///          P(t+1) = P(t) × exp((μ − σ²/2)·Δt + σ·√Δt·Z)
    ///      where Z ~ N(0,1) (standard normal random variable).
    ///   5. Returns the 10th, 25th, 50th (median), 75th, and 90th percentile across
    ///      all simulated paths at each future step.
    ///
    /// DISCLAIMER: This is a statistical model, not a crystal ball. GBM assumes
    /// constant drift and volatility and normally distributed returns — all
    /// simplifications that real markets routinely violate. Treat it as an
    /// educational illustration of possible outcomes, not financial advice.
    /// </summary>
    public class PriceForecaster
    {
        private readonly Random _rng = new();

        public ForecastResult Forecast(
            List<StockPrice> prices,
            int forwardPeriods = 30,
            int simulations    = 600,
            int historyBars    = 50)
        {
            if (prices.Count < 10)
                throw new InvalidOperationException("Not enough price history to build a forecast.");

            // Scale down simulations for large period counts to keep WASM responsive
            if (forwardPeriods > 200) simulations = 200;
            else if (forwardPeriods > 50) simulations = 400;

            // ── 1. Log-returns ───────────────────────────────────────────────────
            var logReturns = new List<double>();
            for (int i = 1; i < prices.Count; i++)
                logReturns.Add(Math.Log((double)(prices[i].Close / prices[i - 1].Close)));

            double mu    = logReturns.Average();
            double sigma = StdDev(logReturns);

            // GBM drift-corrected term (Itô's lemma correction: subtract ½σ²)
            double drift  = mu - 0.5 * sigma * sigma;
            double lastPx = (double)prices.Last().Close;

            // ── 2. Monte Carlo ───────────────────────────────────────────────────
            // paths[s][t] = simulated price at step t in simulation s
            double[][] paths = new double[simulations][];
            for (int s = 0; s < simulations; s++)
            {
                paths[s] = new double[forwardPeriods];
                double px = lastPx;
                for (int t = 0; t < forwardPeriods; t++)
                {
                    px         *= Math.Exp(drift + sigma * NextNormal());
                    paths[s][t] = px;
                }
            }

            // ── 3. Percentile bands at each forward step ─────────────────────────
            TimeSpan step      = prices.Count >= 2
                ? prices[^1].Date - prices[^2].Date
                : TimeSpan.FromHours(1);
            DateTime anchorDate = prices.Last().Date;

            var projection = new List<ForecastPoint>();

            // Anchor point — connects history line to forecast bands
            projection.Add(new ForecastPoint
            {
                Date  = anchorDate,
                Price = lastPx,
                P10   = lastPx, P25 = lastPx,
                P75   = lastPx, P90 = lastPx
            });

            for (int t = 0; t < forwardPeriods; t++)
            {
                var col = paths.Select(p => p[t]).OrderBy(v => v).ToArray();
                int n   = col.Length;

                projection.Add(new ForecastPoint
                {
                    Date  = anchorDate + TimeSpan.FromTicks(step.Ticks * (t + 1)),
                    Price = col[(int)(n * 0.50)],   // median
                    P10   = col[(int)(n * 0.10)],
                    P25   = col[(int)(n * 0.25)],
                    P75   = col[(int)(n * 0.75)],
                    P90   = col[(int)(n * 0.90)]
                });
            }

            // ── 4. Recent history anchor ─────────────────────────────────────────
            int histStart = Math.Max(0, prices.Count - historyBars);
            var history = prices
                .Skip(histStart)
                .Select(p => new ForecastPoint { Date = p.Date, Price = (double)p.Close })
                .ToList();

            return new ForecastResult
            {
                History           = history,
                Projection        = projection,
                DriftPerPeriod    = mu,
                VolatilityPerPeriod = sigma,
                LastPrice         = prices.Last().Close
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private static double StdDev(List<double> values)
        {
            double mean   = values.Average();
            double sumSq  = values.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sumSq / (values.Count - 1));
        }

        /// <summary>Box-Muller transform — produces a standard normal sample.</summary>
        private double NextNormal()
        {
            double u1 = 1.0 - _rng.NextDouble();
            double u2 = 1.0 - _rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
}
