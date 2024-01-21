﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using VCore.Standard;
using VCore.WPF.Prompts;

namespace CTKS_Chart.ViewModels
{
  public class ArchitectViewModel : BasePromptViewModel
  {
    private readonly Asset Asset;

    public ArchitectViewModel(
      IList<Layout> layouts,
      ColorSchemeViewModel colorSchemeViewModel,
      Asset asset)
    {
      this.Asset = asset ?? throw new ArgumentNullException(nameof(asset));
      Layouts = layouts ?? throw new ArgumentNullException(nameof(layouts));
      ColorScheme = colorSchemeViewModel ?? throw new ArgumentNullException(nameof(colorSchemeViewModel));
   
      SelectedLayout = layouts[4];
    }


    public IEnumerable<Layout> Layouts { get; }
    public override string Title { get; set; } = "Architect";
    public Grid MainGrid { get; } = new Grid();
    public Image ChartImage { get; } = new Image();


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
          
          maxValue = selectedLayout.MaxValue;
          minValue = selectedLayout.MinValue;
          candleCount = selectedLayout.Ctks.Candles.Count;

          RaisePropertyChanged();
          RenderOverlay();
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
                
          if(value > 0)
          {
            minValue = value;

            RenderOverlay();
            RaisePropertyChanged();
          }
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

    #region DrawChart

    private DrawnChart DrawChart(
      DrawingContext drawingContext,
      IList<Candle> candles,
      double canvasHeight,
      double canvasWidth,
      int maxCount = 150)
    {
      var skip = candles.Count - maxCount > 0 ? candles.Count - maxCount : 0;

      canvasWidth *= 0.9;
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

    #region RenderOverlay

    public void RenderOverlay()
    {
      Pen shapeOutlinePen = new Pen(Brushes.Transparent, 1);
      shapeOutlinePen.Freeze();

      DrawingGroup dGroup = new DrawingGroup();

      double imageHeight = 1000;
      double imageWidth = 1000;

      var candles = SelectedLayout.Ctks.Candles;

      using (DrawingContext dc = dGroup.Open())
      {
        dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(imageHeight, imageWidth));

        var chart = DrawChart(dc, candles, imageHeight, imageWidth, CandleCount);

        RenderIntersections(dc, SelectedLayout.Ctks.ctksIntersections,
          chart.Candles.ToList(),
          imageHeight,
          imageHeight,
          imageWidth);

        RenderLines(dc, chart.Candles.ToList(),imageHeight, imageWidth);

        DrawingImage dImageSource = new DrawingImage(dGroup);

        Chart = dImageSource;
        this.ChartImage.Source = Chart;
      }
    }

    #endregion

    #region RenderIntersections

    public void RenderIntersections(
      DrawingContext drawingContext,
      IEnumerable<CtksIntersection> intersections,
      IList<ChartCandle> candles,
      double desiredHeight,
      double canvasHeight,
      double canvasWidth)
    {
      var maxCanvasValue = (decimal)TradingHelper.GetValueFromCanvas(desiredHeight, desiredHeight, MaxValue, MinValue);
      var minCanvasValue = (decimal)TradingHelper.GetValueFromCanvas(desiredHeight, -2 * (desiredHeight - canvasHeight), MaxValue, MinValue);

      maxCanvasValue = Math.Max(maxCanvasValue, candles.Max(x => x.Candle.High.Value));
      minCanvasValue = Math.Min(minCanvasValue, candles.Min(x => x.Candle.Low.Value));

      var validIntersection = intersections
        .Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue)
        .ToList();

      foreach (var intersection in validIntersection)
      {
        Brush selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);

        var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, MaxValue, MinValue);

        var frame = intersection.TimeFrame;

        var lineY = canvasHeight - actual;

        FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush);

        drawingContext.DrawText(formattedText, new Point(0, lineY - formattedText.Height / 2));

        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;
        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        drawingContext.DrawLine(pen, new Point(canvasWidth * 0.10, lineY), new Point(canvasWidth, lineY));
      }
    }

    #endregion

    #region RenderIntersections

    public void RenderLines(
      DrawingContext drawingContext,
      IList<ChartCandle> chartCandles,
      double canvasHeight,
      double canvasWidth)
    {
      foreach (var line in SelectedLayout.Ctks.ctksLines.TakeLast(5))
      {
        Brush selectedBrush = Brushes.Yellow;

        var x3 = canvasWidth;

        var firstCandle = chartCandles.SingleOrDefault(x => x.Candle.UnixTime == line.FirstCandleUnixTime);
        var secondCandle = chartCandles.SingleOrDefault(x => x.Candle.UnixTime == line.SecondCandleUnixTime);

        if(firstCandle == null || secondCandle == null)
        {
          continue;
        }

        var ctksLine = CreateLine(line.FirstIndex,line.SecondIndex,canvasHeight,canvasWidth, firstCandle, secondCandle, line.LineType,line.TimeFrame);
        var y3 = TradingHelper.GetPointOnLine(ctksLine.X1, ctksLine.Y1, ctksLine.X2, ctksLine.Y2, x3);


        Pen pen = new Pen(selectedBrush, 1);

        var finalPoint = new Point(x3, canvasHeight - y3);

        drawingContext.DrawLine(pen, ctksLine.StartPoint, finalPoint);
      }
    }

    #endregion

    #region CreateLine

    public CtksLine CreateLine(
      int? firstCandleIndex, 
      int? secondCandleIndex, 
      double canvasHeight,
      double canvasWidth,
      ChartCandle first,
      ChartCandle second,
      LineType lineType, 
      TimeFrame timeFrame)
    {
      var bottom1 = canvasHeight- first.Body.Bottom;
      var bottom2 = canvasHeight-second.Body.Bottom;

      var left1 = first.Body.Left;
      var left2 = second.Body.Left;

      var width1 = first.Body.Width;
      var width2 = second.Body.Width;

      var height1 = first.Body.Height;
      var height2 = second.Body.Height;

      double y1 = 0.0;
      double y2 = 0.0;

      double x1 = 0.0;
      double x2 = 0.0;

      var startPoint = new Point();
      var endPoint = new Point();

      if (lineType == LineType.RightBttom)
      {
        startPoint = new Point(left1 + width1, canvasHeight - bottom1);
        endPoint = new Point(left2 + width2, canvasHeight - bottom2);

        y1 = bottom1;
        y2 = bottom2;

        x1 = left1 + width1;
        x2 = left2 + width2;
      }
      else if (lineType == LineType.LeftTop)
      {
        startPoint = new Point(left1, canvasHeight - bottom1 - height1);
        endPoint = new Point(left2, canvasHeight - bottom2 - height2);

        y1 = bottom1 + height1;
        y2 = bottom2 + height2;

        x1 = left1;
        x2 = left2;
      }
      //else if (lineType == LineType.RightTop)
      //{
      //  startPoint = new Point(Canvas.GetLeft(firstRect) + firstRect.Width, canvasHeight - bottom1 - firstRect.Height);
      //  endPoint = new Point(Canvas.GetLeft(secondrect) + secondrect.Width, canvasHeight - bottom2 - secondrect.Height);

      //  y1 = bottom1 + firstRect.Height;
      //  y2 = bottom2 + secondrect.Height;

      //  x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
      //  x2 = Canvas.GetLeft(secondrect) + secondrect.Width;
      //}
      //else if (lineType == LineType.LeftBottom)
      //{
      //  startPoint = new Point(Canvas.GetLeft(firstRect), canvasHeight - bottom1);
      //  endPoint = new Point(Canvas.GetLeft(secondrect), canvasHeight - bottom2);

      //  y1 = bottom1;
      //  y2 = bottom2;

      //  x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
      //  x2 = Canvas.GetLeft(secondrect) + secondrect.Width;
      //}


      // if (lineType == LineType.RightTop)
      //   Canvas.SetBottom(myPath, canvas.ActualHeight - lastSegment.Point.Y);

      var line = new CtksLine()
      {
        X1 = x1,
        X2 = x2,
        Y1 = y1,
        Y2 = y2,
        StartPoint = startPoint,
        EndPoint = endPoint,
        TimeFrame = timeFrame,
        FirstIndex = firstCandleIndex,
        SecondIndex = secondCandleIndex,
        LineType = lineType
      };

      return line;
    }

    #endregion
  }
}