﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
  public class CandleDictionaryJsonConverter : JsonConverter<Dictionary<long, Candle>>
  {
    public override Dictionary<long, Candle> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var result = new Dictionary<long, Candle>();

      // Ensure the reader is at the StartObject token
      if (reader.TokenType != JsonTokenType.StartObject)
      {
        throw new JsonException();
      }

      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject)
        {
          return result;
        }

        // Read the key as a string
        string keyJson = reader.GetString();
        var key = JsonSerializer.Deserialize<long>(keyJson, options);

        // Move to the value
        reader.Read();

        // Deserialize the value
        var value = JsonSerializer.Deserialize<Candle>(ref reader, options);

        // Add to dictionary
        result.Add(key, value);
      }

      throw new JsonException("Invalid JSON format for Dictionary<Candle, Candle>.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<long, Candle> value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();

      foreach (var kvp in value)
      {
        // Serialize the key as a string
        string keyJson = JsonSerializer.Serialize(kvp.Key, options);
        writer.WritePropertyName(keyJson);

        // Serialize the value
        JsonSerializer.Serialize(writer, kvp.Value, options);
      }

      writer.WriteEndObject();
    }
  }

  public class TimeFrameData
  {
    public TimeFrame TimeFrame { get; set; }

    public string Name { get; set; }
  }
  public static class SimulationTradingBot
  {
    public static (List<Candle> candles, List<Candle> cutCandles, List<Candle> allCandles) GetSimulationCandles(
      int minutes,
      string dataPath,
      string symbol,
      DateTime fromDate)
    {
      var timeframe = (TimeFrame)minutes;

      if (minutes == 5)
        timeframe = TimeFrame.m5;
      if (minutes == 1)
        timeframe = TimeFrame.m1;

      var mainCandles = TradingViewHelper.ParseTradingView(timeframe, dataPath, symbol, saveData: true);

      var candles = mainCandles.Where(x => x.CloseTime < fromDate).ToList();
      var cutCandles = mainCandles.Where(x => x.CloseTime > fromDate).ToList();


      return (candles, cutCandles, mainCandles);
    }

    public static List<Candle> GetIndicatorData(TimeFrameData timeFrameData, Asset asset)
    {
      return TradingViewHelper.ParseTradingView(timeFrameData.TimeFrame, $"Data\\Indicators\\{asset.IndicatorDataPath}, {timeFrameData.Name}.csv", asset.Symbol, saveData: true);
    }

    public static string GetIndicatorDataPath(TimeFrameData timeFrameData, Asset asset)
    {
      return $"Data\\Indicators\\{asset.IndicatorDataPath}, {timeFrameData.Name}.csv";
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

    #region PreLoadIntersections

    static Dictionary<Tuple<string, TimeFrame>, Dictionary<long, Tuple<List<CtksIntersection>, bool>>> preloadedIntersections = new Dictionary<Tuple<string, TimeFrame>, Dictionary<long, Tuple<List<CtksIntersection>, bool>>>();

    public void PreLoadIntersections(Tuple<string, TimeFrame> key, IList<Candle> simulationCandles)
    {
      lock (batton1)
      {
        if (!preloadedIntersections.ContainsKey(key))
        {
          var intersections = new Dictionary<long, Tuple<List<CtksIntersection>, bool>>();

          foreach (var candle in simulationCandles)
          {
            var inters = base.GetIntersections(candle, out var outdated);
            intersections.Add(candle.UnixTime, new Tuple<List<CtksIntersection>, bool>(inters, outdated));
          }


          preloadedIntersections.Add(key, intersections);
        }
      }
    }

    #endregion

    #region PreloadCandles

    static Dictionary<TimeFrame, Dictionary<Tuple<string, TimeFrame>, Dictionary<long, Candle>>> preloadedIndicatorCandles = new Dictionary<TimeFrame, Dictionary<Tuple<string, TimeFrame>, Dictionary<long, Candle>>>();

    static object batton1 = new object();

    public void PreloadCandles(
      Tuple<string, TimeFrame> key,
      IList<Candle> simulationCandles)
    {
      lock (batton1)
      {
        foreach (var indicatorTimeframe in IndicatorTimeframes)
        {
          if (!preloadedIndicatorCandles.ContainsKey(indicatorTimeframe))
          {
            preloadedIndicatorCandles.Add(indicatorTimeframe, new Dictionary<Tuple<string, TimeFrame>, Dictionary<long, Candle>>());
          }

          var preloadecandles = preloadedIndicatorCandles[indicatorTimeframe];

          if (!preloadecandles.ContainsKey(key))
          {
            var dir = "Preloaded simulation data";
            var fileName = Path.Combine(dir, $"{Asset.Symbol}-{key.Item2}-{indicatorTimeframe}_preloaded.txt");
            var indicatorCandles = new Dictionary<long, Candle>();

            if (File.Exists(fileName))
            {
              var json = File.ReadAllText(fileName);
              var options = new JsonSerializerOptions();
              options.Converters.Add(new CandleDictionaryJsonConverter());

              indicatorCandles = JsonSerializer.Deserialize<Dictionary<long, Candle>>(json, options);
            }
            else
            {

              if (!TradingViewHelper.LoadedData[key.Item1].ContainsKey(indicatorTimeframe))
              {
                var data = TradingViewHelper.ParseTradingView(indicatorTimeframe, 
                  SimulationTradingBot.GetIndicatorDataPath(TimeFrameDatas[indicatorTimeframe], Asset), Asset.Symbol,saveData: true);
              }

              foreach (var candle in simulationCandles)
              {
                var dailyCandle = base.GetCandle(new List<TimeFrame>() { indicatorTimeframe }, candle).FirstOrDefault();

                indicatorCandles.Add(candle.UnixTime, dailyCandle);
              }

              Directory.CreateDirectory(dir);

              var options = new JsonSerializerOptions();
              options.WriteIndented = true;
              options.Converters.Add(new CandleDictionaryJsonConverter());

              File.WriteAllText(fileName, JsonSerializer.Serialize(indicatorCandles, options));
            }

            preloadecandles.Add(key, indicatorCandles);
          }
        }
      }
    }

    #endregion

    protected override List<CtksIntersection> GetIntersections(Candle actual, out bool isOutdated)
    {
      var inter = preloadedIntersections[botKey][actual.UnixTime];

      isOutdated = inter.Item2;
      return inter.Item1;
    }

    protected override IList<Candle> GetCandle(IList<TimeFrame> indicatorTimeframes, Candle actualCandle)
    {
      var list = new List<Candle>();

      foreach (var timeFrame in indicatorTimeframes)
      {
        var unix = timeFrame == TimeFrame.D1 ?
          DateTimeHelper.DateTimeToUnixSeconds(actualCandle.OpenTime.AddDays(-1)) :
          actualCandle.UnixTime - actualCandle.UnixDiff;

        //There are missing candles in the data
        if (preloadedIndicatorCandles[timeFrame][botKey].TryGetValue(unix, out var candle))
        {
          list.Add(candle);
        }
        else if (preloadedIndicatorCandles[timeFrame][botKey].TryGetValue(actualCandle.UnixTime, out var candle1))
        {
          list.Add(candle1);
        }
      }

      return list;
    }

    #region HeatBot

    public void HeatBot(IEnumerable<Candle> simulateCandles, AIStrategy aIStrategy)
    {
      var dailyCandles = SimulationTradingBot.GetIndicatorData(TimeFrameDatas[TimeFrame.D1], Asset);

      var lastDailyCandles = dailyCandles
        .Where(x => x.CloseTime <= simulateCandles.First().CloseTime)
        .TakeLast(aIStrategy.takeLastDailyCandles)
        .ToList();

      aIStrategy.lastDailyCandles = lastDailyCandles;
    }

    #endregion

    #region InitializeBot
    Tuple<string, TimeFrame> botKey;
    public void InitializeBot(IList<Candle> simulationCandles)
    {
      var mainCtks = new Ctks(MainLayout, MainLayout.TimeFrame, TradingBot.Asset);

      var timeFrame = simulationCandles.First().TimeFrame;
      botKey = new Tuple<string, TimeFrame>(Asset.Symbol, timeFrame);

      LoadSecondaryLayouts(FromDate);

      MainLayout.Ctks = mainCtks;
      SelectedLayout = MainLayout;
    }

    #endregion

    public TimeFrame TimeFrame { get; set; }

    public Dictionary<TimeFrame, TimeFrameData> TimeFrameDatas { get; } = new Dictionary<TimeFrame, TimeFrameData>()
    {
      {TimeFrame.H4, new TimeFrameData() { TimeFrame = TimeFrame.H4, Name = "240" } },
      {TimeFrame.D1, new TimeFrameData() { TimeFrame = TimeFrame.D1, Name = "1D" } },
    };

    #region LoadLayouts

    protected override async Task LoadLayouts()
    {
      DateTime fromDate = DateTime.Now;
      double splitTake = 0;

      var asset = Asset;
      var dailyCandles = SimulationTradingBot.GetIndicatorData(TimeFrameDatas[TimeFrame.D1], asset);

      foreach (var indiFrame in TradingBotViewModel<Position, BaseStrategy<Position>>.IndicatorTimeframes)
      {
        SimulationTradingBot.GetIndicatorData(TimeFrameDatas[indiFrame], asset);
      }

      //ignore filter starting values of indicators
      var firstValidDate = dailyCandles.First(x => x.IndicatorData.RangeFilter.HighTarget > 0).CloseTime.AddDays(10);
      var lastValidDate = dailyCandles.Last(x => x.IndicatorData.RangeFilter.HighTarget > 0).CloseTime.AddDays(-10);

      fromDate = firstValidDate;


      var allCandles = SimulationTradingBot.GetSimulationCandles(
         Minutes,
         SimulationPromptViewModel.GetSimulationDataPath(Asset.Symbol, Minutes.ToString()), Asset.Symbol, fromDate);

      var simulateCandles = allCandles.cutCandles.Where(x => x.OpenTime.Date > firstValidDate.Date && x.OpenTime.Date < lastValidDate.Date).ToList();
      var candles = allCandles.candles;
      var mainCandles = allCandles.allCandles.Where(x => x.OpenTime.Date > firstValidDate.Date).ToList();


      var timeFrame = simulateCandles.First().TimeFrame;
      var key = new Tuple<string, TimeFrame>(Asset.Symbol, timeFrame);

      LoadSecondaryLayouts(firstValidDate);
      PreloadCandles(key, mainCandles);
      PreLoadIntersections(key, mainCandles);


      if (splitTake != 0)
      {
        var take = (int)(mainCandles.Count / splitTake);

        simulateCandles = simulateCandles.Take(take).ToList();
      }

      FromDate = fromDate;
      InitializeBot(simulateCandles);

      if (TradingBot.Strategy is AIStrategy aIStrategy)
      {
        HeatBot(simulateCandles, aIStrategy);
      }

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

      Simulate(simulateCandles.Skip(50).ToList());
    }

    #endregion

    #region SimulateCandle

    public async Task SimulateCandle(Candle candle)
    {
      DrawingViewModel.ActualCandles.Add(candle);

      if (DrawChart)
      {
        VSynchronizationContext.InvokeOnDispatcher(async () =>
        {
          await RenderLayout(candle);
        });
      }
      else
      {
        await RenderLayout(candle);
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
          if (line.Contains("}{\"TotalValue\""))
          {

          }
          else
          {

            var result = JsonSerializer.Deserialize<SimulationResult>(line);

            result.RunTime = TimeSpan.FromTicks(result.RunTimeTicks);
            SimulationResults.Add(result);
          }

        }

        SimulationResults.LinqSortDescending(x => x.Date);
      }
    }

    #endregion

    #region Simulate

    public CancellationTokenSource cts;
    IDisposable disposable;
    DateTime lastElapsed;
    bool isRunning;

    private async void Simulate(List<Candle> cutCandles)
    {
      try
      {
        RunningTime = new TimeSpan();
        cts?.Cancel();
        cts = new CancellationTokenSource();
        stopRequested = false;
        isRunning = true;

        disposable?.Dispose();

        lastElapsed = DateTime.Now;

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


              await SimulateCandle(actual);


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
          TimeSpan diff = DateTime.Now - lastElapsed;

          RunningTime = RunningTime.Add(diff);

          result.RunTime = RunningTime;
          result.RunTimeTicks = RunningTime.Ticks;
          result.Date = DateTime.Now;
          result.Drawdawn = TradingBot.Strategy.MaxDrawdawnFromMaxTotalValue;

          var json = JsonSerializer.Serialize(result);

          if (File.Exists(results))
          {
            using (StreamWriter w = File.AppendText(results))
            {
              w.WriteLine(json);
            }
          }
          else
          {
            File.WriteAllText(results, json);
          }

          SimulationResults.Add(result);
          SimulationResults.LinqSortDescending(x => x.Date);
        }


        if (TradingBot.Strategy is AIStrategy aIStrategy)
        {
          AIBotRunner.AddFitness(aIStrategy);
        }

        Finished?.Invoke(this, null);
      }
      catch (TaskCanceledException)
      {

      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }

      if (stopRequested)
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

      isRunning = false;
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

      if (!isRunning)
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


    #endregion
  }
}
