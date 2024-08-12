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

    private void CreateBots()
    {
      var adaBotFutures = viewModelsFactory.Create<SimulationTradingBot<FuturesPosition, FuturesSimulationStrategy>>(
        new TradingBot<FuturesPosition, FuturesSimulationStrategy>(GetAsset("ADAUSDT"), new FuturesSimulationStrategy(), TradingBotType.Futures));

      adaBotFutures.DisplayName = "ADAUSDT FUTURES";
      adaBotFutures.DataPath = $"ADAUSDT-15-generated.csv";


      var adaBot = viewModelsFactory.Create<SimulationTradingBot<Position, SimulationStrategy>>(
        new TradingBot<Position, SimulationStrategy>(GetAsset("ADAUSDT"), new SimulationStrategy()));

      adaBot.DisplayName = "ADAUSDT SPOT 15";
      adaBot.DataPath = $"ADAUSDT-15-generated.csv";

      var adaBot1 = viewModelsFactory.Create<SimulationTradingBot<Position, SimulationStrategy>>(
        new TradingBot<Position, SimulationStrategy>(GetAsset("ADAUSDT"), new SimulationStrategy()));

      adaBot1.DisplayName = "ADAUSDT SPOT 240";
      adaBot1.DataPath = $"ADAUSDT-240-generated.csv";


      var ltcBot = viewModelsFactory.Create<SimulationTradingBot<Position, SimulationStrategy>>(
        new TradingBot<Position, SimulationStrategy>(GetAsset("LTCUSDT"), new SimulationStrategy()));

      var btcBot = viewModelsFactory.Create<SimulationTradingBot<Position, SimulationStrategy>>(
        new TradingBot<Position, SimulationStrategy>(GetAsset("BTCUSDT"), new SimulationStrategy()));

      btcBot.DataPath = $"BTCUSDT-240-generated.csv";

      Bots.Add(adaBotFutures);
      Bots.Add(adaBot);
      Bots.Add(adaBot1);
      Bots.Add(btcBot);
      Bots.Add(ltcBot);

      foreach (var bot in Bots)
      {
        bot.SaveResults = true;
      }


      SelectedBot = adaBot;
    }

    #region GetAsset

    public static Asset GetAsset(string symbol)
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
            IndicatorDataPath = "BINANCE LTCUSD",
            TimeFrames = timeFrames,
          };
          break;
      }


      return asset;
    }

    #endregion

    private void CreateAiBot()
    {
      var directories = AiPath.Split("\\");

      var date = directories.Reverse().ToArray()[3];
      var number = Path.GetFileName(AiPath).Replace(".txt","");

      var buy = AiPath.Replace("SELL", "BUY");
      var sell = AiPath.Replace("BUY", "SELL");

      BuyBotManager.LoadPopulation(buy);
      SellBotManager.LoadPopulation(sell);

      BuyBotManager.InitializeManager(1);
      SellBotManager.InitializeManager(1);


      BuyBotManager.CreateAgents();
      SellBotManager.CreateAgents();

      var adaAi = viewModelsFactory.Create<SimulationTradingBot<AIPosition, AIStrategy>>(
                   new TradingBot<AIPosition, AIStrategy>(GetAsset("ADAUSDT"), new AIStrategy(BuyBotManager.Agents[0], SellBotManager.Agents[0])));

      adaAi.DisplayName = $"ADAUSDT SPOT AI 240 {date} - {number}";
      adaAi.DataPath = $"ADAUSDT-240-generated.csv";
      adaAi.FromDate = new DateTime(2019, 1, 1);
      adaAi.SaveResults = true;

      SelectedBot = adaAi;
      Bots.Add(adaAi);
    }
  }
}