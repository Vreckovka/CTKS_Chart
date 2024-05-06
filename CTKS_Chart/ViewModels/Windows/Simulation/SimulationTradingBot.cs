using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
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
    }

    public string DisplayName
    {
      get
      {
        return TradingBot.Asset.Symbol;
      }
    }

    #region RunningTime

    private TimeSpan runningTime;

    public TimeSpan RunningTime
    {
      get { return runningTime; }
      set
      {
        if (value != runningTime)
        {
          runningTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    #region LoadLayouts

    private List<Candle> cutCandles = new List<Candle>();
    protected override async Task LoadLayouts(CtksLayout mainLayout)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, DrawingViewModel.CanvasHeight, DrawingViewModel.CanvasWidth, TradingBot.Asset);

      var mainCandles = TradingViewHelper.ParseTradingView(DataPath);

      var fromDate = new DateTime(2018, 9, 21);
      //fromDate = new DateTime(2021,8, 30);

      cutCandles = mainCandles.Where(x => x.CloseTime > fromDate).ToList();
      var candles = mainCandles.Where(x => x.CloseTime < fromDate).ToList();

      DrawingViewModel.ActualCandles = candles;

      var unixDiff = candles[1].UnixTime - candles[0].UnixTime;

      MainLayout.MaxValue = candles.Max(x => x.High.Value);
      MainLayout.MinValue = candles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

      DrawingViewModel.MaxValue = MainLayout.MaxValue;
      DrawingViewModel.MinValue = MainLayout.MinValue;
      DrawingViewModel.MaxUnix = candles.Max(x => x.UnixTime) + (unixDiff * 20);
      DrawingViewModel.MinUnix = DrawingViewModel.MaxUnix - (unixDiff * 100);

      DrawingViewModel.LockChart = true;
      DrawingViewModel.ShowATH = true;

      //Intersection precision testing
      TradingBot.Strategy.EnableManualPositions = false;


      var rangeFilterData = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\bin\\Debug\\netcoreapp3.1\\BINANCE ADAUSDT, 1D.csv";
      TradingBot.Strategy.InnerStrategies.Add(new RangeFilterStrategy(rangeFilterData, TradingBot.Strategy));

      LoadSecondaryLayouts(fromDate);
      PreLoadCTks(fromDate);

      mainLayout.Ctks = mainCtks;
      Layouts.Add(mainLayout);
      SelectedLayout = mainLayout;

      Simulate(cutCandles, InnerLayouts);
    }

    #endregion

    #region SimulateCandle

    private void SimulateCandle(List<CtksLayout> secondaryLayouts, Candle candle)
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

    public override void Start()
    {
      base.Start();
    }

    #region Simulate

    CancellationTokenSource cts;
    IDisposable disposable;
    DateTime lastElapsed;

    private void Simulate(List<Candle> cutCandles, List<CtksLayout> secondaryLayouts)
    {
      RunningTime = new TimeSpan();
      cts?.Cancel();
      cts = new CancellationTokenSource();

      disposable?.Dispose();

      lastElapsed = DateTime.Now;

      if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        disposable = Observable.Interval(TimeSpan.FromSeconds(0.25))
       .ObserveOnDispatcher()
       .Subscribe((x) =>
       {

         TimeSpan diff = DateTime.Now - lastElapsed;

         RunningTime = RunningTime.Add(diff);

         lastElapsed = DateTime.Now;
       });

      Task.Run(async () =>
      {
        for (int i = 0; i < cutCandles.Count; i++)
        {
          var actual = cutCandles[i];

          if (cts.IsCancellationRequested)
            return;

          SimulateCandle(secondaryLayouts, actual);

          await Task.Delay(!IsSimulation || DrawChart ? 1 : 0);
        }

        disposable?.Dispose();

      }, cts.Token);


    }

    #endregion

    public void Stop()
    {
      cts?.Cancel();
      disposable?.Dispose();
      var simulationStrategy = new SimulationStrategy();

      simulationStrategy.Asset = TradingBot.Asset;
      Layouts.Clear();
      InnerLayouts.Clear();

      TradingBot.Strategy = simulationStrategy;
    }

    private void DownloadCandles(string symbol)
    {
      this.binanceDataProvider.GetKlines(symbol, TimeSpan.FromMinutes(240));
    }

  }
}
