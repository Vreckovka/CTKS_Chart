using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Strategy.Futures;
using CTKS_Chart.Trading;
using CTKS_Chart.Views.Prompts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;
using VNeuralNetwork;

namespace CTKS_Chart.ViewModels
{
  public class SimulationPromptViewModel : BasePromptViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IWindowManager windowManager;

    public SimulationPromptViewModel(IViewModelsFactory viewModelsFactory, IWindowManager windowManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      BuyBotManager = SimulationAIPromptViewModel.GetNeatManager(viewModelsFactory, PositionSide.Buy);
      SellBotManager = SimulationAIPromptViewModel.GetNeatManager(viewModelsFactory, PositionSide.Sell);

      CreateBots();

    }

    public override string Title { get; set; } = "Simulation";

    NEATManager<AIBot> BuyBotManager { get; set; }
    NEATManager<AIBot> SellBotManager { get; set; }

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

    protected ActionCommand loadAiBot;


    public ICommand LoadAiBot
    {
      get
      {
        return loadAiBot ??= new ActionCommand(OnLoadAiBot, () => File.Exists(AiPath));
      }
    }

    public void OnLoadAiBot()
    {
      CreateAiBot();
    }

    #endregion

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

    private string aiPath = @"Trainings\10_08_2024_07_56_37\ADA\BUY\25.txt";

    public string AiPath
    {
      get { return aiPath; }
      set
      {
        if (value != aiPath)
        {
          aiPath = value;

          RaisePropertyChanged();
          loadAiBot?.RaiseCanExecuteChanged();
        }
      }
    }

    #endregion

    #region CreateBots

    private void CreateBots()
    {
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "ADAUSDT", "15"));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "ADAUSDT", "120"));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "ADAUSDT", "240"));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "BTCUSDT", "240"));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "ETHUSDT", "240"));
      Bots.Add(GetTradingBot<Position, SimulationStrategy>(viewModelsFactory, "LTCUSDT", "240"));



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
      string timeframe,
      TStrategy strategy = null)
        where TPosition : Position, new()
        where TStrategy : BaseSimulationStrategy<TPosition>, new()
    {
      var newBot = viewModelsFactory.Create<SimulationTradingBot<TPosition, TStrategy>>(
        new TradingBot<TPosition, TStrategy>(GetAsset(symbol, timeframe), strategy ?? new TStrategy()));

      newBot.DisplayName = $"{symbol} {timeframe}";
      newBot.DataPath = GetSimulationDataPath(symbol, timeframe);

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

      BuyBotManager.LoadPopulation(buy);
      SellBotManager.LoadPopulation(sell);

      BuyBotManager.InitializeManager(1);
      SellBotManager.InitializeManager(1);


      BuyBotManager.CreateAgents();
      SellBotManager.CreateAgents();

      var adaAi = GetTradingBot<AIPosition, AIStrategy>(viewModelsFactory,"BTCUSDT","240", new AIStrategy(BuyBotManager.Agents[0], SellBotManager.Agents[0]));

      adaAi.FromDate = new DateTime(2019, 1, 1);
      adaAi.SaveResults = true;

      SelectedBot = adaAi;
      Bots.Add(adaAi);
    }

    #endregion
  }
}