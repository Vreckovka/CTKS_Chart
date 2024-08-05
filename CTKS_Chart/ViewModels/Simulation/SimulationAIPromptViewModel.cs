using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Trading;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    string session;


    public const int inputNumber = 34;
    public int agentNumber = 50;


    public SimulationAIPromptViewModel(IViewModelsFactory viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      BuyBotManager = new AIManager<AIBot>(viewModelsFactory, 0.1f);
      SellBotManager = new AIManager<AIBot>(viewModelsFactory, 0.1f);

      Title = "AI Training";

      session = DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss");

    }

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

    public AIManager<AIBot> BuyBotManager { get; set; }
    public AIManager<AIBot> SellBotManager { get; set; }

    public ChartValues<float> ChartData { get; set; } = new ChartValues<float>();

    public ChartValues<decimal> FullData { get; set; } = new ChartValues<decimal>();
    public ChartValues<float> BestData { get; set; } = new ChartValues<float>();
    public ChartValues<decimal> DrawdawnData { get; set; } = new ChartValues<decimal>();
    public ChartValues<float> FitnessData { get; set; } = new ChartValues<float>();

    public ChartValues<double> NumberOfTradesData { get; set; } = new ChartValues<double>();

    public ObservableCollection<int> Labels { get; set; } = new ObservableCollection<int>();

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

    #region UseRandomDate

    private bool useRandomDate = true;

    public bool UseRandomDate
    {
      get { return useRandomDate; }
      set
      {
        if (value != useRandomDate)
        {
          useRandomDate = value;
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

    private bool changeSymbol = true;

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

    protected override void OnClose(Window window)
    {
      base.OnClose(window);

      OnStop();
    }

    #endregion

    #region Methods

    #region CreateBots

    DateTime lastElapsed;
    public void CreateBots()
    {
      lastElapsed = DateTime.Now;

      BuyBotManager.Initilize(new int[] {
        inputNumber,
        inputNumber * 2,
        inputNumber * 2,
        30 }, agentNumber);

      SellBotManager.Initilize(new int[] {
        inputNumber,
        inputNumber * 2,
        inputNumber * 2,
        15 }, agentNumber);

      Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnDispatcher().Subscribe((x) =>
      {
        TimeSpan diff = DateTime.Now - lastElapsed;

        RunTime = RunTime.Add(diff);

        lastElapsed = DateTime.Now;
      });


      //var manager = new GeneticAiManager(50, GetScore);

      //manager.GeneticAlgorithmManager.OnNewGenerationCreated.Subscribe((x) =>
      //{

      //});

      //manager.GeneticAlgorithmManager.Run(CancellationToken.None);

      CreateStrategies();
    }

    #endregion

    #region CreateStrategies

    Random random = new Random();

    private void CreateStrategies()
    {
      Bots.Clear();

      List<List<SimulationTradingBot<AIPosition, AIStrategy>>> spliited = new List<List<SimulationTradingBot<AIPosition, AIStrategy>>>();

      BuyBotManager.CreateAgents();
      SellBotManager.CreateAgents();

      if (ChangeSymbol)
      {
        Symbol = BuyBotManager.Generation % 2 == 0 ? "ADAUSDT" : "BTCUSDT";
      }

      for (int i = 0; i < BuyBotManager.Agents.Count; i++)
      {
        var bot = GetBot(Symbol, BuyBotManager.Agents[i], SellBotManager.Agents[i]);

        Bots.Add(bot);
      }

      StartBots();
    }

    #endregion

    #region GetBot

    private SimulationTradingBot<AIPosition, AIStrategy>
      GetBot(
      string symbol,
      AIBot buy,
      AIBot sell)
    {
      ToStart++;

      var bot = viewModelsFactory.Create<SimulationTradingBot<AIPosition, AIStrategy>>(
                  new TradingBot<AIPosition, AIStrategy>(GetAsset(symbol), new AIStrategy(buy, sell)));

      bot.DataPath = $"{symbol}-{Minutes}-generated.csv";
      bot.SplitTake = SplitTake;

      if (UseRandomDate)
      {
        var year = random.Next(2019, 2023);
        bot.FromDate = new DateTime(year, random.Next(1, 13), random.Next(1, 25));
      }
      else
      {
        bot.FromDate = new DateTime(2019, 1, 1);
      }

      return bot;
    }

    #endregion

    #region StartBots

    DateTime generationStart;
    private void StartBots()
    {
      generationStart = DateTime.Now;
      List<List<SimulationTradingBot<AIPosition, AIStrategy>>> spliited = new List<List<SimulationTradingBot<AIPosition, AIStrategy>>>();

      int threadCount = 3;
      var splitSize = (Bots.Count / threadCount) + 1;

      Task.Run(() =>
      {
        spliited = Bots.SplitList(splitSize).ToList();

        foreach (var spliited in spliited)
        {
          Task.Run(() =>
          {
            foreach (var bot in spliited)
            {
              bot.Finished += Bot_Finished;

              bot.Start();

              VSynchronizationContext.InvokeOnDispatcher(() =>
              {
                InProgress++;
                ToStart--;
              });
            }
          });
        }
      });
    }

    #endregion

    #region Bot_Finished

    private void Bot_Finished(object sender, EventArgs e)
    {
      lock (this)
      {
        if (sender is SimulationTradingBot<AIPosition, AIStrategy> sim)
        {
          VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          InProgress--;
          FinishedCount++;
        });

          if (FinishedCount == Bots.Count)
          {
            foreach (var bot in Bots)
            {
              bot.Finished -= Bot_Finished;
            }

            VSynchronizationContext.InvokeOnDispatcher(() =>
            {
              UpdateGeneration();
            });
          }
        }
      }
    }

    #endregion

    #region UpdateGeneration

    private async void UpdateGeneration()
    {
      GenerationRunTime = DateTime.Now - generationStart;

      var best = Bots.OrderByDescending(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness).First();

      ChartData.Add(Bots.Average(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness));
      BestData.Add(best.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness);

      Labels.Add(BuyBotManager.Generation);

      if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
      {
        await taskCompletionSource.Task;
      }

      var task = RunTest(best.TradingBot.Strategy);

      if (SelectedBot != null)
        await task;

      FinishedCount = 0;

      SaveProgress(BuyBotManager, "BUY");
      SaveProgress(SellBotManager, "SELL");

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
        new AIBot(new NeuralNetwork(strategy.BuyAIBot.NeuralNetwork)),
        new AIBot(new NeuralNetwork(strategy.SellAIBot.NeuralNetwork)));

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

          FullData.Add(simStrategy.TotalValue);
          DrawdawnData.Add(simStrategy.MaxDrawdawnFromMaxTotalValue);
          NumberOfTradesData.Add(simStrategy.ClosedBuyPositions.Count);

          BestFitness = simStrategy.BuyAIBot.NeuralNetwork.Fitness;
          FitnessData.Add(BestFitness);

          InProgress--;
        });
      }

    }

    #endregion

    #region SaveProgress

    private void SaveProgress(AIManager<AIBot> manager, string folderName)
    {
      var gfolder = Path.Combine("Trainings", session, "ADA", folderName);

      Directory.CreateDirectory(gfolder);

      var best = manager.Networks.OrderByDescending(x => x.Fitness).FirstOrDefault();

      best.SaveNeuralNetwork(Path.Combine(gfolder, $"{manager.Generation}.txt"));
    }

    #endregion

    #region GetAsset

    private Asset GetAsset(string symbol)
    {
      Asset asset = null;
      string path = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\Data";

      TimeFrame[] timeFrames = new TimeFrame[] {
        TimeFrame.W1,
        TimeFrame.W2,
        TimeFrame.M1,
        TimeFrame.M3,
        TimeFrame.M6,
        TimeFrame.M12
        };

      switch (symbol)
      {
        case "ADAUSDT":
          asset = new Asset()
          {
            Symbol = "ADAUSDT",
            NativeRound = 1,
            PriceRound = 4,
            DataPath = path,
            DataSymbol = "BINANCE ADAUSD",
            TimeFrames = timeFrames,
            IndicatorDataPath = "BINANCE ADAUSDT"
          };
          break;
        case "BTCUSDT":
          asset = new Asset()
          {
            Symbol = "BTCUSDT",
            NativeRound = 5,
            PriceRound = 2,
            DataPath = path,
            DataSymbol = "INDEX BTCUSD",
            IndicatorDataPath = "BINANCE BTCUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "LTCUSDT":
          asset = new Asset()
          {
            Symbol = "LTCUSDT",
            NativeRound = 3,
            PriceRound = 2,
            DataPath = path,
            DataSymbol = "BINANCE LTCUSD",
            IndicatorDataPath = "BINANCE LTCUSD",
            TimeFrames = timeFrames,
          };
          break;
      }


      return asset;
    }

    #endregion

    #endregion
  }
}