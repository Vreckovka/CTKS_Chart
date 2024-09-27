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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
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
        var actualCandle = 0;
       
        var indicatorData = 
          new IndicatorData().NumberOfInputs * 
          (TradingBotViewModel<Position, BaseStrategy<Position>>.IndicatorTimeframes.Count );


        return strategyInputs + actualCandle + indicatorData + TakeIntersections;
        //return strategyInputs + actualCandle + indicatorData;
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

      AIBotRunner = new AIBotRunner(logger, viewModelsFactory);

      AIBotRunner.OnGenerationCompleted += (x, y) => UpdateGeneration();
    }

    #endregion

    #region Properties

    public NEATManager<AIBot> BuyBotManager { get; set; }
    public NEATManager<AIBot> SellBotManager { get; set; }
    public AIBotRunner AIBotRunner { get; set; }

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

    private int minutes = 240;

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
      AIBotRunner.Bots.ForEach(x => x.Stop());
    }

    #endregion


    #endregion

    #region Methods

    #region CreateBots

    public void CreateBots()
    {
      BuyBotManager.InitializeManager(AgentCount);
      SellBotManager.InitializeManager(AgentCount);

      AIBotRunner.RunGeneration(
        AgentCount,
        Minutes,
        Symbol,
        0,
        0,
        BuyBotManager.NeatAlgorithm.GenomeList.ToList(),
        SellBotManager.NeatAlgorithm.GenomeList.ToList());
    }

    #endregion

    #region UpdateGeneration

    private async void UpdateGeneration()
    {
      var best = AIBotRunner.Bots.OrderByDescending(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness).First();

      var addStats =BuyBotManager.Generation + 1 % 10 > 1;

      if (addStats)
      {
        ChartData.Add(AIBotRunner.Bots.Average(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness));
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

      for (int i = 0; i < AIBotRunner.Bots.Count; i++)
      {
        var strat = AIBotRunner.Bots[i].TradingBot.Strategy;

        BuyBotManager.NeatAlgorithm.GenomeList[i] = (NeatGenome)strat.BuyAIBot.NeuralNetwork;
        SellBotManager.NeatAlgorithm.GenomeList[i] = (NeatGenome)strat.SellAIBot.NeuralNetwork;
      }
     

      BuyBotManager.UpdateNEATGeneration();
      SellBotManager.UpdateNEATGeneration();

      _ = AIBotRunner.RunGeneration(
       AgentCount,
       Minutes,
       Symbol,
       0,
       0,
       BuyBotManager.NeatAlgorithm.GenomeList.ToList(),
       SellBotManager.NeatAlgorithm.GenomeList.ToList());
    }

    #endregion

    #region RunTest

    TaskCompletionSource<bool> taskCompletionSource;

    Random random = new Random();
    private Task RunTest(AIStrategy strategy)
    {
      taskCompletionSource = new TaskCompletionSource<bool>();

      var testBot = GetBot(TestSymbol,
        new AIBot(new NeatGenome((NeatGenome)strategy.BuyAIBot.NeuralNetwork, 0, 0)),
        new AIBot(new NeatGenome((NeatGenome)strategy.SellAIBot.NeuralNetwork, 0, 0)), Minutes, random, viewModelsFactory, logger);

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

          AIBotRunner.AddFitness(simStrategy);
          AddTestData(simStrategy);

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

    #region GetBot

    public static SimulationTradingBot<AIPosition, AIStrategy>
      GetBot(
      string symbol,
      AIBot buy,
      AIBot sell,
      int minutes,
      Random random,
      IViewModelsFactory viewModelsFactory,
      ILogger logger)
    {
      var bot = SimulationPromptViewModel.GetTradingBot<AIPosition, AIStrategy>(viewModelsFactory, symbol, minutes, logger, new AIStrategy(buy, sell));

      bot.FromDate = new DateTime(2019, 1, 1);

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

    #region GetTrainingPath

    public static string GetTrainingPath(string fileName, string session, string folderName)
    {
      var gfolder = Path.Combine("Trainings", session, folderName);

      Directory.CreateDirectory(gfolder);

      return Path.Combine(gfolder, fileName);
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
      manager.SavePopulation(GetTrainingPath(fileName, session, folderName), GetTrainingPath(bestGenomeFileName, session, folderName));
    }

    #endregion

    #region SaveGenome

    public static void SaveGenome(
      NeatGenome neatGenome,
      string session,
      string fileName)
    {
      var gfolder = Path.Combine("Trainings", session);
      var path = Path.Combine(gfolder, fileName);

      XmlWriterSettings xwSettings = new XmlWriterSettings();
      xwSettings.Indent = true;

      using (XmlWriter xw = XmlWriter.Create(path, xwSettings))
      {
        NeatGenomeXmlIO.WriteComplete(xw, neatGenome, false);
      }
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