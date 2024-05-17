using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Binance.Net.Enums;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using LiveCharts.Wpf.Charts.Base;
using Microsoft.Expression.Interactivity.Core;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF.ItemsCollections;
using PositionSide = CTKS_Chart.Strategy.PositionSide;

namespace CTKS_Chart.ViewModels
{

  public class DrawingViewModel : ViewModel, IDrawingViewModel
  {
    DashStyle pricesDashStyle = new DashStyle(new List<double>() { 2 }, 5);
    public decimal chartDiff = 0.01m;
    public DrawingViewModel(TradingBot tradingBot, Layout layout)
    {
      TradingBot = tradingBot;
      Layout = layout;
    }

    #region Properties

    public Layout Layout { get; }
    public TradingBot TradingBot { get; }
    public RxObservableCollection<RenderedIntesection> RenderedIntersections { get; } = new RxObservableCollection<RenderedIntesection>();

    #region DrawingSettings

    private DrawingSettings drawingSettings = new DrawingSettings();

    public DrawingSettings DrawingSettings
    {
      get { return drawingSettings; }
      set
      {
        if (value != drawingSettings && value != null)
        {
          drawingSettings = value;

          if (drawingSettings != null)
          {
            drawingSettings.RenderLayout = () => RenderOverlay();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DrawnChart

    private DrawnChart drawnChart;

    public DrawnChart DrawnChart
    {
      get { return drawnChart; }
      set
      {
        if (value != drawnChart)
        {
          drawnChart = value;
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

    #region MaxValue

    public decimal maxValue;

    public decimal MaxValue
    {
      get { return maxValue; }
      set
      {
        if (value != maxValue && value > minValue)
        {
          maxValue = value;

          LockChart = false;
          Layout.MaxValue = MaxValue;
          TradingBot.Asset.StartMaxPrice = MaxValue;

          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinValue

    public decimal minValue = (decimal)0.001;

    public decimal MinValue
    {
      get { return minValue; }
      set
      {
        if (value != minValue && value < maxValue)
        {
          minValue = value;
          LockChart = false;
          Layout.MinValue = MinValue;
          TradingBot.Asset.StartLowPrice = MinValue;

          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MaxUnix

    public long maxUnix;

    public long MaxUnix
    {
      get { return maxUnix; }
      set
      {
        if (value != maxUnix)
        {
          maxUnix = value;

          LockChart = false;
          Layout.MaxUnix = MaxUnix;
          TradingBot.Asset.StartMaxUnix = MaxUnix;
          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinUnix

    public long minUnix;

    public long MinUnix
    {
      get { return minUnix; }
      set
      {
        if (value != minUnix)
        {

          minUnix = value;

          LockChart = false;
          Layout.MinUnix = MinUnix;
          TradingBot.Asset.StartMinUnix = MinUnix;
          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }



    #endregion

    #region ActualCandles

    private List<Candle> actualCandles = new List<Candle>();

    public List<Candle> ActualCandles
    {
      get { return actualCandles; }
      set
      {
        if (value != actualCandles)
        {
          actualCandles = value;
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

    #region LockChart

    private bool lockChart;
    private decimal actualPriceChartViewDiff;
    IDisposable disposable;

    public bool LockChart
    {
      get { return lockChart; }
      set
      {
        if (value != lockChart)
        {
          lockChart = value;

          var candlesToRender = ActualCandles.ToList();
          candlesToRender = candlesToRender.Where(x => x.UnixTime + unixDiff >= MinUnix && x.UnixTime - unixDiff <= MaxUnix).ToList();

          if (candlesToRender.Count > 1)
          {
            var close = candlesToRender.SkipLast(1).Last().Close.Value;
            actualPriceChartViewDiff = (maxValue - minValue) / maxValue;
          }


          RenderOverlay();
          RaisePropertyChanged();
        }

        disposable?.Dispose();

        if (!value)
        {
          disposable = Observable.Timer(TimeSpan.FromMinutes(1)).ObserveOnDispatcher().Subscribe((x) => LockChart = true);
        }
      }
    }

    #endregion

    #region CanvasHeight

    private double canvasHeight = 500;

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

    #endregion

    public int InitialCandleCount
    {
      get
      {

        return 200;
      }
    }

    #region ResetChart

    protected ActionCommand resetChart;

    public ICommand ResetChart
    {
      get
      {
        return resetChart ??= new ActionCommand(OnRestChart);
      }
    }


    public void OnRestChart()
    {
      var viewCandles = ActualCandles.TakeLast(InitialCandleCount);

      ResetX(viewCandles);

      var candlesToRender = viewCandles.Where(x => x.UnixTime + unixDiff >= MinUnix && x.UnixTime - unixDiff <= MaxUnix).ToList();

      ResetY(candlesToRender);

      RenderOverlay();
    }

    #region ResetChartX

    protected ActionCommand resetChartX;

    public ICommand ResetChartX
    {
      get
      {
        return resetChartX ??= new ActionCommand(OnRestChartX);
      }
    }


    public void OnRestChartX()
    {
      var viewCandles = ActualCandles.TakeLast(InitialCandleCount);

      ResetX(viewCandles);
      RenderOverlay();
    }

    #endregion

    #region ResetChartY

    protected ActionCommand resetChartY;

    public ICommand ResetChartY
    {
      get
      {
        return resetChartY ??= new ActionCommand(OnRestChartY);
      }
    }


    public void OnRestChartY()
    {
      ResetY(DrawnChart.Candles.Select(x => x.Candle));

      RenderOverlay();
    }

    #endregion

    #region ResetY

    private void ResetY(IEnumerable<Candle> viewCandles)
    {
      var max = viewCandles.Max(x => x.High.Value);
      var min = viewCandles.Min(x => x.Low.Value);
      var diff = (max - min) / max;
      lastLockedCandle = viewCandles.Last();
      var lastClose = lastLockedCandle.Close.Value;

      maxValue = max * (1 + (diff * 0.15m));
      minValue = min * (1 - (diff * 0.15m));

      RaisePropertyChanged(nameof(MaxValue));
      RaisePropertyChanged(nameof(MinValue));
    }

    #endregion

    #region ResetX

    private void ResetX(IEnumerable<Candle> viewCandles)
    {
      maxUnix = viewCandles.Max(x => x.UnixTime) + (unixDiff * 30);
      minUnix = viewCandles.Min(x => x.UnixTime) + (unixDiff * 30);

      RaisePropertyChanged(nameof(MaxUnix));
      RaisePropertyChanged(nameof(MinUnix));
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

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
        {ColorPurpose.MAX_BUY_PRICE, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#9bff3d",
          Purpose = ColorPurpose.MAX_BUY_PRICE
        })},
        {ColorPurpose.MIN_SELL_PRICE, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#fc4e03",
          Purpose = ColorPurpose.MIN_SELL_PRICE
        })},
        {ColorPurpose.AUTOMATIC_BUY, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#88ff80",
          Purpose = ColorPurpose.AUTOMATIC_BUY
        })},
        {ColorPurpose.AUTOMATIC_SELL, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#ff8080",
          Purpose = ColorPurpose.AUTOMATIC_SELL
        })},
        {ColorPurpose.COMBINED_BUY, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#068204",
          Purpose = ColorPurpose.COMBINED_BUY
        })},
        {ColorPurpose.COMBINED_SELL, new ColorSettingViewModel(new ViewModels.ColorSetting()
        {
          Brush = "#820404",
          Purpose = ColorPurpose.COMBINED_SELL
        })},
      };

    }

    #endregion

    #region RenderOverlay

    private List<CtksIntersection> last = null;
    public long unixDiff;
    private Candle lastLockedCandle;
    private decimal? lastAth;
    private DateTime? lastFilledPosition;

    public new void RenderOverlay(decimal? athPrice = null)
    {
      Pen shapeOutlinePen = new Pen(Brushes.Transparent, 1);
      shapeOutlinePen.Freeze();

      DrawingGroup dGroup = new DrawingGroup();

      double imageHeight = CanvasHeight;
      double imageWidth = CanvasWidth;

      if (unixDiff == 0 && ActualCandles.Count > 1)
      {
        unixDiff = ActualCandles[1].UnixTime - ActualCandles[0].UnixTime;
      }

      if (lastLockedCandle == null && ActualCandles.Count > 0)
      {
        lastLockedCandle = ActualCandles.Last();
      }

      if (athPrice != null)
      {
        lastAth = athPrice;
      }


      DrawnChart newChart = null;
      using (DrawingContext dc = dGroup.Open())
      {
        dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(imageWidth, imageHeight));
        var candlesToRender = ActualCandles.ToList();

        candlesToRender = candlesToRender.Where(x => x.UnixTime + unixDiff >= MinUnix && x.UnixTime - unixDiff <= MaxUnix).ToList();

        if (candlesToRender.Count > 0 && TradingBot.Strategy != null)
        {
          var lastCandle = ActualCandles.LastOrDefault();

          if (actualPriceChartViewDiff == 0)
          {
            if (candlesToRender.Count > 1)
            {
              var close = candlesToRender.SkipLast(1).Last().Close.Value;
              actualPriceChartViewDiff = (maxValue - minValue) / maxValue;
            }
          }

          if (candlesToRender.Count > 1 && LockChart)
          {
            var low = lastCandle.Low.Value;
            var high = lastCandle.High.Value;

            var minView = minValue * (1 + actualPriceChartViewDiff * 0.20m);
            var maxView = maxValue * (1 - (actualPriceChartViewDiff * 0.20m));


            if (low < minView)
            {
              var diff = 1 - ((Math.Abs((minView - low) / minView)));

              maxValue = maxValue * diff;
              minValue = minValue * diff;
            }
            else if (high > maxView)
            {
              var diff = (Math.Abs((maxView - high) / maxView)) + 1;

              maxValue = maxValue * diff;
              minValue = minValue * diff;
            }


            RaisePropertyChanged(nameof(MaxValue));
            RaisePropertyChanged(nameof(MinValue));
          }

          if (LockChart)
          {
            if (MaxUnix < lastCandle?.UnixTime)
            {
              var viewCandles = ActualCandles.TakeLast(150);

              maxUnix = viewCandles.Max(x => x.UnixTime) + (unixDiff * 30);
              minUnix = viewCandles.Min(x => x.UnixTime) + (unixDiff * 30);
            }
            else if (lastLockedCandle?.OpenTime != lastCandle?.OpenTime)
            {
              maxUnix += unixDiff;
              minUnix += unixDiff;

              var lastCandleUnix = actualCandles.Last().UnixTime;

              if (lastCandleUnix > maxUnix)
              {
                var diffX = maxUnix - minUnix;
                maxUnix = lastCandleUnix + (long)(diffX * 0.3);
                minUnix = maxUnix - diffX;
              }

              lastLockedCandle = lastCandle;
            }

            RaisePropertyChanged(nameof(MaxUnix));
            RaisePropertyChanged(nameof(MinUnix));
          }


          var removed = RenderedIntersections.Where(x => !TradingBot.Strategy.Intersections.Any(y => y == x.Model)).ToList();
          removed.AddRange(RenderedIntersections.Where(x => x.Model.Value < MinValue || x.Model.Value > MaxValue));

          removed.ForEach(x => RenderedIntersections.Remove(x));

          if (lastFilledPosition != TradingBot.Strategy.AllClosedPositions.Max(x => x.FilledDate))
          {
            RenderedIntersections.Clear();
          }

          lastFilledPosition = TradingBot.Strategy.AllClosedPositions.Max(x => x.FilledDate);


          if (DrawingSettings.ShowIntersections)
          {
            DrawIntersections(dc, TradingBot.Strategy.Intersections,
                            imageHeight,
                            imageWidth,
                            TradingBot.Strategy.AllOpenedPositions.ToList());
          }
          else
          {
            RenderedIntersections.Clear();
          }

          if (DrawingSettings.ShowClusters)
          {

            DrawClusters(dc, TradingBot.Strategy.Intersections,
                                imageHeight,
                                imageWidth,
                                TradingBot.Strategy.AllOpenedPositions.ToList());
          }


          newChart = DrawChart(dc, candlesToRender, imageHeight, imageWidth);
          var chartCandles = newChart.Candles.ToList();

          DrawClosedPositions(dc, TradingBot.Strategy.AllClosedPositions, chartCandles, imageHeight);

          var maxCanvasValue = MaxValue;
          var minCanvasValue = MinValue;
          var chartDiff = (MaxValue - MinValue) * 0.03m;

          maxCanvasValue = MaxValue - chartDiff;
          minCanvasValue = MinValue + chartDiff;

          var lastPrice = lastCandle.Close;

          if (lastPrice < maxCanvasValue && lastPrice > minCanvasValue)
          {
            DrawActualPrice(dc, lastCandle, imageHeight, imageWidth);
          }

          if (TradingBot.Strategy is StrategyViewModel strategyViewModel)
          {
            decimal price = strategyViewModel.AvrageBuyPrice;

            if (DrawingSettings.ShowAveragePrice)
            {
              if (price < maxCanvasValue && price > minCanvasValue)
                DrawAveragePrice(dc, strategyViewModel.AvrageBuyPrice, imageHeight, imageWidth);
            }
          }

          if (DrawingSettings.ShowATH)
          {
            if (lastAth < maxCanvasValue && lastAth > minCanvasValue)
              DrawPriceToATH(dc, lastAth.Value, imageHeight, imageWidth);
          }


          if (TradingBot.Strategy.MaxBuyPrice < maxCanvasValue && TradingBot.Strategy.MaxBuyPrice > minCanvasValue)
            DrawMaxBuyPrice(dc, TradingBot.Strategy.MaxBuyPrice.Value, imageHeight, imageWidth);

          if (TradingBot.Strategy.MinSellPrice < maxCanvasValue && TradingBot.Strategy.MinSellPrice > minCanvasValue)
            DrawMinSellPrice(dc, TradingBot.Strategy.MinSellPrice.Value, imageHeight, imageWidth);

          DrawIndicators(dc);
        }

        Chart = new DrawingImage(dGroup);
        DrawnChart = newChart;
      }
    }

    #endregion

    #region DrawChart

    public DrawnChart DrawChart(
      DrawingContext drawingContext,
      IList<Candle> candles,
      double canvasHeight,
      double canvasWidth)
    {
      var drawnCandles = new List<ChartCandle>();
      long unix_diff = unixDiff;

      if (unixDiff < 604800)
        unix_diff = (long)(unixDiff * 0.60);
      else
        unix_diff = (long)((1 / 2.195) * 2 * unix_diff);

      var width = TradingHelper.GetCanvasValueLinear(canvasWidth, MinUnix + unix_diff, MaxUnix, MinUnix);

      if (candles.Any() && width > 0)
      {
        foreach (var candle in candles)
        {
          var close = TradingHelper.GetCanvasValue(canvasHeight, candle.Close.Value, MaxValue, MinValue);
          var open = TradingHelper.GetCanvasValue(canvasHeight, candle.Open.Value, MaxValue, MinValue);

          var high = TradingHelper.GetCanvasValue(canvasHeight, candle.High.Value, MaxValue, MinValue);
          var low = TradingHelper.GetCanvasValue(canvasHeight, candle.Low.Value, MaxValue, MinValue);

          var selectedBrush = candle.IsGreen ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);

          Pen pen = new Pen(selectedBrush, 0.5);

          var newCandle = new Rect()
          {
            Width = width,
          };

          if (close < 0)
          {
            close = 0;
          }

          double candleHeight = 0;

          if (candle.IsGreen)
          {
            if (open > 0)
              candleHeight = close - open;
            else
              candleHeight = close;
          }
          else
          {
            candleHeight = open - close;
          }

          if (candleHeight == 0 && close > 0)
          {
            candleHeight = 1;
          }

          if (candleHeight > 0)
          {
            newCandle.Height = candleHeight;
          }


          var x = TradingHelper.GetCanvasValueLinear(canvasWidth, candle.UnixTime, MaxUnix, MinUnix);

          newCandle.X = x - (width / 2);

          if (newCandle.X < 0)
          {
            var newWidth = newCandle.Width + newCandle.X;

            if (newWidth > 0)
            {
              newCandle.Width = newWidth;
            }
            else
            {
              newCandle.Width = 0;
            }

            newCandle.X = 0;
          }
          else if (newCandle.X + width > canvasWidth)
          {
            var newWidth = canvasWidth - newCandle.X;

            if (newWidth > 0)
            {
              newCandle.Width = newWidth;
            }
            else
            {
              newCandle.Width = 0;
            }
          }

          if (candle.IsGreen)
          {
            newCandle.Y = canvasHeight - close;
          }
          else
            newCandle.Y = canvasHeight - close - newCandle.Height;

          if (newCandle.Y < 0)
          {
            var newHeight = newCandle.Y + newCandle.Height;

            if (newHeight <= 0)
            {
              newHeight = 0;
            }

            newCandle.Height = newHeight;
            newCandle.Y = 0;
          }


          var wickTop = candle.IsGreen ? close : open;
          var wickBottom = candle.IsGreen ? open : close;


          var topY = canvasHeight - high;
          var bottomY = canvasHeight - wickBottom;

          Rect? topWick = null;
          Rect? bottomWick = null;

          if (x > 0 && x < canvasWidth)
          {
            if (high - wickTop > 0 && high > 0)
            {
              if (wickTop < 0)
              {
                wickTop = 0;
              }

              var topWickHeight = high - wickTop;

              if (topY < 0)
              {
                topWickHeight += topY;
                topY = 0;
              }
              if (topWickHeight > 0)
              {
                topWick = new Rect()
                {
                  Height = topWickHeight,
                  X = x,
                  Y = topY,
                };
              }
            }

            if (wickBottom - low > 0 && wickBottom > 0)
            {
              if (low < 0)
              {
                low = 0;
              }
              var bottomWickHeight = wickBottom - low;

              if (bottomY < 0)
              {
                bottomWickHeight += bottomY;
                bottomY = 0;
              }

              if (bottomWickHeight > 0)
              {
                bottomWick = new Rect()
                {
                  Height = bottomWickHeight,
                  X = x,
                  Y = bottomY,
                };
              }
            }
          }



          if (newCandle.Height > 0 && newCandle.Width > 0)
            drawingContext.DrawRectangle(selectedBrush, pen, newCandle);

          if (topWick != null)
            drawingContext.DrawRectangle(selectedBrush, pen, topWick.Value);

          if (bottomWick != null)
            drawingContext.DrawRectangle(selectedBrush, pen, bottomWick.Value);

          drawnCandles.Add(new ChartCandle()
          {
            Candle = candle,
            Body = newCandle,
            TopWick = topWick,
            BottomWick = bottomWick
          });
        }
      }

      return new DrawnChart()
      {
        Candles = drawnCandles
      };
    }

    #endregion

    #region DrawIntersections

    public void DrawIntersections(
      DrawingContext drawingContext,
      IEnumerable<CtksIntersection> intersections,
      double canvasHeight,
      double canvasWidth,
      IList<Position> allPositions = null,
      TimeFrame minTimeframe = TimeFrame.W1
      )
    {
      var diff = (MaxValue - MinValue) * chartDiff;

      var maxCanvasValue = MaxValue - diff;
      var minCanvasValue = MinValue + diff;

      var validIntersection = intersections
        .Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue && minTimeframe <= x.TimeFrame)
        .ToList();

      foreach (var intersection in validIntersection)
      {
        var selectedBrush = GetIntersectionBrush(allPositions, intersection);

        var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, MaxValue, MinValue);

        var frame = intersection.TimeFrame;

        var lineY = canvasHeight - actual;

        FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush);

        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;
        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));

        var rendered = RenderedIntersections.SingleOrDefault(x => x.Model == intersection);

        if (rendered == null)
          RenderedIntersections.Add(new RenderedIntesection(intersection) { SelectedBrush = selectedBrush });
        else
          rendered.SelectedBrush = selectedBrush;
      }
    }

    #endregion

    #region DrawClosedPositions

    public void DrawClosedPositions(
      DrawingContext drawingContext,
      IEnumerable<Position> positions,
      IList<ChartCandle> candles,
      double canvasHeight)
    {
      var minDate = DateTimeHelper.UnixTimeStampToUtcDateTime(MinUnix);
      var maxDate = DateTimeHelper.UnixTimeStampToUtcDateTime(MaxUnix);

      positions = positions.Where(x => x.FilledDate > minDate && x.FilledDate < maxDate).ToList();

      if (!DrawingSettings.ShowAutoPositions)
      {
        positions = positions.Where(x => !x.IsAutomatic);
      }

      if (!DrawingSettings.ShowManualPositions)
      {
        positions = positions.Where(x => x.IsAutomatic);
      }


      foreach (var position in positions)
      {
        var isActiveBuy = position.Side == PositionSide.Buy && position.State == PositionState.Filled;
        Brush selectedBrush = Brushes.Orange;

        if (position.Side == PositionSide.Buy)
        {
          if (position.IsAutomatic)
          {
            selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_BUY].Brush);
          }
          else
          {
            if (position.State == PositionState.Filled)
            {
              selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.ACTIVE_BUY].Brush);
            }
            else
            {
              selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.FILLED_BUY].Brush);
            }
          }
        }
        else
        {
          if (position.IsAutomatic)
          {
            selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_SELL].Brush);
          }
          else
          {
            selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.FILLED_SELL].Brush);
          }
        }


        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;

        var actual = TradingHelper.GetCanvasValue(canvasHeight, position.Price, MaxValue, MinValue);

        var frame = position.Intersection.TimeFrame;

        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        var positionY = canvasHeight - actual;
        var candle = candles.FirstOrDefault(x => x.Candle.OpenTime <= position.FilledDate && x.Candle.CloseTime >= position.FilledDate);

        if (candle != null)
        {
          var positionX = candle.Body.X - (candle.Body.Width + 5);
          var text = position.Side == PositionSide.Buy ? "B" : "S";
          var fontSize = isActiveBuy ? 25 : 9;

          if (position.IsAutomatic)
          {
            fontSize = (int)(fontSize / 1.5);
          }

          FormattedText formattedText = DrawingHelper.GetFormattedText(text, selectedBrush, fontSize);

          var point = new Point(positionX, positionY - formattedText.Height / 2);

          if (point.X > 0 && point.X < CanvasWidth && point.Y > 0 && point.Y < CanvasHeight)
            drawingContext.DrawText(formattedText, point);
        }
      }
    }

    #endregion

    #region DrawActualPrice

    public void DrawActualPrice(DrawingContext drawingContext, Candle lastCandle, double canvasHeight, double canvasWidth)
    {
      var price = lastCandle.Close;

      if (price > 0 && price > MinValue && price < MaxValue)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price.Value, MaxValue, MinValue);

        var lineY = canvasHeight - close;

        var brush = lastCandle.IsGreen ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);
        var pen = new Pen(brush, 1);

        var text = DrawingHelper.GetFormattedText(price.Value.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 20);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 25, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }
    }

    #endregion

    #region DrawAveragePrice

    public void DrawAveragePrice(DrawingContext drawingContext, decimal price, double canvasHeight, double canvasWidth)
    {
      if (price > 0 && price > MinValue && price < MaxValue)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price, MaxValue, MinValue);

        var lineY = canvasHeight - close;

        var brush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.AVERAGE_BUY].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = pricesDashStyle;

        var text = DrawingHelper.GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #region DrawPriceToATH

    public void DrawPriceToATH(DrawingContext drawingContext, decimal price, double canvasHeight, double canvasWidth)
    {
      if (price > 0 && price > MinValue && price < MaxValue)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price, MaxValue, MinValue);

        var lineY = canvasHeight - close;

        var brush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.ATH].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = pricesDashStyle;

        var text = DrawingHelper.GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);


        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #region DrawMaxBuyPrice

    public void DrawMaxBuyPrice(DrawingContext drawingContext, decimal price, double canvasHeight, double canvasWidth)
    {
      if (price > 0 && price > MinValue && price < MaxValue)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price, MaxValue, MinValue);

        var lineY = canvasHeight - close;

        var brush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.MAX_BUY_PRICE].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = pricesDashStyle;

        var text = DrawingHelper.GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #region DrawMaxBuyPrice

    public void DrawMinSellPrice(DrawingContext drawingContext, decimal price, double canvasHeight, double canvasWidth)
    {
      if (price > 0 && price > MinValue && price < MaxValue)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price, MaxValue, MinValue);

        var lineY = canvasHeight - close;

        var brush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.MIN_SELL_PRICE].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = pricesDashStyle;

        var text = DrawingHelper.GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #region DrawClusters

    public void DrawClusters(
      DrawingContext drawingContext,
      IEnumerable<CtksIntersection> intersections,
      double canvasHeight,
      double canvasWidth,
      IList<Position> allPositions = null,
      TimeFrame minTimeframe = TimeFrame.W1)
    {
      var diff = (MaxValue - MinValue) * chartDiff;

      var maxCanvasValue = MaxValue - diff;
      var minCanvasValue = MinValue + diff;

      var validIntersection = intersections
        .Where(x =>
                minTimeframe <= x.TimeFrame &&
                 x.Cluster != null &&
                 x.TimeFrame >= minTimeframe &&
                 x.Cluster.Intersections.Any()
                )
        .Select(x => new
        {
          minValue = x.Cluster.Intersections.Min(x => x.Value),
          maxValue = x.Cluster.Intersections.Max(x => x.Value),
          intersection = x
        })
        .Where(x => x.maxValue > minCanvasValue &&
                x.minValue < maxCanvasValue)
        .ToList();

      foreach (var actualIntersectionObject in validIntersection)
      {
        var intersection = actualIntersectionObject.intersection;

        var selectedBrush = GetIntersectionBrush(allPositions, intersection);
        var frame = intersection.TimeFrame;

        FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush);

        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;
        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        var maxValue = actualIntersectionObject.maxValue;
        var minValue = actualIntersectionObject.minValue;

        var max = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, maxValue, MaxValue, MinValue);
        var min = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, minValue, MaxValue, MinValue);

        var clusterRect = new Rect()
        {
          X = 0,
          Y = max,
          Height = min - max,
          Width = canvasWidth
        };

        if (canvasHeight < min)
        {
          clusterRect.Height = clusterRect.Height - (min - canvasHeight);
        }

        if (max < 0)
        {
          clusterRect.Y = 0;
          clusterRect.Height += max;
        }


        selectedBrush.Opacity = 0.20;
        drawingContext.DrawRectangle(selectedBrush, pen, clusterRect);



        var rendered = RenderedIntersections.SingleOrDefault(x => x.Model == intersection);

        var clone = selectedBrush.Clone();
        clone.Opacity = 1;

        if (rendered == null)
          RenderedIntersections.Add(new RenderedIntesection(intersection) { SelectedBrush = clone });
        else
          rendered.SelectedBrush = clone;

      }
    }

    #endregion

    #region GetIntersectionBrush

    private Brush GetIntersectionBrush(IList<Position> allPositions, CtksIntersection intersection)
    {
      Brush selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);

      var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, MaxValue, MinValue);

      var lineY = canvasHeight - actual;

      if (!intersection.IsEnabled)
      {
        selectedBrush = DrawingHelper.GetBrushFromHex("#614c4c");
        selectedBrush.Opacity = 0.25;
      }

      if (allPositions != null)
      {
        var positionsOnIntersesction = allPositions
        .Where(x => x.Intersection.IsSame(intersection))
        .ToList();

        var firstPositionsOnIntersesction = positionsOnIntersesction.FirstOrDefault();
        var isOnlyAuto = positionsOnIntersesction.All(x => x.IsAutomatic);
        var isCombined = positionsOnIntersesction.Any(x => x.IsAutomatic) && positionsOnIntersesction.Any(x => !x.IsAutomatic);

        if (firstPositionsOnIntersesction != null)
        {
          selectedBrush =
            firstPositionsOnIntersesction.Side == PositionSide.Buy ?
                isOnlyAuto ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_BUY].Brush) :
                isCombined ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.COMBINED_BUY].Brush) :
                DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.BUY].Brush) :

                isOnlyAuto ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_SELL].Brush) :
                isCombined ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.COMBINED_SELL].Brush) :
                DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.SELL].Brush);
        }
      }


      return selectedBrush;
    }

    #endregion

    public void DrawIndicators(DrawingContext drawingContext)
    {
      //var lastCandle = Layout.;
      //var price = lastCandle.Close;

      //if (price > 0 && price > MinValue && price < MaxValue)
      //{
      //  var close = TradingHelper.GetCanvasValue(canvasHeight, price.Value, MaxValue, MinValue);

      //  var lineY = canvasHeight - close;

      //  var brush = lastCandle.IsGreen ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);
      //  var pen = new Pen(brush, 1);
      //  pen.DashStyle = DashStyles.Dash;

      //  var text = DrawingHelper.GetFormattedText(price.Value.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 20);
      //  drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 25, lineY - text.Height - 5));
      //  drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      //}
    }

    #endregion
  }
}