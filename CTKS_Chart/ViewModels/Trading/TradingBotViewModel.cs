﻿using System;
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
using CTKS_Chart.Strategy.AIStrategy;
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
  public interface ITradingBotViewModel
  {
    public void Start();
    public void Stop();
    public bool IsPaused { get; set; }
    public MainWindow MainWindow { get; set; }

    public void SaveAsset();
    public void SaveLayoutSettings();

    public Asset Asset { get; }
  }

  public class TradingBotViewModel<TPosition, TStrategy> : ViewModel, ITradingBotViewModel
    where TPosition : Position, new()
    where TStrategy : BaseStrategy<TPosition>
  {
    protected readonly IWindowManager windowManager;
    private readonly BinanceBroker binanceBroker;
    protected readonly IViewModelsFactory viewModelsFactory;
    private Stopwatch stopwatch = new Stopwatch();
    private TimeSpan lastElapsed;
    private string layoutPath = Path.Combine(Settings.DataPath, "layout.json");
    public static string stateDataPath = Path.Combine(Settings.DataPath, "state_data.txt");

    public TradingBotViewModel(
      TradingBot<TPosition, TStrategy> tradingBot,
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
    public TradingBot<TPosition, TStrategy> TradingBot { get; }

    public Asset Asset
    {
      get
      {
        return TradingBot.Asset;
      }
    }

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

    public ObservableCollection<CtksLayout> Layouts { get; set; } = new ObservableCollection<CtksLayout>();
    public ObservableCollection<Layout> IndicatorLayouts { get; set; } = new ObservableCollection<Layout>();

    #region DrawingViewModel

    private StrategyDrawingViewModel<TPosition, TStrategy> drawingViewModel;

    public StrategyDrawingViewModel<TPosition, TStrategy> DrawingViewModel
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

    private CtksLayout selectedLayout;

    public CtksLayout SelectedLayout
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

    public CtksLayout MainLayout { get; } = new CtksLayout() { Title = "Main" };

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

          if (IsSimulation && drawChart && DrawingViewModel.ActualCandles.Any())
          {
            DrawingViewModel.OnRestChart();
            DrawingViewModel.EnableAutoLock = true;

          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Commands

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
      var arch = new ArchitectViewModel(Layouts, DrawingViewModel.ColorScheme, viewModelsFactory, new TradingBot<Position, SimulationStrategy>(
        new Asset()
        {
          NativeRound = TradingBot.Asset.NativeRound,
          PriceRound = TradingBot.Asset.PriceRound
        }, new SimulationStrategy()), new Layout());

      windowManager.ShowPrompt<ArchitectView>(new ArchitectPromptViewModel(arch), 1000, 1000);
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
        await TradingBot.Strategy.Reset(actualCachedCandle);
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
      if (TradingBot.Strategy is BinanceSpotStrategy binanceStrategy)
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
        await binanceBroker.Cancel(TradingBot.Asset.Symbol, long.Parse(opend.Id));
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
      windowManager.ShowPrompt<Statistics>(new StatisticsViewModel<TPosition>(TradingBot.Strategy));
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
      var vm = new PositionSizeViewModel<TPosition>(TradingBot.Strategy, actualCachedCandle, windowManager);
      var positionResult = windowManager.ShowQuestionPrompt<PositionSizeView, PositionSizeViewModel<TPosition>>(vm);

      if (positionResult == PromptResult.Ok)
      {
        var result = windowManager.ShowQuestionPrompt("Do you really want to apply these changes?", "Position size mapping");

        if (result == PromptResult.Ok)
        {
          TradingBot.Strategy.PositionSizeMapping = vm.PositionSizeMapping;

          var list = TradingBot.Strategy.OpenBuyPositions.ToList();
          foreach (var buy in list)
          {
            await TradingBot.Strategy.CancelPosition(buy);
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

      DrawingViewModel = viewModelsFactory.Create<StrategyDrawingViewModel<TPosition, TStrategy>>(TradingBot, MainLayout);
      DrawingViewModel.Symbol = Asset.Symbol;

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


      LoadLayoutSettings();
      TradingBot.LoadTimeFrames();
      TradingBot.LoadIndicators();

      if (layoutSettings != null)
      {
        MainLayout.MaxValue = layoutSettings.StartMaxPrice;
        MainLayout.MinValue = layoutSettings.StartLowPrice;
        MainLayout.MaxUnix = layoutSettings.StartMaxUnix;
        MainLayout.MinUnix = layoutSettings.StartMinUnix;

        DrawingViewModel.SetMaxValue(MainLayout.MaxValue);
        DrawingViewModel.SetMinValue(MainLayout.MinValue);
        DrawingViewModel.SetMaxUnix(MainLayout.MaxUnix);
        DrawingViewModel.SetMinUnix(MainLayout.MinUnix);
      }
      else
      {
        LayoutIntervals.ViewModels[6].IsSelected = true;

        KlineInterval = LayoutIntervals.SelectedItem.Model.Interval;
      }

      DrawingViewModel?.SetLock(true);



      RaisePropertyChanged(nameof(ConsoleCollectionLogger));
    }

    #endregion

    #region Start

    public virtual async void Start()
    {
      TradingBot.Strategy.SubscribeToChanges();

      stopwatch.Start();

      await ForexChart_Loaded();

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
            SaveAsset();
          }
        });

        LayoutIntervals.OnActualItemChanged.Subscribe(x =>
      {
        if (x != null && KlineInterval != x.Model.Interval)
        {
          KlineInterval = x.Model.Interval;
        }
      });

        await ChangeKlineInterval();

        if (DrawingViewModel.MaxValue == 0)
        {
          DrawingViewModel.OnRestChart();
        }

      }
    }

    #endregion

    #region ForexChart_Loaded

    private async Task ForexChart_Loaded()
    {
      TradingBot.Strategy.Logger = logger;

      await LoadLayouts();
    }

    #endregion

    #region LoadLayouts

    public TimeFrame minTimeframe = TimeFrame.D1;
    protected List<CtksLayout> InnerLayouts = new List<CtksLayout>();

    protected virtual async Task LoadLayouts()
    {
      var mainCtks = new Ctks(MainLayout, MainLayout.TimeFrame, TradingBot.Asset);

      DrawingViewModel.ActualCandles = (await
        binanceBroker.GetCandles(TradingBot.Asset.Symbol,
        TradingHelper.GetTimeSpanFromInterval(KlineInterval))).ToList();

      await binanceBroker.SubscribeToKlineInterval(TradingBot.Asset.Symbol, OnBinanceKlineUpdate, KlineInterval);


      LoadSecondaryLayouts();
      LoadIndicators();

      MainLayout.Ctks = mainCtks;
      //Layouts.Add(mainLayout);
      SelectedLayout = MainLayout;

      if (DrawingViewModel.ActualCandles.Count > 0)
      {
        await RenderLayout(DrawingViewModel.ActualCandles.Last());
      }
    }

    #endregion

    #region LoadSecondaryLayouts

    public void LoadSecondaryLayouts(DateTime? maxTime = null)
    {
      InnerLayouts.Clear();
      Layouts.Clear();

      foreach (var layoutData in TradingBot.TimeFrames.Where(x => x.Value >= minTimeframe))
      {
        var layout = CreateCtks(layoutData.Key, layoutData.Value, maxTime);

        Layouts.Add(layout);
        InnerLayouts.Add(layout);
      }
    }

    #endregion

    #region LoadIndicators

    protected void LoadIndicators(DateTime? maxTime = null)
    {
      foreach (var layoutData in TradingBot.IndicatorTimeFrames)
      {
        var layout = CreateLayout<Layout>(
          layoutData.Key,
          layoutData.Value,
          maxTime,
          saveData: !IsSimulation && layoutData.Key.Contains(TradingBot.Asset.Symbol)
          );

        IndicatorLayouts.Add(layout);
      }
    }

    #endregion

    #region CheckLayout

    private void CheckLayout(CtksLayout layout, List<Candle> innerCandles)
    {
      var last = innerCandles.Last();
      if (DateTime.Now > TradingViewHelper.GetNextTime(last.OpenTime, layout.TimeFrame))
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

    #region RenderLayout

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    private bool wasLoaded = false;
    DateTime lastFileCheck = DateTime.Now;

    private Candle actualCachedCandle = null;

    public bool IsSimulation { get; set; } = false;

    public static List<TimeFrame> IndicatorTimeframes { get; set; } = new List<TimeFrame>()
    {
      TimeFrame.D1,
      TimeFrame.H4,
    };

    public async Task RenderLayout(Candle actual)
    {
      if (IsPaused)
      {
        return;
      }

      try
      {
        await semaphoreSlim.WaitAsync();

        var indicatorsCandle = GetCandle(IndicatorTimeframes, actual);

        var ctksIntersections = GetIntersections(actual, out var outdated);

        actualCachedCandle = actual;

        if (ctksIntersections.Count == 0)
        {
          return;
        }

        if (ctksIntersections.Count(x => x.Value > actual.Close) == 0)
        {
          return;
        }

        TradingBot.Strategy.Intersections = ctksIntersections;

        if (!(TradingBot.Strategy is AIStrategy))
        {
          AddRangeFilterIntersections(TimeFrame.D1, actual);
          AddRangeFilterIntersections(TimeFrame.W1, actual);
        }


        if (outdated)
          await TradingBot.Strategy.UpdateIntersections(ctksIntersections);


        if (ctksIntersections.Count > 0)
        {
          if (!wasLoaded)
          {
            wasLoaded = true;

            TradingBot.Strategy.LoadState();

            if (!IsSimulation)
            {
              await TradingBot.Strategy.RefreshState();
              VSynchronizationContext.InvokeOnDispatcher(() => MainWindow?.SortActualPositions());
            }

            await TradingBot.Strategy.UpdateIntersections(ctksIntersections);
          }

          await TradingBot.Strategy.ValidatePositions(actual);
          await TradingBot.Strategy.CreatePositions(actual, indicatorsCandle);
        }
        else
        {
          Console.WriteLine("NO INTERSECTIONS, DOING NOTHING !");
        }

        if (DrawChart)
        {
          var athPrice = GetAthPrice();
          VSynchronizationContext.InvokeOnDispatcher(() => DrawingViewModel.RenderOverlay(athPrice, actual));
        }

      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region GetIntersections

    List<CtksIntersection> ctksCachedIntersections = new List<CtksIntersection>();
    protected virtual List<CtksIntersection> GetIntersections(Candle actual, out bool outdated)
    {
      List<CtksLayout> secondaryLayouts = InnerLayouts.ToList();
      var shouldUpdate = false;
      outdated = false;

      for (int i = 0; i < secondaryLayouts.Count; i++)
      {
        var secondaryLayout = secondaryLayouts[i];

        var lastCandle = secondaryLayout.Ctks.Candles?.LastOrDefault();

        if (lastCandle == null)
          continue;

        var isOutDated = false;

        if (IsSimulation)
        {
          isOutDated = actual.OpenTime > TradingViewHelper.GetNextTime(lastCandle.OpenTime, secondaryLayout.TimeFrame);
        }
        else
        {
          isOutDated = TradingViewHelper.IsOutDated(secondaryLayout.TimeFrame, secondaryLayout.AllCandles);
        }

        if (isOutDated || ctksCachedIntersections.Count == 0)
        {
          var fileCheck = true;

          if (!IsSimulation)
            fileCheck = lastFileCheck < DateTime.Now.AddMinutes(1);

          if (!secondaryLayout.IsOutDated || (secondaryLayout.IsOutDated && fileCheck))
          {
            var lastCount = secondaryLayout.Ctks.Candles.Count;
            var secIndex = secondaryLayouts.IndexOf(secondaryLayout);


            GetCtks(secondaryLayout, ref secondaryLayouts, lastCount);

            secondaryLayout = secondaryLayouts[secIndex];

            if (secondaryLayout.Ctks.Candles.Count > lastCount)
              shouldUpdate = true;
          }
        }
      }

      foreach (var indicator in IndicatorLayouts)
      {
        indicator.IsOutDated = TradingViewHelper.IsOutDated(indicator.TimeFrame, indicator.AllCandles);
        var lastCount = indicator.AllCandles.Count;

        if (indicator.IsOutDated)
        {
          var innerCandles = TradingViewHelper.ParseTradingView
            (indicator.TimeFrame, indicator.DataLocation, Asset.Symbol,
            addNotClosedCandle: true, indexCut: lastCount + 1,
            saveData: !IsSimulation && indicator.DataLocation.Contains(TradingBot.Asset.Symbol));

          indicator.IsOutDated = TradingViewHelper.IsOutDated(indicator.TimeFrame, indicator.AllCandles);

          if (innerCandles.Count > lastCount)
          {
            indicator.AllCandles = innerCandles;
            shouldUpdate = true;
          }
        }
      }

      var newInters = new List<CtksIntersection>();

      if (shouldUpdate || ctksCachedIntersections.Count == 0)
      {
        for (int y = 0; y < secondaryLayouts.Count; y++)
        {
          var intersections = secondaryLayouts[y].Ctks.Intersections;

          var validIntersections = intersections
            .Where(x => x.Value < decimal.MaxValue && x.Value > decimal.MinValue)
            .Where(x => Math.Round(x.Value, TradingBot.Asset.PriceRound) > 0)
            .ToList();

          newInters.AddRange(validIntersections);
        }

        newInters = newInters.OrderByDescending(x => x.Value).ToList();

        var duplicates = newInters.GroupBy(x => x.Value);

        foreach (var duplicate in duplicates.Where(x => x.Count() > 1))
        {
          var list = duplicate.ToList();

          for (int i = 1; i < list.Count; i++)
          {
            newInters.Remove(list[i]);
          }
        }

        var lowest = actual.Close.Value * (decimal)0.3;

        newInters = newInters.Where(x => x.Value > lowest).ToList();
      }


      if (newInters.Count > 0)
      {
        ctksCachedIntersections = newInters;
        outdated = true;
      }

      return ctksCachedIntersections;
    }

    #endregion

    #region GetCtks

    protected virtual void GetCtks(CtksLayout ctksLayout, ref List<CtksLayout> secondaryLayouts, int lastCount)
    {
      var innerCandles = TradingViewHelper.ParseTradingView(ctksLayout.TimeFrame, ctksLayout.DataLocation, Asset.Symbol, addNotClosedCandle: true, indexCut: lastCount + 1);

      ctksLayout.Ctks.CrateCtks(innerCandles);

      ctksLayout.IsOutDated = TradingViewHelper.IsOutDated(ctksLayout.TimeFrame, innerCandles);
    }

    #endregion

    #region CreateCtks

    protected CtksLayout CreateCtks(
      string location,
      TimeFrame timeFrame,
      DateTime? maxTime = null,
      decimal? pmax = null,
      decimal? pmin = null,
      bool saveData = false)
    {
      var layout = CreateLayout<CtksLayout>(location, timeFrame, maxTime, pmax, pmin, saveData: saveData);

      var ctks = new Ctks(layout, timeFrame, TradingBot.Asset);

      ctks.CrateCtks(layout.AllCandles);

      layout.Ctks = ctks;

      return layout;
    }


    #endregion

    #region GetCandle

    Dictionary<TimeFrame, Candle> cachedCandles = new Dictionary<TimeFrame, Candle>();

    protected virtual IList<Candle> GetCandle(IList<TimeFrame> timeFrames, Candle actualCandle)
    {
      var list = new List<Candle>();

      foreach (var timeFrame in timeFrames)
      {
        var existing = cachedCandles.TryGetValue(timeFrame, out var lastCandle);

        if (lastCandle == null || !TradingHelper.IsWithinRange(lastCandle, actualCandle))
        {
          lastCandle = TradingHelper.GetActualEqivalentCandle(Asset.Symbol, timeFrame, actualCandle);

          if (!existing)
          {
            cachedCandles.Add(timeFrame, lastCandle);
          }
          else
          {
            cachedCandles[timeFrame] = lastCandle;
          }
        }

        list.Add(lastCandle);
      }


      return list;
    }

    #endregion

    #region AddRangeFilterIntersections

    Candle lastDailyRangeCandle;
    Candle lastWeeklyRangeCandle;

    private void AddRangeFilterIntersections(TimeFrame timeFrame, Candle actualCandle)
    {
      Candle rangeCandle = GetCandle(new List<TimeFrame>() { timeFrame }, actualCandle).FirstOrDefault();

      if (timeFrame == TimeFrame.D1)
      {
        if (lastDailyRangeCandle == rangeCandle)
          return;
        else
          lastDailyRangeCandle = rangeCandle;
      }
      else if (timeFrame == TimeFrame.W1)
      {
        if (lastWeeklyRangeCandle == rangeCandle)
          return;
        else
          lastDailyRangeCandle = rangeCandle;
      }


      var rgL = TradingBot.Strategy.Intersections
         .FirstOrDefault(x => x.IntersectionType == IntersectionType.RangeFilter && x.Tag == Tag.RangeFilterLow && x.TimeFrame == timeFrame);
      var rgH = TradingBot.Strategy.Intersections
        .FirstOrDefault(x => x.IntersectionType == IntersectionType.RangeFilter && x.Tag == Tag.RangeFilterHigh && x.TimeFrame == timeFrame);
      var rg = TradingBot.Strategy.Intersections
        .FirstOrDefault(x => x.IntersectionType == IntersectionType.RangeFilter && x.Tag == Tag.None && x.TimeFrame == timeFrame);

      if (rangeCandle != null)
      {
        var minDiff = 0.05m;
        var low = Math.Round(rangeCandle.IndicatorData.RangeFilter.LowTarget, TradingBot.Asset.PriceRound);
        var rf = Math.Round(rangeCandle.IndicatorData.RangeFilter.RangeFilter, TradingBot.Asset.PriceRound);
        var high = Math.Round(rangeCandle.IndicatorData.RangeFilter.HighTarget, TradingBot.Asset.PriceRound);

        if (rgL != null && rgL.Value > 0)
        {
          if (Math.Abs((rgL.Value - low) / rgL.Value) > minDiff)
          {
            rgL.Value = low;
          }
        }
        else if (low > 0)
        {
          rgL = new CtksIntersection()
          {
            Value = low,
            IntersectionType = IntersectionType.RangeFilter,
            Tag = Tag.RangeFilterLow,
            TimeFrame = timeFrame
          };

          TradingBot.Strategy.Intersections.Add(rgL);
        }

        if (rg != null)
        {
          rg.Value = rf;
        }
        else if (rf > 0)
        {
          rg = new CtksIntersection()
          {
            Value = rf,
            IntersectionType = IntersectionType.RangeFilter,
            TimeFrame = timeFrame
          };
          TradingBot.Strategy.Intersections.Add(rg);
        }

        if (rgH != null && rgH.Value > 0)
        {
          if (Math.Abs((rgH.Value - high) / rgH.Value) > minDiff)
          {
            rgH.Value = high;
          }
        }
        else if (high > 0)
        {
          rgH = new CtksIntersection()
          {
            Value = high,
            IntersectionType = IntersectionType.RangeFilter,
            Tag = Tag.RangeFilterHigh,
            TimeFrame = timeFrame
          };

          TradingBot.Strategy.Intersections.Add(rgH);
        }
      }
    }

    #endregion

    #region GetRangeFilterIntersections

    protected IList<CtksIntersection> GetRangeFilterIntersections(
      IList<CtksIntersection> intersections,
      TimeFrame timeFrame,
      Candle rangeCandle)
    {
      var rgL = intersections
         .FirstOrDefault(x => x.IntersectionType == IntersectionType.RangeFilter && x.Tag == Tag.RangeFilterLow && x.TimeFrame == timeFrame);
      var rgH = intersections
        .FirstOrDefault(x => x.IntersectionType == IntersectionType.RangeFilter && x.Tag == Tag.RangeFilterHigh && x.TimeFrame == timeFrame);
      var rg = intersections
        .FirstOrDefault(x => x.IntersectionType == IntersectionType.RangeFilter && x.Tag == Tag.None && x.TimeFrame == timeFrame);

      var list = new List<CtksIntersection>();

      if (rangeCandle != null)
      {
        var minDiff = 0.05m;
        var low = Math.Round(rangeCandle.IndicatorData.RangeFilter.LowTarget, TradingBot.Asset.PriceRound);
        var rf = Math.Round(rangeCandle.IndicatorData.RangeFilter.RangeFilter, TradingBot.Asset.PriceRound);
        var high = Math.Round(rangeCandle.IndicatorData.RangeFilter.HighTarget, TradingBot.Asset.PriceRound);

        if (rgL != null && rgL.Value > 0)
        {
          if (Math.Abs((rgL.Value - low) / rgL.Value) > minDiff)
          {
            rgL.Value = low;
          }
        }
        else if (low > 0)
        {
          rgL = new CtksIntersection()
          {
            Value = low,
            IntersectionType = IntersectionType.RangeFilter,
            Tag = Tag.RangeFilterLow,
            TimeFrame = timeFrame
          };

          list.Add(rgL);
        }

        if (rg != null)
        {
          rg.Value = rf;
        }
        else if (rf > 0)
        {
          rg = new CtksIntersection()
          {
            Value = rf,
            IntersectionType = IntersectionType.RangeFilter,
            TimeFrame = timeFrame
          };

          list.Add(rg);
        }

        if (rgH != null && rgH.Value > 0)
        {
          if (Math.Abs((rgH.Value - high) / rgH.Value) > minDiff)
          {
            rgH.Value = high;
          }
        }
        else if (high > 0)
        {
          rgH = new CtksIntersection()
          {
            Value = high,
            IntersectionType = IntersectionType.RangeFilter,
            Tag = Tag.RangeFilterHigh,
            TimeFrame = timeFrame
          };

          list.Add(rgH);
        }
      }

      return list;
    }

    #endregion

    #region CreateLayout

    private T CreateLayout<T>(
      string location,
      TimeFrame timeFrame,
      DateTime? maxTime = null,
      decimal? pmax = null,
      decimal? pmin = null,
      bool saveData = false)
      where T : Layout, new()
    {
      var candles = TradingViewHelper.ParseTradingView(timeFrame, location, TradingBot.Asset.Symbol, maxTime, saveData: saveData);

      var max = pmax ?? candles.Max(x => x.High.Value);
      var min = pmin ?? candles.Min(x => x.Low.Value);

      var layout = new T()
      {
        Title = timeFrame.ToString(),
        MaxValue = max,
        MinValue = min,
        TimeFrame = timeFrame,
        DataLocation = location,
        AllCandles = candles,
      };

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


        VSynchronizationContext.InvokeOnDispatcher(async () =>
        {
          if (SelectedLayout != null)
            await RenderLayout(actual);
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

          decimal? athPrice = GetToAthPrice(lastStates.Max(x => x.TotalValue) ?? 0);

          if (lastStates.Any(x => x.AthPrice > 0))
          {
            athPrice = athPrice != 0 ? athPrice : lastStates.Last(x => x.AthPrice > 0).AthPrice;
          }

          lastState = new State()
          {
            Date = actual.OpenTime.Date,
            TotalProfit = TradingBot.Strategy.TotalProfit,
            TotalValue = TradingBot.Strategy.TotalValue,
            TotalNative = TradingBot.Strategy.TotalNativeAsset,
            TotalNativeValue = TradingBot.Strategy.TotalNativeAssetValue,
            AthPrice = athPrice != 0 ? athPrice : actual.Close,
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

        if (DrawingViewModel.ActualCandles.Min(x => x.UnixTime) > DrawingViewModel.MinUnix)
        {
          await FetchAdditionalCandles();
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
          await ChangeKlineInterval();

          DrawingViewModel.OnRestChart();

          DrawingViewModel.LockChart = true;
        }

        if (DrawingViewModel.ActualCandles.Count > 0)
        {
          DrawingViewModel.RenderOverlay(GetAthPrice());
        }
      }
    }

    #endregion

    #region ChangeKlineInterval

    private async Task ChangeKlineInterval()
    {
      await binanceBroker.SubscribeToKlineInterval(TradingBot.Asset.Symbol, OnBinanceKlineUpdate, KlineInterval);

      DrawingViewModel.ActualCandles = (await binanceBroker.GetCandles(
        TradingBot.Asset.Symbol,
        TradingHelper.GetTimeSpanFromInterval(KlineInterval))).ToList();

      if (DrawingViewModel.ActualCandles.Count > 0)
      {
        DrawingViewModel.unixDiff = DrawingViewModel.ActualCandles[1].UnixTime - DrawingViewModel.ActualCandles[0].UnixTime;
      }
    }

    #endregion

    #region FetchAdditionalCandles

    private async Task FetchAdditionalCandles()
    {
      var startDate = DateTimeHelper.UnixTimeStampToUtcDateTime(DrawingViewModel.MinUnix);
      var candles = DrawingViewModel.ActualCandles;

      DateTime? firstDate = candles.First().OpenTime;
      DateTime? lastCloseTime = null;

      while (DrawingViewModel.MinUnix < candles.Min(x => x.UnixTime))
      {
        //was added to the end
        var lastValues =
          (await binanceBroker.GetCandles(TradingBot.Asset.Symbol,
          TradingHelper.GetTimeSpanFromInterval(KlineInterval), endTime: firstDate))
          .OrderByDescending(x => x.CloseTime);

        firstDate = lastValues.Min(x => x.OpenTime);

        if (lastCloseTime == firstDate)
        {
          break;
        }

        lastCloseTime = firstDate;

        candles.AddRange(lastValues);
      }

      candles = candles.OrderBy(x => x.CloseTime).ToList();

      DrawingViewModel.ActualCandles = candles;
    }

    #endregion

    #region LoadLayoutSettings

    public LayoutSettings layoutSettings;

    public void LoadLayoutSettings()
    {
      if (File.Exists(layoutPath))
      {
        var data = File.ReadAllText(layoutPath);
        layoutSettings = JsonSerializer.Deserialize<LayoutSettings>(data);

        if (layoutSettings != null)
        {
          var savedLayout = LayoutIntervals.ViewModels.SingleOrDefault(x => x.Model.Interval == layoutSettings.LayoutInterval);

          if (savedLayout != null)
          {
            savedLayout.IsSelected = true;
            klineInterval = savedLayout.Model.Interval;
            var selected = LayoutIntervals.ViewModels.SingleOrDefault(x => x.Model.Interval == klineInterval);

            if (selected != null)
            {
              selected.IsSelected = true;
            }
          }

          if (layoutSettings.ColorSettings != null)
          {
            var lsit = DrawingViewModel.ColorScheme.ColorSettings.ToList();
            foreach (var setting in lsit)
            {
              var found = layoutSettings.ColorSettings.FirstOrDefault(x => x.Purpose == setting.Key);

              if (found != null)
                DrawingViewModel.ColorScheme.ColorSettings[setting.Key] = new ColorSettingViewModel(found);
            }
          }

          DrawingViewModel.DrawingSettings = layoutSettings.DrawingSettings;

          if (layoutSettings.IndicatorSettings != null)
          {
            foreach (var indicatorSetting in layoutSettings.IndicatorSettings)
            {
              DrawingViewModel.IndicatorSettings.Add(indicatorSetting);
            }
          }
          else
          {
            DrawingViewModel.IndicatorSettings.Add(new IndicatorSettings()
            {
              TimeFrame = TimeFrame.D1,
            });
          }
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
          LayoutInterval = KlineInterval,
          ColorSettings = DrawingViewModel.ColorScheme.ColorSettings.Select(x => x.Value.Model),
          DrawingSettings = DrawingViewModel.DrawingSettings,
          StartLowPrice = DrawingViewModel.MinValue,
          StartMaxPrice = DrawingViewModel.MaxValue,
          StartMaxUnix = DrawingViewModel.MaxUnix,
          StartMinUnix = DrawingViewModel.MinUnix,
          IndicatorSettings = DrawingViewModel.IndicatorSettings
        };

        var options = new JsonSerializerOptions()
        {
          WriteIndented = true
        };

        layoutSettings = settings;

        File.WriteAllText(layoutPath, JsonSerializer.Serialize(settings, options));
      }
    }

    #endregion

    #region SaveAsset

    public void SaveAsset()
    {
      if (!IsSimulation)
      {
        TradingBot.Asset.RunTimeTicks = TotalRunTime.Ticks;


        var options = new JsonSerializerOptions()
        {
          WriteIndented = true
        };

        var json = JsonSerializer.Serialize<Asset>(TradingBot.Asset, options);
        File.WriteAllText(Path.Combine(Settings.DataPath, "asset.json"), json);
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

    public virtual void Stop()
    {
    }

    #endregion

    #endregion
  }
}
