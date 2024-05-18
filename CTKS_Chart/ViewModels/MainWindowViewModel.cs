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
using System.Windows.Media.Imaging;
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

    private ITradingBot tradingBotViewModel;

    public ITradingBot TradingBotViewModel
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
      var prompt = ViewModelsFactory.Create<SimulationPromptViewModel>();
      TradingBotViewModel.IsPaused = true;
        
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


      var selectedBot = new BaseTradingBot<Position, BinanceStrategy>(asset, ViewModelsFactory.Create<BinanceStrategy>());

      TradingBotViewModel = ViewModelsFactory.Create<BaseTradingBotViewModel<Position, BinanceStrategy>>(selectedBot);


      TradingBotViewModel.MainWindow = (MainWindow)Window;
      TradingBotViewModel.Start();

      Title = selectedBot.Asset.Symbol;



      ((MainWindow)Window).ChangeIcon(selectedBot.Asset.Symbol);

    }

    #endregion

    #endregion
  }
}

