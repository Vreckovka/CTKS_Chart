using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTKS_Chart.Binance;
using CTKS_Chart.Binance.Data;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using Logger;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;

namespace CTKS_Chart.ViewModels
{
  public class SimulationTradingBot : TradingBotViewModel
  {
    private readonly BinanceDataProvider binanceDataProvider;

    public SimulationTradingBot(
      TradingBot tradingBot,
      ILogger logger,
      IWindowManager windowManager,
      BinanceBroker binanceBroker,
      BinanceDataProvider binanceDataProvider,
      IViewModelsFactory viewModelsFactory) :
      base(tradingBot, logger, windowManager, binanceBroker, viewModelsFactory)
    {
      this.binanceDataProvider = binanceDataProvider ?? throw new ArgumentNullException(nameof(binanceDataProvider));

      drawChart = true;
      IsSimulation  = false;
    }

   
    private List<Candle> cutCandles = new List<Candle>();
    protected override async Task LoadLayouts(Layout mainLayout)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, CanvasHeight, CanvasWidth, TradingBot.Asset);

      var tradingView__ada_1D = $"ADAUSDT-240-generated.csv";


      var mainCandles = TradingHelper.ParseTradingView(tradingView__ada_1D);

      MainLayout.MaxValue = mainCandles.Max(x => x.High.Value);
      MainLayout.MinValue = mainCandles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

      var fromDate = new DateTime(2018, 9, 21);

      cutCandles = mainCandles.Where(x => x.CloseTime > fromDate).ToList();
      DrawingViewModel.ActualCandles = mainCandles.Where(x => x.CloseTime < fromDate).ToList();

      MainLayout.MaxValue = cutCandles.Max(x => x.High.Value);
      MainLayout.MinValue = cutCandles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

      DrawingViewModel.MaxValue = MainLayout.MaxValue;
      DrawingViewModel.MinValue = MainLayout.MinValue;
      DrawingViewModel.LockChart = true;

    



      TradingBot.Strategy.InnerStrategies.Add(new RangeFilterStrategy("C:\\Users\\Roman Pecho\\Desktop\\BINANCE ADAUSD, 1D.csv", TradingBot.Strategy));

      LoadSecondaryLayouts(mainLayout, mainCtks);

      Simulate(cutCandles, InnerLayouts, IsSimulation || drawChart ? 2 : 0);
    }

    #region SimulateCandle

    private void SimulateCandle(List<Layout> secondaryLayouts, Candle candle)
    {
      DrawingViewModel.ActualCandles.Add(candle);
      //CalculateActualProfits(candle);

      if(drawChart)
      {
        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          RenderLayout(secondaryLayouts, candle);
        });
      }
      else
      {
        RenderLayout(secondaryLayouts, candle);
      }
     
    }

    #endregion

    private void CalculateActualProfits(Candle actual)
    {
      foreach (var position in TradingBot.Strategy.ActualPositions)
      {
        var filledSells = position.OpositPositions.Where(x => x.State == PositionState.Filled).ToList();

        var realizedProfit = filledSells.Sum(x => x.OriginalPositionSize + x.Profit);
        var leftSize = position.OpositPositions.Where(x => x.State == PositionState.Open).Sum(x => x.PositionSizeNative);
        var fees = position.Fees ?? 0 + filledSells.Sum(x => x.Fees ?? 0);

        var profit = (realizedProfit + (leftSize * actual.Close.Value)) - position.OriginalPositionSize - fees;
        position.ActualProfit = profit;
      }
    }

    #region Simulate

    private void Simulate(List<Candle> cutCandles, List<Layout> secondaryLayouts, int delay = 500)
    {

      Task.Run(async () =>
      {
        for (int i = 0; i < cutCandles.Count; i++)
        {
          var actual = cutCandles[i];

          SimulateCandle(secondaryLayouts, actual);

         await Task.Delay(delay);
        }
      });
    }

    #endregion

    private void DownloadCandles()
    {
      bool download = false;

      if (download)
        this.binanceDataProvider.GetKlines("ADAUSDT", TimeSpan.FromMinutes(240));

    }

  }
}
