using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Strategy.Futures;
using CTKS_Chart.Trading;
using CTKS_Chart.Views.Prompts;
using Logger;
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
      BuyBotManager = SimulationAIPromptViewModel.GetNeatManager(viewModelsFactory, PositionSide.Buy);
      SellBotManager = SimulationAIPromptViewModel.GetNeatManager(viewModelsFactory, PositionSide.Sell);

      CreateBots();

    }

    public override string Title { get; set; } = "Simulation";

    NEATManager<AIBot> BuyBotManager { get; set; }
    NEATManager<AIBot> SellBotManager { get; set; }

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

      windowManager.ShowPrompt<DownloadSymbolView>(vm, 350, 350);
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
      }


      return asset;
    }

    #endregion

    #region CreateAiBot

    private void CreateAiBot()
    {
      var directories = AiPath.Split("\\");

      var buy = AiPath.Replace("SELL", "BUY");
      var sell = AiPath.Replace("BUY", "SELL");

      BuyBotManager.LoadBestGenome(buy);
      SellBotManager.LoadBestGenome(sell);

      BuyBotManager.InitializeManager(1);
      SellBotManager.InitializeManager(1);


      BuyBotManager.CreateAgents();
      SellBotManager.CreateAgents();

      var adaAi = GetTradingBot<AIPosition, AIStrategy>(viewModelsFactory, RunData.Symbol, RunData.Minutes, logger, new AIStrategy(BuyBotManager.Agents[0], SellBotManager.Agents[0]));

      var dailyCandles = TradingViewHelper.ParseTradingView(TimeFrame.D1, $"Data\\Indicators\\{adaAi.Asset.IndicatorDataPath}, 1D.csv", adaAi.Asset.Symbol, saveData: true);

      //ignore filter starting values of indicators
      adaAi.FromDate = dailyCandles.First(x => x.IndicatorData.RangeFilterData.HighTarget > 0).CloseTime.AddDays(30);

      adaAi.SaveResults = true;
      adaAi.DisplayName += " AI";
      adaAi.TradingBot.Strategy.StartingBudget = RunData.StartingBudget;
      adaAi.TradingBot.Strategy.Budget = RunData.StartingBudget;

      SelectedBot = adaAi;
      Bots.Add(adaAi);
    }

    #endregion 

    #endregion
  }
}