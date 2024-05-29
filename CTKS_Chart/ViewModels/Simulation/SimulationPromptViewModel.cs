﻿using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.Futures;
using CTKS_Chart.Trading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;

namespace CTKS_Chart.ViewModels
{
  public class SimulationPromptViewModel : BasePromptViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;

    public SimulationPromptViewModel(IViewModelsFactory viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      CreateBots();  
    }

    public override string Title { get; set; } = "Simulation";

    #region Bots

    private ObservableCollection<ITradingBotViewModel> bots = new ObservableCollection<ITradingBotViewModel>();

    public ObservableCollection<ITradingBotViewModel> Bots
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

    #region OnClose

    protected override void OnClose(Window window)
    {
      base.OnClose(window);

      this.SelectedBot.Stop();
    }

    #endregion

    private void CreateBots()
    {
      var path = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\Data";

      var timeFrames = new TimeFrame[] {
        TimeFrame.W1,
        TimeFrame.W2,
        TimeFrame.M1,
        TimeFrame.M3,
        TimeFrame.M6,
        TimeFrame.M12
        };

      var adaBotFutures = viewModelsFactory.Create<SimulationTradingBot<FuturesPosition, FuturesSimulationStrategy>>(new TradingBot<FuturesPosition, FuturesSimulationStrategy>(new Asset()
      {
        Symbol = "ADAUSDT",
        NativeRound = 1,
        PriceRound = 4,
        DataPath = path,
        DataSymbol = "BINANCE ADAUSD",
        TimeFrames = timeFrames,
      }, new FuturesSimulationStrategy(), TradingBotType.Futures));

      adaBotFutures.DisplayName = "ADAUSDT FUTURES";
      adaBotFutures.DataPath = $"ADAUSDT-15-generated.csv";

      var adaBot = viewModelsFactory.Create<SimulationTradingBot<Position, SimulationStrategy>>(new TradingBot<Position, SimulationStrategy>(new Asset()
      {
        Symbol = "ADAUSDT",
        NativeRound = 1,
        PriceRound = 4,
        DataPath = path,
        DataSymbol = "BINANCE ADAUSD",
        TimeFrames = timeFrames,
      }, new SimulationStrategy()));

      adaBot.DisplayName = "ADAUSDT SPOT 15";
      adaBot.DataPath = $"ADAUSDT-15-generated.csv";

      var adaBot1 = viewModelsFactory.Create<SimulationTradingBot<Position, SimulationStrategy>>(new TradingBot<Position, SimulationStrategy>(new Asset()
      {
        Symbol = "ADAUSDT",
        NativeRound = 1,
        PriceRound = 4,
        DataPath = path,
        DataSymbol = "BINANCE ADAUSD",
        TimeFrames = timeFrames,
      }, new SimulationStrategy()));

      adaBot1.DisplayName = "ADAUSDT SPOT 240";
      adaBot1.DataPath = $"ADAUSDT-240-generated.csv";


      var ltcBot = viewModelsFactory.Create<SimulationTradingBot<Position, SimulationStrategy>>(new TradingBot<Position, SimulationStrategy>(new Asset()
      {
        Symbol = "LTCUSDT",
        NativeRound = 3,
        PriceRound = 2,
        DataPath = path,
        DataSymbol = "BINANCE LTCUSD",
        TimeFrames = timeFrames,
      }, new SimulationStrategy()));

      var btcBot = viewModelsFactory.Create<SimulationTradingBot<Position, SimulationStrategy>>(new TradingBot<Position, SimulationStrategy>(new Asset()
      {
        Symbol = "BTCUSDT",
        NativeRound = 5,
        PriceRound = 2,
        DataPath = path,
        DataSymbol = "INDEX BTCUSD",
        TimeFrames = timeFrames,
      }, new SimulationStrategy()));

      btcBot.DataPath = $"BTCUSDT-240-generated.csv";

      Bots.Add(adaBotFutures);
      Bots.Add(adaBot);
      Bots.Add(adaBot1);
      Bots.Add(btcBot);
      Bots.Add(ltcBot);

      adaBotFutures.DrawChart = true;
      adaBotFutures.DrawingViewModel.EnableAutoLock = true;
      adaBotFutures.Delay = 100;

      SelectedBot = adaBot1;
    }
  }
}