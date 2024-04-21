using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CTKS_Chart.Binance;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using CTKS_Chart.Views;
using CTKS_Chart.Views.Prompts;
using Logger;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Logger;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.Prompt;
using Path = System.IO.Path;

namespace CTKS_Chart.ViewModels
{
  public class TradingBotViewModel : ViewModel
  {
    private readonly IWindowManager windowManager;
    private readonly BinanceBroker binanceBroker;
    private readonly IViewModelsFactory viewModelsFactory;
    private Stopwatch stopwatch = new Stopwatch();
    private TimeSpan lastElapsed;
    private string layoutPath = "layout.json";
    public static string stateDataPath = Path.Combine(Settings.DataPath, "state_data.txt");

    public TradingBotViewModel(
      TradingBot tradingBot,
      ILogger logger,
      IWindowManager windowManager,
      BinanceBroker binanceBroker,
      IViewModelsFactory viewModelsFactory)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.binanceBroker = binanceBroker ?? throw new ArgumentNullException(nameof(binanceBroker));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      TradingBot = tradingBot;
    }

    #region Properties

    //TODO: Replace by TradingBotView
    public MainWindow MainWindow { get; set; }
    public TradingBot TradingBot { get; }


    #region  ConsoleCollectionLogger

    public CollectionLogger ConsoleCollectionLogger
    {
      get { return (CollectionLogger)logger.LoggerContainer; }

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


    public ObservableCollection<Layout> Layouts { get; set; } = new ObservableCollection<Layout>();

    #region DrawingViewModel

    private DrawingViewModel drawingViewModel;

    public DrawingViewModel DrawingViewModel
    {
      get { return drawingViewModel; }
      set
      {
        if (value != drawingViewModel)
        {
          drawingViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SelectedLayout

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



    public Layout MainLayout { get; } = new Layout() { Title = "Main" };

    #region TotalRunTime

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

    #region MaxBuyPrice

    public decimal? MaxBuyPrice
    {
      get { return TradingBot.Strategy.MaxBuyPrice; }
      set
      {
        if (value != TradingBot.Strategy.MaxBuyPrice)
        {
          TradingBot.Strategy.MaxBuyPrice = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    public ItemsViewModel<LayoutIntervalViewModel> LayoutIntervals { get; } = new ItemsViewModel<LayoutIntervalViewModel>();

    #endregion

    #region DrawChart

    private bool drawChart = true;

    public bool DrawChart
    {
      get { return drawChart; }
      set
      {
        if (value != drawChart)
        {
          drawChart = value;
          RaisePropertyChanged();
        }
      }
    }

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
      var index = InnerLayouts.IndexOf(layout);
      var globalIndex = Layouts.IndexOf(layout);

      if (index >= 0)
      {
        layout = CreateCtksChart(
          layout.DataLocation,
          layout.TimeFrame,
          DrawingViewModel.ChartImage.ActualWidth,
          DrawingViewModel.ChartImage.ActualHeight,
          null,
          maxValue,
          minValue);

        InnerLayouts.RemoveAt(index);
        InnerLayouts.Insert(index, layout);

        Layouts.RemoveAt(globalIndex);
        Layouts.Insert(globalIndex, layout);

        layout.Canvas.Height = DrawingViewModel.ChartImage.ActualHeight;
        layout.Canvas.Width = DrawingViewModel.ChartImage.ActualWidth;
      }

      SelectedLayout = layout;
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

    #region OpenArchitectView

    protected ActionCommand openArchitectView;

    public ICommand OpenArchitectView
    {
      get
      {
        return openArchitectView ??= new ActionCommand(OnOpenArchitectView);
      }
    }

    protected virtual void OnOpenArchitectView()
    {
      var arch = new ArchitectViewModel(Layouts, DrawingViewModel.ColorScheme, viewModelsFactory, TradingBot.Asset);

      windowManager.ShowPrompt<ArchitectView>(arch, 1000, 1000);
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

    #region ClearMaxBuyPrice

    protected ActionCommand clearMaxBuyPrice;

    public ICommand ClearMaxBuyPrice
    {
      get
      {
        return clearMaxBuyPrice ??= new ActionCommand(OnClearMaxBuyPrice);
      }
    }

    protected void OnClearMaxBuyPrice()
    {
      var positionResult = windowManager.ShowQuestionPrompt("Do you really want to clear MAX BUY PRICE?", "Clear MAX BUY PRICE");

      if (positionResult == PromptResult.Ok)
      {
        TradingBot.Strategy.MaxBuyPrice = null;
      }
    }

    #endregion

    #region clearMinSellPrice

    protected ActionCommand clearMinSellPrice;

    public ICommand ClearMinSellPrice
    {
      get
      {
        return clearMinSellPrice ??= new ActionCommand(OnClearMinSellPrice);
      }
    }

    protected void OnClearMinSellPrice()
    {
      var positionResult = windowManager.ShowQuestionPrompt("Do you really want to clear MIN SELL PRICE?", "Clear MIN SELL PRICE");

      if (positionResult == PromptResult.Ok)
      {
        TradingBot.Strategy.MinSellPrice = null;
      }
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      TradingBot.LoadTimeFrames();
      DrawingViewModel = viewModelsFactory.Create<DrawingViewModel>(TradingBot, MainLayout);

      MainLayout.MaxValue = TradingBot.StartingMaxPrice;
      MainLayout.MinValue = TradingBot.StartingMinPrice;

      DrawingViewModel.MaxValue = MainLayout.MaxValue;
      DrawingViewModel.MinValue = MainLayout.MinValue;

      foreach (KlineInterval interval in EnumHelper.GetAllValues(KlineInterval.GetType()))
      {
        var length = TradingHelper.GetTimeSpanFromInterval(interval);
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
      KlineInterval = LayoutIntervals.SelectedItem.Model.Interval;

      LayoutIntervals.OnActualItemChanged.Subscribe(x =>
      {
        if (x != null)
        {
         
          KlineInterval = x.Model.Interval;
        }
      });
    }

    #endregion

    #region Start

    public void Start()
    {
      LoadLayoutSettings();

      stopwatch.Start();

      if (!IsSimulation)
      {
        Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnDispatcher().Subscribe((x) =>
        {
          var diff = stopwatch.Elapsed - lastElapsed;
          ActiveTime += diff;
          TotalRunTime += diff;

          lastElapsed = stopwatch.Elapsed;

          if (Math.Round(activeTime.TotalSeconds, 0) % 10 == 0)
          {
            TradingBot.Asset.RunTimeTicks = TotalRunTime.Ticks;
            var options = new JsonSerializerOptions()
            {
              WriteIndented = true
            };

            var json = JsonSerializer.Serialize<Asset>(TradingBot.Asset, options);
            File.WriteAllText(Path.Combine(Settings.DataPath, "asset.json"), json);
          }
        });
      };

      ForexChart_Loaded();
    }

    #endregion

    #region ForexChart_Loaded

    private async void ForexChart_Loaded()
    {
      TradingBot.Strategy.Logger = logger;

      await LoadLayouts(MainLayout);
    }

    #endregion

    #region LoadLAyouts

    TimeFrame minTimeframe = TimeFrame.D1;
    protected List<Layout> InnerLayouts = new List<Layout>();

    protected virtual async Task LoadLayouts(Layout mainLayout)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, DrawingViewModel.CanvasHeight, DrawingViewModel.CanvasWidth, TradingBot.Asset);

      DrawingViewModel.ActualCandles = (await binanceBroker.GetCandles(TradingBot.Asset.Symbol, TradingHelper.GetTimeSpanFromInterval(KlineInterval))).ToList();

      await binanceBroker.SubscribeToKlineInterval(TradingBot.Asset.Symbol, OnBinanceKlineUpdate, KlineInterval);

      LoadSecondaryLayouts(mainLayout, mainCtks);

      if (DrawingViewModel.ActualCandles.Count > 0)
      {
        RenderLayout(InnerLayouts, DrawingViewModel.ActualCandles.Last());
      }
    }

    #endregion

    #region LoadSecondaryLayouts

    protected void LoadSecondaryLayouts(Layout mainLayout, Ctks mainCtks, DateTime? maxTime = null)
    {
      foreach (var layoutData in TradingBot.TimeFrames.Where(x => x.Value >= minTimeframe))
      {
        var layout = CreateCtksChart(layoutData.Key, layoutData.Value, DrawingViewModel.CanvasWidth, DrawingViewModel.CanvasHeight, maxTime);

        Layouts.Add(layout);
        InnerLayouts.Add(layout);
      }

      mainLayout.Ctks = mainCtks;
      Layouts.Add(mainLayout);
      SelectedLayout = mainLayout;

    }

    #endregion

    #region CheckLayout

    private void CheckLayout(Layout layout, List<Candle> innerCandles)
    {
      if (DateTime.Now > innerCandles.Last().CloseTime)
      {
        layout.IsOutDated = true;
        lastFileCheck = DateTime.Now;
      }
      else
      {
        layout.IsOutDated = false;
      }
    }

    #endregion

    #region RenderLayout

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    private bool shouldUpdate = true;
    private bool wasLoaded = false;
    List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();
    DateTime lastFileCheck = DateTime.Now;
    private decimal? lastAthPriceUpdated = null;
    private Candle actual = null;

    public bool IsSimulation { get; set; } = false;



    #region IsPaused

    private bool isPaused;

    public bool IsPaused
    {
      get { return isPaused; }
      set
      {
        if (value != isPaused)
        {
          isPaused = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    private Dictionary<TimeFrame, bool> stopParsingForNewData = new Dictionary<TimeFrame, bool>()
    {
      { TimeFrame.M12,false },
      { TimeFrame.M6,false },
      { TimeFrame.M3,false },
      { TimeFrame.M1,false },
      { TimeFrame.W2,false },
      { TimeFrame.W1,false }
    };

    List<Layout> preloadedLayots = new List<Layout>();
    protected void PreLoadCTks(DateTime startTime)
    {
      preloadedLayots = new List<Layout>();

      foreach (var layoutData in TradingBot.TimeFrames.Where(x => x.Value >= minTimeframe))
      {
        var candles = TradingHelper.ParseTradingView(layoutData.Key).Where(x => x.CloseTime > startTime);

        foreach (var candle in candles)
        {
          var layout = CreateCtksChart(layoutData.Key, layoutData.Value, DrawingViewModel.CanvasWidth, DrawingViewModel.CanvasHeight, candle.OpenTime);

          preloadedLayots.Add(layout);
        }
      }
    }

    public async void RenderLayout(List<Layout> secondaryLayouts, Candle actual)
    {
      if (IsPaused)
      {
        return;
      }

      try
      {
        await semaphoreSlim.WaitAsync();


        for (int i = 0; i < secondaryLayouts.Count; i++)
        {
          var secondaryLayout = secondaryLayouts[i];

          var lastCandle = secondaryLayout.Ctks.Candles.Last();

          if (IsSimulation && stopParsingForNewData[secondaryLayout.TimeFrame] || secondaryLayout == null)
          {
            continue;
          }

          if (actual.CloseTime > TradingHelper.GetNextTime(lastCandle.CloseTime, secondaryLayout.TimeFrame))
          {
            var fileCheck = true;

            if (!IsSimulation)
              fileCheck = lastFileCheck < DateTime.Now.AddMinutes(1);

            if (!secondaryLayout.IsOutDated || (secondaryLayout.IsOutDated && fileCheck))
            {
              var lastCount = secondaryLayout.Ctks.Candles.Count;

              if (IsSimulation)
              {
                var newLayout = preloadedLayots.SingleOrDefault(x => x.Ctks.Candles.Count == lastCount + 1 && x.TimeFrame == secondaryLayout.TimeFrame);

                if (newLayout != null)
                {
                  var secIndex = secondaryLayouts.IndexOf(secondaryLayout);

                  secondaryLayouts[secIndex] = newLayout;
                  secondaryLayout = newLayout;
                }
              }
              else
              {
                var innerCandles = TradingHelper.ParseTradingView(secondaryLayout.DataLocation, addNotClosedCandle: true, indexCut: lastCount + 1);

                VSynchronizationContext.InvokeOnDispatcher(() => secondaryLayout.Ctks.CrateCtks(innerCandles, () => CreateChart(secondaryLayout, DrawingViewModel.CanvasHeight, DrawingViewModel.CanvasWidth, innerCandles)));

                CheckLayout(secondaryLayout, innerCandles);
              }


              if (secondaryLayout.Ctks.Candles.Count > lastCount)
                shouldUpdate = true;
            }
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

          ctksIntersections = ctksIntersections
            .Where(x => Math.Round(x.Value, TradingBot.Asset.PriceRound) > 0)
            .OrderByDescending(x => x.Value).ToList();
          shouldUpdate = false;


          if (ctksIntersections.Count > 0)
            TradingBot.Strategy.UpdateIntersections(ctksIntersections);
        }

        if (ctksIntersections.Count == 0)
        {
          return;
        }

        if (ctksIntersections.Count(x => x.Value > actual.Close) == 0)
        {
          return;
        }

        TradingBot.Strategy.Intersections = ctksIntersections;
        var athPrice = GetAthPrice();

        if (DrawChart)
          VSynchronizationContext.InvokeOnDispatcher(() => DrawingViewModel.RenderOverlay(ctksIntersections, athPrice, DrawingViewModel.CanvasHeight));

        this.actual = actual;

        if (ctksIntersections.Count > 0)
        {
          if (!wasLoaded)
          {
            wasLoaded = true;

            TradingBot.Strategy.LoadState();
            await TradingBot.Strategy.RefreshState();
            VSynchronizationContext.InvokeOnDispatcher(() => MainWindow?.SortActualPositions());

            if (ctksIntersections.Count > 0)
              TradingBot.Strategy.UpdateIntersections(ctksIntersections);
          }


          if (lastAthPriceUpdated != athPrice && TradingBot.Strategy.AutoATHPriceAsMaxBuy)
          {
            lastAthPriceUpdated = athPrice;

            if (athPrice > 0 && TradingBot.Strategy.OpenSellPositions.Count > 1)
            {
              TradingBot.Strategy.MaxBuyPrice = athPrice;
            }
            else
            {
              TradingBot.Strategy.MaxBuyPrice = decimal.MaxValue;
            }
          }

          TradingBot.Strategy.ValidatePositions(actual);
          TradingBot.Strategy.CreatePositions(actual);
        }
        else
        {
          Console.WriteLine("NO INTERSECTIONS, DOING NOTHING !");
        }

        if (DrawChart)
          VSynchronizationContext.InvokeOnDispatcher(() => DrawingViewModel.RenderOverlay(ctksIntersections, athPrice, DrawingViewModel.CanvasHeight));
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region CreateChart

    private void CreateChart(Layout layout, double canvasHeight, double canvasWidth, IList<Candle> candles, int? pmaxCount = null)
    {
      layout.Canvas.Children.Clear();
      var maxCount = (pmaxCount ?? candles.Count) + 1;

      var skip = candles.Count - maxCount > 0 ? candles.Count - maxCount : 0;

      if (candles.Any())
      {
        int y = 0;
        for (int i = skip; i < candles.Count; i++)
        {
          var point = candles[i];
          var valueForCanvas = TradingHelper.GetCanvasValue(canvasHeight, point.Close.Value, layout.MaxValue, layout.MinValue);
          var width = canvasWidth / maxCount;

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var lastCandle = new Rectangle()
          {
            Width = width,
            Height = 25,
            Fill = green ? DrawingHelper.GetBrushFromHex(DrawingViewModel.ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : DrawingHelper.GetBrushFromHex(DrawingViewModel.ColorScheme.ColorSettings[ColorPurpose.RED].Brush),
          };

          Panel.SetZIndex(lastCandle, 99);

          var open = i > 0 ?
            TradingHelper.GetCanvasValue(canvasHeight, candles[i - 1].Close.Value, layout.MaxValue, layout.MinValue) :
            TradingHelper.GetCanvasValue(canvasHeight, candles[i].Open.Value, layout.MaxValue, layout.MinValue);

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

    #region CreateCtksChart

    protected Layout CreateCtksChart(string location, TimeFrame timeFrame,
      double canvasWidth,
      double canvasHeight,
      DateTime? maxTime = null,
      decimal? pmax = null,
      decimal? pmin = null)
    {
      var candles = TradingHelper.ParseTradingView(location, maxTime);

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

    #region OnBinanceKlineUpdate

    private List<State> lastStates = new List<State>();
    private State lastState = null;
    private SemaphoreSlim binanceKlineUpdateLock = new SemaphoreSlim(1, 1);

    private async void OnBinanceKlineUpdate(IBinanceStreamKline binanceStreamKline)
    {
      try
      {
        await binanceKlineUpdateLock.WaitAsync();

        var actual = new Candle()
        {
          Close = binanceStreamKline.ClosePrice,
          High = binanceStreamKline.HighPrice,
          Low = binanceStreamKline.LowPrice,
          Open = binanceStreamKline.OpenPrice,
          CloseTime = binanceStreamKline.CloseTime,
          OpenTime = binanceStreamKline.OpenTime,
        };

        actual.UnixTime = ((DateTimeOffset)actual.OpenTime).ToUnixTimeSeconds();

        var lastCandle = DrawingViewModel.ActualCandles.Last();

        if (lastCandle.OpenTime != actual.OpenTime && lastCandle.OpenTime < actual.OpenTime && lastCandle.CloseTime < actual.CloseTime)
        {
          DrawingViewModel.ActualCandles.Add(actual);
        }
        else
        {
          DrawingViewModel.ActualCandles.RemoveAt(DrawingViewModel.ActualCandles.Count - 1);
          DrawingViewModel.ActualCandles.Add(actual);
        }

        if (actual.Close != null)
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
          if (SelectedLayout != null && SelectedLayout.Canvas == null)
            RenderLayout(InnerLayouts, actual);
        });

        if (lastState == null)
        {
          if (File.Exists(stateDataPath))
          {
            lastStates.Clear();
            var lines = File.ReadLines(stateDataPath);

            foreach (var line in lines)
            {
              lastStates.Add(JsonSerializer.Deserialize<State>(line));
            }
          }

          lastState = lastStates.LastOrDefault();
        }



        if ((actual.OpenTime.Date > lastState?.Date || lastState?.Date == null) && TradingBot.Strategy.TotalValue > 0)
        {
          var totalManualProfit = TradingBot.Strategy.ClosedBuyPositions.Where(x => !x.IsAutomatic).Sum(x => x.TotalProfit);

          var totalAutoProfit = TradingBot.Strategy.ClosedBuyPositions
            .Where(x => x.IsAutomatic)
            .Sum(x => x.TotalProfit);

          var actualAutoValue = totalAutoProfit + TradingBot.Strategy.ActualPositions
            .Where(x => x.IsAutomatic)
            .Sum(x => x.ActualProfit);

          var actualValue =
            totalManualProfit +
            TradingBot.Strategy
            .ActualPositions.Where(x => !x.IsAutomatic)
            .Sum(x => x.ActualProfit);

          var athPrice = GetToAthPrice(lastStates.Max(x => x.TotalValue) ?? 0);
          lastState = new State()
          {
            Date = actual.OpenTime.Date,
            TotalProfit = TradingBot.Strategy.TotalProfit,
            TotalValue = TradingBot.Strategy.TotalValue,
            TotalNative = TradingBot.Strategy.TotalNativeAsset,
            TotalNativeValue = TradingBot.Strategy.TotalNativeAssetValue,
            AthPrice = athPrice != 0 ? athPrice : lastStates.Last(x => x.AthPrice > 0).AthPrice,
            ClosePrice = actual.Close ?? 0,
            ActualAutoValue = actualAutoValue,
            ActualValue = actualValue,
            TotalAutoProfit = totalAutoProfit,
            TotalManualProfit = totalManualProfit,
          };

          lastState.ValueToNative = Math.Round(lastState.TotalValue.Value / lastState.ClosePrice.Value, TradingBot.Asset.NativeRound);

          if (TradingBot.Asset.Symbol == "BTCUSDT")
          {
            lastState.ValueToBTC = lastState.ValueToNative;
          }
          else
          {
            var btcPrice = await binanceBroker.GetTicker("BTCUSDT");

            if (btcPrice != null)
            {
              lastState.ValueToBTC = Math.Round(lastState.TotalValue.Value / btcPrice.Value, 5);
            }
          }


          lastStates.Add(lastState);

          using (StreamWriter w = File.AppendText(stateDataPath))
          {
            w.WriteLine(JsonSerializer.Serialize(lastState));
          }
        }

        if (lastState != null)
        {
          DailyChange = TradingBot.Strategy.TotalValue - lastState.TotalValue ?? 0;
          FromAllTimeHigh = TradingBot.Strategy.TotalValue - lastStates.Max(x => x.TotalValue ?? 0);
        }
      }
      finally
      {
        binanceKlineUpdateLock.Release();
      }
    }

    #endregion

    #region RecreateChart

    private async void RecreateChart(bool fetchNewCandles = false)
    {
      if (TradingBot != null)
      {
        if (fetchNewCandles && !IsSimulation)
        {
          DrawingViewModel.ActualCandles = 
            (await binanceBroker.GetCandles(TradingBot.Asset.Symbol, TradingHelper.GetTimeSpanFromInterval(KlineInterval)))
            .ToList();
          await binanceBroker.SubscribeToKlineInterval(TradingBot.Asset.Symbol, OnBinanceKlineUpdate, KlineInterval);

          if (DrawingViewModel.ActualCandles.Count > 0)
          {
            DrawingViewModel.unixDiff = DrawingViewModel.ActualCandles[1].UnixTime - DrawingViewModel.ActualCandles[0].UnixTime;

            DrawingViewModel.minUnix = DrawingViewModel.ActualCandles.First().UnixTime ;
            DrawingViewModel.maxUnix = DrawingViewModel.ActualCandles.Last().UnixTime ;

            DrawingViewModel.Raise(nameof(DrawingViewModel.MinUnix));
            DrawingViewModel.Raise(nameof(DrawingViewModel.MaxUnix));
          }
        }

        if (DrawingViewModel.ActualCandles.Count > 0)
        {
          DrawingViewModel.RenderOverlay(ctksIntersections, GetAthPrice(), DrawingViewModel.CanvasHeight);
        }
      }
    }

    #endregion

    #region LoadLayoutSettings

    public void LoadLayoutSettings()
    {
      if (File.Exists(layoutPath))
      {
        var data = File.ReadAllText(layoutPath);
        var settings = JsonSerializer.Deserialize<LayoutSettings>(data);

        if (settings != null)
        {
          DrawingViewModel.ShowClosedPositions = settings.ShowClosedPositions;
          var savedLayout = LayoutIntervals.ViewModels.SingleOrDefault(x => x.Model.Interval == settings.LayoutInterval);

          if (savedLayout != null)
            savedLayout.IsSelected = true;

          if (settings.ColorSettings != null)
          {
            var lsit = DrawingViewModel.ColorScheme.ColorSettings.ToList();
            foreach (var setting in lsit)
            {
              var found = settings.ColorSettings.FirstOrDefault(x => x.Purpose == setting.Key);

              if (found != null)
                DrawingViewModel.ColorScheme.ColorSettings[setting.Key] = new ColorSettingViewModel(found);
            }
          }

          DrawingViewModel.ShowAveragePrice = settings.ShowAveragePrice;
          DrawingViewModel.ShowATH = settings.ShowATH;

          if (settings.CandleCount > 0)
            DrawingViewModel.CandleCount = settings.CandleCount;
        }
      }
    }

    #endregion

    #region SaveLayoutSettings

    public void SaveLayoutSettings()
    {
      if (!IsSimulation)
      {
        var settings = new LayoutSettings()
        {
          ShowClosedPositions = DrawingViewModel.ShowClosedPositions,
          LayoutInterval = LayoutIntervals.SelectedItem.Model.Interval,
          ColorSettings = DrawingViewModel.ColorScheme.ColorSettings.Select(x => x.Value.Model),
          ShowAveragePrice = DrawingViewModel.ShowAveragePrice,
          ShowATH = DrawingViewModel.ShowATH,
          CandleCount = DrawingViewModel.CandleCount
        };

        var options = new JsonSerializerOptions()
        {
          WriteIndented = true
        };

        File.WriteAllText(layoutPath, JsonSerializer.Serialize(settings, options));
      }
    }

    #endregion

    #region GetToAthPrice

    private long sellId = 0;
    private decimal lastAthPrice = 0;

    public decimal GetToAthPrice(decimal ath)
    {
      var strategy = TradingBot.Strategy;
      var openBuys = strategy.OpenBuyPositions;
      var openSells = strategy.OpenSellPositions;

      var sells = openSells.OrderByDescending(x => x.CreatedDate).ToList();

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

    #region GetAthPrice

    private bool wasLoadedAvg = false;
    public decimal GetAthPrice()
    {
      decimal price = 0;

      if (lastStates.Count == 0)
        price = GetToAthPrice(TradingBot.Strategy.MaxTotalValue);
      else if (lastStates.Count > 0)
      {
        if (!wasLoadedAvg)
        {
          sellId = 0;
          wasLoadedAvg = true;
        }

        var ath = lastStates.Max(x => x.TotalValue ?? 0);

        price = GetToAthPrice(ath);
      }

      return price;
    }

    #endregion

    #endregion
  }
}
