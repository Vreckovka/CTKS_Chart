using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Trading;
using LiveCharts;
using Logger;
using SharpNeat.Decoders;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TradingBroker.MachineLearning;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;
using VNeuralNetwork;

namespace CTKS_Chart.ViewModels
{
  public class SimulationAIPromptViewModel : BasePromptViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly ILogger logger;
    string session;

    #region AgentCount

    private int agentCount = 1;

    public int AgentCount
    {
      get { return agentCount; }
      set
      {
        if (value != agentCount)
        {
          agentCount = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    //Less intersections make bot taking less trades, since position size is based on this 
    //and it does not work for lower timeframes, since it has to make lot of trades
    public static int TakeIntersections = 15;
    public static int inputNumber
    {

      get
      {
        var strategyInputs = 3;
        var actualCandle = 4;
       
        var indicatorData = new IndicatorData().NumberOfInputs * 2;

        return strategyInputs + actualCandle + indicatorData;
      }
    }

    #region Constructors

    public SimulationAIPromptViewModel(IViewModelsFactory viewModelsFactory, ILogger logger)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.logger = logger;
      BuyBotManager = GetNeatManager(viewModelsFactory, PositionSide.Buy);
      SellBotManager = GetNeatManager(viewModelsFactory, PositionSide.Sell);

      Title = "AI Training";

      session = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");

    }

    #endregion

    #region Properties

    #region Bots

    private ObservableCollection<SimulationTradingBot<AIPosition, AIStrategy>> bots = new ObservableCollection<SimulationTradingBot<AIPosition, AIStrategy>>();

    public ObservableCollection<SimulationTradingBot<AIPosition, AIStrategy>> Bots
    {
      get { return bots; }
      set
      {
        if (value != bots)
        {
          bots = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public NEATManager<AIBot> BuyBotManager { get; set; }
    public NEATManager<AIBot> SellBotManager { get; set; }


    public ChartValues<float> ChartData { get; set; } = new ChartValues<float>();
    public ChartValues<decimal> FullData { get; set; } = new ChartValues<decimal>();
    public ChartValues<float> BestData { get; set; } = new ChartValues<float>();
    public ChartValues<decimal> DrawdawnData { get; set; } = new ChartValues<decimal>();
    public ChartValues<float> FitnessData { get; set; } = new ChartValues<float>();
    public ChartValues<double> NumberOfTradesData { get; set; } = new ChartValues<double>();

    #region Labels

    private List<string> labels = new List<string>();

    public List<string> Labels
    {
      get { return labels; }
      set
      {
        if (value != labels)
        {
          labels = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public Func<double, string> PercFormatter { get; set; } = value => value.ToString("N2");
    public Func<double, string> YFormatter { get; set; } = value => value.ToString("N0");



    #region BestFitness

    private float bestFitness;

    public float BestFitness
    {
      get { return bestFitness; }
      set
      {
        if (value != bestFitness)
        {
          bestFitness = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowTestBot

    private bool showTestBot;

    public bool ShowTestBot
    {
      get { return showTestBot; }
      set
      {
        if (value != showTestBot)
        {
          showTestBot = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SelectedBot

    private SimulationTradingBot<AIPosition, AIStrategy> selectedBot;

    public SimulationTradingBot<AIPosition, AIStrategy> SelectedBot
    {
      get { return selectedBot; }
      set
      {
        if (value != selectedBot)
        {
          selectedBot = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    #region Symbol

    private string symbol = "ADAUSDT";

    public string Symbol
    {
      get { return symbol; }
      set
      {
        if (value != symbol)
        {
          symbol = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Minutes

    private int minutes = 120;

    public int Minutes
    {
      get { return minutes; }
      set
      {
        if (value != minutes)
        {
          minutes = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SplitTake

    private double splitTake = 4.5;

    public double SplitTake
    {
      get { return splitTake; }
      set
      {
        if (value != splitTake)
        {
          splitTake = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ChangeSymbol

    private bool changeSymbol;

    public bool ChangeSymbol
    {
      get { return changeSymbol; }
      set
      {
        if (value != changeSymbol)
        {
          changeSymbol = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TestSymbol

    private string testSymbol = "ADAUSDT";

    public string TestSymbol
    {
      get { return testSymbol; }
      set
      {
        if (value != testSymbol)
        {
          testSymbol = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region StartCommand

    protected ActionCommand startCommand;

    public ICommand StartCommand
    {
      get
      {
        return startCommand ??= new ActionCommand(OnStart);
      }
    }


    public void OnStart()
    {
      CreateBots();
    }

    #endregion

    #region StopCommand

    protected ActionCommand stopCommand;


    public ICommand StopCommand
    {
      get
      {
        return stopCommand ??= new ActionCommand(OnStop);
      }
    }

    public void OnStop()
    {
      Bots.ForEach(x => x.Stop());
    }

    #endregion


    #endregion

    #region Methods

    #region CreateBots

    DateTime lastElapsed;

    public void CreateBots()
    {
      lastElapsed = DateTime.Now;

      BuyBotManager.InitializeManager(AgentCount);
      SellBotManager.InitializeManager(AgentCount);

      Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnDispatcher().Subscribe((x) =>
      {
        TimeSpan diff = DateTime.Now - lastElapsed;

        RunTime = RunTime.Add(diff);

        lastElapsed = DateTime.Now;
      });

      CreateStrategies();
    }

    #endregion

    #region CreateStrategies

    Random random = new Random();

    private void CreateStrategies()
    {
      Bots.Clear();
      generationStart = DateTime.Now;
      BuyBotManager.CreateAgents();
      SellBotManager.CreateAgents();

      if (ChangeSymbol)
      {
        Symbol = BuyBotManager.Generation % 2 == 0 ? "ADAUSDT" : "BTCUSDT";
      }

      for (int i = 0; i < BuyBotManager.Agents.Count; i++)
      {
        var bot = GetBot(
          Symbol,
          BuyBotManager.Agents[i],
          SellBotManager.Agents[i],
          Minutes,
          SplitTake,
          random,
          viewModelsFactory,
          logger,
          IsRandom());
        ToStart++;

        Bots.Add(bot);
      }

      RunGeneration(random, Symbol, IsRandom(), SplitTake, minutes);
    }

    #endregion

    #region IsRandom

    private bool IsRandom()
    {
      return false;
      return BuyBotManager.Generation % 5 == 0 && BuyBotManager.Generation != 0;
    }

    #endregion

    DateTime generationStart;

    #region Bot_Finished

    private void Bot_Finished(object sender, EventArgs e)
    {
      lock (this)
      {
        if (sender is SimulationTradingBot<AIPosition, AIStrategy> sim)
        {
          InProgress--;
          FinishedCount++;

          if (FinishedCount == Bots.Count)
          {
            foreach (var bot in Bots)
            {
              bot.Finished -= Bot_Finished;
            }

            VSynchronizationContext.InvokeOnDispatcher(() =>
            {
              RaisePropertyChanged(nameof(InProgress));
              RaisePropertyChanged(nameof(FinishedCount));

              UpdateGeneration();
            });
          }
        }
      }
    }

    #endregion

    #region AddFitness

    public static void AddFitness(AIStrategy strategy)
    {
      strategy.OriginalFitness = (float)((strategy.TotalValue - strategy.StartingBudget) / strategy.StartingBudget) * 1000;
      float fitness = strategy.OriginalFitness;

      // 2. Calculate the drawdown multiplier
      var drawdown = (float)Math.Abs(strategy.MaxDrawdawnFromMaxTotalValue) / 100;
      float exponent = 3.0f;  // Power function exponent to penalize more for larger drawdowns
      var drawdownMultiplier = (float)Math.Pow(1 - drawdown, exponent);

      // Ensure the multiplier is within a reasonable range (0 to 1)
      drawdownMultiplier = Math.Max(drawdownMultiplier, 0);

      // Adjust fitness only if it's greater than 0
      if (fitness > 0)
      {
        fitness *= drawdownMultiplier;
      }

      // 3. Apply a log function based on the count of closed sell positions
      if (strategy.ClosedSellPositions.Count > 0)
      {
        var tradesInfluence = GetTradesInfluance(strategy.ClosedSellPositions.Count);

        fitness *= tradesInfluence;
      }

      // Ensure fitness is not negative
      strategy.AddFitness(fitness < 0 ? 0 : fitness);
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

    #region UpdateGeneration

    private async void UpdateGeneration()
    {
      GenerationRunTime = DateTime.Now - generationStart;

      Bots.ForEach(x =>
      {
        AddFitness(x.TradingBot.Strategy);
      });

      var best = Bots.OrderByDescending(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness).First();

      var addStats = !IsRandom() && BuyBotManager.Generation + 1 % 10 > 1;

      //var addStats = true;

      if (addStats)
      {
        ChartData.Add(Bots.Average(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness));
        BestData.Add(best.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness);

        Labels.Add(BuyBotManager.Generation.ToString());
        RaisePropertyChanged(nameof(Labels));
      }


      if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
      {
        await taskCompletionSource.Task;
      }

      Task task = null;

      if (addStats)
      {
        SaveProgress(BuyBotManager, session, "BUY");
        SaveProgress(SellBotManager, session, "SELL");

        if (TestSymbol != Symbol || ShowTestBot)
        {
          task = RunTest(best.TradingBot.Strategy);
        }
        else
        {
          AddTestData(best.TradingBot.Strategy);
        }

        //task = RunTest(best.TradingBot.Strategy);
      }


      if (SelectedBot != null)
        await task;

      FinishedCount = 0;

      BuyBotManager.UpdateGeneration();
      SellBotManager.UpdateGeneration();

      CreateStrategies();
    }

    #endregion

    #region RunTest

    TaskCompletionSource<bool> taskCompletionSource;

    private Task RunTest(AIStrategy strategy)
    {
      taskCompletionSource = new TaskCompletionSource<bool>();

      InProgress++;

      var testBot = GetBot(TestSymbol,
        new AIBot(new NeatGenome((NeatGenome)strategy.BuyAIBot.NeuralNetwork, 0, 0)),
        new AIBot(new NeatGenome((NeatGenome)strategy.SellAIBot.NeuralNetwork, 0, 0)), Minutes, SplitTake, random, viewModelsFactory, logger);

      testBot.FromDate = new DateTime(2019, 1, 1);

      SelectedBot = null;

      if (ShowTestBot)
        SelectedBot = testBot;

      if (SelectedBot != null)
      {
        SelectedBot.DrawingViewModel.MinValue = 0.02m;
        SelectedBot.DrawingViewModel.MaxValue = 0.07m;
        SelectedBot.DrawingViewModel.InitialCandleCount = 300;
        SelectedBot.DrawingViewModel.OnRestChartX();

        SelectedBot.DrawChart = true;
      }


      if (SelectedBot != null && SelectedBot.DrawChart)
      {
        testBot.Finished += TestBot_Finished;
        testBot.Start();
      }
      else
      {
        Task.Run(async () =>
          {
            //Pri zmene symbolu sa nacitava nove CTKS a neni dobre
            if (BuyBotManager.Generation == 0)
              await Task.Delay(500);


            testBot.Finished += TestBot_Finished;
            testBot.Start();
          });
      }

      ToStart--;

      return taskCompletionSource.Task;
    }

    #endregion

    #region TestBot_Finished

    private void TestBot_Finished(object sender, EventArgs e)
    {
      if (sender is SimulationTradingBot<AIPosition, AIStrategy> sim)
      {
        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          taskCompletionSource.SetResult(true);

          var simStrategy = sim.TradingBot.Strategy;

          AddFitness(simStrategy);
          AddTestData(simStrategy);

          InProgress--;
        });
      }

    }

    #endregion

    #region AddTestData

    private void AddTestData(AIStrategy aIStrategy)
    {
      FullData.Add(aIStrategy.TotalValue);
      DrawdawnData.Add(aIStrategy.MaxDrawdawnFromMaxTotalValue);
      NumberOfTradesData.Add(aIStrategy.ClosedSellPositions.Count);

      BestFitness = aIStrategy.OriginalFitness;
      FitnessData.Add(BestFitness);
    }

    #endregion

    #region RunGeneration

    public void RunGeneration(
      Random random,
      string symbol,
      bool useRandomDate,
      double pSpliTake,
      int minutes)
    {
      generationStart = DateTime.Now;
      Task.Run(() =>
      {
        DateTime fromDate = DateTime.Now;
        double splitTake = 0;

        var asset = Bots.First().Asset;
        var dailyCandles = SimulationTradingBot.GetIndicatorData(Bots[0].timeFrameDatas[TimeFrame.D1], asset);

        foreach (var indiFrame in Bots[0].IndicatorTimeframes)
        {
          SimulationTradingBot.GetIndicatorData(Bots[0].timeFrameDatas[indiFrame], asset);
        }

        //ignore filter starting values of indicators
        var firstValidDate = dailyCandles.First(x => x.IndicatorData.RangeFilter.HighTarget > 0).CloseTime.AddDays(1);
        var lastValidDate = dailyCandles.Last(x => x.IndicatorData.RangeFilter.HighTarget > 0).CloseTime.AddDays(-1);

        if (useRandomDate)
        {
          var year = random.Next(2020, 2023);
          fromDate = new DateTime(year, random.Next(1, 13), random.Next(1, 25));
          splitTake = pSpliTake;
        }
        else
        {
          fromDate = firstValidDate;
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
          if (splitTake != 0)
          {
            var take = (int)(mainCandles.Count / splitTake);

            simulateCandles = simulateCandles.Take(take).ToList();
          }

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
          tasks.Add(Task.Run(() =>
          {

            ToStart -= take.Count;
            InProgress += take.Count;

            //try
            {

              foreach (var candle in simulateCandles)
              {
                if (take.Any(x => x.stopRequested))
                  return;

                foreach (var bot in take)
                {
                  bot.SimulateCandle(candle);
                }
              }
            }
            //catch (Exception ex)
            {
              //Logger.Logger.Log(ex);
            }


            FinishedCount += take.Count;
            InProgress -= take.Count;

          }));
        }

        Task.WaitAll(tasks.ToArray());

        if (!closing)
          UpdateGeneration();
      });

    }

    #endregion

    #region GetBot

    public static SimulationTradingBot<AIPosition, AIStrategy>
      GetBot(
      string symbol,
      AIBot buy,
      AIBot sell,
      int minutes,
      double splitTake,
      Random random,
      IViewModelsFactory viewModelsFactory,
      ILogger logger,
      bool useRandom = false)
    {
      var bot = SimulationPromptViewModel.GetTradingBot<AIPosition, AIStrategy>(viewModelsFactory, symbol, minutes, logger, new AIStrategy(buy, sell));

      DateTime fromDate = DateTime.Now;

      if (useRandom)
      {
        var year = random.Next(2019, 2023);
        fromDate = new DateTime(year, random.Next(1, 13), random.Next(1, 25));
      }
      else
      {
        fromDate = new DateTime(2019, 1, 1);
      }

      bot.FromDate = fromDate;

      if (useRandom)
        bot.SplitTake = splitTake;

      return bot;
    }

    #endregion

    #region GetNeatManager

    public static NEATManager<AIBot> GetNeatManager(
      IViewModelsFactory viewModelsFactory,
      PositionSide positionSide)
    {
      switch (positionSide)
      {
        case PositionSide.Neutral:
          break;
        case PositionSide.Buy:
          return new NEATManager<AIBot>(
         viewModelsFactory,
         NetworkActivationScheme.CreateAcyclicScheme(),
         inputNumber,
         TakeIntersections * 2,
         QuadraticSigmoid.__DefaultInstance);
        case PositionSide.Sell:
          return new NEATManager<AIBot>(
        viewModelsFactory,
        NetworkActivationScheme.CreateAcyclicScheme(),
        inputNumber + 1,
        TakeIntersections,
        QuadraticSigmoid.__DefaultInstance);
      }

      return null;
    }

    #endregion

    #region SaveProgress

    public static void SaveProgress(NEATManager<AIBot> manager, string session, string folderName, int? generation = null)
    {
      var gfolder = Path.Combine("Trainings", session, folderName);

      Directory.CreateDirectory(gfolder);

      int generationValue = generation != null ? generation.Value : manager.Generation;

      manager.SaveBestGenome(Path.Combine(gfolder, $"{generationValue}.txt"));
    }

    #endregion

    #region SaveProgress

    public static void SaveGeneration(
      NEATManager<AIBot> manager,
      string session,
      string folderName,
      string fileName,
      string bestGenomeFileName)
    {
      var gfolder = Path.Combine("Trainings", session, folderName);

      Directory.CreateDirectory(gfolder);

      manager.SavePopulation(Path.Combine(gfolder, fileName), Path.Combine(gfolder, bestGenomeFileName));
    }

    #endregion

    #region OnClose

    bool closing;
    protected override void OnClose(Window window)
    {
      closing = true;
      base.OnClose(window);

      OnStop();
    }

    #endregion

    #endregion
  }
}