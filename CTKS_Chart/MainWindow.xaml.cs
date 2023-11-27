using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CTKS_Chart.Binance;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Misc;
#pragma warning disable 618

namespace CTKS_Chart
{
  public class Asset
  {
    public string Symbol { get; set; } = "ADAUSDT";
    public int NativeRound { get; set; } = 1;
    public int PriceRound { get; set; } = 4;
  }

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    private BinanceBroker binanceBroker = new BinanceBroker();

    public Asset Asset { get; set; } = new Asset();

#if DEBUG
    public bool IsLive { get; set; } = false;
#endif

#if RELEASE
    public bool IsLive { get; set; } = true;
#endif

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

    public MainWindow()
    {
      VSynchronizationContext.UISynchronizationContext = SynchronizationContext.Current;
      VSynchronizationContext.UIDispatcher = Application.Current.Dispatcher;

      InitializeComponent();
      DataContext = this;

      Strategy = new BinanceStrategy(binanceBroker);
#if DEBUG
      Strategy = new SimulationStrategy();
#endif
      Strategy.Asset = Asset;
      ForexChart_Loaded();


      //binanceBroker.PlaceSpotOrder("BTCUSDT", (decimal)0.001, PositionSide.Buy);
      //binanceBroker.GetOpenOrders("BTCUSDT");
    }

    public ObservableCollection<Layout> Layouts { get; set; } = new ObservableCollection<Layout>();

    public Strategy Strategy { get; set; }

    #region Selected

    private Layout selected;

    public Layout Selected
    {
      get { return selected; }
      set
      {
        if (value != selected)
        {
          selected = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    //double maxValue = 0.40;
    //double minValue = 0.22;

    public double CanvasHeight { get; set; } = 1000;
    public double CanvasWidth { get; set; } = 1000;

    public Layout MainLayout { get; set; }


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
      main_grid.Children.Clear();

      if (layout.Canvas == null)
      {
        chart_image.Visibility = Visibility.Visible;
      }
      else
      {
        main_grid.Children.Add(layout.Canvas);
        chart_image.Visibility = Visibility.Collapsed;
      }

      Selected = layout;
    }

    #endregion

    #region ShowLines

    protected ActionCommand<Ctks> showLines;

    public ICommand ShowLines
    {
      get
      {
        return showLines ??= new ActionCommand<Ctks>(OnShowLines);
      }
    }

    protected virtual void OnShowLines(Ctks ctks)
    {
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

    protected ActionCommand<Ctks> showIntersections;

    public ICommand ShowIntersections
    {
      get
      {
        return showIntersections ??= new ActionCommand<Ctks>(OnShowIntersections);
      }
    }

    protected virtual void OnShowIntersections(Ctks ctks)
    {
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

    #endregion

    #region Methods

    #region ForexChart_Loaded

    private void ForexChart_Loaded()
    {
      //var tradingView_12m = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\BTC-USD.csv";


      var location = "Data";

      var tradingView_btc_12m = $"{location}\\INDEX BTCUSD, 12M.csv";
      var tradingView_btc_6m = $"{location}\\INDEX BTCUSD, 6M.csv";
      var tradingView_btc_3m = $"{location}\\INDEX BTCUSD, 3M.csv";
      var tradingView_btc_1m = $"{location}\\INDEX BTCUSD, 1M.csv";

      var tradingView_btc_2W = $"{location}\\INDEX BTCUSD, 2W.csv";
      var tradingView_btc_1W = $"{location}\\INDEX BTCUSD, 1W.csv";

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

      var tradingView__eth_1W = $"{location}\\BITSTAMP ETHUSD, 1W.csv";


      var tradingView_ltc_240 = $"{location}\\BINANCE LTCUSD.P, 240.csv";

      var tradingView_spy_12 = $"{location}\\BATS SPY, 12M.csv";
      var tradingView_spy_6 = $"{location}\\BATS SPY, 6M.csv";
      var tradingView_spy_3 = $"{location}\\BATS SPY, 3M.csv";
      var tradingView_spy_1D = $"{location}\\BATS SPY, 1D.csv";


      var spy = new[] {
        new Tuple<string, TimeFrame>(tradingView_spy_12, TimeFrame.M12),
        new Tuple<string, TimeFrame>(tradingView_spy_6, TimeFrame.M6),
        new Tuple<string, TimeFrame>(tradingView_spy_3, TimeFrame.M3)
      };

      var btc = new[] {
        new Tuple<string, TimeFrame>(tradingView_btc_12m, TimeFrame.M12),
        new Tuple<string, TimeFrame>(tradingView_btc_6m, TimeFrame.M6),
        new Tuple<string, TimeFrame>(tradingView_btc_3m, TimeFrame.M3),
        new Tuple<string, TimeFrame>(tradingView_btc_1m, TimeFrame.M1),
        new Tuple<string, TimeFrame>(tradingView_btc_1W, TimeFrame.W2)
      };

      var ada = new[] {
        new Tuple<string, TimeFrame>(tradingView__ada_12M, TimeFrame.M12),
        new Tuple<string, TimeFrame>(tradingView__ada_6M, TimeFrame.M6),
        new Tuple<string, TimeFrame>(tradingView__ada_3M, TimeFrame.M3),
        new Tuple<string, TimeFrame>(tradingView__ada_1M, TimeFrame.M1),
        new Tuple<string, TimeFrame>(tradingView__ada_2W, TimeFrame.W2),
       new Tuple<string, TimeFrame>(tradingView__ada_1W, TimeFrame.W1),
      };


      //LoadLayouts(spy, new Tuple<string, TimeFrame>(tradingView_spy_1D, TimeFrame.D1), mainCanvas, 900, 100);
      //LoadLayouts(btc, new Tuple<string, TimeFrame>(tradingView_btc_720m, TimeFrame.D1), mainCanvas, 0, 0);

      MainLayout = new Layout()
      {
        Title = "Main",
        TimeFrame = TimeFrame.D1
      };

      var mainCandles = ParseTradingView(tradingView__ada_1D);


      MainLayout.MaxValue = mainCandles.Max(x => x.High.Value);
      MainLayout.MinValue = mainCandles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

      MaxValue = MainLayout.MaxValue;
      MinValue = MainLayout.MinValue;

      var maxDate = mainCandles.First().Time;

      if (IsLive)
      {
        MainLayout.MaxValue = (decimal)0.40;
        MainLayout.MinValue = (decimal)0.28;

        Strategy.LoadState();
        LoadLayouts(ada, MainLayout, mainCandles, maxDate, 0, mainCandles.Count - 0);
      }
      else
      {
        //Asset = new Asset()
        //{
        //  NativeRound = 8,
        //  PriceRound = 2
        //};

        //mainCandles = ParseTradingView(tradingView_btc_240m);

        //Strategy.Asset = Asset;


        Strategy.LoadState();
        LoadLayouts(ada, MainLayout, mainCandles, maxDate, 0, mainCandles.Count, true);
      
      }

    }

    #endregion

    #region LoadLAyouts

    private List<Candle> ActualCandles = new List<Candle>();
    private List<Layout> InnerLayouts = new List<Layout>();

    private async void LoadLayouts(IList<Tuple<string, TimeFrame>> layoutDatas,
      Layout mainLayout,
      IList<Candle> mainCandles,
      DateTime? maxTime = null,
      int skip = 0,
      int cut = 0,
      bool simulate = false)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, CanvasHeight, CanvasWidth, Asset);


      var cutCandles = mainCandles.TakeLast(cut).ToList();

      if (simulate)
      {
        ActualCandles = mainCandles.Skip(skip).SkipLast(cut).ToList();
      }
      else
      {
        ActualCandles = (await binanceBroker.GetCandles(Asset.Symbol, GetTimeSpanFromInterval(KlineInterval))).ToList();
        await binanceBroker.SubscribeToKlineInterval(OnBinanceKlineUpdate, KlineInterval);
      }

      foreach (var layoutData in layoutDatas)
      {
        var layout = CreateCtksChart(layoutData.Item1, layoutData.Item2, maxTime);

        InnerLayouts.Add(layout);
      }

      mainLayout.Ctks = mainCtks;
      Layouts.Add(mainLayout);
      Selected = mainLayout;


      if (ActualCandles.Count > 0)
      {
        RenderLayout(mainLayout, InnerLayouts, ActualCandles.Last(), ActualCandles);
      }

      if (simulate)
        Simulate(cutCandles, mainLayout, ActualCandles, InnerLayouts, 1);
    }


    private async void RecreateChart()
    {
      if (IsLive)
      {
        ActualCandles = (await binanceBroker.GetCandles(Asset.Symbol, GetTimeSpanFromInterval(KlineInterval))).ToList();
        await binanceBroker.SubscribeToKlineInterval(OnBinanceKlineUpdate, KlineInterval);

        if (ActualCandles.Count > 0)
        {
          RenderLayout(MainLayout, InnerLayouts, ActualCandles.Last(), ActualCandles);
        }
      }

    }

    #endregion

    private void OnActualCandleChange(Layout layout, List<Layout> secondaryLayouts, Candle candle, List<Candle> candles)
    {
      candles.Add(candle);

      RenderLayout(layout, secondaryLayouts, candle, candles);
    }

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

    #region RenderLayout

    private bool shouldUpdate = true;
    List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();

    public void RenderLayout(Layout layout, List<Layout> secondaryLayouts, Candle actual, List<Candle> candles)
    {
      foreach (var secondaryLayout in secondaryLayouts)
      {
        var lastCandle = secondaryLayout.Ctks.Candles.Last();
        if (actual.Time > lastCandle.Time)
        {
          var innerCandles = ParseTradingView(secondaryLayout.DataLocation, actual.Time, addNotClosedCandle: true);

          secondaryLayout.Ctks.CrateCtks(innerCandles, () => CreateChart(secondaryLayout, CanvasHeight, CanvasWidth, innerCandles));

          shouldUpdate = true;
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


      Strategy.ValidatePositions(actual, ctksIntersections);
      Strategy.CreatePositions(actual, ctksIntersections);

      RenderOverlay(layout, ctksIntersections, Strategy, candles);
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

      var ctks = new Ctks(layout, timeFrame, CanvasHeight, CanvasWidth, Asset);

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

      this.chart_image.Source = dImageSource;
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

      var text = GetFormattedText(closePrice.Value.ToString("N4"), brush);
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

    #region RaisePropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

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
          return TimeSpan.FromMinutes(10);
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
  }
}