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
using Logger;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.Other;
using VCore.WPF.ViewModels;

namespace CTKS_Chart.ViewModels
{
  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private readonly ILogger logger;
    private Stopwatch stopwatch = new Stopwatch();
    private TimeSpan lastElapsed;
    private BinanceBroker binanceBroker;

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory, ILogger logger) : base(viewModelsFactory)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      CultureInfo.CurrentCulture = new CultureInfo("en-US");
      binanceBroker = new BinanceBroker(logger);

      ForexChart_Loaded();

      stopwatch.Start();
      Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnDispatcher().Subscribe((x) =>
      {
        var diff = stopwatch.Elapsed - lastElapsed;
        ActiveTime += diff;
        TotalRunTime += diff;

        lastElapsed = stopwatch.Elapsed;

        if (Math.Round(activeTime.TotalSeconds, 0) % 10 == 0)
        {
          TradingBot.Asset.RunTimeTicks = TotalRunTime.Ticks;
          var json = JsonSerializer.Serialize<Asset>(TradingBot.Asset);
          File.WriteAllText("asset.json", json);
        }
      });
    }

    #region Properties

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

#if DEBUG
    public bool Simulation { get; set; } = false;
#endif

#if RELEASE
    public bool Simulation { get; set; } = false;
#endif

#if DEBUG
    public bool IsLive { get; set; } = false;
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
        if (value != maxValue)
        {
          maxValue = value;
          RaisePropertyChanged();
          MainLayout.MaxValue = MaxValue;
          TradingBot.Asset.StartMaxPrice = MaxValue;

          RecreateChart();
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
        if (value != minValue)
        {
          minValue = value;
          RaisePropertyChanged();
          MainLayout.MinValue = MinValue;
          TradingBot.Asset.StartLowPrice = MinValue;

          RecreateChart();
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
          RecreateChart();
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

    public double CanvasHeight { get; set; } = 1000;
    public double CanvasWidth { get; set; } = 1000;
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
      MainGrid.Children.Clear();

      if (layout.Canvas == null)
      {
        MainGrid.Children.Add(ChartImage);
      }
      else
      {
        MainGrid.Children.Add(layout.Canvas);
      }

      SelectedLayout = layout;
    }

    #endregion


    #region ShowCanvas

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
      await TradingBot.Strategy.Reset(actual);
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

    #endregion

    #region Methods

    public override void Initialize()
    {
      base.Initialize();

      MainGrid.Children.Add(ChartImage);
    }

    #region ForexChart_Loaded

    private async void ForexChart_Loaded()
    {
      //var tradingView_12m = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\BTC-USD.csv";


      var location = "Data";

      var tradingView_btc_12m = $"{location}\\INDEX BTCUSD, 12M.csv";
      var tradingView_btc_6m = $"{location}\\INDEX BTCUSD, 6M.csv";
      var tradingView_btc_3m = $"{location}\\INDEX BTCUSD, 3M.csv";
      var tradingView_btc_1m = $"{location}\\INDEX BTCUSD, 1M.csv";
      var tradingView_btc_2W = $"{location}\\INDEX BTCUSD, 2W.csv";
      var tradingView_btc_1W = $"{location}\\INDEX BTCUSD, 1W.csv";
      var tradingView_btc_1D = $"{location}\\INDEX BTCUSD, 1D.csv";

      var tradingView_btc_720m = $"{location}\\INDEX BTCUSD, 720.csv";
      var tradingView_btc_240m = $"{location}\\INDEX BTCUSD, 240.csv";

      var tradingView__ada_12M = $"{location}\\BINANCE ADAUSD, 12M.csv";
      var tradingView__ada_6M = $"{location}\\BINANCE ADAUSD, 6M.csv";
      var tradingView__ada_3M = $"{location}\\BINANCE ADAUSD, 3M.csv";
      var tradingView__ada_1M = $"{location}\\BINANCE ADAUSD, 1M.csv";
      var tradingView__ada_2W = $"{location}\\BINANCE ADAUSD, 2W.csv";
      var tradingView__ada_1W = $"{location}\\BINANCE ADAUSD, 1W.csv";
      var tradingView__ada_1D = $"{location}\\BINANCE ADAUSD, 1D.csv";
      var tradingView__ada_1D_2 = $"{location}\\BINANCE ADAUSDT, 1D_2.0.csv";
      var tradingView__ada_360 = $"{location}\\BINANCE ADAUSD, 360.csv";
      var tradingView__ada_240 = $"{location}\\BINANCE ADAUSD, 240.csv";
      var tradingView__ada_120 = $"{location}\\BINANCE ADAUSDT, 120.csv";
      var tradingView__ada_15 = $"{location}\\BINANCE ADAUSDT, 15.csv";

      var tradingView__eth_1W = $"{location}\\BITSTAMP ETHUSD, 1W.csv";


      var tradingView_ltc_240 = $"{location}\\BINANCE LTCUSD.P, 240.csv";

      var tradingView_spy_12 = $"{location}\\BATS SPY, 12M.csv";
      var tradingView_spy_6 = $"{location}\\BATS SPY, 6M.csv";
      var tradingView_spy_3 = $"{location}\\BATS SPY, 3M.csv";
      var tradingView_spy_1D = $"{location}\\BATS SPY, 1D.csv";

      var tradingView__ltc_12M = $"{location}\\BINANCE LTCUSD, 12M.csv";
      var tradingView__ltc_6M = $"{location}\\BINANCE LTCUSD, 6M.csv";
      var tradingView__ltc_3M = $"{location}\\BINANCE LTCUSD, 3M.csv";
      var tradingView__ltc_1M = $"{location}\\BINANCE LTCUSD, 1M.csv";
      var tradingView__ltc_2W = $"{location}\\BINANCE LTCUSD, 2W.csv";
      var tradingView__ltc_1W = $"{location}\\BINANCE LTCUSD, 1W.csv";


      var spy = new[] {
        new Tuple<string, TimeFrame>(tradingView_spy_12, TimeFrame.M12),
        new Tuple<string, TimeFrame>(tradingView_spy_6, TimeFrame.M6),
        new Tuple<string, TimeFrame>(tradingView_spy_3, TimeFrame.M3)
      };

      var btc = new Dictionary<string, TimeFrame> {
        {tradingView_btc_12m, TimeFrame.M12},
        {tradingView_btc_6m, TimeFrame.M6},
        {tradingView_btc_3m, TimeFrame.M3},
        {tradingView_btc_1m, TimeFrame.M1},
        {tradingView_btc_2W, TimeFrame.W2},
        {tradingView_btc_1W, TimeFrame.W1},
      };

      var ada = new Dictionary<string, TimeFrame> {
        {tradingView__ada_12M, TimeFrame.M12},
        {tradingView__ada_6M, TimeFrame.M6},
        {tradingView__ada_3M, TimeFrame.M3},
        {tradingView__ada_1M, TimeFrame.M1},
        {tradingView__ada_2W, TimeFrame.W2},
        {tradingView__ada_1W, TimeFrame.W1},
      };

      var ltc = new Dictionary<string, TimeFrame> {
        {tradingView__ltc_12M, TimeFrame.M12},
        {tradingView__ltc_6M, TimeFrame.M6},
        {tradingView__ltc_3M, TimeFrame.M3},
        {tradingView__ltc_1M, TimeFrame.M1},
        {tradingView__ltc_2W, TimeFrame.W2},
        {tradingView__ltc_1W, TimeFrame.W1},
      };

      var asset = JsonSerializer.Deserialize<Asset>(File.ReadAllText("asset.json"));

      asset.RunTime = TimeSpan.FromTicks(asset.RunTimeTicks);

      MainLayout = new Layout()
      {
        Title = "Main",
        TimeFrame = TimeFrame.D1
      };

      Strategy strategy = new BinanceStrategy(binanceBroker, logger, IsLive);

      if (!IsLive)
        strategy = new SimulationStrategy();


      var adaBot = new TradingBot(new Asset()
      {
        Symbol = "ADAUSDT",
        NativeRound = 1,
        PriceRound = 4
      }, ada, strategy);

      var ltcBot = new TradingBot(new Asset()
      {
        Symbol = "LTCUSDT",
        NativeRound = 3,
        PriceRound = 2
      }, ltc, strategy);

      var btcBot = new TradingBot(new Asset()
      {
        Symbol = "BTCUSDT",
        NativeRound = 5,
        PriceRound = 2
      }, btc, strategy);


      var dic = asset.Symbol == "BTCUSDT" ? btc : asset.Symbol == "ADAUSDT" ? ada : ltc;

      if (IsLive)
        TradingBot = new TradingBot(asset, dic, strategy);
      else
      {
        TradingBot = adaBot;
      }

      strategy.Asset = TradingBot.Asset;

      MainLayout.MaxValue = TradingBot.StartingMaxPrice;
      MainLayout.MinValue = TradingBot.StartingMinPrice;

      maxValue = MainLayout.MaxValue;
      minValue = MainLayout.MinValue;

      if (IsLive)
      {
        await LoadLayouts(MainLayout);
      }
      else
      {
        var mainCandles = ParseTradingView(tradingView__ada_1D);

        MainLayout.MaxValue = mainCandles.Max(x => x.High.Value);
        MainLayout.MinValue = mainCandles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

        var maxDate = mainCandles.First().Time;

        await LoadLayouts(MainLayout, mainCandles, maxDate, 1900, mainCandles.Count, true);
      }

      //Do not raise 

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
      bool simulate = false)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, CanvasHeight, CanvasWidth, TradingBot.Asset);

      if (simulate && mainCandles != null)
      {
        ActualCandles = mainCandles.Skip(skip).SkipLast(cut).ToList();
      }
      else
      {
        ActualCandles = (await binanceBroker.GetCandles(TradingBot.Asset.Symbol, GetTimeSpanFromInterval(KlineInterval))).ToList();
        await binanceBroker.SubscribeToKlineInterval(TradingBot.Asset.Symbol, OnBinanceKlineUpdate, KlineInterval);
      }

      foreach (var layoutData in TradingBot.TimeFrames)
      {
        var layout = CreateCtksChart(layoutData.Key, layoutData.Value, maxTime);


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
        var cutCandles = mainCandles.Skip(skip).TakeLast(cut).ToList();

        Simulate(cutCandles, mainLayout, ActualCandles, InnerLayouts, 1);
      }
    }

    #endregion

    #region RecreateChart

    private async void RecreateChart()
    {
      if (IsLive)
      {
        ActualCandles = (await binanceBroker.GetCandles(TradingBot.Asset.Symbol, GetTimeSpanFromInterval(KlineInterval))).ToList();
        await binanceBroker.SubscribeToKlineInterval(TradingBot.Asset.Symbol, OnBinanceKlineUpdate, KlineInterval);

        if (ActualCandles.Count > 0)
        {
          RenderLayout(MainLayout, InnerLayouts, ActualCandles.Last(), ActualCandles);
        }
      }

    }

    #endregion

    #region CheckLayout

    private void CheckLayout(Layout layout)
    {
      var innerCandles = ParseTradingView(layout.DataLocation);

      if (DateTime.Now > GetNextTime(innerCandles.Last().Time, layout.TimeFrame))
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
        RenderOverlay(layout, ctksIntersections, TradingBot.Strategy, candles);

        this.actual = actual;

        foreach (var secondaryLayout in secondaryLayouts)
        {
          var lastCandle = secondaryLayout.Ctks.Candles.Last();

          if (actual.Time > GetNextTime(lastCandle.Time, secondaryLayout.TimeFrame))
          {
            var innerCandles = ParseTradingView(secondaryLayout.DataLocation, actual.Time, addNotClosedCandle: true);

            secondaryLayout.Ctks.CrateCtks(innerCandles, () => CreateChart(secondaryLayout, CanvasHeight, CanvasWidth, innerCandles));

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

        if (!wasLoaded)
        {
          TradingBot.Strategy.LoadState();
          await TradingBot.Strategy.RefreshState();
          wasLoaded = true;
        }

        
        TradingBot.Strategy.ValidatePositions(actual);
        TradingBot.Strategy.CreatePositions(actual);

        if (!Simulation)
          RenderOverlay(layout, ctksIntersections, TradingBot.Strategy, candles);
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region CreateCtksChart

    private Layout CreateCtksChart(string location, TimeFrame timeFrame, DateTime? maxTime = null)
    {
      var candles = ParseTradingView(location, maxTime);

      var canvas = new Canvas();

      var layout = new Layout()
      {
        Title = timeFrame.ToString(),
        Canvas = canvas,
        MaxValue = candles.Max(x => x.High.Value),
        MinValue = candles.Min(x => x.Low.Value),
        TimeFrame = timeFrame,
        DataLocation = location,
      };

      canvas.Width = CanvasWidth;
      canvas.Height = CanvasHeight;

      var ctks = new Ctks(layout, timeFrame, CanvasHeight, CanvasWidth, TradingBot.Asset);

      ctks.CrateCtks(candles, () => CreateChart(layout, CanvasHeight, CanvasWidth, candles));

      layout.Ctks = ctks;

      Layouts.Add(layout);

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
            Time = dateTime,
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
            Fill = green ? Brushes.Green : Brushes.Red,
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

    private Tuple<double, double> DrawChart(
      DrawingContext drawingContext,
      Layout layout,
      IList<Candle> candles,
      int maxCount = 150)
    {
      var canvasWidth = CanvasWidth * 0.85 - 150;

      var skip = candles.Count - maxCount > 0 ? candles.Count - maxCount : 0;

      var width = canvasWidth / maxCount;
      var margin = width * 0.95;

      double minDrawnPoint = 0;
      double maxDrawnPoint = 0;
      var maxDrawinPoint = GetCanvasValue(CanvasHeight, layout.MaxValue, layout.MaxValue, layout.MinValue);
      var minDrawinPoint = GetCanvasValue(CanvasHeight, layout.MinValue, layout.MaxValue, layout.MinValue);

      if (candles.Any())
      {
        int y = 0;
        for (int i = skip; i < candles.Count; i++)
        {
          var point = candles[i];

          var close = GetCanvasValue(CanvasHeight, point.Close.Value, layout.MaxValue, layout.MinValue);
          var open = GetCanvasValue(CanvasHeight, point.Open.Value, layout.MaxValue, layout.MinValue);

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var selectedBrush = green ? Brushes.Green : Brushes.Red;

          Pen pen = new Pen(selectedBrush, 3);
          Pen wickPen = new Pen(selectedBrush, 1);

          var newCandle = new Rect()
          {
            Width = width - margin,
          };

          var lastClose = i > 0 ?
            GetCanvasValue(CanvasHeight, candles[i - 1].Close.Value, layout.MaxValue, layout.MinValue) :
            GetCanvasValue(CanvasHeight, candles[i].Open.Value, layout.MaxValue, layout.MinValue);

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
            newCandle.Y = CanvasHeight - close;
          else
            newCandle.Y = CanvasHeight - close - newCandle.Height;


          var topWickCanvas = GetCanvasValue(CanvasHeight, candles[i].High.Value, layout.MaxValue, layout.MinValue);
          var bottomWickCanvas = GetCanvasValue(CanvasHeight, candles[i].Low.Value, layout.MaxValue, layout.MinValue);

          var wickTop = green ? close : open;
          var wickBottom = green ? open : close;

          var topWick = new Rect()
          {
            Height = topWickCanvas - wickTop,
            X = newCandle.X,
            Y = CanvasHeight - wickTop - (topWickCanvas - wickTop),
          };

          var bottomWick = new Rect()
          {
            Height = wickBottom - bottomWickCanvas,
            X = newCandle.X,
            Y = CanvasHeight - wickBottom,
          };


          drawingContext.DrawRectangle(selectedBrush, pen, newCandle);

          //if (topWick.Y + topWick.Height > maxDrawinPoint)
          //  topWick.Height = CanvasHeight - wickTop;

          //if (bottomWick.Y - bottomWick.Height < 0)
          //{
          //  bottomWick.Height = CanvasHeight - bottomWick.Y;
          //}

          drawingContext.DrawRectangle(selectedBrush, wickPen, topWick);
          drawingContext.DrawRectangle(selectedBrush, wickPen, bottomWick);


          y++;

          if (bottomWick.Y < minDrawnPoint)
          {
            minDrawnPoint = bottomWick.Y;
          }

          if (topWick.Y > maxDrawnPoint)
          {
            maxDrawnPoint = topWick.Y;
          }
        }
      }

      return new Tuple<double, double>(minDrawnPoint, maxDrawnPoint);
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
      Strategy strategy,
      IList<Candle> candles)
    {
      Pen shapeOutlinePen = new Pen(Brushes.Transparent, 1);
      shapeOutlinePen.Freeze();

      DrawingGroup dGroup = new DrawingGroup();

      using (DrawingContext dc = dGroup.Open())
      {
        dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(1000, 1000));

        var drawPoints = DrawChart(dc, layout, candles);
        double desiredCanvasHeight = CanvasHeight;

        if (drawPoints.Item2 > CanvasHeight)
        {
          desiredCanvasHeight = drawPoints.Item2;
        }

        RenderIntersections(dc, layout, ctksIntersections, strategy.AllOpenedPositions.ToList(), desiredCanvasHeight, candles, IsLive ? TimeFrame.W1 : TimeFrame.M1);


        DrawActualPrice(dc, layout, candles);
      }

      DrawingImage dImageSource = new DrawingImage(dGroup);

      Chart = dImageSource;
      this.ChartImage.Source = Chart;
    }

    #endregion

    #region DrawActualPrice

    public void DrawActualPrice(DrawingContext drawingContext, Layout layout, IList<Candle> candles)
    {
      var lastCandle = candles.Last();
      var closePrice = lastCandle.Close;

      var close = GetCanvasValue(CanvasHeight, closePrice.Value, layout.MaxValue, layout.MinValue);

      var lineY = CanvasHeight - close;

      var brush = Brushes.Yellow;
      var pen = new Pen(brush, 1);
      pen.DashStyle = DashStyles.Dash;

      var text = GetFormattedText(closePrice.Value.ToString($"N{TradingBot.Asset.PriceRound}"), brush);
      drawingContext.DrawText(text, new Point(CanvasWidth - text.Width - 25, lineY - text.Height - 5));
      drawingContext.DrawLine(pen, new Point(0, lineY), new Point(CanvasWidth, lineY));
    }

    #endregion

    #region GetFormattedText

    private FormattedText GetFormattedText(string text, Brush brush)
    {
      return new FormattedText(text, CultureInfo.GetCultureInfo("en-us"),
        FlowDirection.LeftToRight,
        new Typeface(new FontFamily("Arial").ToString()),
        12, brush);
    }

    #endregion

    #region RenderIntersections

    public void RenderIntersections(
      DrawingContext drawingContext,
      Layout layout,
      IEnumerable<CtksIntersection> intersections,
      IList<Position> allPositions,
      double desiredHeight,
      IList<Candle> candles,
      TimeFrame minTimeframe = TimeFrame.W1
      )
    {
      var maxCanvasValue = (decimal)GetValueFromCanvas(desiredHeight, desiredHeight, layout.MaxValue, layout.MinValue);
      var minCanvasValue = (decimal)GetValueFromCanvas(desiredHeight, -2 * (desiredHeight - CanvasHeight), layout.MaxValue, layout.MinValue);

      //150 = max candle number on chart
      var maxCandle = candles.TakeLast(150).Max(x => x.High);

      if (maxCandle > maxCanvasValue)
      {
        maxCanvasValue = maxCandle.Value;
      }

      var validIntersection = intersections
        .Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue && minTimeframe <= x.TimeFrame)
        .ToList();

      foreach (var intersection in validIntersection)
      {
        Pen pen = new Pen(Brushes.Gray, 1);
        pen.DashStyle = DashStyles.Dash;
        Brush selectedBrush = Brushes.White;

        var actual = GetCanvasValue(CanvasHeight, intersection.Value, layout.MaxValue, layout.MinValue);

        var frame = intersection.TimeFrame;

        pen.Thickness = GetPositionThickness(frame);

        var lineY = CanvasHeight - actual;

        var positionsOnIntersesction = allPositions
          .Where(x => x.Intersection?.Value == intersection.Value)
          .ToList();

        var firstPositionsOnIntersesction = positionsOnIntersesction.FirstOrDefault();
        var sum = positionsOnIntersesction.Sum(x => x.PositionSize);

        if (firstPositionsOnIntersesction != null)
        {
          selectedBrush = firstPositionsOnIntersesction.Side == PositionSide.Buy ? Brushes.Green : Brushes.Red;
          pen.Brush = selectedBrush;
        }

        //var text = sum > 0 ? $"{intersection.Value.ToString("N4")} - {sum.ToString("N4")}" : $"{intersection.Value.ToString("N4")}";

        if (frame >= TimeFrame.W1)
        {
          FormattedText formattedText = new FormattedText(intersection.Value.ToString(),
            CultureInfo.GetCultureInfo("en-us"),
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Arial").ToString()),
            12, selectedBrush);

          drawingContext.DrawText(formattedText, new Point(0, lineY));
        }



        drawingContext.DrawLine(pen, new Point(150, lineY), new Point(CanvasWidth, lineY));
      }
    }

    #endregion

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
          Time = binanceStreamKline.OpenTime
        };

        var lastCandle = ActualCandles.Last();

        if (lastCandle.Time != actual.Time)
        {
          ActualCandles.Add(actual);
        }
        else
        {
          ActualCandles.RemoveAt(ActualCandles.Count - 1);
          ActualCandles.Add(actual);
        }

        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          RenderLayout(MainLayout, InnerLayouts, actual, ActualCandles);
        });
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

    #endregion
  }
}

