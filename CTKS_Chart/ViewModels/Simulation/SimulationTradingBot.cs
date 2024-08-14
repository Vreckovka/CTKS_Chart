using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CTKS_Chart.Binance;
using CTKS_Chart.Binance.Data;
using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Trading;
using CTKS_Chart.Views;
using CTKS_Chart.Views.Simulation;
using Logger;
using Microsoft.Expression.Interactivity.Core;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;

namespace CTKS_Chart.ViewModels
{
  public static class SimulationTradingBot
  {

    public static (List<Candle> candles, List<Candle> cutCandles, List<Candle> allCandles) GetSimulationCandles(int minutes, string dataPath, string symbol, DateTime fromDate)
    {
      var timeframe = (TimeFrame)minutes;

      if (minutes == 5)
        timeframe = TimeFrame.m5;

      var mainCandles = TradingViewHelper.ParseTradingView(timeframe, dataPath, symbol, saveData: true);

      var candles = mainCandles.Where(x => x.CloseTime < fromDate).ToList();
      var cutCandles = mainCandles.Where(x => x.CloseTime > fromDate).ToList();


      return (candles, cutCandles, mainCandles);
    }
  }

  public class SimulationTradingBot<TPosition, TStrategy> : TradingBotViewModel<TPosition, TStrategy>, ISimulationTradingBot
    where TPosition : Position, new()
    where TStrategy : BaseSimulationStrategy<TPosition>, new()
  {
    string results;
    public event EventHandler Finished;
    public bool SaveResults { get; set; }

    AIBot BUY_BOT;
    AIBot SELL_BOT;

    public SimulationTradingBot(
      TradingBot<TPosition, TStrategy> tradingBot,
      ILogger logger,
      IWindowManager windowManager,
      BinanceBroker binanceBroker,
      IViewModelsFactory viewModelsFactory) :
      base(tradingBot, logger, windowManager, binanceBroker, viewModelsFactory)
    {

      IsSimulation = true;

      TradingBot.Strategy.EnableRangeFilterStrategy = true;
      DrawChart = false;

      results = $"{TradingBot.Asset.Symbol}_simulation_results.txt";

      //DownloadCandles(TradingBot.Asset.Symbol, TimeSpan.FromMinutes(15));

      if (string.IsNullOrEmpty(DisplayName))
      {
        DisplayName = tradingBot.Asset.Symbol;
      }

      if (tradingBot.Strategy is AIStrategy aIStrategy)
      {
        BUY_BOT = aIStrategy.BuyAIBot;
        SELL_BOT = aIStrategy.SellAIBot;
      }
    }

    #region Properties

    public ObservableCollection<SimulationResult> SimulationResults { get; } = new ObservableCollection<SimulationResult>();

    private string displayName;
    public string DisplayName
    {
      get
      {
        return displayName;
      }
      set
      {
        if (value != displayName)
        {
          displayName = value;
          RaisePropertyChanged();
        }
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

    #region Delay

    private int delay = 1;

    public int Delay
    {
      get { return delay; }
      set
      {
        if (value != delay)
        {
          delay = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public DateTime FromDate { get; set; } = new DateTime(2018, 9, 21);
    public int Minutes { get; set; } = 720;
    public double SplitTake { get; set; }

    #endregion

    #region Commnads


    #region ShowStatistics

    private ActionCommand<SimulationResult> showStatistics;

    public ICommand ShowStatistics
    {
      get
      {
        return showStatistics ??= new ActionCommand<SimulationResult>(OnShowStatistics);
      }
    }

    protected virtual void OnShowStatistics(SimulationResult simulationResult)
    {
      windowManager.ShowPrompt<SimulationStatisticsView>(new SimulationStatisticsViewModel(TradingBot.Asset, simulationResult.DataPoints), 1000, 1000);
    }

    #endregion

    #endregion

    #region Methods

    public void HeatBot(IEnumerable<Candle> simulateCandles, AIStrategy aIStrategy)
    {
      var dailyCandles = TradingViewHelper.ParseTradingView(TimeFrame.D1, $"Data\\Indicators\\{Asset.IndicatorDataPath}, 1D.csv", Asset.Symbol, saveData: true);

      var lastDailyCandles = dailyCandles
        .Where(x => x.CloseTime < simulateCandles.First().CloseTime)
        .TakeLast(aIStrategy.takeLastDailyCandles)
        .ToList();

      aIStrategy.lastDailyCandles = lastDailyCandles;
      aIStrategy.lastDailyCandle = lastDailyCandles.Last();
    }

    public void InitializeBot()
    {
      var mainCtks = new Ctks(MainLayout, MainLayout.TimeFrame, TradingBot.Asset);

      LoadSecondaryLayouts(FromDate);
      PreLoadCTks(FromDate);

      MainLayout.Ctks = mainCtks;
      SelectedLayout = MainLayout;
    }

    #region LoadLayouts

    protected override async Task LoadLayouts()
    {
      InitializeBot();

      var dailyCandles = TradingViewHelper.ParseTradingView(TimeFrame.D1, $"Data\\Indicators\\{Asset.IndicatorDataPath}, 1D.csv", Asset.Symbol, saveData: true);
      var allCandles = SimulationTradingBot.GetSimulationCandles(
         Minutes,
         DataPath, Asset.Symbol, FromDate);

      var cutCandles = allCandles.cutCandles;
      var candles = allCandles.candles;
      var mainCandles = allCandles.allCandles;

      DrawingViewModel.ActualCandles = candles;

      var unixDiff = candles[1].UnixTime - candles[0].UnixTime;

      MainLayout.MaxValue = candles.Max(x => x.High.Value);
      MainLayout.MinValue = candles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

      if (DrawingViewModel.MaxValue == 0)
      {
        DrawingViewModel.MaxValue = MainLayout.MaxValue;
        DrawingViewModel.MinValue = MainLayout.MinValue;
      }

      DrawingViewModel.MaxUnix = candles.Max(x => x.UnixTime) + (unixDiff * 20);
      DrawingViewModel.MinUnix = DrawingViewModel.MaxUnix - (unixDiff * 100);

      DrawingViewModel.LockChart = true;


      var rangeAdaFilterData = "BINANCE ADAUSDT, 1D.csv";
      var rangeBtcFilterData = "INDEX BTCUSD, 1D.csv";

      TradingBot.Strategy.InnerStrategies.Add(new RangeFilterStrategy<TPosition>(rangeAdaFilterData, Asset.Symbol, rangeBtcFilterData, TradingBot.Strategy));


      var simulateCandles = cutCandles.ToList();

      if (SplitTake != 0)
      {
        var take = (int)(mainCandles.Count / SplitTake);

        simulateCandles = cutCandles.Take(take).ToList();
      }

      if (TradingBot.Strategy is AIStrategy aIStrategy)
      {
        HeatBot(cutCandles, aIStrategy);
      }


      Simulate(simulateCandles);
    }

    #endregion


    #region SimulateCandle

    public void SimulateCandle(Candle candle)
    {
      DrawingViewModel.ActualCandles.Add(candle);

      if (DrawChart)
      {
        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          RenderLayout(candle);
        });
      }
      else
      {
        RenderLayout(candle);
      }

    }

    #endregion

    #region LoadSimulationResults

    public void LoadSimulationResults()
    {
      SimulationResults.Clear();

      if (File.Exists(results))
      {
        var content = File.ReadAllLines(results);

        foreach (var line in content)
        {
          var result = JsonSerializer.Deserialize<SimulationResult>(line);

          result.RunTime = TimeSpan.FromTicks(result.RunTimeTicks);
          SimulationResults.Add(result);
        }

        SimulationResults.LinqSortDescending(x => x.Date);
      }
    }

    #endregion

    #region Simulate

    public CancellationTokenSource cts;
    IDisposable disposable;
    DateTime lastElapsed;

    private async void Simulate(List<Candle> cutCandles)
    {
      try
      {
        RunningTime = new TimeSpan();
        cts?.Cancel();
        cts = new CancellationTokenSource();
        stopRequested = false;

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

        if (DrawChart)
        {
          if (DrawingViewModel.MaxValue == 0)
          {
            DrawingViewModel.OnRestChart();
          }

          DrawingViewModel.OnRestChartX();
        }

        var result = new SimulationResult()
        {
          BotName = DisplayName
        };

        IsPaused = false;

        await Task.Run(async () =>
        {
          DateTime lastDay = DateTime.MinValue;
          for (int i = 0; i < cutCandles.Count; i++)
          {
            try
            {
              var actual = cutCandles[i];

              if (cts.IsCancellationRequested)
                return;


              SimulateCandle(actual);


              if (cts.IsCancellationRequested)
                return;

              await Task.Delay(!IsSimulation || DrawChart ? Delay : 0);

              if (lastDay < actual.OpenTime.Date)
              {
                lastDay = actual.OpenTime.Date;
                result.DataPoints.Add(new SimulationResultDataPoint()
                {
                  Date = lastDay,
                  TotalNative = TradingBot.Strategy.TotalNativeAsset,
                  TotalValue = TradingBot.Strategy.TotalValue,
                  TotalNativeValue = TradingBot.Strategy.TotalNativeAssetValue,
                  Close = actual.Close.Value
                });
              }
              if (TradingBot.Strategy.TotalValue > result.MaxValue.Value)
              {
                //Ignore high after last market low
                if (actual.OpenTime < new DateTime(2023, 5, 1))
                {
                  result.MaxValue.Value = TradingBot.Strategy.TotalValue;
                  result.MaxValue.Candle = actual;
                  result.LowAfterMaxValue = new SimulationResultValue(decimal.MaxValue);
                }
              }
              else if (TradingBot.Strategy.TotalValue < result.LowAfterMaxValue.Value)
              {
                result.LowAfterMaxValue.Value = TradingBot.Strategy.TotalValue;
                result.MaxValue.Candle = actual;
              }
              disposable?.Dispose();
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
              Logger.Log(ex);
            }
          }
        }, cts.Token);

        if (!cts.IsCancellationRequested && SaveResults)
        {
          DrawingViewModel.OnRestChart();
          DrawingViewModel.Render();

          result.TotalValue = TradingBot.Strategy.TotalValue;
          result.TotalProfit = TradingBot.Strategy.TotalProfit;
          result.TotalNativeValue = TradingBot.Strategy.TotalNativeAssetValue;
          result.TotalNative = TradingBot.Strategy.TotalNativeAsset;
          result.RunTime = RunningTime;
          result.RunTimeTicks = RunningTime.Ticks;
          result.Date = DateTime.Now;
          result.Drawdawn = TradingBot.Strategy.MaxDrawdawnFromMaxTotalValue;

          var json = JsonSerializer.Serialize(result);

          if (File.Exists(results))
          {
            using (StreamWriter w = File.AppendText(results))
            {
              w.WriteLine(JsonSerializer.Serialize(result));
            }
          }
          else
          {
            File.WriteAllText(results, json);
          }

          SimulationResults.Add(result);
          SimulationResults.LinqSortDescending(x => x.Date);
        }

        Finished?.Invoke(this, null);
      }
      catch(TaskCanceledException)
      {

      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }

      if(stopRequested)
      {

        var simulationStrategy = new TStrategy();

        simulationStrategy.EnableRangeFilterStrategy = true;
        simulationStrategy.Asset = TradingBot.Asset;
        Layouts.Clear();
        InnerLayouts.Clear();


        if (simulationStrategy is AIStrategy aIStrategy)
        {
          aIStrategy.BuyAIBot = BUY_BOT;
          aIStrategy.SellAIBot = SELL_BOT;
        }

        TradingBot.Strategy = simulationStrategy;
      }
    }

    #endregion

    #region Stop

    public bool stopRequested = false;
    public override void Stop()
    {
      cts?.Cancel();
      disposable?.Dispose();
      IsPaused = true;
      stopRequested = true;

    }

    #endregion


    #endregion
  }
}
