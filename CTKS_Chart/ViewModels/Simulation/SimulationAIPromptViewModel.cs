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

    const int inputNumber = 20;
    private readonly IViewModelsFactory viewModelsFactory;
    string session;

    public SimulationAIPromptViewModel(IViewModelsFactory viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      BotManager = new AIManager<AIBot>(viewModelsFactory);
      session = DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss");
      CreateBots();
    }

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

    AIManager<AIBot> BotManager { get; set; }

    public ChartValues<decimal> ChartData { get; set; } = new ChartValues<decimal>();

    public ChartValues<decimal> FullData { get; set; } = new ChartValues<decimal>();
    public ChartValues<decimal> BestData { get; set; } = new ChartValues<decimal>();

    public ObservableCollection<int> Labels { get; set; } = new ObservableCollection<int>();

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

    //#region SelectedBot

    //private SimulationTradingBot<AIPosition, AIStrategy> selectedBot;

    //public SimulationTradingBot<AIPosition, AIStrategy> SelectedBot
    //{
    //  get { return selectedBot; }
    //  set
    //  {
    //    if (value != selectedBot)
    //    {
    //      SelectedBot?.Stop();
    //      selectedBot = value;

    //      selectedBot.LoadSimulationResults();
    //      RaisePropertyChanged();
    //    }
    //  }
    //}

    //#endregion

    public void CreateBots()
    {


      BotManager.Initilize(new int[] { inputNumber, inputNumber * 2, inputNumber * 2, 2 }, 60);

      CreateStrategies();


    }
    string path = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\Data";
    TimeFrame[] timeFrames = new TimeFrame[] {
        TimeFrame.W1,
        TimeFrame.W2,
        TimeFrame.M1,
        TimeFrame.M3,
        TimeFrame.M6,
        TimeFrame.M12
        };


    Random random = new Random();
    List<List<SimulationTradingBot<AIPosition, AIStrategy>>> spliited = new List<List<SimulationTradingBot<AIPosition, AIStrategy>>>();
    private void CreateStrategies()
    {
      Bots.Clear();

      BotManager.CreateAgents();

      foreach (var agent in BotManager.Agents)
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
     }, new AIStrategy(agent)));

        adaAi.DisplayName = "ADAUSDT SPOT AI 15";
        adaAi.DataPath = $"ADAUSDT-240-generated.csv";
        adaAi.FromDate = new DateTime(random.Next(2019, 2023), random.Next(1, 13), random.Next(1, 25));
        //adaAi.FromDate = new DateTime(2019, 1, 1);
        adaAi.Take = 1000;
        adaAi.DataTimeFrame = TimeFrame.H4;

        Bots.Add(adaAi);
      }


      //SelectedBot.DrawChart = true;

      Task.Run(() =>
      {
        spliited = Bots.SplitList(20).ToList();

        foreach(var spliited in spliited)
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

            VSynchronizationContext.InvokeOnDispatcher(() =>
            {
              UpdateGeneration();
            });
          }

        }
      }
    }

    #region UpdateGeneration

    private async void UpdateGeneration()
    {
  
      var best = Bots.OrderByDescending(x => x.TradingBot.Strategy.TotalValue).First();

      await RunTest(best.TradingBot.Strategy.AIBot);

      GenerationCount++;
      FinishedCount = 0;
      ChartData.Add(Bots.Average(x => x.TradingBot.Strategy.TotalValue));
      BestData.Add(Bots.Max(x => x.TradingBot.Strategy.TotalValue));

      Labels.Add(GenerationCount);

      SaveProgress();
      BotManager.UpdateGeneration();
      CreateStrategies();
    }

    #endregion

    TaskCompletionSource<bool> taskCompletionSource;

    private Task RunTest(AIBot aIBot)
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
    }, new AIStrategy(new AIBot(new NeuralNetwork(aIBot.NeuralNetwork)))));

      adaAi.DisplayName = "ADAUSDT SPOT AI 15";
      adaAi.DataPath = $"ADAUSDT-240-generated.csv";
      adaAi.FromDate = new DateTime(2019, 1, 1);
      adaAi.DataTimeFrame = TimeFrame.H4;

      Task.Run(() =>
      {
        adaAi.Start();
        adaAi.Finished += AdaAi_Finished;

      
      });

      return taskCompletionSource.Task;
    }

    private void AdaAi_Finished(object sender, EventArgs e)
    {
      if(sender is SimulationTradingBot<AIPosition, AIStrategy> sim)
      {
        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          taskCompletionSource.SetResult(true);
          FullData.Add(sim.TradingBot.Strategy.TotalValue);
        });
      }
     
    }

    public void SaveProgress()
    {
      var bestGhost = BotManager.Networks.OrderByDescending(x => x.Fitness).FirstOrDefault();

      var gfolder = System.IO.Path.Combine("Trainings", session, "ADA");

      Directory.CreateDirectory(gfolder);

      bestGhost.SaveNeuralNetwork(System.IO.Path.Combine(gfolder, $"{GenerationCount}.txt"));
    }
  }
}