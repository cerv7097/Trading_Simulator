# TradeSim — Blazor WebAssembly Paper Trading Simulator

A paper trading simulator built with Blazor WebAssembly and C# that fetches live market data, runs a technical-analysis-based strategy, and forecasts future price paths using Monte Carlo simulation.

---

## Features

### Strategy Engine
- **SMA Crossover Strategy** — generates a BUY signal when the 20-period SMA crosses above the 50-period SMA, and a SELL signal on the reverse
- **Stop-Loss & Take-Profit** — automatically exits a position when price hits a user-defined percentage threshold
- **Position Sizing** — deploys a configurable percentage of the portfolio per trade
- **Slippage & Commission Modeling** — applies realistic fill prices and flat broker fees on every order
- **Max Drawdown Tracking** — calculates the largest peak-to-trough decline across the simulation

### Technical Indicators
- **SMA 20 / SMA 50** — short and long-term trend overlays on the price chart
- **RSI (14)** — Relative Strength Index with Wilder smoothing; overbought (>70) and oversold (<30) annotations
- **MACD (12, 26, 9)** — MACD line, signal line, and histogram

### Monte Carlo Price Forecasting
Implements **Geometric Brownian Motion (GBM)** — the same statistical model underlying Black-Scholes options pricing — to simulate 200–600 stochastic price paths one calendar month forward. Results are displayed as **P10 / P25 / P50 / P75 / P90 percentile bands**, giving a probabilistic view of potential price outcomes based on the asset's historical drift and volatility.

### Charts & Visualizations (ApexCharts)
- Interactive candlestick chart with SMA overlays and annotated BUY/SELL markers
- RSI panel with overbought/oversold reference lines
- MACD panel with histogram
- Portfolio equity curve (area chart)
- Monte Carlo forecast chart with confidence bands

### Results Dashboard
- Total profit/loss, closed trade count, win rate, and max drawdown stat cards
- Color-coded trade log table (fill price, shares, commission, profit, exit reason)
- Collapsible glossary explaining every term and acronym in plain English

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | Blazor WebAssembly (.NET 9) |
| Language | C# |
| Charts | Blazor-ApexCharts 6.0.1 |
| Styling | Bootstrap 5 + custom CSS (Inter & JetBrains Mono fonts) |
| Market Data | [Twelve Data API](https://twelvedata.com) |

---

## Project Structure

```
BlazorTradingApp/
├── Models/
│   ├── StockPrice.cs          # OHLCV + indicator fields (SMA, RSI, MACD)
│   ├── Trade.cs               # Individual trade record
│   ├── SimulationConfig.cs    # User-configurable strategy parameters
│   ├── SimulationResult.cs    # Backtest output (trades, equity curve, drawdown)
│   └── ForecastResult.cs      # GBM forecast with percentile bands
├── Services/
│   ├── TwelveDataService.cs   # Twelve Data API client (OHLCV, intervals)
│   ├── TradingSimulator.cs    # SMA/RSI/MACD calculation + strategy engine
│   └── PriceForecaster.cs     # Monte Carlo GBM forecasting engine
├── Pages/
│   └── Home.razor             # Main UI — config, charts, trade log
├── Layout/
│   ├── MainLayout.razor       # Top-bar layout
│   └── NavMenu.razor          # Navigation (minimal — single-page app)
└── wwwroot/
    ├── appsettings.json        # API key configuration
    ├── css/app.css             # Light financial theme
    └── index.html              # App entry point
```

---

## Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- A free API key from [Twelve Data](https://twelvedata.com)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/cerv7097/Trading_Simulator.git
   cd Trading_Simulator
   ```

2. **Add your API key**

   Open `wwwroot/appsettings.json` and replace the key:
   ```json
   {
     "TwelveData": {
       "ApiKey": "YOUR_API_KEY_HERE"
     }
   }
   ```

3. **Run the app**
   ```bash
   dotnet run
   ```
   Then open `http://localhost:5078` in your browser.

---

## Usage

1. Enter a **stock symbol** (e.g. `AAPL`, `TSLA`, `MSFT`) and select a **time interval**
2. Configure your strategy parameters — portfolio balance, risk %, stop-loss, take-profit, commission, and slippage
3. Click **Run Simulation**
4. Review the candlestick chart with BUY/SELL annotations, indicator panels, equity curve, Monte Carlo forecast, and trade log

---

## Disclaimer

This application is for **educational and paper trading purposes only**. The Monte Carlo forecast is a statistical model — it does not predict future prices. Nothing in this app constitutes financial advice.
