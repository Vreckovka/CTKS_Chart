using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Trading;
using Logger;
using SharpNeat.Genomes.Neat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;

namespace CTKS_Chart.ViewModels
{
  public class AIBotRunner : ViewModel
  {
    public event EventHandler OnGenerationCompleted;


    DateTime lastElapsed;
    DateTime generationStart;
    Random random = new Random();

    public AIBotRunner(
      ILogger logger,
      IViewModelsFactory viewModelsFactory)
    {
      Logger = logger;
      ViewModelsFactory = viewModelsFactory;
    }

    #region Properties

    #region ToStart

    private int toStart;

    public int ToStart
    {
      get { return toStart; }
      set
      {
        if (value != toStart)
        {
          toStart = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region InProgress

    private int inProgress;

    public int InProgress
    {
      get { return inProgress; }
      set
      {
        if (value != inProgress)
        {
          inProgress = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region FinishedCount

    private int finishedCount;

    public int FinishedCount
    {
      get { return finishedCount; }
      set
      {
        if (value != finishedCount)
        {
          finishedCount = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region RunTime

    private TimeSpan runTime;

    public TimeSpan RunTime
    {
      get { return runTime; }
      set
      {
        if (value != runTime)
        {
          runTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region GenerationRunTime

    private TimeSpan generationRunTime;

    public TimeSpan GenerationRunTime
    {
      get { return generationRunTime; }
      set
      {
        if (value != generationRunTime)
        {
          generationRunTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    public List<SimulationTradingBot<AIPosition, AIStrategy>> Bots { get; set; } = new List<SimulationTradingBot<AIPosition, AIStrategy>>();
    ILogger Logger { get; }
    IViewModelsFactory ViewModelsFactory { get; }

    #endregion

    #region Methods

    #region RunGeneration

    bool canRunGeneration = true;
    IDisposable intervalDisposable;

    public Task RunGeneration(
      int agentCount,
      int minutes,
      string symbol,
      int maxTake,
      int randomStartIndex,
      List<NeatGenome> buyGenomes,
      List<NeatGenome> sellGenomes)
    {
      if (intervalDisposable == null)
      {
        lastElapsed = DateTime.Now;

        intervalDisposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe((x) =>
        {
          TimeSpan diff = DateTime.Now - lastElapsed;

          RunTime.Add(diff);

          lastElapsed = DateTime.Now;
        });
      }

      if (canRunGeneration)
      {
        canRunGeneration = false;

        serialDisposable.Disposable?.Dispose();

        return CreateStrategies(agentCount, minutes, symbol, buyGenomes, sellGenomes, maxTake, randomStartIndex);
      }

      return Task.CompletedTask;
    }

    #endregion

    #region CreateStrategies

    private Task CreateStrategies(
      int agentCount,
      int minutes,
      string symbol,
      List<NeatGenome> buyGenomes,
      List<NeatGenome> sellGenomes,
      int maxTake,
      int randomStartIndex)
    {
      Bots.ForEach(x => x.Stop());
      Bots.Clear();

      ToStart = 0;
      FinishedCount = 0;

      for (int i = 0; i < agentCount; i++)
      {
        var buyBotManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Buy);
        buyBotManager.SetBestGenome(buyGenomes[i]);
        buyBotManager.InitializeManager(1);
        buyBotManager.CreateAgents();

        var sellBotManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Sell);
        sellBotManager.SetBestGenome(sellGenomes[i]);
        sellBotManager.InitializeManager(1);
        sellBotManager.CreateAgents();

        buyBotManager.Agents[0].NeuralNetwork.InputCount = buyGenomes[i].NodeList.Count(x => x.NodeType == SharpNeat.Network.NodeType.Input);
        sellBotManager.Agents[0].NeuralNetwork.InputCount = sellGenomes[i].NodeList.Count(x => x.NodeType == SharpNeat.Network.NodeType.Input);

        ((NeatGenome)buyBotManager.Agents[0].NeuralNetwork).Id = buyGenomes[i].Id;
        ((NeatGenome)sellBotManager.Agents[0].NeuralNetwork).Id = sellGenomes[i].Id;

        var bot = SimulationAIPromptViewModel.GetBot(
          symbol,
          buyBotManager.Agents[0],
          sellBotManager.Agents[0],
          minutes,
          random,
          ViewModelsFactory,
          Logger);

        ToStart++;

        Bots.Add(bot);
      }


      return RunBots(random, symbol, minutes, maxTake, randomStartIndex);
    }

    #endregion

    static Dictionary<string, int> cachedData = new Dictionary<string, int>();
    static object cachedAllDataLock = new object();

    private void CacheAllData(string symbol, int minutes)
    {
      lock (cachedAllDataLock)
      {
        if (!cachedData.ContainsKey(symbol))
        {
          var asset = Bots.First().Asset;

          var dailyCandles = SimulationTradingBot
               .GetIndicatorData(Bots[0].TimeFrameDatas[TimeFrame.D1], asset)
               .Where(x => x.IndicatorData.RangeFilter.HighTarget > 0)
               .ToList();

          var firstValidDate = dailyCandles.First().CloseTime;
          var lastValidDate = dailyCandles.Last().CloseTime;
          var fromDate = firstValidDate;

          var allCandles = SimulationTradingBot.GetSimulationCandles(
            minutes,
            SimulationPromptViewModel.GetSimulationDataPath(symbol, minutes.ToString()), symbol, fromDate);

          var simulateCandles = allCandles.cutCandles.Where(x => x.OpenTime.Date > firstValidDate.Date && x.OpenTime.Date < lastValidDate.Date).ToList();
          var candles = allCandles.candles;
          var mainCandles = allCandles.allCandles.Where(x => x.OpenTime.Date > firstValidDate.Date).ToList();


          var timeFrame = simulateCandles.First().TimeFrame;
          var key = new Tuple<string, TimeFrame>(Bots[0].Asset.Symbol, timeFrame);

          Bots[0].LoadSecondaryLayouts(firstValidDate);
          Bots[0].PreloadCandles(key, mainCandles);
          Bots[0].PreLoadIntersections(key, mainCandles);

          cachedData.Add(symbol, minutes);
        } 
      }
    }

    #region RunBots

    object batton = new object();
    object batton1 = new object();


    private Task RunBots(
              Random random,
              string symbol,
              int minutes,
              int maxTake,
              int randomStartIndex)
    {
      generationStart = DateTime.Now;

      return Task.Run(() =>
      {
        lock (batton1)
        {
          try
          {

            CacheAllData(symbol, minutes);

            DateTime fromDate = DateTime.Now;

            var asset = Bots.First().Asset;

            var dailyCandles = SimulationTradingBot
                .GetIndicatorData(Bots[0].TimeFrameDatas[TimeFrame.D1], asset)
                .Where(x => x.IndicatorData.RangeFilter.HighTarget > 0)
                .ToList();

            var selectedCandles = dailyCandles;

            if (maxTake > 0)
            {
              selectedCandles = dailyCandles.Skip(randomStartIndex).Take(maxTake).ToList();
            }

            // If you want to set firstValidDate and lastValidDate using the selected candles
            var firstValidDate = selectedCandles.First().CloseTime.AddDays(10);
            var lastValidDate = selectedCandles.Last().CloseTime.AddDays(-10);
            fromDate = firstValidDate;

            foreach (var indiFrame in TradingBotViewModel<Position, BaseStrategy<Position>>.IndicatorTimeframes)
            {
              SimulationTradingBot.GetIndicatorData(Bots[0].TimeFrameDatas[indiFrame], asset);
            }


            var allCandles = SimulationTradingBot.GetSimulationCandles(
               minutes,
               SimulationPromptViewModel.GetSimulationDataPath(symbol, minutes.ToString()), symbol, fromDate);

            var simulateCandles = allCandles.cutCandles.Where(x => x.OpenTime.Date > firstValidDate.Date && x.OpenTime.Date < lastValidDate.Date).ToList();
            var candles = allCandles.candles;
            var mainCandles = allCandles.allCandles.Where(x => x.OpenTime.Date > firstValidDate.Date).ToList();

            var timeFrame = simulateCandles.First().TimeFrame;
            var key = new Tuple<string, TimeFrame>(Bots[0].Asset.Symbol, timeFrame);

            Bots[0].LoadSecondaryLayouts(firstValidDate);
            Bots[0].PreloadCandles(key, mainCandles);
            Bots[0].PreLoadIntersections(key, mainCandles);

            foreach (var bot in Bots)
            {
              bot.FromDate = fromDate;
              bot.InitializeBot(simulateCandles);
              bot.HeatBot(simulateCandles, bot.TradingBot.Strategy);
            }


            ToStart = Bots.Count;

            var min = Math.Min(Bots.Count, 10);

            var splitTakeC = Bots.SplitList(Bots.Count / min);
            var tasks = new List<Task>();

            foreach (var take in splitTakeC)
            {
              tasks.Add(Task.Run(async () =>
              {
                lock (batton)
                {
                  ToStart -= take.Count;
                  InProgress += take.Count;
                }

                try
                {

                  foreach (var candle in simulateCandles)
                  {
                    if (take.Any(x => x.stopRequested))
                      return;

                    foreach (var bot in take)
                    {
                      await bot.SimulateCandle(candle);
                    }
                  }
                }
                catch (Exception ex)
                {
                  Logger.Log(ex);
                }

                lock (batton)
                {
                  FinishedCount += take.Count;
                  InProgress -= take.Count;
                }
              }));
            }

            Task.WaitAll(tasks.ToArray());

            if (!Bots.Any(x => x.stopRequested))
              GenerationCompleted();
            else
            {
              InProgress = 0;
              FinishedCount = 0;
            }

            canRunGeneration = true;
          }
          catch (Exception ex)
          {
            Logger.Log(ex);
          }
        }
      });
    }

    #endregion

    #region GenerationCompleted

    SerialDisposable serialDisposable = new SerialDisposable();

    private void GenerationCompleted()
    {
      GenerationRunTime = DateTime.Now - generationStart;

      Bots.ForEach(x =>
      {
        AddFitness(x.TradingBot.Strategy);
      });

      OnGenerationCompleted?.Invoke(null, null);
    }

    #endregion

    #region AddFitness

    public static void AddFitness(AIStrategy strategy)
    {
      strategy.OriginalFitness = (float)((strategy.TotalValue - strategy.StartingBudget) / strategy.StartingBudget) * 1000;
      float fitness = strategy.OriginalFitness;

      var closedBuy = strategy.ClosedBuyPositions.ToList();
      var closedSell = strategy.ClosedSellPositions.ToList();

      var drawdown = (float)Math.Abs(strategy.MaxDrawdawnFromMaxTotalValue) / 100;
      float exponent = 1.75f;
      var drawdownMultiplier = (float)Math.Pow(1 - drawdown, exponent);

      drawdownMultiplier = Math.Max(drawdownMultiplier, 0);

      if (fitness > 0)
      {
        fitness *= drawdownMultiplier;
      }

      if (closedSell.Count > 0)
      {
        var tradesInfluence = GetTradesInfluance(closedSell.Count);

        fitness *= tradesInfluence;
      }

      var negativeTrades = closedBuy.Count(x => x.FinalProfit < 0);
      var allTrades = closedBuy.Count();

      if (allTrades > 0)
      {
        var ratio = (float)negativeTrades / allTrades;

        fitness *= 1 - ratio;

        strategy.AddFitness(fitness < 0 ? 0 : fitness);
      }
      else
      {
        strategy.AddFitness(0);
      }
    }

    #endregion


    private static float GetTradesInfluance(double tradeCount)
    {
      // Apply logarithm to reduce influence as trade count grows
      double logarithmicInfluence = Math.Log(tradeCount, 5);

      //var logarithmicInfluence = tradeCount / 500 ;

      //if (tradeCount < 400)
      //  logarithmicInfluence = 1;

      return (float)logarithmicInfluence;
    }

    #endregion


    public override void Dispose()
    {
      serialDisposable?.Dispose();
      intervalDisposable?.Dispose();

      base.Dispose();
    }
  }
}