using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTKS_Chart.Binance;
using CTKS_Chart.Binance.Data;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using CTKS_Chart.Views;
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

      IsSimulation = true;

      DrawChart = false;

      //DownloadCandles("BTCUSDT");
    }



    #region DataPath

    private string dataPath;

    public string DataPath
    {
      get { return dataPath; }
      set
      {
        if (value != dataPath)
        {
          dataPath = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion




    private List<Candle> cutCandles = new List<Candle>();
    protected override async Task LoadLayouts(Layout mainLayout)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, DrawingViewModel.CanvasHeight, DrawingViewModel.CanvasWidth, TradingBot.Asset);

   
      //var tradingView__ada_1D = $"D:\\Aplikacie\\Skusobne\\CTKS_Chart\\Data\\BINANCE ADAUSD, 1D.csv";

      var mainCandles = TradingHelper.ParseTradingView(DataPath);

      MainLayout.MaxValue = mainCandles.Max(x => x.High.Value);
      MainLayout.MinValue = mainCandles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

      var fromDate = new DateTime(2018, 9, 21);
      //fromDate = new DateTime(2021,8, 30);

      cutCandles = mainCandles.Where(x => x.CloseTime > fromDate).ToList();
      DrawingViewModel.ActualCandles = mainCandles.Where(x => x.CloseTime < fromDate).ToList();

      MainLayout.MaxValue = cutCandles.Max(x => x.High.Value);
      MainLayout.MinValue = cutCandles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

      DrawingViewModel.MaxValue = MainLayout.MaxValue;
      DrawingViewModel.MinValue = MainLayout.MinValue;
      DrawingViewModel.LockChart = true;
      DrawingViewModel.ShowATH = true;
      //TradingBot.Strategy.EnableManualPositions = false;

      var rangeFilterData = "C:\\Users\\Roman Pecho\\Desktop\\BINANCE ADAUSD, 1D.csv";
      TradingBot.Strategy.InnerStrategies.Add(new RangeFilterStrategy(rangeFilterData, TradingBot.Strategy));

      LoadSecondaryLayouts(mainLayout, mainCtks, fromDate);
      PreLoadCTks(fromDate);

      Simulate(cutCandles, InnerLayouts);
    }

    #region SimulateCandle

    private void SimulateCandle(List<Layout> secondaryLayouts, Candle candle)
    {
      DrawingViewModel.ActualCandles.Add(candle);

      if (DrawChart)
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



    #region Simulate

    private void Simulate(List<Candle> cutCandles, List<Layout> secondaryLayouts)
    {

      Task.Run(async () =>
      {
        for (int i = 0; i < cutCandles.Count; i++)
        {
          var actual = cutCandles[i];

          SimulateCandle(secondaryLayouts, actual);

          await Task.Delay(!IsSimulation || DrawChart ? 1 : 0);
        }
      });
    }

    #endregion

    private void DownloadCandles(string symbol)
    {
      this.binanceDataProvider.GetKlines(symbol, TimeSpan.FromMinutes(240));
    }

  }
}
