using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Binance.Net.Enums;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using LiveCharts.Wpf.Charts.Base;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF.ItemsCollections;
using PositionSide = CTKS_Chart.Strategy.PositionSide;

namespace CTKS_Chart.ViewModels
{
  public class DrawingViewModel : ViewModel
  {
    public DrawingViewModel(TradingBot tradingBot, Layout layout)
    {
      TradingBot = tradingBot;
      Layout = layout;

    }

    #region Properties

    public Layout Layout { get; }
    public TradingBot TradingBot { get; }


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
          maxValue = Math.Round(value, TradingBot.Asset.PriceRound);

          Layout.MaxValue = MaxValue;
          TradingBot.Asset.StartMaxPrice = MaxValue;

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
          minValue = Math.Round(value, TradingBot.Asset.PriceRound);

          Layout.MinValue = MinValue;
          TradingBot.Asset.StartLowPrice = MinValue;

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
          RenderOverlay();

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


          RenderOverlay();
          RaisePropertyChanged();
        }
      }
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

          RenderOverlay();
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

          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public Image ChartImage { get; } = new Image();

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
      };

    }

    #endregion

    #region RenderOverlay

    private List<CtksIntersection> last = null;
    private decimal? lastAthPrice = null;

    public void RenderOverlay(List<CtksIntersection> ctksIntersections = null, bool isSimulaton = false, decimal? athPrice = null, double canvasHeight = 1000)
    {
      if (ctksIntersections == null)
      {
        ctksIntersections = last;
      }

      if (athPrice != null)
      {
        lastAthPrice = athPrice;
      }


      if (ctksIntersections == null)
        return;


      last = ctksIntersections;

      Pen shapeOutlinePen = new Pen(Brushes.Transparent, 1);
      shapeOutlinePen.Freeze();

      DrawingGroup dGroup = new DrawingGroup();

      double imageHeight = 1000;
      double imageWidth = 1000;

      using (DrawingContext dc = dGroup.Open())
      {
        dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(imageHeight, imageWidth));

        var chart = DrawChart(dc, Layout, ActualCandles, imageHeight, imageWidth, CandleCount);
        double desiredCanvasHeight = imageHeight;

        if (chart.MinDrawnPoint > imageHeight)
        {
          desiredCanvasHeight = chart.MinDrawnPoint;
        }

        var chartCandles = chart.Candles.ToList();

        if (chartCandles.Any())
        {
          RenderIntersections(dc, Layout, ctksIntersections,
        TradingBot.Strategy.AllOpenedPositions.ToList(),
        chartCandles,
        desiredCanvasHeight,
        imageHeight,
        imageWidth,
        !isSimulaton ? TimeFrame.W1 : TimeFrame.M1);

          if (ShowClosedPositions)
          {
            var validPositions = TradingBot.Strategy.AllClosedPositions.Where(x => x.FilledDate > ActualCandles.First().OpenTime).ToList();

            RenderClosedPosiotions(dc, Layout,
              validPositions,
              chartCandles,
              imageHeight,
              imageWidth);
          }

          DrawActualPrice(dc, Layout, ActualCandles, imageHeight, imageWidth);

          decimal price = TradingBot.Strategy.AvrageBuyPrice;

          var maxCanvasValue = (decimal)TradingHelper.GetValueFromCanvas(desiredCanvasHeight, desiredCanvasHeight, Layout.MaxValue, Layout.MinValue);
          var minCanvasValue = (decimal)TradingHelper.GetValueFromCanvas(desiredCanvasHeight, -2 * (desiredCanvasHeight - canvasHeight), Layout.MaxValue, Layout.MinValue);

          maxCanvasValue = Math.Max(maxCanvasValue, chartCandles.Max(x => x.Candle.High.Value));
          minCanvasValue = Math.Min(minCanvasValue, chartCandles.Min(x => x.Candle.Low.Value));

          if (ShowAveragePrice)
          {
            if (price < maxCanvasValue && price > minCanvasValue)
              DrawAveragePrice(dc, Layout, TradingBot.Strategy.AvrageBuyPrice, imageHeight, imageWidth);
          }


          if (ShowATH)
          {
            if (lastAthPrice < maxCanvasValue && lastAthPrice > minCanvasValue)
              DrawPriceToATH(dc, Layout, lastAthPrice.Value, imageHeight, imageWidth);
          }
        }

        DrawingImage dImageSource = new DrawingImage(dGroup);

        Chart = dImageSource;
        this.ChartImage.Source = Chart;
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

          var close = TradingHelper.GetCanvasValue(canvasHeight, point.Close.Value, layout.MaxValue, layout.MinValue);
          var open = TradingHelper.GetCanvasValue(canvasHeight, point.Open.Value, layout.MaxValue, layout.MinValue);

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var selectedBrush = green ? TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);

          Pen pen = new Pen(selectedBrush, 3);
          Pen wickPen = new Pen(selectedBrush, 1);

          var newCandle = new Rect()
          {
            Width = width - margin,
          };

          var lastClose = i > 0 ?
            TradingHelper.GetCanvasValue(canvasHeight, candles[i - 1].Close.Value, layout.MaxValue, layout.MinValue) :
            TradingHelper.GetCanvasValue(canvasHeight, candles[i].Open.Value, layout.MaxValue, layout.MinValue);

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


          var topWickCanvas = TradingHelper.GetCanvasValue(canvasHeight, high, layout.MaxValue, layout.MinValue);
          var bottomWickCanvas = TradingHelper.GetCanvasValue(canvasHeight, low, layout.MaxValue, layout.MinValue);

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
      var maxCanvasValue = (decimal)TradingHelper.GetValueFromCanvas(desiredHeight, desiredHeight, layout.MaxValue, layout.MinValue);
      var minCanvasValue = (decimal)TradingHelper.GetValueFromCanvas(desiredHeight, -2 * (desiredHeight - canvasHeight), layout.MaxValue, layout.MinValue);

      maxCanvasValue = Math.Max(maxCanvasValue, candles.Max(x => x.Candle.High.Value));
      minCanvasValue = Math.Min(minCanvasValue, candles.Min(x => x.Candle.Low.Value));

      var validIntersection = intersections
        .Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue && minTimeframe <= x.TimeFrame)
        .ToList();

      foreach (var intersection in validIntersection)
      {
        Brush selectedBrush = TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);
        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;

        var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, layout.MaxValue, layout.MinValue);

        var frame = intersection.TimeFrame;

        pen.Thickness = GetPositionThickness(frame);

        var lineY = canvasHeight - actual;

        var positionsOnIntersesction = allPositions
          .Where(x => x.Intersection.IsSame(intersection))
          .ToList();

        var firstPositionsOnIntersesction = positionsOnIntersesction.FirstOrDefault();

        if (firstPositionsOnIntersesction != null)
        {
          selectedBrush = firstPositionsOnIntersesction.Side == PositionSide.Buy ?
            TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.BUY].Brush) :
            TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.SELL].Brush);
          pen.Brush = selectedBrush;
        }

        if (frame >= minTimeframe)
        {
          Brush positionBrush = TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);

          if (firstPositionsOnIntersesction != null)
          {
            positionBrush = firstPositionsOnIntersesction.Side == PositionSide.Buy ?
              TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.BUY].Brush) :
              TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.SELL].Brush);
          }
          else
          {
            positionBrush = TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);
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

        Brush selectedBrush = isActive ? TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.ACTIVE_BUY].Brush) :
            position.Side == PositionSide.Buy ?
              TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.FILLED_BUY].Brush) :
              TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.FILLED_SELL].Brush); ;


        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;

        var actual = TradingHelper.GetCanvasValue(canvasHeight, position.Price, layout.MaxValue, layout.MinValue);

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

    #region DrawActualPrice

    public void DrawActualPrice(DrawingContext drawingContext, Layout layout, IList<Candle> candles, double canvasHeight, double canvasWidth)
    {
      var lastCandle = candles.Last();
      var closePrice = lastCandle.Close;

      var close = TradingHelper.GetCanvasValue(canvasHeight, closePrice.Value, layout.MaxValue, layout.MinValue);

      var lineY = canvasHeight - close;

      var brush = lastCandle.IsGreen ? TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);
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
        var close = TradingHelper.GetCanvasValue(canvasHeight, price, layout.MaxValue, layout.MinValue);

        var lineY = canvasHeight - close;

        var brush = TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.AVERAGE_BUY].Brush);
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
        var close = TradingHelper.GetCanvasValue(canvasHeight, price, layout.MaxValue, layout.MinValue);

        var lineY = canvasHeight - close;

        var brush = TradingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.ATH].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = DashStyles.Dash;

        var text = GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
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