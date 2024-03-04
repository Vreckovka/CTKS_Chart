﻿using System;
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
    private decimal? lastAthPrice = null;

    public new void RenderOverlay(
      List<CtksIntersection> ctksIntersections = null, 
      bool isSimulaton = false, 
      decimal? athPrice = null,
      double canvasHeight = 1000)
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


          if (TradingBot.Strategy.MaxBuyPrice < maxCanvasValue && TradingBot.Strategy.MaxBuyPrice > minCanvasValue)
            DrawMaxBuyPrice(dc, Layout, TradingBot.Strategy.MaxBuyPrice.Value, imageHeight, imageWidth);

          if (TradingBot.Strategy.MinSellPrice < maxCanvasValue && TradingBot.Strategy.MinSellPrice > minCanvasValue)
            DrawMinSellPrice(dc, Layout, TradingBot.Strategy.MinSellPrice.Value, imageHeight, imageWidth);
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
      var skip = candles.Count - maxCount > 0 ? candles.Count - maxCount : 0;

      canvasWidth *= 0.85;
      var startGap = canvasWidth * 0.15;
      canvasWidth -= startGap;

      var width = canvasWidth / maxCount;
      var margin = width * 0.25 > 5 ? width * 0.25 : 5;

      if (margin > width)
      {
        margin = width * 0.95;
      }

      double minDrawnPoint = 0;
      double maxDrawnPoint = 0;
      var drawnCandles = new List<ChartCandle>();

      var maxPoint = TradingHelper.GetCanvasValue(canvasHeight, MaxValue, MaxValue, MinValue);
      var minPoint = TradingHelper.GetCanvasValue(canvasHeight, MinValue, MaxValue, MinValue);

      if (candles.Any())
      {
        int y = 0;
        for (int i = skip; i < candles.Count; i++)
        {
          var point = candles[i];

          var close = TradingHelper.GetCanvasValue(canvasHeight, point.Close.Value, MaxValue, MinValue);
          var open = TradingHelper.GetCanvasValue(canvasHeight, point.Open.Value, MaxValue, MinValue);

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var selectedBrush = green ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);

          Pen pen = new Pen(selectedBrush, 3);
          Pen wickPen = new Pen(selectedBrush, 1);

          var newCandle = new Rect()
          {
            Width = width - margin,
          };

          var lastClose = i > 0 ?
            TradingHelper.GetCanvasValue(canvasHeight, candles[i - 1].Close.Value, MaxValue, MinValue) :
            TradingHelper.GetCanvasValue(canvasHeight, candles[i].Open.Value, MaxValue, MinValue);

          if (green)
          {
            newCandle.Height = close - lastClose;
          }
          else
          {
            newCandle.Height = lastClose - close;
          }

          var position = (y + 1) * width;
          newCandle.X = startGap + position + margin / 2;

          if (green)
            newCandle.Y = canvasHeight - close;
          else
            newCandle.Y = canvasHeight - close - newCandle.Height;

          var high = candles[i].High.Value;
          var low = candles[i].Low.Value;


          if (point.Low < MinValue)
            low = MinValue;

          if (point.High > MaxValue)
            high = MaxValue;


          var topWickCanvas = TradingHelper.GetCanvasValue(canvasHeight, high, MaxValue, MinValue);
          var bottomWickCanvas = TradingHelper.GetCanvasValue(canvasHeight, low, MaxValue, MinValue);


          var wickTop = green ? close : open;
          var wickBottom = green ? open : close;

          var topY = canvasHeight - wickTop - (topWickCanvas - wickTop);
          var bottomY = canvasHeight - wickBottom;

          Rect? topWick = null;
          Rect? bottomWick = null;

          if (topWickCanvas - wickTop > 0)
          {
            topWick = new Rect()
            {
              Height = topWickCanvas - wickTop,
              X = newCandle.X + (newCandle.Width / 2),
              Y = topY,
            };
          }

          if (wickBottom - bottomWickCanvas > 0)
          {
            bottomWick = new Rect()
            {
              Height = wickBottom - bottomWickCanvas,
              X = newCandle.X + (newCandle.Width / 2),
              Y = bottomY,
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

          if (bottomWick != null && bottomWick.Value.Y > maxDrawnPoint)
          {
            maxDrawnPoint = bottomWick.Value.Y;
          }

          if (topWick != null && topWick.Value.Y < minDrawnPoint)
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
        Brush selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);

        var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, layout.MaxValue, layout.MinValue);

        var frame = intersection.TimeFrame;

        var lineY = canvasHeight - actual;

        var positionsOnIntersesction = allPositions
          .Where(x => x.Intersection.IsSame(intersection))
          .ToList();

        var firstPositionsOnIntersesction = positionsOnIntersesction.FirstOrDefault();
        var isOnlyAuto = positionsOnIntersesction.All(x => x.IsAutomatic);
        var isCombined = positionsOnIntersesction.Any(x => x.IsAutomatic) && positionsOnIntersesction.Any(x => !x.IsAutomatic);

        if (frame >= minTimeframe)
        {
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

          if (!intersection.IsEnabled)
          {
            selectedBrush = DrawingHelper.GetBrushFromHex("#FFC0CB");
          }

          FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush);

          drawingContext.DrawText(formattedText, new Point(0, lineY - formattedText.Height / 2));

          Pen pen = new Pen(selectedBrush, 1);
          pen.DashStyle = DashStyles.Dash;
          pen.Thickness = DrawingHelper.GetPositionThickness(frame);

          drawingContext.DrawLine(pen, new Point(canvasWidth * 0.10, lineY), new Point(canvasWidth, lineY));
        }
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
      double canvasWidth
      )
    {
      foreach (var position in positions)
      {
        var isActive = position.Side == PositionSide.Buy && position.State == PositionState.Filled;

        Brush selectedBrush = isActive ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.ACTIVE_BUY].Brush) :
            position.Side == PositionSide.Buy ?
              DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.FILLED_BUY].Brush) :
              DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.FILLED_SELL].Brush); ;


        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;

        var actual = TradingHelper.GetCanvasValue(canvasHeight, position.Price, layout.MaxValue, layout.MinValue);

        var frame = position.Intersection.TimeFrame;

        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        var lineY = canvasHeight - actual;
        var candle = candles.FirstOrDefault(x => x.Candle.OpenTime < position.FilledDate && x.Candle.CloseTime > position.FilledDate);

        if (candle != null)
        {
          var text = position.Side == PositionSide.Buy ? "B" : "S";
          FormattedText formattedText = DrawingHelper.GetFormattedText(text, selectedBrush, isActive ? 25 : 9);

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

      var brush = lastCandle.IsGreen ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);
      var pen = new Pen(brush, 1);
      pen.DashStyle = DashStyles.Dash;

      var text = DrawingHelper.GetFormattedText(closePrice.Value.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 20);
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

        var brush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.AVERAGE_BUY].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = DashStyles.Dash;

        var text = DrawingHelper.GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
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

        var brush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.ATH].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = DashStyles.Dash;

        var text = DrawingHelper.GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #region DrawMaxBuyPrice

    public void DrawMaxBuyPrice(DrawingContext drawingContext, Layout layout, decimal price, double canvasHeight, double canvasWidth)
    {
      if (price > 0)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price, layout.MaxValue, layout.MinValue);

        var lineY = canvasHeight - close;

        var brush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.MAX_BUY_PRICE].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = DashStyles.Dash;

        var text = DrawingHelper.GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #region DrawMaxBuyPrice

    public void DrawMinSellPrice(DrawingContext drawingContext, Layout layout, decimal price, double canvasHeight, double canvasWidth)
    {
      if (price > 0)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price, layout.MaxValue, layout.MinValue);

        var lineY = canvasHeight - close;

        var brush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.MIN_SELL_PRICE].Brush);
        var pen = new Pen(brush, 1.5);
        pen.DashStyle = DashStyles.Dash;

        var text = DrawingHelper.GetFormattedText(price.ToString($"N{TradingBot.Asset.PriceRound}"), brush, 15);
        drawingContext.DrawText(text, new Point(canvasWidth - text.Width - 15, lineY - text.Height - 5));
        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));
      }

    }

    #endregion

    #endregion
  }
}