using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
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

      SelectedLayout = layouts[5];
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

          if (value > 0)
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

      if (candles.Any())
      {
        int y = 0;
        for (int i = skip; i < candles.Count; i++)
        {
          var point = candles[i];

          var close = TradingHelper.GetCanvasValue(canvasHeight, point.Close.Value, MaxValue, MinValue);
          var open = TradingHelper.GetCanvasValue(canvasHeight, point.Open.Value, MaxValue, MinValue);

          var high = TradingHelper.GetCanvasValue(canvasHeight, point.High.Value, MaxValue, MinValue);
          var low = TradingHelper.GetCanvasValue(canvasHeight, point.Low.Value, MaxValue, MinValue);

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

          if (lastClose < 0)
          {
            lastClose = 0;
          }

          if (close < 0)
          {
            close = 0;
          }

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



          var wickTop = green ? close : open;
          var wickBottom = green ? open : close;

          var topY = canvasHeight - wickTop - (high - wickTop);
          var bottomY = canvasHeight - wickBottom;

          Rect? topWick = null;
          Rect? bottomWick = null;

          if (high - wickTop > 0 && high > 0)
          {
            if (wickTop < 0)
            {
              wickTop = 0;
            }

            topWick = new Rect()
            {
              Height = high - wickTop,
              X = newCandle.X + (newCandle.Width / 2),
              Y = topY,
            };
          }

          if (wickBottom - low > 0 && wickBottom > 0)
          {
            if (low < 0)
            {
              low = 0;
            }

            bottomWick = new Rect()
            {
              Height = wickBottom - low,
              X = newCandle.X + (newCandle.Width / 2),
              Y = bottomY,
            };
          }


          if (newCandle.Height > 0)
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

        var drawnChart = DrawChart(dc, candles, imageHeight, imageWidth, CandleCount);
        var lines = RenderLines(dc, drawnChart.Candles.ToList(), imageHeight, imageWidth);

        List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();
        var lastCandle = drawnChart.Candles.Last();

        foreach (var line in lines)
        {
          var actualLeft = lastCandle.Body.Left + lastCandle.Body.Width / 2;
          var actual = TradingHelper.GetPointOnLine(line.StartPoint.X, line.StartPoint.Y, line.EndPoint.X, line.EndPoint.Y, actualLeft);
          var value = Math.Round(TradingHelper.GetValueFromCanvas(imageHeight, imageHeight - actual, MaxValue, MinValue), Asset.PriceRound);

          if(value > 0)
          {
            var intersection = new CtksIntersection()
            {
              Value = value,
              TimeFrame = line.TimeFrame,
              Line = line
            };

            ctksIntersections.Add(intersection);
          }
       
        }

        RenderIntersections(dc, ctksIntersections,
          drawnChart.Candles.ToList(),
          imageHeight,
          imageHeight,
          imageWidth);

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
      //var maxCanvasValue = (decimal)TradingHelper.GetValueFromCanvas(desiredHeight, desiredHeight, MaxValue, MinValue);
      //var minCanvasValue = (decimal)TradingHelper.GetValueFromCanvas(desiredHeight, -2 * (desiredHeight - canvasHeight), MaxValue, MinValue);

      //maxCanvasValue = Math.Max(maxCanvasValue, candles.Max(x => x.Candle.High.Value));
      //minCanvasValue = Math.Min(minCanvasValue, candles.Min(x => x.Candle.Low.Value));

      var validIntersection = intersections
        .Where(x => x.Value > MinValue && x.Value < MaxValue)
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

    #region RenderLines

    public IEnumerable<CtksLine> RenderLines(
      DrawingContext drawingContext,
      IList<ChartCandle> chartCandles,
      double canvasHeight,
      double canvasWidth)
    {
      var lines = SelectedLayout.Ctks.ctksLines.TakeLast(15).ToList();
      var list = new List<CtksLine>();

      foreach (var line in lines)
      {
        Brush selectedBrush = Brushes.Yellow;

        var x3 = canvasWidth;

        var firstCandle = chartCandles.SingleOrDefault(x => x.Candle.UnixTime == line.FirstPoint.UnixTime);
        var secondCandle = chartCandles.SingleOrDefault(x => x.Candle.UnixTime == line.SecondPoint.UnixTime);

        if (firstCandle == null || secondCandle == null)
        {
          continue;
        }

        var ctksLine = CreateLine(line.FirstIndex, line.SecondIndex, canvasHeight, canvasWidth, line, firstCandle, secondCandle, line.LineType, line.TimeFrame);
        var y3 = TradingHelper.GetPointOnLine(ctksLine.StartPoint.X, ctksLine.StartPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, x3);
                
        while (y3 < 0 && x3 > 0)
        {
          x3 -= 1;
          y3 = TradingHelper.GetPointOnLine(ctksLine.StartPoint.X, ctksLine.StartPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, x3);
        }

        while (y3 > canvasHeight)
        {
          x3 -= 1;
          y3 = TradingHelper.GetPointOnLine(ctksLine.StartPoint.X, ctksLine.StartPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, x3);
        }

        if (x3 < 0)
          continue;

        Pen pen = new Pen(selectedBrush, 1);

        var finalPoint = new Point(x3, y3);

        drawingContext.DrawLine(pen, ctksLine.StartPoint, ctksLine.EndPoint);
        drawingContext.DrawLine(pen, ctksLine.EndPoint, finalPoint);

        list.Add(ctksLine);
      }

      return list;
    }

    #endregion

    #region CreateLine

    public CtksLine CreateLine(
      int? firstCandleIndex,
      int? secondCandleIndex,
      double canvasHeight,
      double canvasWidth,
      CtksLine ctksLine,
      ChartCandle first,
      ChartCandle second,
      LineType lineType,
      TimeFrame timeFrame)
    {

      var bottom1 = TradingHelper.GetCanvasValue(canvasHeight, ctksLine.FirstPoint.Price, MaxValue, MinValue);
      var bottom2 = TradingHelper.GetCanvasValue(canvasHeight, ctksLine.SecondPoint.Price, MaxValue, MinValue);

      var left1 = first.Body.Left;
      var left2 = second.Body.Left;

      var width1 = first.Body.Width;
      var width2 = second.Body.Width;

      var startPoint = new Point();
      var endPoint = new Point();

      if (lineType == LineType.RightBottom)
      {
        startPoint = new Point(left1 + width1, canvasHeight - bottom1);
        endPoint = new Point(left2 + width2, canvasHeight - bottom2);
      }
      else if (lineType == LineType.LeftTop)
      {
        startPoint = new Point(left1, canvasHeight - bottom1);
        endPoint = new Point(left2, canvasHeight - bottom2);
      }
      else if (lineType == LineType.RightTop)
      {
        startPoint = new Point(left1 + width1, canvasHeight - bottom1);
        endPoint = new Point(left2 + width2, canvasHeight - bottom2);
      }
      else if (lineType == LineType.LeftBottom)
      {
        startPoint = new Point(left1, canvasHeight - bottom1);
        endPoint = new Point(left2, canvasHeight - bottom2);
      }

      var line = new CtksLine()
      {
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