using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CTKS_Chart.Trading
{
  public class Ctks
  {
    private Canvas canvas;

    private readonly Layout layout;
    private readonly TimeFrame timeFrame;

    private double canvasHeight;
    private double canvasWidth;
    private readonly Asset asset;

    public Ctks(Layout layout, TimeFrame timeFrame, double canvasHeight, double canvasWidth, Asset asset)
    {
      this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
      this.canvas = layout.Canvas;
      this.timeFrame = timeFrame;

      this.canvasHeight = canvasHeight;
      this.canvasWidth = canvasWidth;
      this.asset = asset ?? throw new ArgumentNullException(nameof(asset));
    }

    public List<CtksLine> ctksLines = new List<CtksLine>();
    public List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();

    public List<Path> renderedLines = new List<Path>();
    public List<RenderedIntersection> renderedIntersections = new List<RenderedIntersection>();

    public bool LinesVisible { get; set; }
    public bool IntersectionsVisible { get; set; }

    public IList<Candle> Candles { get; set; }

  

    #region CreateLine

    public CtksLine CreateLine(
      int firstCandleIndex, 
      int secondCandleIndex,
      Candle firstCandle,
      Candle secondCandle,
      LineType lineType, 
      TimeFrame timeFrame)
    {
      var candles = canvas.Children.OfType<Rectangle>().ToList();
      var firstRect = candles[firstCandleIndex];
      var secondrect = candles[secondCandleIndex];

      double x1 = 0.0;
      double x2 = 0.0;

      decimal price1 = 0;
      decimal price2 = 0;

      switch(lineType)
      {
        case LineType.LeftBottom:
        case LineType.RightBottom:
          price1 = firstCandle.IsGreen ? firstCandle.Open.Value : firstCandle.Close.Value;
          price2 = secondCandle.IsGreen ? secondCandle.Open.Value : secondCandle.Close.Value;
          break;
        case LineType.LeftTop:
        case LineType.RightTop:
          price1 = firstCandle.IsGreen ? firstCandle.Close.Value : firstCandle.Open.Value;
          price2 = secondCandle.IsGreen ? secondCandle.Close.Value : secondCandle.Open.Value;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(lineType), lineType, null);
      }

      switch (lineType)
      {
        case LineType.LeftBottom:
        case LineType.LeftTop:
          x1 = Canvas.GetLeft(firstRect);
          x2 = Canvas.GetLeft(secondrect);
          break;
        case LineType.RightBottom:
        case LineType.RightTop:
          x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
          x2 = Canvas.GetLeft(secondrect) + secondrect.Width;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(lineType), lineType, null);
      }


      var y1 = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, price1, layout.MaxValue, layout.MinValue);
      var y2 = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, price2, layout.MaxValue, layout.MinValue);

      var startPoint = new Point(x1 + 1, y1);
      var endPoint = new Point(x2 + 1, y2);

      var line = new CtksLine()
      {
        StartPoint = startPoint,
        EndPoint = endPoint,
        TimeFrame = timeFrame,
        FirstIndex = firstCandleIndex,
        SecondIndex = secondCandleIndex,
        LineType = lineType,
        FirstPoint = new CtksLinePoint()
        {
          Price = price1,
          UnixTime = firstCandle.UnixTime
        },
        SecondPoint = new CtksLinePoint()
        {
          Price = price2,
          UnixTime = secondCandle.UnixTime
        }
      };

      ctksLines.Add(line);

      return line;
    }

    #endregion

    #region CreateLines

    public void CreateLines(IList<Candle> candles, TimeFrame timeFrame)
    {
      for (int i = 0; i < candles.Count - 2; i++)
      {
        var currentCandle = candles[i];
        var nextCandle = candles[i + 1];

        if (currentCandle.IsGreen)
        {
          if (nextCandle.IsGreen)
          {
            CreateLine(i, i + 1, currentCandle, nextCandle, LineType.LeftTop, timeFrame);
            CreateLine(i, i + 1, currentCandle, nextCandle, LineType.RightBottom, timeFrame);
          }
          else 
          {
            if (currentCandle.Open < nextCandle.Close)
              CreateLine(i, i + 1, currentCandle, nextCandle, LineType.RightBottom, timeFrame);
            else if (currentCandle.Open > nextCandle.Close)
              CreateLine(i, i + 1, currentCandle, nextCandle, LineType.LeftBottom, timeFrame);
          }
        }
        else
        {
          if (!nextCandle.IsGreen)
          {
            CreateLine(i, i + 1, currentCandle, nextCandle, LineType.RightTop, timeFrame);
            CreateLine(i, i + 1, currentCandle, nextCandle, LineType.LeftBottom, timeFrame);
          }
          else 
          {
            if (currentCandle.Open > nextCandle.Close)
              CreateLine(i, i + 1, currentCandle, nextCandle, LineType.RightTop, timeFrame);
            else if (currentCandle.Open < nextCandle.Close)
              CreateLine(i, i + 1, currentCandle, nextCandle, LineType.LeftTop, timeFrame);
          }
        }
      }
    }

    #endregion

    #region AddIntersections

    private static int intersectionId = 0;
    public void AddIntersections()
    {
      var lastCandle = canvas.Children.OfType<Rectangle>().Last();
      var highestClose = Candles.Max(x => x.High);
      var lowestClose = Candles.Min(x => x.Low);

      foreach (var line in ctksLines)
      {
        var actualLeft = Canvas.GetLeft(lastCandle) + lastCandle.Width / 2;
        var actual = canvasHeight - TradingHelper.GetPointOnLine(line.StartPoint.X, line.StartPoint.Y, line.EndPoint.X, line.EndPoint.Y, actualLeft);
        var value = Math.Round(TradingHelper.GetValueFromCanvas(canvasHeight, actual, layout.MaxValue, layout.MinValue), asset.PriceRound);

        if(value > lowestClose * (decimal)0.995 && value < highestClose * 100)
        {
          var intersection = new CtksIntersection()
          {
            Value = value,
            TimeFrame = line.TimeFrame,
            Line = line
          };

          ctksIntersections.Add(intersection);
          intersectionId++;
        }
      }
    }

    #endregion

    #region ClearRenderedIntersections

    public void ClearRenderedIntersections()
    {
      IntersectionsVisible = false;

      foreach (var intersection in renderedIntersections)
      {
        canvas.Children.Remove(intersection.Line);
        canvas.Children.Remove(intersection.Text);
        canvas.Children.Remove(intersection.Mark);
      }

      renderedIntersections.Clear();
    }

    #endregion

    #region ClearRenderedLines

    public void ClearRenderedLines()
    {
      foreach (var line in renderedLines)
      {
        canvas.Children.Remove(line);
      }

      renderedLines.Clear();
    }

    #endregion

    #region RenderIntersections

    public void RenderIntersections(decimal? max = null, IEnumerable<CtksIntersection> intersections = null)
    {
      if (canvas == null)
      {
        return;
      }

      IntersectionsVisible = true;

      var lastCandle = canvas.Children.OfType<Rectangle>().Last();

      var inter = intersections ?? ctksIntersections;

      var maxCanvasValue = TradingHelper.GetValueFromCanvas(canvasHeight, canvasHeight, layout.MaxValue, layout.MinValue);
      var minCanvasValue = TradingHelper.GetValueFromCanvas(canvasHeight, 0, layout.MaxValue, layout.MinValue);

      foreach (var intersection in inter.Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue))
      {
        var circle = new Ellipse();
        var size = 3;
        circle.Width = size;
        circle.Height = size;
        circle.Fill = Brushes.Red;

        var actualLeft = Canvas.GetLeft(lastCandle) + lastCandle.Width / 2;

        var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, layout.MaxValue, layout.MinValue);

        Canvas.SetLeft(circle, actualLeft - size / 2.0);
        Canvas.SetBottom(circle, actual - size / 2.0);

        var target = new Line();
        target.Stroke = Brushes.Gray;

        var frame = intersection.TimeFrame;

        switch (frame)
        {
          case TimeFrame.Null:
            target.StrokeThickness = 1;
            break;
          case TimeFrame.M12:
            target.StrokeThickness = 4;
            break;
          case TimeFrame.M6:
            target.StrokeThickness = 2;
            break;
          case TimeFrame.M3:
            target.StrokeThickness = 1;
            break;
          default:
            target.StrokeThickness = 1;
            break;
        }

        target.X1 = 150;
        target.X2 = canvasWidth;
        target.StrokeDashArray = new DoubleCollection() { 1, 1 };

        var lineY = canvasHeight - actual;

        target.Y1 = lineY;
        target.Y2 = lineY;

        var text = new TextBlock();


        if (intersection.Value > max)
        {
          return;
        }

        text.Text = intersection.Value.ToString("N4");
        text.Foreground = Brushes.White;

        Panel.SetZIndex(circle, 99);
        Panel.SetZIndex(text, 99);

        Canvas.SetLeft(text, 0);
        Canvas.SetBottom(text, actual);


        canvas.Children.Add(circle);
        canvas.Children.Add(target);
        canvas.Children.Add(text);

        renderedIntersections.Add(new RenderedIntersection()
        {
          Line = target,
          Text = text,
          Mark = circle
        });
      }
    }

    #endregion

    #region RenderLines

    public void RenderLines()
    {
      foreach (var ctksLine in ctksLines)
      {
        var x3 = canvasWidth;
        var y3 = TradingHelper.GetPointOnLine(ctksLine.StartPoint.X, ctksLine.StartPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, x3);

        PathFigure pathFigure = new PathFigure();
        LineSegment segment = new LineSegment();
        Path myPath = new Path();

        myPath.Stroke = Brushes.Yellow;
        myPath.StrokeThickness = 1;


        pathFigure.StartPoint = ctksLine.StartPoint;
        segment.Point = ctksLine.EndPoint;

        LineSegment lastSegment = new LineSegment();
        lastSegment.Point = new Point(x3, y3);

        PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();
        myPathSegmentCollection.Add(segment);
        myPathSegmentCollection.Add(lastSegment);

        pathFigure.Segments = myPathSegmentCollection;

        PathFigureCollection myPathFigureCollection = new PathFigureCollection();
        myPathFigureCollection.Add(pathFigure);

        PathGeometry myPathGeometry = new PathGeometry();
        myPathGeometry.Figures = myPathFigureCollection;
        myPath.Data = myPathGeometry;


        canvas.Children.Add(myPath);
        renderedLines.Add(myPath);
      }

      RenderIntersections();
    }

    #endregion

    public void CrateCtks(IList<Candle> candles, Action createChart)
    {
      Candles = null;
      canvas.Children.Clear();
      ctksIntersections.Clear();
      renderedIntersections.Clear();
      ctksLines.Clear();

      createChart();
      Candles = candles;

      CreateLines(candles, timeFrame);
      AddIntersections();
    }
  }
}
