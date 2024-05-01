﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CTKS_Chart.Binance;
using CTKS_Chart.Binance.Data;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using CTKS_Chart.Views;
using CTKS_Chart.Views.Prompts;
using Logger;
using VCore.ItemsCollections;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Logger;
using VCore.WPF.Misc;
using VCore.WPF.Other;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.Prompt;
using Path = System.IO.Path;
using PositionSide = CTKS_Chart.Strategy.PositionSide;

namespace CTKS_Chart.ViewModels
{
  public static class Settings
  {
    public const string DataPath = "Data";
  }

  //TODO: CTKSLines save to extra file and load from there
  //TODO: Every layout could be separate file
  //TODO: Put all settings in same folder (Now it is scattered in main folder + State + Data) 
  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private readonly IWindowManager windowManager;
    private readonly BinanceDataProvider binanceDataProvider;

    #region Constructors

    public MainWindowViewModel(
      IViewModelsFactory viewModelsFactory, 
      IWindowManager windowManager) : base(viewModelsFactory)
    {
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      CultureInfo.CurrentCulture = new CultureInfo("en-US");
    }

    #endregion

    #region Properties

    #region TradingBotViewModel

    private TradingBotViewModel tradingBotViewModel;

    public TradingBotViewModel TradingBotViewModel
    {
      get { return tradingBotViewModel; }
      set
      {
        if (value != tradingBotViewModel)
        {
          tradingBotViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region OpenTesting

    protected ActionCommand openTesting;

    public ICommand OpenTesting
    {
      get
      {
        return openTesting ??= new ActionCommand(OnOpenTesting);
      }
    }

    public void OnOpenTesting()
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

      var strategy = new SimulationStrategy();

      var adaBot = new TradingBot(new Asset()
      {
        Symbol = "ADAUSDT",
        NativeRound = 1,
        PriceRound = 4,
        DataPath = path,
        DataSymbol = "BINANCE ADAUSD",
        TimeFrames = timeFrames,
      }, strategy);

      var ltcBot = new TradingBot(new Asset()
      {
        Symbol = "LTCUSDT",
        NativeRound = 3,
        PriceRound = 2,
        DataPath = path,
        DataSymbol = "BINANCE LTCUSD",
        TimeFrames = timeFrames,
      }, strategy);

      var btcBot = new TradingBot(new Asset()
      {
        Symbol = "BTCUSDT",
        NativeRound = 5,
        PriceRound = 2,
        DataPath = path,
        DataSymbol = "INDEX BTCUSD",
        TimeFrames = timeFrames,
      }, strategy);


      var tradingView__ada_1D = $"ADAUSDT-240-generated.csv";
      //tradingView__ada_1D = $"BTCUSDT-240-generated.csv";

      var bot = ViewModelsFactory.Create<SimulationTradingBot>(adaBot);
      var prompt = ViewModelsFactory.Create<SimulationPromptViewModel>(bot);
      TradingBotViewModel.IsPaused = true;
      bot.DataPath = tradingView__ada_1D;

      windowManager.ShowPrompt<SimulationView>(prompt);

      TradingBotViewModel.IsPaused = false;
    }

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      var asset = JsonSerializer.Deserialize<Asset>(File.ReadAllText(Path.Combine(Settings.DataPath, "asset.json")));

      asset.RunTime = TimeSpan.FromTicks(asset.RunTimeTicks);


      var selectedBot = new TradingBot(asset, ViewModelsFactory.Create<BinanceStrategy>());

      TradingBotViewModel = ViewModelsFactory.Create<TradingBotViewModel>(selectedBot);


      TradingBotViewModel.MainWindow = (MainWindow)Window;
      TradingBotViewModel.Start();

      Title = selectedBot.Asset.Symbol;

     
    }
      #endregion

  

    protected override void OnClose(Window window)
    {
      base.OnClose(window);

      //chromeDriverProvider.Dispose();
    }
    #endregion
  }
}

