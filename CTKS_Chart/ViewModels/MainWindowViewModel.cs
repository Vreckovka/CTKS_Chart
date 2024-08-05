using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CTKS_Chart.Binance;
using CTKS_Chart.Binance.Data;
using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.Futures;
using CTKS_Chart.Trading;
using CTKS_Chart.Views;
using CTKS_Chart.Views.Prompts;
using CTKS_Chart.Views.Simulation;
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

#if DEBUG
      IsDebug = true;
#endif
    }

    #endregion

    #region Properties

    public bool IsDebug { get; set; }

    #region TradingBotType

    private TradingBotType? botType;

    public TradingBotType? BotType
    {
      get { return botType; }
      set
      {
        if (value != botType)
        {
          botType = value;

          ITradingBotViewModel selectedBot = null;
          
          switch (botType.Value)
          {
            case TradingBotType.Spot:
              var spot = new TradingBot<Position, BinanceSpotStrategy>(asset, ViewModelsFactory.Create<BinanceSpotStrategy>(), TradingBotType.Spot);

              selectedBot = ViewModelsFactory.Create<TradingBotViewModel<Position, BinanceSpotStrategy>>(spot);
             
              break;
            case TradingBotType.Futures:
              var futures = new TradingBot<FuturesPosition, BinanceFuturesStrategy>(asset, ViewModelsFactory.Create<BinanceFuturesStrategy>(), TradingBotType.Futures);
              selectedBot = ViewModelsFactory.Create<TradingBotViewModel<FuturesPosition, BinanceFuturesStrategy>>(futures);
              break;
          }

          TradingBotViewModel = selectedBot;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TradingBotViewModel

    private ITradingBotViewModel tradingBotViewModel;

    public ITradingBotViewModel TradingBotViewModel
    {
      get { return tradingBotViewModel; }
      set
      {
        if (value != tradingBotViewModel)
        {
          tradingBotViewModel = value;

          SetTradingBot(tradingBotViewModel);
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public ObservableCollection<ITradingBotViewModel> TradingBots { get; } = new ObservableCollection<ITradingBotViewModel>();

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
      var prompt = ViewModelsFactory.Create<SimulationPromptViewModel>();
      TradingBotViewModel.IsPaused = true;

      windowManager.ShowPrompt<SimulationView>(prompt, 1000, 1000);

      TradingBotViewModel.IsPaused = false;
    }

    #endregion

    #region OpenAiTesting

    protected ActionCommand openAiTesting;

    public ICommand OpenAiTesting
    {
      get
      {
        return openAiTesting ??= new ActionCommand(OnOpenAiTesting);
      }
    }

    public void OnOpenAiTesting()
    {
      var prompt = ViewModelsFactory.Create<SimulationAIPromptViewModel>();
      TradingBotViewModel.IsPaused = true;

      windowManager.ShowPrompt<AiSimulationView>(prompt, 1250, 1000,false);

      TradingBotViewModel.IsPaused = false;
    }

    #endregion

    #region Icon

    private BitmapSource icon;

    public BitmapSource Icon
    {
      get { return icon; }
      set
      {
        if (value != icon)
        {
          icon = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Asset

    private Asset asset;

    public Asset Asset
    {
      get { return asset; }
      set
      {
        if (value != asset)
        {
          asset = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      Asset = JsonSerializer.Deserialize<Asset>(File.ReadAllText(Path.Combine(Settings.DataPath, "asset.json")));
      Asset.RunTime = TimeSpan.FromTicks(Asset.RunTimeTicks);
      
      BotType = TradingBotType.Spot;
    }

    #endregion

    #region SetTradingBot

    private void SetTradingBot(ITradingBotViewModel tradingBot)
    {
      TradingBotViewModel.MainWindow = (MainWindow)Window;

#if !DEBUG
      TradingBotViewModel.Start();
#endif
      Title = tradingBot.Asset.Symbol;

      ChangeIcon(tradingBot.Asset.Symbol);
    }

    #endregion

    #region ChangeIcon

    public void ChangeIcon(string symbol)
    {
      BitmapSource icon = null;
      symbol = symbol.ToUpper();

      if (symbol.Contains("BTC"))
      {
        icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(Resource.bitcoin.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
      }
      else if (symbol.Contains("LTC"))
      {
        icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(Resource.litecoin.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
      }
      else if (symbol.Contains("ADA"))
      {
        icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(Resource.cardano.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
      }
      else if (symbol.Contains("BNB"))
      {
        icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(Resource.binance.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
      }
      else if (symbol.Contains("ETH"))
      {
        icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(Resource.ethereum.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
      }

      this.Icon = icon;
    }

    #endregion

    #endregion
  }
}

