using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Trading;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Prompts;
using VNeuralNetwork;

namespace CTKS_Chart.ViewModels
{
  public class SimulationAIPromptViewModel : BasePromptViewModel
  {

    public const int inputNumber = 34;
    private readonly IViewModelsFactory viewModelsFactory;
    string session;


    string path = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\Data";

    TimeFrame[] timeFrames = new TimeFrame[] {
        TimeFrame.W1,
        TimeFrame.W2,
        TimeFrame.M1,
        TimeFrame.M3,
        TimeFrame.M6,
        TimeFrame.M12
        };

    public SimulationAIPromptViewModel(IViewModelsFactory viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      BuyBotManager = new AIManager<AIBuyBot>(viewModelsFactory);
      SellBotManager = new AIManager<AIBuyBot>(viewModelsFactory);

      Title = "AI Training";

      session = DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss");
      CreateBots();
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

    AIManager<AIBuyBot> BuyBotManager { get; set; }
    AIManager<AIBuyBot> SellBotManager { get; set; }

    public ChartValues<decimal> ChartData { get; set; } = new ChartValues<decimal>();

    public ChartValues<decimal> FullData { get; set; } = new ChartValues<decimal>();
    public ChartValues<decimal> BestData { get; set; } = new ChartValues<decimal>();
    public ChartValues<decimal> DrawdawnData { get; set; } = new ChartValues<decimal>();
    public ChartValues<float> FitnessData { get; set; } = new ChartValues<float>();

    public ObservableCollection<int> Labels { get; set; } = new ObservableCollection<int>();

    public Func<double, string> PercFormatter { get; set; } = value => value.ToString("N2");

    public Func<double, string> YFormatter { get; set; } = value => value.ToString("N0");

    #region GenerationCount

    private int generationCount;

    public int GenerationCount
    {
      get { return generationCount; }
      set
      {
        if (value != generationCount)
        {
          generationCount = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    #endregion

    #region Methods

    #region CreateBots

    public void CreateBots()
    {
      BuyBotManager.Initilize(new int[] {
        inputNumber,
        inputNumber * 2,
        inputNumber * 2,
        30 }, 60);

      SellBotManager.Initilize(new int[] {
        inputNumber,
        inputNumber * 2,
        inputNumber * 2,
        15 }, 60);

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

      for (int i = 0; i < BuyBotManager.Agents.Count; i++)
      {
        var adaAi = viewModelsFactory.Create<SimulationTradingBot<AIPosition, AIStrategy>>(
                   new TradingBot<AIPosition, AIStrategy>(new Asset()
                   {
                     Symbol = "ADAUSDT",
                     NativeRound = 1,
                     PriceRound = 4,
                     DataPath = path,
                     DataSymbol = "BINANCE ADAUSD",
                     TimeFrames = timeFrames,
                   }, new AIStrategy(BuyBotManager.Agents[i], SellBotManager.Agents[i])));

        adaAi.DisplayName = "ADAUSDT SPOT AI 15";
        adaAi.DataPath = $"ADAUSDT-240-generated.csv";
        var year = random.Next(2019, 2023);

        adaAi.FromDate = new DateTime(2019, 1, 1);
        //adaAi.FromDate = new DateTime(year, random.Next(1, 13), random.Next(1, 25));
        //adaAi.Take = 500;
        adaAi.DataTimeFrame = TimeFrame.H4;

        Bots.Add(adaAi);
      }

      Task.Run(() =>
      {
        spliited = Bots.SplitList(20).ToList();

        foreach (var spliited in spliited)
        {
          Task.Run(() =>
          {
            foreach (var bot in spliited)
            {
              bot.Finished += Bot_Finished;

              bot.Start();
            }
          });
        }
      });
    }

    #endregion

    #region CreateBotsFullRun

    bool fullRun = false;
    public void CreateBotsFullRun()
    {
      FinishedCount = 0;
      Bots.Clear();

      BuyBotManager.CreateAgents();
      SellBotManager.CreateAgents();

      for (int i = 0; i < BuyBotManager.Agents.Count; i++)
      {
        var adaAi = viewModelsFactory.Create<SimulationTradingBot<AIPosition, AIStrategy>>(
   new TradingBot<AIPosition, AIStrategy>(new Asset()
   {
     Symbol = "ADAUSDT",
     NativeRound = 1,
     PriceRound = 4,
     DataPath = path,
     DataSymbol = "BINANCE ADAUSD",
     TimeFrames = timeFrames,
   }, new AIStrategy(BuyBotManager.Agents[i], SellBotManager.Agents[i])));

        adaAi.DisplayName = "ADAUSDT SPOT AI 15";
        adaAi.DataPath = $"ADAUSDT-240-generated.csv";
        var year = random.Next(2019, 2023);

        //adaAi.FromDate = new DateTime(2019, 1, 1);
        adaAi.FromDate = new DateTime(year, random.Next(1, 13), random.Next(1, 25));
        adaAi.Take = 2190;
        //adaAi.DataTimeFrame = TimeFrame.H4;

        Bots.Add(adaAi);
      }


      List<List<SimulationTradingBot<AIPosition, AIStrategy>>> spliited = new List<List<SimulationTradingBot<AIPosition, AIStrategy>>>();

      Task.Run(() =>
      {
        spliited = Bots.SplitList(20).ToList();

        foreach (var spliited in spliited)
        {
          Task.Run(() =>
          {
            foreach (var bot in spliited)
            {
              bot.Finished += Bot_Finished;

              bot.Start();
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
          FinishedCount++;
        });

          if (FinishedCount == Bots.Count)
          {
            foreach (var bot in Bots)
            {
              bot.Finished -= Bot_Finished;
            }

            if (fullRun == false)
            {
              fullRun = true;

             CreateBotsFullRun();
            }
            else
            {
              VSynchronizationContext.InvokeOnDispatcher(() =>
              {
                UpdateGeneration();
              });
            }
          }
        }
      }
    }

    #endregion

    #region UpdateGeneration

    private async void UpdateGeneration()
    {
      fullRun = false;
      var best = Bots.OrderByDescending(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness).First();

      FitnessData.Add(BestFitness);
      ChartData.Add(Bots.Average(x => x.TradingBot.Strategy.TotalValue));
      BestData.Add(Bots.Max(x => x.TradingBot.Strategy.TotalValue));
      Labels.Add(GenerationCount);

      await RunTest(best.TradingBot.Strategy);

      BestFitness = best.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness;

      GenerationCount++;
      FinishedCount = 0;

      SaveProgress();
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

      var adaAi = viewModelsFactory.Create<SimulationTradingBot<AIPosition, AIStrategy>>(
    new TradingBot<AIPosition, AIStrategy>(new Asset()
    {
      Symbol = "ADAUSDT",
      NativeRound = 1,
      PriceRound = 4,
      DataPath = path,
      DataSymbol = "BINANCE ADAUSD",
      TimeFrames = timeFrames,
    }, new AIStrategy(
      new AIBuyBot(new NeuralNetwork(strategy.BuyAIBot.NeuralNetwork)),
      new AIBuyBot(new NeuralNetwork(strategy.SellAIBot.NeuralNetwork)
      ))));

      adaAi.DisplayName = "ADAUSDT SPOT AI 15";
      adaAi.DataPath = $"ADAUSDT-240-generated.csv";
      adaAi.FromDate = new DateTime(2019, 1, 1);
      adaAi.DataTimeFrame = TimeFrame.H4;

      SelectedBot = null;

      if (ShowTestBot)
        SelectedBot = adaAi;

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
        adaAi.Finished += AdaAi_Finished;
        adaAi.Start();
      }
      else
      {
        Task.Run(() =>
        {
          adaAi.Finished += AdaAi_Finished;
          adaAi.Start();
        });
      }



      return taskCompletionSource.Task;
    }

    #endregion

    #region AdaAi_Finished

    private void AdaAi_Finished(object sender, EventArgs e)
    {
      if (sender is SimulationTradingBot<AIPosition, AIStrategy> sim)
      {
        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          taskCompletionSource.SetResult(true);
          FullData.Add(sim.TradingBot.Strategy.TotalValue);
          DrawdawnData.Add(sim.TradingBot.Strategy.MaxDrawdawnFromMaxTotalValue);
        });
      }

    }

    #endregion

    #region SaveProgress

    public void SaveProgress()
    {
      var bestBuy = BuyBotManager.Networks.OrderByDescending(x => x.Fitness).FirstOrDefault();

      var gfolder = System.IO.Path.Combine("Trainings", session, "ADA", "BUY");

      Directory.CreateDirectory(gfolder);

      bestBuy.SaveNeuralNetwork(System.IO.Path.Combine(gfolder, $"{GenerationCount}.txt"));


      var bestSell = SellBotManager.Networks.OrderByDescending(x => x.Fitness).FirstOrDefault();

      gfolder = System.IO.Path.Combine("Trainings", session, "ADA", "SELL");

      Directory.CreateDirectory(gfolder);

      bestSell.SaveNeuralNetwork(System.IO.Path.Combine(gfolder, $"{GenerationCount}.txt"));
    }

    #endregion 

    #endregion
  }
}