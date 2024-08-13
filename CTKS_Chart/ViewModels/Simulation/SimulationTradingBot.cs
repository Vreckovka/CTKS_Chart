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

      if(tradingBot.Strategy is  AIStrategy aIStrategy)
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
    public TimeFrame DataTimeFrame { get; set; } = TimeFrame.Null;
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

    #region LoadLayouts

    private List<Candle> simulateCandles = new List<Candle>();
    private decimal startingBudget = 0;

    protected override async Task LoadLayouts(CtksLayout mainLayout)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, TradingBot.Asset);

      var dailyCandles = TradingViewHelper.ParseTradingView(TimeFrame.D1, $"Data\\Indicators\\{Asset.IndicatorDataPath}, 1D.csv", Asset.Symbol, saveData: true);
      var mainCandles = TradingViewHelper.ParseTradingView(DataTimeFrame, DataPath, Asset.Symbol, saveData: true);

      //fromDate = new DateTime(2021,8, 30);

      var cutCandles = mainCandles.Where(x => x.CloseTime > FromDate).ToList();
      var candles = mainCandles.Where(x => x.CloseTime < FromDate).ToList();

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

      LoadSecondaryLayouts(FromDate);
      PreLoadCTks(FromDate);

      mainLayout.Ctks = mainCtks;
      //Layouts.Add(mainLayout);
      SelectedLayout = mainLayout;

      var simulateCandles = cutCandles.ToList();

      if (SplitTake != 0)
      {
        var take = (int)(mainCandles.Count / SplitTake);

        simulateCandles = cutCandles.Take(take).ToList();
      }

      if (TradingBot.Strategy is AIStrategy aIStrategy)
      {
        var lastDailyCandles = dailyCandles
          .Where(x => x.CloseTime < simulateCandles.First().CloseTime)
          .TakeLast(aIStrategy.takeLastDailyCandles)
          .ToList();

        aIStrategy.lastDailyCandles = lastDailyCandles;
        aIStrategy.lastDailyCandle = lastDailyCandles.Last();
      }

      startingBudget = TradingBot.Strategy.StartingBudget;

      Simulate(simulateCandles, InnerLayouts);
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

    CancellationTokenSource cts;
    IDisposable disposable;
    DateTime lastElapsed;

    private async void Simulate(List<Candle> cutCandles, List<CtksLayout> secondaryLayouts)
    {
      try
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


        await Task.Run(async () =>
        {
          DateTime lastDay = DateTime.MinValue;
          for (int i = 0; i < cutCandles.Count; i++)
          {
            var actual = cutCandles[i];

            if (cts.IsCancellationRequested)
              return;

            SimulateCandle(secondaryLayouts, actual);

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
          }

          disposable?.Dispose();

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
      catch (Exception ex)
      {
        Logger.Log(ex);
      }
    }

    #endregion

    #region Stop

    public override void Stop()
    {
      cts?.Cancel();
      disposable?.Dispose();
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

    #endregion


    #endregion
  }
}
