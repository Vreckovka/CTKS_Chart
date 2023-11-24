﻿using System;
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
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Misc;
#pragma warning disable 618

namespace CTKS_Chart
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    public ObservableCollection<Layout> Layouts { get; set; } = new ObservableCollection<Layout>();

    public Strategy Strategy { get; set; } = new Strategy();

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
          OnPropertyChanged();
        }
      }
    }

    #endregion


    //double maxValue = 0.40;
    //double minValue = 0.22;

    public double CanvasHeight { get; set; } = 1000;
    public double CanvasWidth { get; set; } = 1000;

    public MainWindow()
    {
      VSynchronizationContext.UISynchronizationContext = SynchronizationContext.Current;
      VSynchronizationContext.UIDispatcher = Application.Current.Dispatcher;

      InitializeComponent();
      DataContext = this;

      ForexChart_Loaded();
    }


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


      var location = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\Data";

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
      };


      //LoadLayouts(spy, new Tuple<string, TimeFrame>(tradingView_spy_1D, TimeFrame.D1), mainCanvas, 900, 100);
      //LoadLayouts(btc, new Tuple<string, TimeFrame>(tradingView_btc_720m, TimeFrame.D1), mainCanvas, 0, 0);

      var main = new Layout()
      {
        Title = "Main",
        TimeFrame = TimeFrame.D1
      };

      var mainCandles = ParseTradingView(tradingView_btc_240m);
      
      main.MaxValue = mainCandles.Max(x => x.High);
      main.MinValue = 25000;

      var maxDate = mainCandles.First().Time;

      LoadLayouts(btc, main, mainCandles, maxDate, 0, mainCandles.Count);

    }

    #endregion

    #region LoadLAyouts

    private void LoadLayouts(IList<Tuple<string, TimeFrame>> layoutDatas,
      Layout mainLayout,
      IList<Candle> mainCandles,
      DateTime? maxTime = null,
      int skip = 0,
      int cut = 0)
    {
      var ctks = new Ctks(mainLayout, mainLayout.TimeFrame, CanvasHeight, CanvasWidth);

      var cutCandles = mainCandles.TakeLast(cut).ToList();
      var candles = mainCandles.Skip(skip).SkipLast(cut).ToList();

      var innerLayouts = new List<Layout>();

      foreach (var layoutData in layoutDatas)
      {
        var layout = CreateCtksChart(layoutData.Item1, layoutData.Item2, maxTime);

        innerLayouts.Add(layout);
      }

      mainLayout.Ctks = ctks;
      Layouts.Add(mainLayout);
      Selected = mainLayout;


      Simulate(cutCandles, mainLayout, candles, innerLayouts, 15);
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
      Task.Run(async () =>
      {
        bool shouldUpdate = true;
        List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();

        for (int i = 0; i < cutCandles.Count; i++)
        {
          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            var acutal = cutCandles[i];
            candles.Add(acutal);

            foreach (var secondaryLayout in secondaryLayouts)
            {
              var lastCandle = secondaryLayout.Ctks.Candles.Last();
              if (acutal.Time > lastCandle.Time)
              {
                var innerCandles = ParseTradingView(secondaryLayout.DataLocation, acutal.Time, addNotClosedCandle: true);

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
                  .Where(x => x.Value < layout.MaxValue * 2 && x.Value > layout.MinValue * (decimal)0.5).ToList();

                ctksIntersections.AddRange(validIntersections);
              }

              ctksIntersections = ctksIntersections.OrderByDescending(x => x.Value).ToList();
              shouldUpdate = false;
            }
         

            Strategy.ValidatePositions(acutal.High, acutal.Low, acutal.Close, ctksIntersections);
            Strategy.CreatePositions(acutal.Low, acutal.Close, ctksIntersections);

            RenderOverlay(layout, ctksIntersections, Strategy, candles);
          });

          await Task.Delay(delay);
        }
      });
    }

    #endregion

    #region CreateCtksChart

    private Layout CreateCtksChart(string location, TimeFrame timeFrame,  DateTime? maxTime = null)
    {
      var candles = ParseTradingView(location, maxTime);
      
      var canvas = new Canvas();
      canvas.MouseMove += Canvas_MouseMove;

      var layout = new Layout()
      {
        Title = timeFrame.ToString(),
        Canvas = canvas,
        MaxValue = candles.Max(x => x.High),
        MinValue = candles.Min(x => x.Low),
        TimeFrame = timeFrame,
        DataLocation = location,
      };

      canvas.Width = CanvasWidth;
      canvas.Height = CanvasHeight;

      var ctks = new Ctks(layout, timeFrame, CanvasHeight, CanvasWidth);

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
          var valueForCanvas = GetCanvasValue(canvasHeight, point.Close, layout.MaxValue, layout.MinValue);
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
            GetCanvasValue(canvasHeight, candles[i - 1].Close, layout.MaxValue, layout.MinValue) :
            GetCanvasValue(canvasHeight, candles[i].Open, layout.MaxValue, layout.MinValue);

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

    private void DrawChart(DrawingContext drawingContext, Layout layout, IList<Candle> candles, int maxCount = 150)
    {
      var canvasWidth = CanvasWidth * 0.85 - 150;

      var skip = candles.Count - maxCount > 0 ? candles.Count - maxCount : 0;

      var width = canvasWidth / maxCount;
      var margin = width * 0.95;

      if (candles.Any())
      {
        int y = 0;
        for (int i = skip; i < candles.Count; i++)
        {
          var point = candles[i];

          var close = GetCanvasValue(CanvasHeight, point.Close, layout.MaxValue, layout.MinValue);
          var open = GetCanvasValue(CanvasHeight, point.Open, layout.MaxValue, layout.MinValue);

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var selectedBrush = green ? Brushes.Green : Brushes.Red;

          Pen pen = new Pen(selectedBrush, 2);
          Pen wickPen = new Pen(selectedBrush, 0.5);

          var newCandle = new Rect()
          {
            Width = width - margin,
          };

          var lastClose = i > 0 ?
            GetCanvasValue(CanvasHeight, candles[i - 1].Close, layout.MaxValue, layout.MinValue) :
            GetCanvasValue(CanvasHeight, candles[i].Open, layout.MaxValue, layout.MinValue);

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


          var topWickCanvas = GetCanvasValue(CanvasHeight, candles[i].High, layout.MaxValue, layout.MinValue);
          var bottomWickCanvas = GetCanvasValue(CanvasHeight, candles[i].Low, layout.MaxValue, layout.MinValue);

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
        }
      }
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

    #region Canvas_MouseMove

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
      //var canvas = sender as Canvas;
      //var mosue = Mouse.GetPosition(canvas);

      //Canvas.SetLeft(coordinates, mosue.X + 20);
      //Canvas.SetTop(coordinates, mosue.Y - 10);

      //var canvasValue = mosue.Y;

      //coordinates.Text = $"{GetValueFromCanvas(CanvasHeight, canvas.ActualHeight - canvasValue).ToString("N2")}";
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

        RenderIntersections(dc, layout, ctksIntersections, strategy.AllOpenedPositions.ToList());

        DrawChart(dc, layout, candles);
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

      var close = GetCanvasValue(CanvasHeight, closePrice, layout.MaxValue, layout.MinValue);

      var lineY = CanvasHeight - close;

      var brush = Brushes.Yellow;
      var pen = new Pen(brush, 1);
      pen.DashStyle = DashStyles.Dash;

      var text = GetFormattedText(closePrice.ToString("N4"), brush);
      drawingContext.DrawText(text, new Point(CanvasWidth - text.Width - 25, lineY - text.Height - 5));
      drawingContext.DrawLine(pen, new Point(0, lineY), new Point(CanvasWidth, lineY));
    }

    #endregion

    #region RenderPositions

    public void RenderPositions(
      DrawingContext drawingContext,
      Layout layout,
      List<CtksIntersection> ctksIntersections,
      IList<Position> allPositions)
    {
      var renderedPositions = new List<CtksIntersection>();

      var maxCanvasValue = (decimal)GetValueFromCanvas(CanvasHeight, CanvasHeight, layout.MaxValue, layout.MinValue);
      var minCanvasValue = (decimal)GetValueFromCanvas(CanvasHeight, 0, layout.MaxValue, layout.MinValue);

      var valid = ctksIntersections.Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue);

      foreach (var intersection in valid)
      {
        var positionsOnIntersesction = allPositions
       .Where(x => x.Intersection?.Id == intersection.Id )
       .ToList();

        var firstPositionsOnIntersesction = positionsOnIntersesction.FirstOrDefault();
        var sum = positionsOnIntersesction.Sum(x => x.PositionSize);

        if (firstPositionsOnIntersesction != null && !renderedPositions.Contains(intersection))
        {
          var selectedBrush = firstPositionsOnIntersesction.Side == PositionSide.Buy ? Brushes.Green : Brushes.Red;

          Pen pen = new Pen(selectedBrush, 2);
          pen.DashStyle = DashStyles.Dash;

          var frame = intersection.TimeFrame;

          pen.Thickness = GetPositionThickness(frame);

          //target.StrokeDashArray = new DoubleCollection() { 1, 1 };

          var actual = GetCanvasValue(CanvasHeight, intersection.Value, layout.MaxValue, layout.MinValue);
          var lineY = CanvasHeight - actual;


          drawingContext.DrawText(GetFormattedText(sum.ToString("N4"), selectedBrush), new Point(50, lineY));
          drawingContext.DrawLine(pen, new Point(150, lineY), new Point(CanvasWidth, lineY));
          renderedPositions.Add(intersection);
        }
      }
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
          return 1;
      }
    }

    #endregion

    #region OnPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #endregion
  }

}