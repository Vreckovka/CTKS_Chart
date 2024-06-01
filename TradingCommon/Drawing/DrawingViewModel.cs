using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CTKS_Chart.Trading;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.WPF.Misc;

namespace CTKS_Chart.ViewModels
{
  public class DrawingViewModel : ViewModel, IDrawingViewModel
  {
    #region Properties

    public string Symbol { get; set; }

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

    #region MaxValue

    protected decimal maxValue;

    public decimal MaxValue
    {
      get { return maxValue; }
      set
      {
        if (value != maxValue && value > minValue)
        {
          maxValue = value;

          LockChart = false;

          Render();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinValue

    protected decimal minValue = (decimal)0.001;

    public decimal MinValue
    {
      get { return minValue; }
      set
      {
        if (value != minValue && value < maxValue)
        {
          minValue = value;

          if (minValue < 0)
          {
            minValue = 0.0001m;
          }

          LockChart = false;

          Render();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MaxUnix

    protected long maxUnix;

    public long MaxUnix
    {
      get { return maxUnix; }
      set
      {
        if (value != maxUnix && value > minUnix)
        {
          maxUnix = value;

          if (maxUnix < 0)
          {
            maxUnix = (long)1;
          }

          LockChart = false;

          Render();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinUnix

    protected long minUnix;

    public long MinUnix
    {
      get { return minUnix; }
      set
      {
        if (value != minUnix && value < maxUnix)
        {
          minUnix = value;

          if (minUnix < 0)
          {
            minUnix = (long)1;
          }

          LockChart = false;

          Render();
          RaisePropertyChanged();
        }
      }
    }



    #endregion

    #region LockChart

    protected bool lockChart;
    protected decimal actualPriceChartViewDiff;

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

          Render();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CanvasHeight

    protected double canvasHeight = 1000;

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

    protected double canvasWidth = 1000;

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

    public bool EnableAutoLock { get; set; } = true;

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
            drawingSettings.RenderLayout = () => Render();
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

    private ImageSource chart;

    public ImageSource Chart
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

    #region Overlay

    private ImageSource overlay;

    public ImageSource Overlay
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

    #region InitialCandleCount

    public int InitialCandleCount
    {
      get
      {

        return 100;
      }
    }


    #endregion

    public RxObservableCollection<RenderedIntesection> RenderedIntersections { get; } = new RxObservableCollection<RenderedIntesection>();
    public RxObservableCollection<DrawingRenderedLabel> RenderedLabels { get; } = new RxObservableCollection<DrawingRenderedLabel>();
    public RxObservableCollection<IndicatorSettings> IndicatorSettings { get; } = new RxObservableCollection<IndicatorSettings>();

    #endregion

    #region Commands

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

      Render();
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
      Render();
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

      Render();
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


      IndicatorSettings.ItemUpdated.ObserveOnDispatcher().Subscribe(x => Render());
    }

    #endregion

    #region RenderOverlay

    protected List<CtksIntersection> last = null;
    public long unixDiff;
    protected Candle lastLockedCandle;

    protected DateTime? lastFilledPosition;
    protected Candle actual;

    public virtual void Render(Candle actual = null)
    {
      try
      {
        if (actual != null)
          this.actual = actual;

        Pen shapeOutlinePen = new Pen(Brushes.Transparent, 1);
        shapeOutlinePen.Freeze();

        DrawingGroup dGroup = new DrawingGroup();

        if (unixDiff == 0 && ActualCandles.Count > 1)
        {
          unixDiff = ActualCandles[1].UnixTime - ActualCandles[0].UnixTime;
        }

        if (lastLockedCandle == null && ActualCandles.Count > 0)
        {
          lastLockedCandle = ActualCandles.Last();
        }



        DrawnChart newChart = null;
        Candle lastCandle = this.actual;
        WriteableBitmap writeableBmp = BitmapFactory.New((int)CanvasWidth, (int)CanvasHeight);

        using (writeableBmp.GetBitmapContext())
        {
          using (DrawingContext dc = dGroup.Open())
          {
            dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(CanvasWidth, CanvasHeight));
            var candlesToRender = ActualCandles.ToList();

            candlesToRender = candlesToRender.Where(x => x.UnixTime + unixDiff >= MinUnix && x.UnixTime - unixDiff <= MaxUnix).ToList();

            if (candlesToRender.Count > 0)
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
                  var viewCandles = ActualCandles.TakeLast(InitialCandleCount);

                  maxUnix = viewCandles.Max(x => x.UnixTime) + (unixDiff * 30);
                  minUnix = viewCandles.Min(x => x.UnixTime) + (unixDiff * 30);
                }
                else if (lastLockedCandle?.OpenTime != lastCandle?.OpenTime)
                {
                  maxUnix += unixDiff;
                  minUnix += unixDiff;

                  var lastCandleUnix = ActualCandles.Last().UnixTime;

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

            newChart = DrawChart(writeableBmp, candlesToRender, CanvasHeight, CanvasWidth);

            DrawIndicators(dc);
            OnRender(newChart, dc, dGroup, writeableBmp);
          }
        }

        Chart = writeableBmp;
        Overlay = new DrawingImage(dGroup);
        DrawnChart = newChart;
      }
      catch (Exception ex) { }
    }

    #endregion

    #region OnRender

    public virtual void OnRender(DrawnChart drawnChart, DrawingContext dc, DrawingGroup dGroup, WriteableBitmap writeableBitmap)
    {
    }

    #endregion

    #region DrawChart

    public DrawnChart DrawChart(
      WriteableBitmap drawingContext,
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

      //var greenBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush);
      //var redBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);

      var greenColor = DrawingHelper.GetColorFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush);
      var redColor = DrawingHelper.GetColorFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);

      var greenBrush = new SolidColorBrush(greenColor);
      var redBrush = new SolidColorBrush(redColor);

      greenBrush.Freeze();
      redBrush.Freeze();

      Pen greenPen = new Pen(greenBrush, 0.5);
      Pen redPen = new Pen(redBrush, 0.5);

      greenPen.Freeze();
      redPen.Freeze();

      if (candles.Any() && width > 0)
      {
        foreach (var candle in candles)
        {
          var close = TradingHelper.GetCanvasValue(canvasHeight, candle.Close.Value, MaxValue, MinValue);
          var open = TradingHelper.GetCanvasValue(canvasHeight, candle.Open.Value, MaxValue, MinValue);

          var high = TradingHelper.GetCanvasValue(canvasHeight, candle.High.Value, MaxValue, MinValue);
          var low = TradingHelper.GetCanvasValue(canvasHeight, candle.Low.Value, MaxValue, MinValue);

          var selectedBrush = candle.IsGreen ? greenBrush : redBrush;
          var pen = candle.IsGreen ? greenPen : redPen;
          var color = candle.IsGreen ? greenColor : redColor;

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
            //drawingContext.DrawRectangle(selectedBrush, pen, newCandle);

            //if (unix_diff > 1)
            drawingContext.DrawRectangle(
              (int)newCandle.Left,
              (int)newCandle.Top,
              (int)newCandle.Left + (int)newCandle.Width,
              (int)newCandle.Top + (int)newCandle.Height,
              color);

            drawingContext.FillRectangle(
             (int)newCandle.Left,
             (int)newCandle.Top,
             (int)newCandle.Left + (int)newCandle.Width,
             (int)newCandle.Top + (int)newCandle.Height,
             color);
            //else
            //  drawingContext.DrawRectangle(selectedBrush, pen, newCandle);

            rendered = true;
          }

          if (topWick != null)
          {
            drawingContext.DrawLine((int)x, (int)newCandle.Top, (int)x, (int)topWick.Value.Top, color);
            rendered = true;
          }
          if (bottomWick != null)
          {
            drawingContext.DrawLine((int)x, (int)newCandle.Top + (int)newCandle.Height, (int)x, (int)bottomWick.Value.Top + (int)bottomWick.Value.Height, color);
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

    #region DrawIndicators

    public void DrawIndicators(DrawingContext drawingContext)
    {
      foreach (var indicatorSettings in IndicatorSettings.Where(x => x.Show))
      {
        if (TradingViewHelper.LoadedData[Symbol].TryGetValue(indicatorSettings.TimeFrame, out var candles))
        {
          candles = candles.Where(x => x.IndicatorData?.RangeFilterData?.RangeFilter > 0).ToList();

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

    #region Raise

    public void Raise(string name)
    {
      RaisePropertyChanged(name);
    }

    #endregion

    #region Setters 

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

    #endregion
  }
}