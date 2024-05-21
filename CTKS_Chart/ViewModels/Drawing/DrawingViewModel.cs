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
  public enum IndicatorType
  {
    RangeFilter,
  }

  public class IndicatorSettings : ViewModel
  {
    #region Show

    private bool show = true;

    public bool Show
    {
      get { return show; }
      set
      {
        if (value != show)
        {
          show = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TimeFrame

    private TimeFrame timeFrame;

    public TimeFrame TimeFrame
    {
      get { return timeFrame; }
      set
      {
        if (value != timeFrame)
        {
          timeFrame = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Name

    private string name;

    public string Name
    {
      get { return name; }
      set
      {
        if (value != name)
        {
          name = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IndicatorType

    private IndicatorType indicatorType;

    public IndicatorType IndicatorType
    {
      get { return indicatorType; }
      set
      {
        if (value != indicatorType)
        {
          indicatorType = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }

  public class DrawingViewModel<TPosition, TStrategy> : ViewModel, IDrawingViewModel
    where TPosition : Position, new()
    where TStrategy : BaseStrategy<TPosition>
  {
    DashStyle pricesDashStyle = new DashStyle(new List<double>() { 2 }, 5);
    public decimal chartDiff = 0.01m;
    public DrawingViewModel(BaseTradingBot<TPosition, TStrategy> tradingBot, Layout layout)
    {
      TradingBot = tradingBot;
      Layout = layout;

      drawingSettings = new DrawingSettings();
      drawingSettings.RenderLayout = () => RenderOverlay();
    }

    #region Properties

    public Layout Layout { get; }
    public BaseTradingBot<TPosition, TStrategy> TradingBot { get; }
    public RxObservableCollection<RenderedIntesection> RenderedIntersections { get; } = new RxObservableCollection<RenderedIntesection>();
    public RxObservableCollection<DrawingRenderedLabel> RenderedLabels { get; } = new RxObservableCollection<DrawingRenderedLabel>();

    #region DrawingSettings

    private DrawingSettings drawingSettings;

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

    private decimal maxValue;

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

          RenderOverlay();
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
          minValue = value;
          LockChart = false;
          Layout.MinValue = MinValue;

          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MaxUnix

    private long maxUnix;

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

          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinUnix

    private long minUnix;

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

    #region IsActualCandleVisible

    public bool IsActualCandleVisible
    {
      get
      {
        var lastRendered = drawnChart?.Candles.LastOrDefault();

        if (lastRendered != null)
        {
          return lastRendered.Candle.OpenTime == actual?.OpenTime
            && lastRendered.Candle.High > MinValue && lastRendered.Candle.Low < MaxValue;
        }


        return false;

      }
    }

    #endregion

    public RxObservableCollection<IndicatorSettings> IndicatorSettings { get; } = new RxObservableCollection<IndicatorSettings>();

    #endregion

    #region InitialCandleCount

    public int InitialCandleCount
    {
      get
      {

        return 200;
      }
    }


    #endregion

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
      var viewCandles = ActualCandles.TakeLast(unixDiff == 1 ? 500 : InitialCandleCount);
      lockChart = false;

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
      if (viewCandles.Count() == 0)
      {
        viewCandles = ActualCandles;
      }

      if (viewCandles.Any())
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
    }

    #endregion

    #region ResetX

    private void ResetX(IEnumerable<Candle> viewCandles)
    {
      if (viewCandles.Count() == 0)
      {
        viewCandles = ActualCandles;
      }

      if (viewCandles.Any())
      {
        var max = viewCandles.Max(x => x.UnixTime);
        var min = viewCandles.Min(x => x.UnixTime);
        var diff = (max - min) / (double)max;

        var padding = (long)(diff * 0.30 * max);

        maxUnix = max + padding;
        minUnix = min;

        RaisePropertyChanged(nameof(MaxUnix));
        RaisePropertyChanged(nameof(MinUnix));
      }
    }

    #endregion

    #endregion

    public bool EnableAutoLock { get; set; }

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


      IndicatorSettings.ItemUpdated.ObserveOnDispatcher().Subscribe(x => RenderOverlay());
    }

    #endregion


    public void Raise(string name)
    {
      RaisePropertyChanged(name);
    }

    #region RenderOverlay

    private List<CtksIntersection> last = null;
    public long unixDiff;
    private Candle lastLockedCandle;
    private decimal? lastAth;
    private DateTime? lastFilledPosition;
    private Candle actual;

    public virtual void RenderOverlay(decimal? athPrice = null, Candle actual = null)
    {
      if (actual != null)
        this.actual = actual;

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
      Candle lastCandle = this.actual;

      using (DrawingContext dc = dGroup.Open())
      {
        dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(imageWidth, imageHeight));
        var candlesToRender = ActualCandles.ToList();

        candlesToRender = candlesToRender.Where(x => x.UnixTime + unixDiff >= MinUnix && x.UnixTime - unixDiff <= MaxUnix).ToList();

        if (candlesToRender.Count > 0 && TradingBot.Strategy != null)
        {
          lastCandle = ActualCandles.LastOrDefault();

          actualPriceChartViewDiff = (maxValue - minValue) / maxValue;

   
          if (candlesToRender.Count > 1 && LockChart)
          {
            var low = lastCandle.Low.Value;
            var high = lastCandle.High.Value;

            var minView = minValue * (1 + (actualPriceChartViewDiff * 0.12m));
            var maxView = maxValue * (1 - (actualPriceChartViewDiff * 0.12m));


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
        }

        RenderIntersections(dc);
        DrawIndicators(dc);

        newChart = DrawChart(dc, candlesToRender, imageHeight, imageWidth);
        var chartCandles = newChart.Candles.ToList();

        DrawClosedPositions(dc, TradingBot.Strategy.AllClosedPositions, chartCandles, imageHeight);

        var maxCanvasValue = MaxValue;
        var minCanvasValue = MinValue;
        var chartDiff = (MaxValue - MinValue) * 0.03m;

        maxCanvasValue = MaxValue - chartDiff;
        minCanvasValue = MinValue + chartDiff;

        if (lastCandle != null)
        {
          var lastPrice = lastCandle.Close;

          DrawActualPrice(dc, lastCandle, imageHeight);
        }


        if (TradingBot.Strategy is StrategyViewModel strategyViewModel)
        {
          decimal price = strategyViewModel.AvrageBuyPrice;

          if (DrawingSettings.ShowAveragePrice)
          {
            DrawAveragePrice(dc, strategyViewModel.AvrageBuyPrice, imageHeight);
          }
          else
          {
            RenderedLabels.Remove(RenderedLabels.SingleOrDefault(x => x.Tag == "average_price"));
          }
        }

        if (DrawingSettings.ShowATH)
        {
          DrawPriceToATH(dc, lastAth, imageHeight);
        }
        else
        {
          RenderedLabels.Remove(RenderedLabels.SingleOrDefault(x => x.Tag == "ath_price"));
        }


        DrawMaxBuyPrice(dc, TradingBot.Strategy.MaxBuyPrice, imageHeight);
        DrawMinSellPrice(dc, TradingBot.Strategy.MinSellPrice, imageHeight);

      }

      Chart = new DrawingImage(dGroup);
      DrawnChart = newChart;


      if (IsActualCandleVisible && EnableAutoLock)
      {
        lockChart = true;
        RaisePropertyChanged(nameof(LockChart));
      }
    }

    #endregion

    #region RenderIntersections

    public void RenderIntersections(DrawingContext dc)
    {
      var removed = RenderedIntersections.Where(x => !TradingBot.Strategy.Intersections.Any(y => y == x.Model)).ToList();
      removed.AddRange(RenderedIntersections.Where(x => x.Model.Cluster == null)
        .Where(x => x.Model.Value < MinValue || x.Model.Value > MaxValue));

      removed.ForEach(x => RenderedIntersections.Remove(x));

      if (lastFilledPosition != TradingBot.Strategy.AllClosedPositions.Max(x => x.FilledDate))
      {
        RenderedIntersections.Clear();
      }

      lastFilledPosition = TradingBot.Strategy.AllClosedPositions.Max(x => x.FilledDate);


      if (DrawingSettings.ShowIntersections)
      {
        DrawIntersections(dc, TradingBot.Strategy.Intersections,
                        CanvasHeight,
                        CanvasWidth,
                        TradingBot.Strategy.AllOpenedPositions.ToList());
      }
      else
      {
        RenderedIntersections.Clear();
      }

      removed.AddRange(RenderedIntersections.Where(x => x.Model.Cluster != null)
              .Where(x => !(x.Max > MinValue &&
                          x.Min < MaxValue)));

      removed.ForEach(x => RenderedIntersections.Remove(x));

      if (DrawingSettings.ShowClusters)
      {

        DrawClusters(dc, TradingBot.Strategy.Intersections,
                            CanvasHeight,
                            CanvasWidth,
                            TradingBot.Strategy.AllOpenedPositions.ToList());
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
        unix_diff = (long)(unixDiff * 0.75);
      else
        unix_diff = (long)((1 / 2.195) * 2 * unix_diff);

      if (unix_diff <= 0)
      {
        unix_diff = 1;
      }

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

          bool rendered = false;

          if (newCandle.Height > 0 && newCandle.Width > 0)
          {
            if (unix_diff > 1)
              drawingContext.DrawRectangle(selectedBrush, null, newCandle);
            else
              drawingContext.DrawRectangle(selectedBrush, pen, newCandle);

            rendered = true;
          }

          if (topWick != null)
          {
            drawingContext.DrawLine(pen, new Point(x, newCandle.Top), new Point(x, topWick.Value.Top));
            rendered = true;
          }
          if (bottomWick != null)
          {
            drawingContext.DrawLine(pen, new Point(x, newCandle.Top + newCandle.Height), new Point(x, bottomWick.Value.Top + bottomWick.Value.Height));
            rendered = true;
          }

          if (rendered)
          {
            drawnCandles.Add(new ChartCandle()
            {
              Candle = candle,
              Body = newCandle,
              TopWick = topWick,
              BottomWick = bottomWick
            });
          }

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
      IList<TPosition> allPositions = null,
      TimeFrame minTimeframe = TimeFrame.W1
      )
    {
      var diff = (MaxValue - MinValue) * chartDiff;

      var maxCanvasValue = MaxValue;
      var minCanvasValue = MinValue;

      var validIntersection = intersections
        .Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue && minTimeframe <= x.TimeFrame)
        .ToList();

      foreach (var intersection in validIntersection)
      {
        var selectedBrush = GetIntersectionBrush(allPositions, intersection);

        var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, MaxValue, MinValue);

        var frame = intersection.TimeFrame;

        var lineY = canvasHeight - actual;


        FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush.Item2);

        Pen pen = new Pen(selectedBrush.Item2, 1);
        pen.DashStyle = DashStyles.Dash;
        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));

        var rendered = RenderedIntersections.SingleOrDefault(x => x.Model == intersection);

        if (rendered == null)
          RenderedIntersections.Add(new RenderedIntesection(intersection) { SelectedHex = selectedBrush.Item1, Brush = selectedBrush.Item2 });
        else
        {
          rendered.SelectedHex = selectedBrush.Item1;
          var brush = selectedBrush.Item2.Clone();
          brush.Opacity = 100;

          rendered.Brush = brush;
        }
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
          var text = position.Side == PositionSide.Buy ? "B" : "S";
          var fontSize = isActiveBuy ? 16 : 8;
          FormattedText formattedText = DrawingHelper.GetFormattedText(text, selectedBrush, fontSize);

          if (position.IsAutomatic)
          {
            fontSize = (int)(fontSize / 1.33);
          }

          var positionX = candle.Body.X - (formattedText.Width / 2);

          var point = new Point(positionX, positionY - formattedText.Height / 2);

          if (point.X > 0 && point.X < CanvasWidth && point.Y > 0 && point.Y < CanvasHeight)
            drawingContext.DrawText(formattedText, point);
        }
      }
    }

    #endregion

    #region DrawActualPrice

    public void DrawActualPrice(DrawingContext drawingContext, Candle lastCandle, double canvasHeight)
    {
      var brush = lastCandle.IsGreen ? ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush : ColorScheme.ColorSettings[ColorPurpose.RED].Brush;

      DrawPrice(drawingContext, lastCandle.Close, "actual_price", brush, canvasHeight);
    }

    #endregion

    #region DrawAveragePrice

    public void DrawAveragePrice(DrawingContext drawingContext, decimal price, double canvasHeight)
    {
      DrawPrice(drawingContext, price, "average_price", ColorScheme.ColorSettings[ColorPurpose.AVERAGE_BUY].Brush, canvasHeight);
    }

    #endregion

    #region DrawPriceToATH

    public void DrawPriceToATH(DrawingContext drawingContext, decimal? price, double canvasHeight)
    {
      DrawPrice(drawingContext, price, "ath_price", ColorScheme.ColorSettings[ColorPurpose.ATH].Brush, canvasHeight);
    }

    #endregion

    #region DrawMaxBuyPrice

    public void DrawMaxBuyPrice(DrawingContext drawingContext, decimal? price, double canvasHeight)
    {
      DrawPrice(drawingContext, price, "max_buy", ColorScheme.ColorSettings[ColorPurpose.MAX_BUY_PRICE].Brush, canvasHeight);
    }

    #endregion

    #region DrawMinSellPrice

    public void DrawMinSellPrice(DrawingContext drawingContext, decimal? price, double canvasHeight)
    {
      DrawPrice(drawingContext, price, "min_sell", ColorScheme.ColorSettings[ColorPurpose.MIN_SELL_PRICE].Brush, canvasHeight);
    }

    #endregion

    #region DrawClusters

    public void DrawClusters(
      DrawingContext drawingContext,
      IEnumerable<CtksIntersection> intersections,
      double canvasHeight,
      double canvasWidth,
      IList<TPosition> allPositions = null,
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

        FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush.Item2);

        Pen pen = new Pen(selectedBrush.Item2, 1);
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


        selectedBrush.Item2.Opacity = 0.20;
        drawingContext.DrawRectangle(selectedBrush.Item2, null, clusterRect);

        var rendered = RenderedIntersections.SingleOrDefault(x => x.Model == intersection);
        var clone = selectedBrush.Item2.Clone();

        clone.Opacity = 1;

        if (rendered == null)
          RenderedIntersections.Add(new RenderedIntesection(intersection)
          {
            SelectedHex = selectedBrush.Item1,
            Brush = clone,
            Min = minValue,
            Max =
            maxValue
          });
        else
        {
          rendered.SelectedHex = selectedBrush.Item1;
          var brush = selectedBrush.Item2.Clone();
          brush.Opacity = 100;

          rendered.Brush = brush;
        }
         

      }
    }

    #endregion

    #region GetIntersectionBrush

    private Tuple<string, Brush> GetIntersectionBrush(IList<TPosition> allPositions, CtksIntersection intersection)
    {
      string selectedHex = ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush;

      var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, MaxValue, MinValue);

      var lineY = canvasHeight - actual;

      if (!intersection.IsEnabled)
      {
        selectedHex = "#614c4c";
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
          selectedHex =
            firstPositionsOnIntersesction.Side == PositionSide.Buy ?
                isOnlyAuto ? ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_BUY].Brush :
                isCombined ? ColorScheme.ColorSettings[ColorPurpose.COMBINED_BUY].Brush :
               ColorScheme.ColorSettings[ColorPurpose.BUY].Brush :

                isOnlyAuto ? ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_SELL].Brush :
                isCombined ? ColorScheme.ColorSettings[ColorPurpose.COMBINED_SELL].Brush :
                ColorScheme.ColorSettings[ColorPurpose.SELL].Brush;
        }
      }

      var selectedBrush = DrawingHelper.GetBrushFromHex(selectedHex);
      if (!intersection.IsEnabled)
      {
        selectedBrush.Opacity = 0.25;
      }

      return new Tuple<string, Brush>(selectedHex, selectedBrush);
    }

    #endregion

    #region DrawPrice

    public void DrawPrice(DrawingContext drawingContext, decimal? price, string tag, string brush, double canvasHeight)
    {
      if (price > 0 && price > MinValue && price < MaxValue)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price.Value, MaxValue, MinValue);
        var selectedBrush = DrawingHelper.GetBrushFromHex(brush);

        var lineY = canvasHeight - close;

        var pen = new Pen(selectedBrush, 1);

        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));

        var text = price.Value.ToString($"N{TradingBot.Asset.PriceRound}");

        var existing = RenderedLabels.SingleOrDefault(x => x.Tag == tag);

        if (existing != null)
        {
          existing.Model = text;
          existing.SelectedHex = brush;
          existing.Brush = selectedBrush;
          existing.Price = price.Value;
        }
        else
        {
          RenderedLabels.Add(new DrawingRenderedLabel(text) { SelectedHex = brush, Price = price.Value, Tag = tag, Brush = selectedBrush });
        }
      }
      else
      {
        var existing = RenderedLabels.SingleOrDefault(x => x.Tag == tag);

        if (existing != null)
        {
          RenderedLabels.Remove(existing);
        }
      }
    }

    #endregion

    #region DrawIndicators

    public void DrawIndicators(DrawingContext drawingContext)
    {
      foreach (var indicatorSettings in IndicatorSettings.Where(x => x.Show))
      {
        if (TradingViewHelper.LoadedData.TryGetValue(indicatorSettings.TimeFrame, out var candles))
        {
          candles = candles.Where(x => x.IndicatorData.RangeFilterData.RangeFilter > 0).ToList();

          if (candles.Count > 1)
          {
            var diff = candles[1].UnixTime - candles[0].UnixTime;

            var equivalentDataCandles = candles
              .Where(x => x.UnixTime >= MinUnix - diff &&
              ((DateTimeOffset)x.CloseTime).ToUnixTimeSeconds() <= MaxUnix + diff).ToList();

            if (equivalentDataCandles != null && equivalentDataCandles.Count > 0)
            {
              DrawRangeFilter(drawingContext, equivalentDataCandles, diff);
            }
          }
        }
      }
    }

    #endregion

    #region DrawRangeFilter

    public void DrawRangeFilter(DrawingContext drawingContext, IList<Candle> validCandles, long diff)
    {
      var last = validCandles.Last();

      foreach (var candle in validCandles)
      {
        var indicatorData = candle.IndicatorData.RangeFilterData;

        var selectedBrush = indicatorData.Upward ? DrawingHelper.GetBrushFromHex("#80eb34") : DrawingHelper.GetBrushFromHex("#eb4034");
        var max = CanvasHeight - TradingHelper.GetCanvasValue(CanvasHeight, indicatorData.HighTarget, MaxValue, MinValue);
        var min = CanvasHeight - TradingHelper.GetCanvasValue(CanvasHeight, indicatorData.LowTarget, MaxValue, MinValue);
        var filterY = CanvasHeight - TradingHelper.GetCanvasValue(CanvasHeight, indicatorData.RangeFilter, MaxValue, MinValue);

        var pen = new Pen(selectedBrush.Clone(), 1);
        var range_pen = new Pen(selectedBrush.Clone(), 5);


        selectedBrush.Opacity = 0.10;
        var start_x = TradingHelper.GetCanvasValueLinear(canvasWidth, candle.UnixTime, MaxUnix, MinUnix);
        var end_x = TradingHelper.GetCanvasValueLinear(canvasWidth, candle.UnixTime + diff, MaxUnix, MinUnix);


        var indicatorRect = new Rect()
        {
          X = start_x,
          Y = max,
          Height = min - max,
          Width = end_x - start_x
        };

        if (indicatorRect.Width + start_x > 0 &&
          indicatorRect.Height + max > 0 &&
          indicatorRect.Height + (CanvasHeight - min) > 0)
        {
          if (start_x < 0)
          {
            indicatorRect.X = 0;
            indicatorRect.Width += start_x;
          }

          if (end_x > canvasWidth)
          {
            indicatorRect.Width += canvasWidth - end_x;
          }

          if (start_x + indicatorRect.Width > canvasWidth)
          {
            indicatorRect.Width -= start_x + indicatorRect.Width - canvasWidth;
          }

          if (CanvasHeight < min)
          {
            indicatorRect.Height += (CanvasHeight - min);
          }

          if (max < 0)
          {
            indicatorRect.Y = 0;
            indicatorRect.Height += max;
          }

          GeometryGroup ellipses = new GeometryGroup();
          ellipses.Children.Add(new RectangleGeometry(indicatorRect));
          ellipses.Children.Add(new LineGeometry());

          if (indicatorData.RangeFilter < MaxValue && indicatorData.RangeFilter > MinValue)
            drawingContext.DrawLine(range_pen, new Point(indicatorRect.X, filterY), new Point(indicatorRect.X + indicatorRect.Width, filterY));

          drawingContext.DrawRectangle(selectedBrush, null, indicatorRect);
        }

      }
    }

    #endregion

    public void SetMaxValue(decimal newValue)
    {
      maxValue = newValue;
    }

    public void SetMinValue(decimal newValue)
    {
      minValue = newValue;
    }

    public void SetMaxUnix(long newValue)
    {
      maxUnix = newValue;
    }

    public void SetMinUnix(long newValue)
    {
      minUnix = newValue;
    }

    public void SetLock(bool newValue)
    {
      lockChart = newValue;
    }

    #endregion
  }
}