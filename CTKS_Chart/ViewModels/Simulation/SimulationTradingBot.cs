﻿using System;
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
using CTKS_Chart.Binance;
using CTKS_Chart.Binance.Data;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using CTKS_Chart.Views;
using Logger;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;

namespace CTKS_Chart.ViewModels
{
  public class SimulationResultValue
  {
    public SimulationResultValue()
    {

    }

    public SimulationResultValue(decimal value)
    {
      Value = value;
    }

    public decimal Value { get; set; }
    public Candle Candle { get; set; }
  }
  public class SimulationResult
  {
    public SimulationResult()
    {
    }

    public decimal TotalValue { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalNativeValue { get; set; }
    public decimal TotalNative { get; set; }
    public TimeSpan RunTime { get; set; }
    public long RunTimeTicks { get; set; }
    public DateTime Date { get; set; }
    public string BotName { get; set; }

    public SimulationResultValue MaxValue { get; set; } = new SimulationResultValue();
    public SimulationResultValue LowAfterMaxValue { get; set; } = new SimulationResultValue(decimal.MaxValue);

    public decimal Drawdawn
    {
      get
      {
        if (MaxValue.Value > 0)
          return (MaxValue.Value - LowAfterMaxValue.Value) / MaxValue.Value * 100;

        return 0;
      }
    }
  }

  public class SimulationTradingBot<TPosition, TStrategy> : TradingBotViewModel<TPosition, TStrategy>
    where TPosition : Position, new()
    where TStrategy : BaseSimulationStrategy<TPosition>, new()
  {
    private readonly BinanceDataProvider binanceDataProvider;
    string results;

    public SimulationTradingBot(
      TradingBot<TPosition, TStrategy> tradingBot,
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

      results = $"{TradingBot.Asset.Symbol}_simulation_results.txt";
      LoadSimulationResults();
      //DownloadCandles(TradingBot.Asset.Symbol, TimeSpan.FromMinutes(15));

      if (string.IsNullOrEmpty(DisplayName))
      {
        DisplayName = tradingBot.Asset.Symbol;
      }
    }

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

    #region LoadLayouts

    private List<Candle> cutCandles = new List<Candle>();
    protected override async Task LoadLayouts(CtksLayout mainLayout)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, TradingBot.Asset);

      var mainCandles = TradingViewHelper.ParseTradingView(TimeFrame.H4, DataPath);

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

      DrawingViewModel.DrawingSettings.ShowAveragePrice = false;
      DrawingViewModel.DrawingSettings.ShowAutoPositions = false;
      DrawingViewModel.DrawingSettings.ShowManualPositions = false;

      DrawingViewModel.LockChart = true;
      DrawingViewModel.DrawingSettings.ShowATH = true;

      var rangeAdaFilterData = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\bin\\Debug\\netcoreapp3.1\\BINANCE ADAUSDT, 1D.csv";
      var rangeBtcFilterData = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\bin\\Debug\\netcoreapp3.1\\INDEX BTCUSD, 1D.csv";

      TradingBot.Strategy.InnerStrategies.Add(new RangeFilterStrategy<TPosition>(rangeAdaFilterData, rangeBtcFilterData, TradingBot.Strategy));

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

    #region LoadSimulationResults

    private void LoadSimulationResults()
    {
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
        DrawingViewModel.OnRestChart();
      }

      var result = new SimulationResult()
      {
        BotName = DisplayName
      };


      await Task.Run(async () =>
      {
        for (int i = 0; i < cutCandles.Count; i++)
        {
          var actual = cutCandles[i];

          if (cts.IsCancellationRequested)
            return;

          SimulateCandle(secondaryLayouts, actual);

          await Task.Delay(!IsSimulation || DrawChart ? Delay : 0);


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

      if (!cts.IsCancellationRequested)
      {
        DrawingViewModel.OnRestChart();
        DrawingViewModel.RenderOverlay();

        result.TotalValue = TradingBot.Strategy.TotalValue;
        result.TotalProfit = TradingBot.Strategy.TotalProfit;
        result.TotalNativeValue = TradingBot.Strategy.TotalNativeAssetValue;
        result.TotalNative = TradingBot.Strategy.TotalNativeAsset;
        result.RunTime = RunningTime;
        result.RunTimeTicks = RunningTime.Ticks;
        result.Date = DateTime.Now;

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
    }

    #endregion

    #region Stop

    public override void Stop()
    {
      cts?.Cancel();
      disposable?.Dispose();
      var simulationStrategy = new TStrategy();

      simulationStrategy.Asset = TradingBot.Asset;
      Layouts.Clear();
      InnerLayouts.Clear();

      TradingBot.Strategy = simulationStrategy;
    }

    #endregion

    private void DownloadCandles(string symbol, TimeSpan timeSpan)
    {
      this.binanceDataProvider.GetKlines(symbol, timeSpan);
    }

  }
}
