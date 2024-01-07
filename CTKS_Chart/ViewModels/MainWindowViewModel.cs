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
    private readonly IWindowManager windowManager;
    private Stopwatch stopwatch = new Stopwatch();
    private TimeSpan lastElapsed;
    private BinanceBroker binanceBroker;

    private string layoutPath = "layout.json";

    #region Constructors

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory, ILogger logger, IWindowManager windowManager) : base(viewModelsFactory)
    {
      Logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

      CultureInfo.CurrentCulture = new CultureInfo("en-US");
      binanceBroker = new BinanceBroker(logger);

      ShowClosedPositions = IsLive;

      foreach (KlineInterval interval in EnumHelper.GetAllValues(KlineInterval.GetType()))
      {
        var length = GetTimeSpanFromInterval(interval);
        var strLength = "";
        var type = "";

        if (length.TotalMinutes < 1)
        {
          strLength = $"{length.TotalSeconds}";
          type = "s";
        }
        else if (length.TotalHours < 1)
        {
          strLength = $"{length.TotalMinutes}";
          type = "m";
        }
        else if (length.TotalDays < 1)
        {
          strLength = $"{length.TotalHours}";
          type = "H";
        }
        else if (length.TotalDays < 7)
        {
          strLength = $"{length.TotalDays}";
          type = "D";
        }
        else if (length.TotalDays < 30)
        {
          strLength = $"{length.Days / 7}";
          type = "W";
        }
        else
        {
          strLength = $"{length.Days / 30}";
          type = "M";
        }

        LayoutIntervals.Add(new LayoutIntervalViewModel(new LayoutInterval()
        {
          Interval = interval,
          Title = $"{strLength}{type}"
        }));
      }

      LayoutIntervals.ViewModels[6].IsSelected = true;
      klineInterval = LayoutIntervals.SelectedItem.Model.Interval;

      ColorScheme = new ColorSchemeViewModel();

      ColorScheme.ColorSettings = new Dictionary<ColorPurpose, ColorSettingViewModel>()
      {
        {ColorPurpose.GREEN, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#00ff00",
          Purpose = ColorPurpose.GREEN
        })},
        {ColorPurpose.BUY, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#00ff00",
          Purpose = ColorPurpose.BUY
        })},
        {ColorPurpose.FILLED_BUY, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#00ff00",
          Purpose = ColorPurpose.FILLED_BUY
        })},
        {ColorPurpose.RED, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#ff0000",
          Purpose = ColorPurpose.RED
        })},
        {ColorPurpose.FILLED_SELL, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#ff0000",
          Purpose = ColorPurpose.FILLED_SELL
        })},
        {ColorPurpose.SELL, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#ff0000",
          Purpose = ColorPurpose.SELL
        })},
        {ColorPurpose.NO_POSITION, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#252525",
          Purpose = ColorPurpose.NO_POSITION
        })},
        {ColorPurpose.ACTIVE_BUY, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#ff00ff",
          Purpose = ColorPurpose.ACTIVE_BUY
        })},
        {ColorPurpose.AVERAGE_BUY, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#ffFFff",
          Purpose = ColorPurpose.AVERAGE_BUY
        })},
        {ColorPurpose.ATH, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#00FFFF",
          Purpose = ColorPurpose.ATH
        })},
      };
    }

    #endregion

    #region Properties

    #region LockChart

    private bool lockChart = true;

    public bool LockChart
    {
      get { return lockChart; }
      set
      {
        if (value != lockChart)
        {
          lockChart = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ColorScheme

    private ColorSchemeViewModel colorScheme;

    public ColorSchemeViewModel ColorScheme
    {
      get { return colorScheme; }
      set
      {
        if (value != colorScheme)
        {
          colorScheme = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TradingBot

    private TradingBot tradingBot;

    public TradingBot TradingBot
    {
      get { return tradingBot; }
      set
      {
        if (value != tradingBot)
        {
          tradingBot = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CandleCount

    private int candleCount = 150;

    public int CandleCount
    {
      get { return candleCount; }
      set
      {
        if (value != candleCount)
        {
          candleCount = value;
          RecreateChart();

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowClosedPositions

    private bool showClosedPositions;

    public bool ShowClosedPositions
    {
      get { return showClosedPositions; }
      set
      {
        if (value != showClosedPositions)
        {
          showClosedPositions = value;

          if (MainLayout != null)
            RenderLayout(MainLayout, InnerLayouts, actual, ActualCandles);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region  ConsoleCollectionLogger

    public CollectionLogger ConsoleCollectionLogger
    {
      get { return (CollectionLogger)logger.LoggerContainer; }

    }

    #endregion

    #region ShowAveragePrice

    private bool showAveragePrice;

    public bool ShowAveragePrice
    {
      get { return showAveragePrice; }
      set
      {
        if (value != showAveragePrice)
        {
          showAveragePrice = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowATH

    private bool showATH;

    public bool ShowATH
    {
      get { return showATH; }
      set
      {
        if (value != showATH)
        {
          showATH = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Logger

    private ILogger logger;

    public ILogger Logger
    {
      get { return logger; }
      set
      {
        if (value != logger)
        {
          logger = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public ItemsViewModel<LayoutIntervalViewModel> LayoutIntervals { get; } = new ItemsViewModel<LayoutIntervalViewModel>();

#if DEBUG
    public bool Simulation { get; set; } = false;
#endif

#if RELEASE
    public bool Simulation { get; set; } = false;
#endif

#if DEBUG
    public bool IsLive { get; set; } = true;
#endif

#if RELEASE
    public bool IsLive { get; set; } = true;
#endif
    public TimeSpan TotalRunTime
    {
      get
      {
        return TradingBot.Asset.RunTime;
      }
      set
      {
        TradingBot.Asset.RunTime = value;
        RaisePropertyChanged();
      }
    }

    #region MaxValue

    private decimal maxValue;

    public decimal MaxValue
    {
      get { return maxValue; }
      set
      {
        if (value != maxValue && value > minValue)
        {
          maxValue = Math.Round(value, TradingBot.Asset.PriceRound);

          MainLayout.MaxValue = MaxValue;
          TradingBot.Asset.StartMaxPrice = MaxValue;
          SelectedLayout.MaxValue = MaxValue;

          RecreateChart();

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinValue

    private decimal minValue = (decimal)0.001;

    public decimal MinValue
    {
      get { return minValue; }
      set
      {
        if (value != minValue && value < maxValue)
        {
          minValue = Math.Round(value, TradingBot.Asset.PriceRound);

          MainLayout.MinValue = MinValue;
          TradingBot.Asset.StartLowPrice = MinValue;
          SelectedLayout.MinValue = MinValue;

          RecreateChart();

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region KlineInterval

#if DEBUG
    private KlineInterval klineInterval = KlineInterval.OneHour;
#endif

#if RELEASE
    private KlineInterval klineInterval = KlineInterval.OneHour;
#endif

    public KlineInterval KlineInterval
    {
      get { return klineInterval; }
      set
      {
        if (value != klineInterval)
        {
          klineInterval = value;
          RaisePropertyChanged();
          RecreateChart(true);
        }
      }
    }

    #endregion

    #region DailyChange

    private decimal dailyChange;

    public decimal DailyChange
    {
      get { return dailyChange; }
      set
      {
        if (value != dailyChange)
        {
          dailyChange = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region FromAllTimeHigh

    private decimal fromAllTimeHigh;

    public decimal FromAllTimeHigh
    {
      get { return fromAllTimeHigh; }
      set
      {
        if (value != fromAllTimeHigh)
        {
          fromAllTimeHigh = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public ObservableCollection<Layout> Layouts { get; set; } = new ObservableCollection<Layout>();


    #region Selected

    private Layout selectedLayout;

    public Layout SelectedLayout
    {
      get { return selectedLayout; }
      set
      {
        if (value != selectedLayout)
        {
          selectedLayout = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CanvasHeight

    private double canvasHeight = 1000;

    public double CanvasHeight
    {
      get { return canvasHeight; }
      set
      {
        if (value != canvasHeight)
        {
          canvasHeight = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CanvasWidth

    private double canvasWidth = 1000;

    public double CanvasWidth
    {
      get { return canvasWidth; }
      set
      {
        if (value != canvasWidth)
        {
          canvasWidth = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public Layout MainLayout { get; set; }

    public Grid MainGrid { get; } = new Grid();

    public Image ChartImage { get; } = new Image();

    #region ActiveTime

    private TimeSpan activeTime;

    public TimeSpan ActiveTime
    {
      get { return activeTime; }
      set
      {
        if (value != activeTime)
        {
          activeTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Chart

    private DrawingImage chart;

    public DrawingImage Chart
    {
      get { return chart; }
      set
      {
        if (value != chart)
        {
          chart = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region ShowCanvas

    protected ActionCommand<Layout> showCanvas;

    public ICommand ShowCanvas
    {
      get
      {
        return showCanvas ??= new ActionCommand<Layout>(OnShowCanvas);
      }
    }

    protected virtual void OnShowCanvas(Layout layout)
    {
      if (layout.Canvas == null)
      {
        MainGrid.Children.Clear();

        MainGrid.Children.Add(ChartImage);

        SelectedLayout = layout;
      }
      else
      {
        ScaleCanvas(layout);
      }
    }

    #endregion

    #region ScaleCanvas

    private void ScaleCanvas(Layout layout, decimal? maxValue = null, decimal? minValue = null)
    {
      MainGrid.Children.Clear();

      var index = InnerLayouts.IndexOf(layout);
      var globalIndex = Layouts.IndexOf(layout);

      if (index >= 0)
      {
        layout = CreateCtksChart(
          layout.DataLocation,
          layout.TimeFrame,
          ChartImage.ActualWidth,
          ChartImage.ActualHeight,
          null,
          maxValue,
          minValue);

        InnerLayouts.RemoveAt(index);
        InnerLayouts.Insert(index, layout);

        Layouts.RemoveAt(globalIndex);
        Layouts.Insert(globalIndex, layout);

        layout.Canvas.Height = ChartImage.ActualHeight;
        layout.Canvas.Width = ChartImage.ActualWidth;
      }

      if (layout != null)
        MainGrid.Children.Add(layout.Canvas);

      SelectedLayout = layout;
    }

    #endregion

    #region ResetBot

    protected ActionCommand resetBot;

    public ICommand ResetBot
    {
      get
      {
        return resetBot ??= new ActionCommand(OnResetBot);
      }
    }

    protected virtual async void OnResetBot()
    {
      var answer = windowManager.ShowQuestionPrompt("Do you really want to RESET bot?", "Reset Bot");
      if (answer == PromptResult.Ok)
      {
        await TradingBot.Strategy.Reset(actual);
      }

    }

    #endregion

    #region FetchMissingInfo

    protected ActionCommand fetchMissingInfo;

    public ICommand FetchMissingInfo
    {
      get
      {
        return fetchMissingInfo ??= new ActionCommand(OnFetchMissingInfo);
      }
    }

    protected virtual async void OnFetchMissingInfo()
    {
      if (TradingBot.Strategy is BinanceStrategy binanceStrategy)
        await binanceStrategy.FetchMissingInfo();


      var old = TradingBot.Strategy;
      TradingBot.Strategy = null;
      TradingBot.Strategy = old;
    }

    #endregion

    #region ShowLines

    protected ActionCommand showLines;

    public ICommand ShowLines
    {
      get
      {
        return showLines ??= new ActionCommand(OnShowLines);
      }
    }

    protected virtual void OnShowLines()
    {
      var ctks = SelectedLayout.Ctks;

      if (ctks.LinesVisible)
      {
        ctks.ClearRenderedLines();
      }
      else
      {
        ctks.RenderLines();
      }

      ctks.LinesVisible = !ctks.LinesVisible;
    }

    #endregion

    #region ShowIntersections

    protected ActionCommand showIntersections;

    public ICommand ShowIntersections
    {
      get
      {
        return showIntersections ??= new ActionCommand(OnShowIntersections);
      }
    }

    protected virtual void OnShowIntersections()
    {
      var ctks = SelectedLayout.Ctks;

      if (ctks.IntersectionsVisible)
      {
        ctks.ClearRenderedIntersections();
      }
      else
      {
        ctks.RenderIntersections();
      }
    }

    #endregion

    #region CancelPositions

    protected ActionCommand cancelPositions;

    public ICommand CancelPositions
    {
      get
      {
        return cancelPositions ??= new ActionCommand(OnCancelPositions);
      }
    }

    protected async virtual void OnCancelPositions()
    {
      var open = await binanceBroker.GetOpenOrders(TradingBot.Asset.Symbol);

      foreach (var opend in open)
      {
        await binanceBroker.Close(TradingBot.Asset.Symbol, long.Parse(opend.Id));
      }
    }

    #endregion

    #region OpenStatistics

    protected ActionCommand openStatistics;

    public ICommand OpenStatistics
    {
      get
      {
        return openStatistics ??= new ActionCommand(OnOpenStatistics);
      }
    }

    protected void OnOpenStatistics()
    {
      windowManager.ShowPrompt<Statistics>(new StatisticsViewModel(TradingBot.Strategy));
    }

    #endregion

    #region OpenPositionSize

    protected ActionCommand openPositionSize;

    public ICommand OpenPositionSize
    {
      get
      {
        return openPositionSize ??= new ActionCommand(OnOpenPositionSize);
      }
    }

    protected async void OnOpenPositionSize()
    {
      var vm = new PositionSizeViewModel(TradingBot.Strategy, actual, windowManager);
      var positionResult = windowManager.ShowQuestionPrompt<PositionSizeView, PositionSizeViewModel>(vm);

      if (positionResult == PromptResult.Ok)
      {
        var result = windowManager.ShowQuestionPrompt("Do you really want to apply these changes?", "Position size mapping");

        if (result == PromptResult.Ok)
        {
          TradingBot.Strategy.PositionSizeMapping = vm.PositionSizeMapping;

          var list = TradingBot.Strategy.OpenBuyPositions.ToList();
          foreach (var buy in list)
          {
            await TradingBot.Strategy.OnCancelPosition(buy);
          }
        }
      }
    }

    #endregion

    #region OpenAssetSetting

    protected ActionCommand openAssetSetting;

    public ICommand OpenAssetSetting
    {
      get
      {
        return openAssetSetting ??= new ActionCommand(OnOpenAssetSetting);
      }
    }

    protected void OnOpenAssetSetting()
    {
      var vm = new AssetSettingViewModel(TradingBot.Asset);
      var positionResult = windowManager.ShowQuestionPrompt<AssetSettingView, AssetSettingViewModel>(vm);

      if (positionResult == PromptResult.Ok)
      {
        var result = windowManager.ShowQuestionPrompt("Do you really want to apply these changes?", "Asset setting mapping");

        if (result == PromptResult.Ok)
        {

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

      MainGrid.Children.Add(ChartImage);

      LayoutIntervals.OnActualItemChanged.Subscribe(x =>
      {
        if (x != null)
          KlineInterval = x.Model.Interval;
      });

      LoadLayoutSettings();
      ForexChart_Loaded();

      stopwatch.Start();
      Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnDispatcher().Subscribe((x) =>
      {
        var diff = stopwatch.Elapsed - lastElapsed;
        ActiveTime += diff;
        TotalRunTime += diff;

        lastElapsed = stopwatch.Elapsed;

        if (Math.Round(activeTime.TotalSeconds, 0) % 10 == 0 && IsLive)
        {
          TradingBot.Asset.RunTimeTicks = TotalRunTime.Ticks;
          var options = new JsonSerializerOptions()
          {
            WriteIndented = true
          };

          var json = JsonSerializer.Serialize<Asset>(TradingBot.Asset, options);
          File.WriteAllText("asset.json", json);
        }
      });
    }

    #endregion

    #region ForexChart_Loaded

    private async void ForexChart_Loaded()
    {
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

      MainLayout = new Layout()
      {
        Title = "Main",
        TimeFrame = TimeFrame.D1
      };

      Strategy.Strategy strategy = new BinanceStrategy(binanceBroker, logger);

      if (!IsLive)
        strategy = new SimulationStrategy();


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
        TradingBot = new TradingBot(asset, strategy);
      else
      {
        TradingBot = btcBot;
      }

      TradingBot.LoadTimeFrames();


      strategy.Asset = TradingBot.Asset;

      MainLayout.MaxValue = TradingBot.StartingMaxPrice;
      MainLayout.MinValue = TradingBot.StartingMinPrice;

      maxValue = MainLayout.MaxValue;
      minValue = MainLayout.MinValue;
      this.Title = TradingBot.Asset.Symbol;

      if (IsLive)
      {
        TradingBot.Strategy.Logger = logger;
        await LoadLayouts(MainLayout);
      }
      else
      {
        var tradingView__ada_1D = $"{TradingBot.Asset.DataPath}\\{TradingBot.Asset.DataSymbol}, 1D.csv";

        var mainCandles = ParseTradingView(tradingView__ada_1D);

        MainLayout.MaxValue = mainCandles.Max(x => x.High.Value);
        MainLayout.MinValue = mainCandles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

        var maxDate = mainCandles.First().CloseTime;
        //246
        await LoadLayouts(MainLayout, mainCandles, maxDate, fromTime: new DateTime(2023, 12, 1), simulate: true);

        MaxValue = MainLayout.MaxValue;
        MinValue = MainLayout.MinValue;
      }
    }

    #endregion

    #region LoadLAyouts

    private List<Candle> ActualCandles = new List<Candle>();
    private List<Layout> InnerLayouts = new List<Layout>();

    private async Task LoadLayouts(
      Layout mainLayout,
      IList<Candle> mainCandles = null,
      DateTime? maxTime = null,
      int skip = 0,
      int cut = 0,
      DateTime? fromTime = null,
      bool simulate = false)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, CanvasHeight, CanvasWidth, TradingBot.Asset);
      List<Candle> cutCandles = new List<Candle>();

      if (simulate && mainCandles != null)
      {
        ActualCandles = mainCandles.Skip(skip).SkipLast(cut).ToList();

        if (fromTime != null)
        {
          cutCandles = mainCandles.Where(x => x.CloseTime > fromTime.Value).ToList();
          ActualCandles = mainCandles.Where(x => x.CloseTime < fromTime.Value).ToList();

          MainLayout.MaxValue = cutCandles.Max(x => x.High.Value);
          MainLayout.MinValue = cutCandles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);
        }
      }
      else
      {
        ActualCandles = (await binanceBroker.GetCandles(TradingBot.Asset.Symbol, GetTimeSpanFromInterval(KlineInterval))).ToList();

        await binanceBroker.SubscribeToKlineInterval(TradingBot.Asset.Symbol, OnBinanceKlineUpdate, KlineInterval);
      }

      foreach (var layoutData in TradingBot.TimeFrames)
      {
        var layout = CreateCtksChart(layoutData.Key, layoutData.Value, CanvasWidth, CanvasHeight, maxTime);

        Layouts.Add(layout);
        InnerLayouts.Add(layout);
      }

      mainLayout.Ctks = mainCtks;
      Layouts.Add(mainLayout);
      SelectedLayout = mainLayout;


      if (ActualCandles.Count > 0)
      {
        RenderLayout(mainLayout, InnerLayouts, ActualCandles.Last(), ActualCandles);
      }

      if (simulate && mainCandles != null)
      {
        Simulate(cutCandles, mainLayout, ActualCandles, InnerLayouts, 1);
      }
    }

    #endregion

    #region RecreateChart

    private async void RecreateChart(bool fetchNewCandles = false)
    {
      if (TradingBot != null)
      {
        if (fetchNewCandles)
        {
          ActualCandles = (await binanceBroker.GetCandles(TradingBot.Asset.Symbol, GetTimeSpanFromInterval(KlineInterval))).ToList();
          await binanceBroker.SubscribeToKlineInterval(TradingBot.Asset.Symbol, OnBinanceKlineUpdate, KlineInterval);
        }

        if (ActualCandles.Count > 0)
        {
          RenderOverlay(MainLayout, ctksIntersections, TradingBot.Strategy, ActualCandles);
        }
      }
    }

    #endregion

    #region CheckLayout

    private void CheckLayout(Layout layout)
    {
      var innerCandles = ParseTradingView(layout.DataLocation);

      if (DateTime.Now > GetNextTime(innerCandles.Last().CloseTime, layout.TimeFrame))
      {
        layout.IsOutDated = true;
      }
      else
      {
        layout.IsOutDated = false;
      }
    }

    #endregion

    #region OnActualCandleChange

    private Candle actual = null;
    private void OnActualCandleChange(Layout layout, List<Layout> secondaryLayouts, Candle candle, List<Candle> candles)
    {
      candles.Add(candle);

      RenderLayout(layout, secondaryLayouts, candle, candles);
    }

    #endregion

    #region Simulate

    private void Simulate(
      List<Candle> cutCandles,
      Layout layout,
      List<Candle> candles,
      List<Layout> secondaryLayouts,
      int delay = 500)
    {
      if (candles.Count > 0)
      {
        RenderLayout(layout, secondaryLayouts, candles.Last(), candles);
      }

      Task.Run(async () =>
      {
        for (int i = 0; i < cutCandles.Count; i++)
        {
          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            var actual = cutCandles[i];

            OnActualCandleChange(layout, secondaryLayouts, actual, candles);
          });

          await Task.Delay(delay);
        }
      });
    }

    #endregion

    #region GetNextTime

    private DateTime GetNextTime(DateTime date, TimeFrame timeFrame)
    {
      switch (timeFrame)
      {
        case TimeFrame.Null:
          break;
        case TimeFrame.M12:
          return date.AddMonths(12);
        case TimeFrame.M6:
          return date.AddMonths(6);
        case TimeFrame.M3:
          return date.AddMonths(3);
        case TimeFrame.M1:
          return date.AddMonths(1);
        case TimeFrame.W2:
          return date.AddDays(14);
        case TimeFrame.W1:
          return date.AddDays(7);
        case TimeFrame.D1:
          return date.AddDays(1);
      }

      return DateTime.MinValue;
    }

    #endregion

    #region RenderLayout

    private bool shouldUpdate = true;
    private bool wasLoaded = false;
    List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();
    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public async void RenderLayout(Layout layout, List<Layout> secondaryLayouts, Candle actual, List<Candle> candles)
    {
      try
      {
        await semaphoreSlim.WaitAsync();

        if (IsLive)
          RenderOverlay(layout, ctksIntersections, TradingBot.Strategy, candles);

        this.actual = actual;

        foreach (var secondaryLayout in secondaryLayouts)
        {
          var lastCandle = secondaryLayout.Ctks.Candles.Last();
          if (actual.CloseTime > GetNextTime(lastCandle.CloseTime, secondaryLayout.TimeFrame))
          {
            var lastCount = secondaryLayout.Ctks.Candles.Count;
            var innerCandles = ParseTradingView(secondaryLayout.DataLocation, actual.CloseTime, addNotClosedCandle: true);

            secondaryLayout.Ctks.CrateCtks(innerCandles, () => CreateChart(secondaryLayout, CanvasHeight, CanvasWidth, innerCandles));

            if (innerCandles.Count > lastCount)
              shouldUpdate = true;

            if (IsLive)
              CheckLayout(secondaryLayout);
          }
        }


        if (shouldUpdate)
        {
          ctksIntersections.Clear();

          for (int y = 0; y < secondaryLayouts.Count; y++)
          {
            var intersections = secondaryLayouts[y].Ctks.ctksIntersections;

            var validIntersections = intersections
              .Where(x => x.Value < decimal.MaxValue && x.Value > decimal.MinValue).ToList();

            ctksIntersections.AddRange(validIntersections);
          }

          ctksIntersections = ctksIntersections.OrderByDescending(x => x.Value).ToList();
          shouldUpdate = false;
        }

        if (ctksIntersections.Count == 0)
        {
          return;
        }

        TradingBot.Strategy.Intersections = ctksIntersections;

        if (ctksIntersections.Count > 0)
        {
          if (!wasLoaded)
          {
            wasLoaded = true;

            TradingBot.Strategy.LoadState();
            await TradingBot.Strategy.RefreshState();
            ((MainWindow)Window).SortActualPositions();
          }


          TradingBot.Strategy.ValidatePositions(actual);
          TradingBot.Strategy.CreatePositions(actual);
        }
        else
        {
          Console.WriteLine("NO INTERSECTIONS, DOING NOTHING !");
        }


        RenderOverlay(layout, ctksIntersections, TradingBot.Strategy, candles);
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region CreateCtksChart

    private Layout CreateCtksChart(string location, TimeFrame timeFrame,
      double canvasWidth,
      double canvasHeight,
      DateTime? maxTime = null,
      decimal? pmax = null,
      decimal? pmin = null)
    {
      var candles = ParseTradingView(location, maxTime);

      var canvas = new Canvas();

      var max = pmax ?? candles.Max(x => x.High.Value);
      var min = pmin ?? candles.Min(x => x.Low.Value);

      var layout = new Layout()
      {
        Title = timeFrame.ToString(),
        Canvas = canvas,
        MaxValue = max,
        MinValue = min,
        TimeFrame = timeFrame,
        DataLocation = location,
      };

      canvas.Width = canvasWidth;
      canvas.Height = canvasHeight;

      var ctks = new Ctks(layout, timeFrame, canvasHeight, canvasWidth, TradingBot.Asset);

      ctks.CrateCtks(candles, () => CreateChart(layout, canvasHeight, canvasWidth, candles));

      layout.Ctks = ctks;

      return layout;
    }


    #endregion

    #region ParseTradingView

    private List<Candle> ParseTradingView(string path, DateTime? maxDate = null, int skip = 0, int cut = 0, bool addNotClosedCandle = false)
    {
      var list = new List<Candle>();

      var file = File.ReadAllText(path);

      var lines = file.Split("\n").Skip(1 + skip).ToArray();
      CultureInfo.CurrentCulture = new CultureInfo("en-US");

      foreach (var line in lines.TakeLast(lines.Length - cut))
      {
        var data = line.Split(",");

        long.TryParse(data[0], out var unixTimestamp);
        decimal.TryParse(data[1], out var openParsed);
        decimal.TryParse(data[2], out var highParsed);
        decimal.TryParse(data[3], out var lowParsed);
        decimal.TryParse(data[4], out var closeParsed);


        var dateTime = DateTimeHelper.UnixTimeStampToUtcDateTime(unixTimestamp);

        var isOverDate = dateTime > maxDate;

        if (isOverDate && addNotClosedCandle)
        {
          isOverDate = false;
          addNotClosedCandle = false;
        }

        if (!isOverDate)
        {
          list.Add(new Candle()
          {
            Close = closeParsed,
            Open = openParsed,
            High = highParsed,
            Low = lowParsed,
            CloseTime = dateTime,
            UnixTime = unixTimestamp
          });
        }
        else
        {
          break;
        }
      }

      return list;
    }

    #endregion

    #region CreateChart

    private void CreateChart(Layout layout, double canvasHeight, double canvasWidth, IList<Candle> candles, int? pmaxCount = null)
    {
      canvasWidth = canvasWidth * 0.85;

      var maxCount = pmaxCount ?? candles.Count;

      var skip = candles.Count - maxCount > 0 ? candles.Count - maxCount : 0;

      if (candles.Any())
      {
        int y = 0;
        for (int i = skip; i < candles.Count; i++)
        {
          var point = candles[i];
          var valueForCanvas = GetCanvasValue(canvasHeight, point.Close.Value, layout.MaxValue, layout.MinValue);
          var width = canvasWidth / maxCount;

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var lastCandle = new Rectangle()
          {
            Width = width,
            Height = 25,
            Fill = green ? GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush),
          };

          Panel.SetZIndex(lastCandle, 99);

          var open = i > 0 ?
            GetCanvasValue(canvasHeight, candles[i - 1].Close.Value, layout.MaxValue, layout.MinValue) :
            GetCanvasValue(canvasHeight, candles[i].Open.Value, layout.MaxValue, layout.MinValue);

          if (green)
          {
            lastCandle.Height = valueForCanvas - open;
          }
          else
          {
            lastCandle.Height = open - valueForCanvas;
          }

          layout.Canvas.Children.Add(lastCandle);

          if (green)
            Canvas.SetBottom(lastCandle, open);
          else
            Canvas.SetBottom(lastCandle, open - lastCandle.Height);

          Canvas.SetLeft(lastCandle, ((y + 1) * width) + 2);
          y++;
        }
      }
    }

    #endregion

    #region DrawChart

    private DrawnChart DrawChart(
      DrawingContext drawingContext,
      Layout layout,
      IList<Candle> candles,
      double canvasHeight,
      double canvasWidth,
      int maxCount = 150)
    {
      canvasWidth = canvasWidth * 0.85 - 150;

      var skip = candles.Count - maxCount > 0 ? candles.Count - maxCount : 0;

      var width = canvasWidth / maxCount;
      var margin = width * 0.95;

      double minDrawnPoint = 0;
      double maxDrawnPoint = 0;
      var drawnCandles = new List<ChartCandle>();

      if (candles.Any())
      {
        int y = 0;
        for (int i = skip; i < candles.Count; i++)
        {
          var point = candles[i];

          var close = GetCanvasValue(canvasHeight, point.Close.Value, layout.MaxValue, layout.MinValue);
          var open = GetCanvasValue(canvasHeight, point.Open.Value, layout.MaxValue, layout.MinValue);

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var selectedBrush = green ? GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);

          Pen pen = new Pen(selectedBrush, 3);
          Pen wickPen = new Pen(selectedBrush, 1);

          var newCandle = new Rect()
          {
            Width = width - margin,
          };

          var lastClose = i > 0 ?
            GetCanvasValue(canvasHeight, candles[i - 1].Close.Value, layout.MaxValue, layout.MinValue) :
            GetCanvasValue(canvasHeight, candles[i].Open.Value, layout.MaxValue, layout.MinValue);

          if (green)
          {
            newCandle.Height = close - lastClose;
          }
          else
          {
            newCandle.Height = lastClose - close;
          }

          newCandle.X = 150 + (y + 1) * width;

          if (green)
            newCandle.Y = canvasHeight - close;
          else
            newCandle.Y = canvasHeight - close - newCandle.Height;

          var high = candles[i].High.Value;
          var low = candles[i].Low.Value;

          if (!LockChart)
          {
            if (point.Low < layout.MinValue)
              low = layout.MinValue;

            if (point.High > layout.MaxValue)
              high = layout.MaxValue;
          }


          var topWickCanvas = GetCanvasValue(canvasHeight, high, layout.MaxValue, layout.MinValue);
          var bottomWickCanvas = GetCanvasValue(canvasHeight, low, layout.MaxValue, layout.MinValue);

          var wickTop = green ? close : open;
          var wickBottom = green ? open : close;

          Rect? topWick = null;
          Rect? bottomWick = null;

          if (topWickCanvas - wickTop > 0)
          {
            topWick = new Rect()
            {
              Height = topWickCanvas - wickTop,
              X = newCandle.X,
              Y = canvasHeight - wickTop - (topWickCanvas - wickTop),
            };
          }

          if (wickBottom - bottomWickCanvas > 0)
          {
            bottomWick = new Rect()
            {
              Height = wickBottom - bottomWickCanvas,
              X = newCandle.X,
              Y = canvasHeight - wickBottom,
            };
          }

          drawingContext.DrawRectangle(selectedBrush, pen, newCandle);


          if (topWick != null)
            drawingContext.DrawRectangle(selectedBrush, wickPen, topWick.Value);

          if (bottomWick != null)
            drawingContext.DrawRectangle(selectedBrush, wickPen, bottomWick.Value);

          drawnCandles.Add(new ChartCandle()
          {
            Candle = point,
            Body = newCandle,
            TopWick = topWick,
            BottomWick = bottomWick
          });

          y++;

          if (bottomWick != null && bottomWick.Value.Y < minDrawnPoint)
          {
            maxDrawnPoint = bottomWick.Value.Y;
          }

          if (topWick != null && topWick.Value.Y > maxDrawnPoint)
          {
            minDrawnPoint = topWick.Value.Y;
          }
        }
      }

      return new DrawnChart()
      {
        MaxDrawnPoint = maxDrawnPoint,
        MinDrawnPoint = minDrawnPoint,
        Candles = drawnCandles
      };
    }

    #endregion

    #region GetValueFromCanvas

    private double GetValueFromCanvas(double canvasHeight, double value, decimal maxValue, decimal minValue)
    {
      canvasHeight = canvasHeight * 0.75;

      var logMaxValue = Math.Log10((double)maxValue);
      var logMinValue = Math.Log10((double)minValue);

      var logRange = logMaxValue - logMinValue;

      return Math.Pow(10, (value * logRange / canvasHeight) + logMinValue);
    }

    #endregion

    #region GetCanvasValue

    private double GetCanvasValue(double canvasHeight, decimal value, decimal maxValue, decimal minValue)
    {
      canvasHeight = canvasHeight * 0.75;

      var logValue = Math.Log10((double)value);
      var logMaxValue = Math.Log10((double)maxValue);
      var logMinValue = Math.Log10((double)minValue);

      var logRange = logMaxValue - logMinValue;
      double diffrence = logValue - logMinValue;

      return diffrence * canvasHeight / logRange;
    }

    #endregion

    #region RenderOverlay

    public void RenderOverlay(
      Layout layout,
      List<CtksIntersection> ctksIntersections,
      Strategy.Strategy strategy,
      IList<Candle> candles)
    {
      Pen shapeOutlinePen = new Pen(Brushes.Transparent, 1);
      shapeOutlinePen.Freeze();

      DrawingGroup dGroup = new DrawingGroup();

      double imageHeight = 1000;
      double imageWidth = 1000;

      using (DrawingContext dc = dGroup.Open())
      {
        dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(imageHeight, imageWidth));

        var chart = DrawChart(dc, layout, candles, imageHeight, imageWidth, CandleCount);
        double desiredCanvasHeight = imageHeight;

        if (chart.MinDrawnPoint > imageHeight)
        {
          desiredCanvasHeight = chart.MinDrawnPoint;
        }

        var chartCandles = chart.Candles.ToList();

        if (chartCandles.Any())
        {
          RenderIntersections(dc, layout, ctksIntersections,
        strategy.AllOpenedPositions.ToList(),
        chartCandles,
        desiredCanvasHeight,
        imageHeight,
        imageWidth,
        !Simulation ? TimeFrame.W1 : TimeFrame.M1);

          if (ShowClosedPositions)
          {
            var validPositions = strategy.AllClosedPositions.Where(x => x.FilledDate > candles.First().OpenTime).ToList();

            RenderClosedPosiotions(dc, layout,
              validPositions,
              chartCandles,
              imageHeight,
              imageWidth);
          }

          DrawActualPrice(dc, layout, candles, imageHeight, imageWidth);

          decimal price = TradingBot.Strategy.AvrageBuyPrice;

          var maxCanvasValue = (decimal)GetValueFromCanvas(desiredCanvasHeight, desiredCanvasHeight, layout.MaxValue, layout.MinValue);
          var minCanvasValue = (decimal)GetValueFromCanvas(desiredCanvasHeight, -2 * (desiredCanvasHeight - canvasHeight), layout.MaxValue, layout.MinValue);

          maxCanvasValue = Math.Max(maxCanvasValue, chartCandles.Max(x => x.Candle.High.Value));
          minCanvasValue = Math.Min(minCanvasValue, chartCandles.Min(x => x.Candle.Low.Value));

          if (ShowAveragePrice)
          {
            if (price < maxCanvasValue && price > minCanvasValue)
              DrawAveragePrice(dc, layout, TradingBot.Strategy.AvrageBuyPrice, imageHeight, imageWidth);
          }


          if (ShowATH)
          {
            if (lastStates.Count == 0)
              price = GetToAthPrice(TradingBot.Strategy.MaxTotalValue);
            else if (lastStates.Count > 0)
            {
              if (!wasLoadedAvg)
              {
                sellId = 0;
                wasLoadedAvg = true;
              }

              var ath = lastStates.Max(x => x.TotalValue);

              price = GetToAthPrice(ath);
            }

            if (price < maxCanvasValue && price > minCanvasValue)
              DrawPriceToATH(dc, layout, price, imageHeight, imageWidth);
          }
        }

        DrawingImage dImageSource = new DrawingImage(dGroup);

        Chart = dImageSource;
        this.ChartImage.Source = Chart;
      }
    }

    #endregion

    private bool wasLoadedAvg = false;

    #region DrawActualPrice

    public void DrawActualPrice(DrawingContext drawingContext, Layout layout, IList<Candle> candles, double canvasHeight, double canvasWidth)
    {
      var lastCandle = candles.Last();
      var closePrice = lastCandle.Close;

      var close = GetCanvasValue(canvasHeight, closePrice.Value, layout.MaxValue, layout.MinValue);

      var lineY = canvasHeight - close;

      var brush = lastCandle.IsGreen ? GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);
      var pen = new Pen(brush, 1);
      pen.DashStyle = DashStyles.Dash;

      var text = GetFormattedText(closePrice.Value.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 20);
      drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 25, lineY - text.Height - 5));
      drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
    }

    #endregion

    #region DrawAveragePrice

    public void DrawAveragePrice(DrawingContext drawingContext, Layout layout, decimal price, double canvasHeight, double canvasWidth)
    {
      if (price > 0)
      {
        var close = GetCanvasValue(canvasHeight, price, layout.MaxValue, layout.MinValue);

        var lineY = canvasHeight - close;

        var brush = GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.AVERAGE_BUY].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = DashStyles.Dash;

        var text = GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #region DrawPriceToATH

    public void DrawPriceToATH(DrawingContext drawingContext, Layout layout, decimal price, double canvasHeight, double canvasWidth)
    {
      if (price > 0)
      {
        var close = GetCanvasValue(canvasHeight, price, layout.MaxValue, layout.MinValue);

        var lineY = canvasHeight - close;

        var brush = GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.ATH].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = DashStyles.Dash;

        var text = GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #region GetToAthPrice

    private long sellId = 0;
    private decimal lastAthPrice = 0;

    private decimal GetToAthPrice(decimal ath)
    {
      var strategy = TradingBot.Strategy;
      var openBuys = strategy.OpenBuyPositions;
      var openSells = strategy.OpenSellPositions;

      var sells = openSells.OrderBy(x => x.Price).ToList();

      if (!openSells.Any())
      {
        return 0;
      }

      var newId = sells.FirstOrDefault()?.Id;
      if (sellId != newId && newId != null)
      {
        var allOpen = openBuys.Sum(x => x.PositionSize);
        var leftValue = allOpen + strategy.Budget;

        var total = leftValue;
        var totalNative = sells.Sum(x => x.PositionSizeNative);

        decimal price = 0;

        for (int i = 0; i < sells.Count; i++)
        {
          var sell = sells[i];

          total += sell.Price * sell.OriginalPositionSizeNative;
          totalNative -= sell.OriginalPositionSizeNative;

          var actualTotal = total + sell.Price * totalNative;
          var nextTotal = actualTotal;

          if (i + 1 < sells.Count)
          {
            var nextSell = sells[i + 1];

            var nextTotalNative = totalNative - nextSell.OriginalPositionSizeNative;

            nextTotal = total + nextSell.Price * nextTotalNative + nextSell.Price * nextSell.OriginalPositionSizeNative;
          }

          if (nextTotal > ath && totalNative > 0)
          {
            if (i == 0 && lastState?.ClosePrice != null)
            {
              totalNative = sells.Sum(x => x.PositionSizeNative);
              actualTotal = leftValue + lastState.ClosePrice.Value * totalNative;

              var ntn = lastState.ClosePrice.Value * totalNative;
              var y = (ath - actualTotal);

              lastAthPrice = (ntn + y) / totalNative;
            }
            else
            {
              var ntn = sell.Price * totalNative;
              var y = (ath - actualTotal);

              lastAthPrice = (ntn + y) / totalNative;
            }

            sellId = newId.Value;

            return lastAthPrice;
          }
        }
      }

      return lastAthPrice;
    }

    #endregion

    #region RenderIntersections

    public void RenderIntersections(
      DrawingContext drawingContext,
      Layout layout,
      IEnumerable<CtksIntersection> intersections,
      IList<Position> allPositions,
      IList<ChartCandle> candles,
      double desiredHeight,
      double canvasHeight,
      double canvasWidth,
      TimeFrame minTimeframe = TimeFrame.W1
      )
    {
      var maxCanvasValue = (decimal)GetValueFromCanvas(desiredHeight, desiredHeight, layout.MaxValue, layout.MinValue);
      var minCanvasValue = (decimal)GetValueFromCanvas(desiredHeight, -2 * (desiredHeight - canvasHeight), layout.MaxValue, layout.MinValue);

      maxCanvasValue = Math.Max(maxCanvasValue, candles.Max(x => x.Candle.High.Value));
      minCanvasValue = Math.Min(minCanvasValue, candles.Min(x => x.Candle.Low.Value));

      var validIntersection = intersections
        .Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue && minTimeframe <= x.TimeFrame)
        .ToList();

      foreach (var intersection in validIntersection)
      {
        Brush selectedBrush = GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);
        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;

        var actual = GetCanvasValue(canvasHeight, intersection.Value, layout.MaxValue, layout.MinValue);

        var frame = intersection.TimeFrame;

        pen.Thickness = GetPositionThickness(frame);

        var lineY = canvasHeight - actual;

        var positionsOnIntersesction = allPositions
          .Where(x => x.Intersection?.Value == intersection.Value)
          .ToList();

        var firstPositionsOnIntersesction = positionsOnIntersesction.FirstOrDefault();

        if (firstPositionsOnIntersesction != null)
        {
          selectedBrush = firstPositionsOnIntersesction.Side == PositionSide.Buy ? GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.BUY].Brush) : GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.SELL].Brush);
          pen.Brush = selectedBrush;
        }

        if (frame >= minTimeframe)
        {
          Brush positionBrush = GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);

          if (firstPositionsOnIntersesction != null)
          {
            positionBrush = firstPositionsOnIntersesction.Side == PositionSide.Buy ?
              GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.BUY].Brush) :
              GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.SELL].Brush);
          }
          else
          {
            positionBrush = GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);
          }

          FormattedText formattedText = GetFormattedText(intersection.Value.ToString(), positionBrush);

          drawingContext.DrawText(formattedText, new Point(0, lineY - formattedText.Height / 2));
        }

        drawingContext.DrawLine(pen, new Point(canvasWidth * 0.10, lineY), new Point(canvasWidth, lineY));
      }
    }

    #endregion

    #region RenderClosedPosiotions

    public void RenderClosedPosiotions(
      DrawingContext drawingContext,
      Layout layout,
      IEnumerable<Position> positions,
      IList<ChartCandle> candles,
      double canvasHeight,
      double canvasWidth,
      TimeFrame minTimeframe = TimeFrame.W1
      )
    {
      foreach (var position in positions)
      {
        var isActive = position.Side == PositionSide.Buy && position.State == PositionState.Filled;

        Brush selectedBrush = isActive ? GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.ACTIVE_BUY].Brush) :
            position.Side == PositionSide.Buy ?
              GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.FILLED_BUY].Brush) :
              GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.FILLED_SELL].Brush); ;


        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;

        var actual = GetCanvasValue(canvasHeight, position.Price, layout.MaxValue, layout.MinValue);

        var frame = position.Intersection.TimeFrame;

        pen.Thickness = GetPositionThickness(frame);

        var lineY = canvasHeight - actual;
        var candle = candles.FirstOrDefault(x => x.Candle.OpenTime < position.FilledDate && x.Candle.CloseTime > position.FilledDate);

        if (frame >= minTimeframe && candle != null)
        {
          var text = position.Side == PositionSide.Buy ? "B" : "S";
          FormattedText formattedText = GetFormattedText(text, selectedBrush, isActive ? 25 : 9);

          drawingContext.DrawText(formattedText, new Point(candle.Body.X - 25, lineY - formattedText.Height / 2));
        }
      }
    }

    #endregion

    private SolidColorBrush GetBrushFromHex(string hex)
    {
      return (SolidColorBrush)new BrushConverter().ConvertFrom(hex);
    }

    #region GetPositionThickness

    private double GetPositionThickness(TimeFrame timeFrame)
    {
      switch (timeFrame)
      {
        case TimeFrame.M12:
          return 8;
        case TimeFrame.M6:
          return 6;
        case TimeFrame.M3:
          return 4;
        case TimeFrame.M1:
          return 2;
        case TimeFrame.W2:
          return 1;
        default:
          return 0.5;
      }
    }

    #endregion

    #region OnBinanceKlineUpdate

    private List<State> lastStates = new List<State>();
    private State lastState = null;
    private void OnBinanceKlineUpdate(IBinanceStreamKline binanceStreamKline)
    {
      lock (this)
      {
        var actual = new Candle()
        {
          Close = binanceStreamKline.ClosePrice,
          High = binanceStreamKline.HighPrice,
          Low = binanceStreamKline.LowPrice,
          Open = binanceStreamKline.OpenPrice,
          CloseTime = binanceStreamKline.CloseTime,
          OpenTime = binanceStreamKline.OpenTime,
        };

        var lastCandle = ActualCandles.Last();

        if (lastCandle.OpenTime != actual.OpenTime && lastCandle.OpenTime < actual.OpenTime && lastCandle.CloseTime < actual.CloseTime)
        {
          ActualCandles.Add(actual);
        }
        else
        {
          ActualCandles.RemoveAt(ActualCandles.Count - 1);
          ActualCandles.Add(actual);
        }

        if (IsLive && actual.Close != null)
        {
          foreach (var position in TradingBot.Strategy.ActualPositions)
          {
            var filledSells = position.OpositPositions.Where(x => x.State == PositionState.Filled).ToList();

            var realizedProfit = filledSells.Sum(x => x.OriginalPositionSize + x.Profit);
            var leftSize = position.OpositPositions.Where(x => x.State == PositionState.Open).Sum(x => x.PositionSizeNative);
            var fees = position.Fees ?? 0 + filledSells.Sum(x => x.Fees ?? 0);

            var profit = (realizedProfit + (leftSize * actual.Close.Value)) - position.OriginalPositionSize - fees;
            position.ActualProfit = profit;
          }

          TradingBot.Strategy.TotalActualProfit = TradingBot.Strategy.ActualPositions.Sum(x => x.ActualProfit);
        }

        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          if (SelectedLayout.Canvas == null)
            RenderLayout(MainLayout, InnerLayouts, actual, ActualCandles);
        });

        if (lastState == null)
        {
          if (File.Exists("state_data.txt"))
          {
            lastStates.Clear();
            var lines = File.ReadLines(@"state_data.txt");

            foreach (var line in lines)
            {
              lastStates.Add(JsonSerializer.Deserialize<State>(line));
            }
          }

          lastState = lastStates.LastOrDefault();
        }



        if ((actual.OpenTime.Date > lastState?.Date || lastState?.Date == null) && TradingBot.Strategy.TotalValue > 0)
        {
          lastState = new State()
          {
            Date = actual.OpenTime.Date,
            TotalProfit = TradingBot.Strategy.TotalProfit,
            TotalValue = TradingBot.Strategy.TotalValue,
            TotalNative = TradingBot.Strategy.TotalNativeAsset,
            TotalNativeValue = TradingBot.Strategy.TotalNativeAssetValue,
            AthPrice = GetToAthPrice(lastStates.Max(x => x.TotalValue)),
            ClosePrice = actual.Close
          };

          lastStates.Add(lastState);

          using (StreamWriter w = File.AppendText("state_data.txt"))
          {
            w.WriteLine(JsonSerializer.Serialize(lastState));
          }
        }

        if (lastState != null)
        {
          DailyChange = TradingBot.Strategy.TotalValue - lastState.TotalValue;
          FromAllTimeHigh = TradingBot.Strategy.TotalValue - lastStates.Max(x => x.TotalValue);
        }

      }
    }

    #endregion

    #region GetTimeSpanFromInterval

    private TimeSpan GetTimeSpanFromInterval(KlineInterval klineInterval)
    {
      switch (klineInterval)
      {
        case KlineInterval.OneSecond:
          return TimeSpan.FromSeconds(1);
        case KlineInterval.OneMinute:
          return TimeSpan.FromMinutes(1);
        case KlineInterval.ThreeMinutes:
          return TimeSpan.FromMinutes(3);
        case KlineInterval.FiveMinutes:
          return TimeSpan.FromMinutes(5);
        case KlineInterval.FifteenMinutes:
          return TimeSpan.FromMinutes(15);
        case KlineInterval.ThirtyMinutes:
          return TimeSpan.FromMinutes(30);
        case KlineInterval.OneHour:
          return TimeSpan.FromHours(1);
        case KlineInterval.TwoHour:
          return TimeSpan.FromHours(2);
        case KlineInterval.FourHour:
          return TimeSpan.FromHours(4);
        case KlineInterval.SixHour:
          return TimeSpan.FromHours(6);
        case KlineInterval.EightHour:
          return TimeSpan.FromHours(8);
        case KlineInterval.TwelveHour:
          return TimeSpan.FromHours(12);
        case KlineInterval.OneDay:
          return TimeSpan.FromDays(1);
        case KlineInterval.ThreeDay:
          return TimeSpan.FromDays(3);
        case KlineInterval.OneWeek:
          return TimeSpan.FromDays(7);
        case KlineInterval.OneMonth:
          return TimeSpan.FromDays(30);
        default:
          throw new ArgumentOutOfRangeException(nameof(klineInterval), klineInterval, null);
      }
    }

    #endregion

    #region LoadLayoutSettings

    private void LoadLayoutSettings()
    {
      if (File.Exists(layoutPath) && IsLive)
      {
        var data = File.ReadAllText(layoutPath);
        var settings = JsonSerializer.Deserialize<LayoutSettings>(data);

        if (settings != null)
        {
          showClosedPositions = settings.ShowClosedPositions;
          var savedLayout = LayoutIntervals.ViewModels.SingleOrDefault(x => x.Model.Interval == settings.LayoutInterval);

          if (savedLayout != null)
            savedLayout.IsSelected = true;

          if (settings.ColorSettings != null)
          {
            var lsit = ColorScheme.ColorSettings.ToList();
            foreach (var setting in lsit)
            {
              var found = settings.ColorSettings.FirstOrDefault(x => x.Purpose == setting.Key);

              if (found != null)
                ColorScheme.ColorSettings[setting.Key] = new ColorSettingViewModel(found);
            }
          }

          ShowAveragePrice = settings.ShowAveragePrice;
          ShowATH = settings.ShowATH;

          if (settings.CandleCount > 0)
            CandleCount = settings.CandleCount;
        }
      }
    }

    #endregion

    #region SaveLayoutSettings

    public void SaveLayoutSettings()
    {
      if (IsLive)
      {
        var settings = new LayoutSettings()
        {
          ShowClosedPositions = ShowClosedPositions,
          LayoutInterval = LayoutIntervals.SelectedItem.Model.Interval,
          ColorSettings = ColorScheme.ColorSettings.Select(x => x.Value.Model),
          ShowAveragePrice = ShowAveragePrice,
          ShowATH = ShowATH,
          CandleCount = CandleCount
        };

        var options = new JsonSerializerOptions()
        {
          WriteIndented = true
        };

        File.WriteAllText(layoutPath, JsonSerializer.Serialize(settings, options));
      }

    }

    #endregion

    #region GetFormattedText

    private FormattedText GetFormattedText(string text, Brush brush, int fontSize = 12)
    {
      return new FormattedText(text, CultureInfo.GetCultureInfo("en-us"),
        FlowDirection.LeftToRight,
        new Typeface(new FontFamily("Arial").ToString()),
        fontSize, brush);
    }

    #endregion

    #endregion
  }
}

