using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Strategy.Futures;
using CTKS_Chart.Trading;
using CTKS_Chart.Views.Prompts;
using Logger;
using SharpNeat.Genomes.Neat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;
using VNeuralNetwork;

namespace CTKS_Chart.ViewModels
{
  public class RunData : ViewModel
  {
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

    #region StartingBudget

    private decimal startingBudget = 1000;

    public decimal StartingBudget
    {
      get { return startingBudget; }
      set
      {
        if (value != startingBudget)
        {
          startingBudget = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }

  public class SimulationPromptViewModel : BasePromptViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IWindowManager windowManager;
    private readonly ILogger logger;

    public SimulationPromptViewModel(IViewModelsFactory viewModelsFactory, IWindowManager windowManager, ILogger logger)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.logger = logger;

      CreateBots();

    }

    public override string Title { get; set; } = "Simulation";

    public RunData RunData { get; set; } = new RunData();

    #region Bots

    private ObservableCollection<ISimulationTradingBot> bots = new ObservableCollection<ISimulationTradingBot>();

    public ObservableCollection<ISimulationTradingBot> Bots
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

    #region SelectedBot

    private ISimulationTradingBot selectedBot;

    public ISimulationTradingBot SelectedBot
    {
      get { return selectedBot; }
      set
      {
        if (value != selectedBot)
        {
          SelectedBot?.Stop();
          selectedBot = value;

          selectedBot.LoadSimulationResults();
          RaisePropertyChanged();
        }
      }
    }

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
      this.SelectedBot.Start();
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
      this.SelectedBot.Stop();
    }

    #endregion


    #region LoadAiBot

    private ActionCommand loadAiBot;

    public ICommand LoadAiBot
    {
      get
      {
        if (loadAiBot == null)
        {
          loadAiBot = new ActionCommand(OnLoadAiBot);
        }

        return loadAiBot;
      }
    }


    public virtual void OnLoadAiBot()
    {
      using (OpenFileDialog openFileDialog = new OpenFileDialog())
      {
        // Set properties for OpenFileDialog
        openFileDialog.Title = "Select a File";
        openFileDialog.Filter = "All files (*.*)|*.*";
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;
        openFileDialog.InitialDirectory = Path.GetDirectoryName("Trainings");

        // Show the dialog and check if the user selected a file
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
          // Get the selected file path
          AiPath = openFileDialog.FileName;

        }
      }
    }

    #endregion


    #region StartAiBot

    protected ActionCommand startAiBot;


    public ICommand StartAiBot
    {
      get
      {
        return startAiBot ??= new ActionCommand(OnStartAiBot, () => File.Exists(AiPath));
      }
    }

    public void OnStartAiBot()
    {
      CreateAiBot();
    }

    #endregion

    #endregion

    #region Methods

    #region OnClose

    protected override void OnClose(Window window)
    {
      base.OnClose(window);

      this.SelectedBot?.Stop();
    }

    #endregion

    #region DownloadSymbol

    protected ActionCommand downloadSymbol;

    public ICommand DownloadSymbol
    {
      get
      {
        return downloadSymbol ??= new ActionCommand(OnDownloadSymbol);
      }
    }


    public void OnDownloadSymbol()
    {
      var vm = viewModelsFactory.Create<DownloadSymbolViewModel>();

      windowManager.ShowPrompt<DownloadSymbolView>(vm, 350, 350, false);
    }

    #endregion

    #region AiPath

    private string aiPath = @"D:\Aplikacie\Skusobne\CTKS_Chart\CouldComputingServer\bin\Release\netcoreapp3.1\Trainings\16_08_2024_17_35_56\Generation 600\BUY.txt";

    public string AiPath
    {
      get { return aiPath; }
      set
      {
        if (value != aiPath)
        {
          aiPath = value;

          RaisePropertyChanged();
          startAiBot?.RaiseCanExecuteChanged();
        }
      }
    }

    #endregion

    #region CreateBots

    private void CreateBots()
    {
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "ADAUSDT", 15, logger));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "ADAUSDT", 120, logger));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "ADAUSDT", 240, logger));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "BTCUSDT", 240, logger));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "ETHUSDT", 240, logger));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "LTCUSDT", 240, logger));



      foreach (var bot in Bots)
      {
        bot.SaveResults = true;
      }


      SelectedBot = Bots[2];
    }

    #endregion

    public static SimulationTradingBot<TPosition, TStrategy> GetTradingBot<TPosition, TStrategy>(
      IViewModelsFactory viewModelsFactory,
      string symbol,
      int minutes,
      ILogger logger,
      TStrategy strategy = null)
        where TPosition : Position, new()
        where TStrategy : BaseSimulationStrategy<TPosition>, new()
    {
      var newBot = viewModelsFactory.Create<SimulationTradingBot<TPosition, TStrategy>>(
        new TradingBot<TPosition, TStrategy>(GetAsset(symbol, minutes.ToString()), strategy ?? new TStrategy()));

      newBot.DisplayName = $"{symbol} {minutes.ToString()}";
      newBot.DataPath = GetSimulationDataPath(symbol, minutes.ToString());
      newBot.TradingBot.Strategy.Logger = logger;
      newBot.Minutes = minutes;

      return newBot;
    }

    public static string GetSimulationDataPath(string symbol, string timeframe)
    {
      return $"Training data\\{symbol}-{timeframe}-generated.csv";
    }

    #region GetAsset

    public static Asset GetAsset(string symbol, string timeFrame)
    {
      Asset asset = null;
      string path = "Chart Data";

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
            IndicatorDataPath = "BINANCE LTCUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "ETHUSDT":
          asset = new Asset()
          {
            Symbol = "ETHUSDT",
            NativeRound = 4,
            PriceRound = 2,
            DataPath = path,
            DataSymbol = "BINANCE ETHUSDT",
            IndicatorDataPath = "BINANCE ETHUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "BNBUSDT":
          asset = new Asset()
          {
            Symbol = "BNBUSDT",
            NativeRound = 3,
            PriceRound = 1,
            DataPath = path,
            DataSymbol = "BINANCE BNBUSDT",
            IndicatorDataPath = "BINANCE BNBUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "EOSUSDT":
          asset = new Asset()
          {
            Symbol = "EOSUSDT",
            NativeRound = 1,
            PriceRound = 4,
            DataPath = path,
            DataSymbol = "BINANCE EOSUSDT",
            IndicatorDataPath = "BINANCE EOSUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "ALGOUSDT":
          asset = new Asset()
          {
            Symbol = "ALGOUSDT",
            NativeRound = 3,
            PriceRound = 2,
            DataPath = path,
            DataSymbol = "BINANCE ALGOUSDT",
            IndicatorDataPath = "BINANCE ALGOUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "LINKUSDT":
          asset = new Asset()
          {
            Symbol = "LINKUSDT",
            NativeRound = 2,
            PriceRound = 2,
            DataPath = path,
            DataSymbol = "BINANCE LINKUSDT",
            IndicatorDataPath = "BINANCE LINKUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "COTIUSDT":
          asset = new Asset()
          {
            Symbol = "COTIUSDT",
            NativeRound = 0,
            PriceRound = 5,
            DataPath = path,
            DataSymbol = "BINANCE COTIUSD",
            IndicatorDataPath = "BINANCE COTIUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "SOLUSDT":
          asset = new Asset()
          {
            Symbol = "SOLUSDT",
            NativeRound = 3,
            PriceRound = 2,
            DataPath = path,
            DataSymbol = "BINANCE SOLUSDT",
            IndicatorDataPath = "BINANCE SOLUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "MATICUSDT":
          asset = new Asset()
          {
            Symbol = "MATICUSDT",
            NativeRound = 1,
            PriceRound = 4,
            DataPath = path,
            DataSymbol = "BINANCE MATICUSDT",
            IndicatorDataPath = "BINANCE MATICUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "AVAXUSDT":
          asset = new Asset()
          {
            Symbol = "AVAXUSDT",
            NativeRound = 2,
            PriceRound = 2,
            DataPath = path,
            DataSymbol = "BINANCE AVAXUSDT",
            IndicatorDataPath = "BINANCE AVAXUSDT",
            TimeFrames = timeFrames,
          };
          break;
        case "GALAUSDT":
          asset = new Asset()
          {
            Symbol = "GALAUSDT",
            NativeRound = 0,
            PriceRound = 5,
            DataPath = path,
            DataSymbol = "BINANCE GALAUSDT",
            IndicatorDataPath = "BINANCE GALAUSDT",
            TimeFrames = timeFrames,
          };
          break;
      }


      return asset;
    }

    #endregion

    #region CreateAiBot

    private async void CreateAiBot()
    {

      var directories = AiPath.Split("\\");

      var buy = AiPath.Replace("SELL", "BUY");
      var sell = AiPath.Replace("BUY", "SELL");

      var BuyBotManager = SimulationAIPromptViewModel.GetNeatManager(viewModelsFactory, PositionSide.Buy);
      var SellBotManager = SimulationAIPromptViewModel.GetNeatManager(viewModelsFactory, PositionSide.Sell);

      BuyBotManager.LoadBestGenome(buy);
      SellBotManager.LoadBestGenome(sell);

      BuyBotManager.InitializeManager(1);
      SellBotManager.InitializeManager(1);


      BuyBotManager.CreateAgents();
      SellBotManager.CreateAgents();

      var adaAi = GetTradingBot<AIPosition, AIStrategy>(viewModelsFactory, RunData.Symbol, RunData.Minutes, logger, new AIStrategy(BuyBotManager.Agents[0], SellBotManager.Agents[0]));

      foreach (var indiFrame in TradingBotViewModel<Position, BaseStrategy<Position>>.IndicatorTimeframes)
      {
        SimulationTradingBot.GetIndicatorData(adaAi.TimeFrameDatas[indiFrame], adaAi.Asset);
      }

      adaAi.SaveResults = true;
      adaAi.DisplayName += " AI";
      adaAi.TradingBot.Strategy.StartingBudget = RunData.StartingBudget;
      adaAi.TradingBot.Strategy.Budget = RunData.StartingBudget;

      SelectedBot = adaAi;
      Bots.Add(adaAi);

      await Task.Run(() =>
      {
        for (int i = 0; i < 5; i++)
        {
          Task.Run(async () =>
          {
            var symbolsToTest = new string[] {
            "COTIUSDT",
            //"ADAUSDT",
            //"BTCUSDT",
            //"MATICUSDT",
            //"BNBUSDT",
            //"ALGOUSDT"
            };


            var fitness = new List<float>();

            var buyG = BuyBotManager.NeatAlgorithm.GenomeList[0];
            var sellG = SellBotManager.NeatAlgorithm.GenomeList[0];

            foreach (var symbol in symbolsToTest)
            {
              var aIBotRunner = new AIBotRunner(logger, viewModelsFactory);

              await aIBotRunner.RunGeneration(
                1,
                240,
                symbol,
                0,
                0,
                new List<NeatGenome>() { new NeatGenome(buyG, buyG.Id, 0) },
                new List<NeatGenome>() { new NeatGenome(sellG, sellG.Id, 0) }
                );

              var neat = aIBotRunner.Bots[0].TradingBot.Strategy.BuyAIBot.NeuralNetwork;

              fitness.Add(neat.Fitness);

              Debug.WriteLine(aIBotRunner.Bots[0].TradingBot.Strategy.TotalValue);
            }
          });
        }
      });
     


      await Task.Run(async () =>
      {
        var symbolsToTest = new string[] {
            "COTIUSDT",
          //"ADAUSDT",
          //"BTCUSDT",
          //"MATICUSDT",
          //"BNBUSDT",
          //"ALGOUSDT"
        };


        var fitness = new List<float>();

        var buyG = BuyBotManager.NeatAlgorithm.GenomeList[0];
        var sellG = SellBotManager.NeatAlgorithm.GenomeList[0];

        foreach (var symbol in symbolsToTest)
        {
          var aIBotRunner = new AIBotRunner(logger, viewModelsFactory);

          await aIBotRunner.RunGeneration(
            5,
            240,
            symbol,
            0,
            0,
            new List<NeatGenome>() {
              new NeatGenome(buyG, buyG.Id, 0),
              new NeatGenome(buyG, buyG.Id, 0),
              new NeatGenome(buyG, buyG.Id, 0),
              new NeatGenome(buyG, buyG.Id, 0),
              new NeatGenome(buyG, buyG.Id, 0)
            },
            new List<NeatGenome>() {
              new NeatGenome(sellG, sellG.Id, 0),
              new NeatGenome(sellG, sellG.Id, 0),
              new NeatGenome(sellG, sellG.Id, 0),
              new NeatGenome(sellG, sellG.Id, 0),
              new NeatGenome(sellG, sellG.Id, 0)
            }
            );

          Debug.WriteLine(aIBotRunner.Bots[0].TradingBot.Strategy.TotalValue);
          Debug.WriteLine(aIBotRunner.Bots[1].TradingBot.Strategy.TotalValue);
          Debug.WriteLine(aIBotRunner.Bots[2].TradingBot.Strategy.TotalValue);
          Debug.WriteLine(aIBotRunner.Bots[3].TradingBot.Strategy.TotalValue);
          Debug.WriteLine(aIBotRunner.Bots[4].TradingBot.Strategy.TotalValue);
        }
      });
    }

    #endregion 

    #endregion
  }
}