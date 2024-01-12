using System;
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
using PositionSide = CTKS_Chart.Strategy.PositionSide;

namespace CTKS_Chart.ViewModels
{
  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    #region Constructors

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory) : base(viewModelsFactory)
    {
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
    
#if DEBUG
    public bool IsLive { get; set; } = true;
#endif

#if RELEASE
    public bool IsLive { get; set; } = true;
#endif


#if DEBUG
    public bool Simulation { get; set; } = false;
#endif

#if RELEASE
    public bool Simulation { get; set; } = false;
#endif


    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      string path = "Data";
      
      if (!IsLive)
        path = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\Data";

      var asset = JsonSerializer.Deserialize<Asset>(File.ReadAllText("asset.json"));

      asset.RunTime = TimeSpan.FromTicks(asset.RunTimeTicks);
      var timeFrames = new TimeFrame[] {
        TimeFrame.W1,
        TimeFrame.W2,
        TimeFrame.M1,
        TimeFrame.M3,
        TimeFrame.M6,
        TimeFrame.M12 };

      Strategy.Strategy strategy = ViewModelsFactory.Create<BinanceStrategy>();

      if (!IsLive)
        strategy = new SimulationStrategy();

      TradingBot selectedBot = null;

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

      if (IsLive)
        selectedBot = new TradingBot(asset, strategy);
      else
      {
        selectedBot = adaBot;
      }

      strategy.Asset = selectedBot.Asset;

      TradingBotViewModel = ViewModelsFactory.Create<TradingBotViewModel>(selectedBot);
      TradingBotViewModel.IsLive = IsLive;
      TradingBotViewModel.Simulation = Simulation;
      TradingBotViewModel.MainWindow = (MainWindow) Window;

      TradingBotViewModel.Start();
      Title = selectedBot.Asset.Symbol;
    }

    #endregion

    #endregion
  }
}

