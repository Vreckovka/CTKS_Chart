using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace CTKS_Chart
{
  public enum LineType
  {
    LeftBottom,
    RightBttom,
    LeftTop,
    RightTop
  }

  public enum TimeFrame
  {
    Null = 7,
    M12 = 6,
    M6 = 5,
    M3 = 4,
    M1 =3,
    W2 = 2,
    W1 = 1,
    D1 =1
  }

  public class CtksIntersection
  {
    public CtksLine Line { get; set; }
    public decimal Value { get; set; }
    public TimeFrame TimeFrame { get; set; }

    public int Id { get; set; }
  }

  public class CtksLine
  {
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }

    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }

    public TimeFrame TimeFrame { get; set; }

  }

  public class RenderedIntersection
  {
    public Line Line { get; set; }
    public Ellipse Mark { get; set; }
    public TextBlock Text { get; set; }
  }




  public class Ctks
  {
    private Canvas canvas;

    private readonly Layout layout;
    private readonly TimeFrame timeFrame;

    private double canvasHeight;
    private double canvasWidth;

    public Ctks(Layout layout, TimeFrame timeFrame, double canvasHeight, double canvasWidth)
    {
      this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
      this.canvas = layout.Canvas;
      this.timeFrame = timeFrame;

      this.canvasHeight = canvasHeight;
      this.canvasWidth = canvasWidth;
    }

    public List<CtksLine> ctksLines = new List<CtksLine>();
    public List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();

    public List<Path> renderedLines = new List<Path>();
    public List<RenderedIntersection> renderedIntersections = new List<RenderedIntersection>();

    public bool LinesVisible { get; set; }
    public bool IntersectionsVisible { get; set; }

    public IList<Candle> Candles { get; set; }

    #region GetPointOnLine

    private double GetPointOnLine(double x1, double y1, double x2, double y2, double x3)
    {
      double deltaY = y2 - y1;
      double deltaX = x2 - x1;

      var slope = deltaY / deltaX;
      //https://www.mathsisfun.com/algebra/line-equation-point-slope.html
      //y − y1 = m(x − x1)
      //x = x1 + ((y -y1) / m)
      //y = m(x − x1) + y1
      return (slope * (x3 - x1)) + y1;
    }

    #endregion

    #region CreateLine

    public void CreateLine(int candleIndex, int secondIndex, LineType lineType, TimeFrame timeFrame)
    {
      var candles = canvas.Children.OfType<Rectangle>().ToList();
      var firstRect = candles[candleIndex];
      var secondrect = candles[secondIndex];

      var bottom1 = Canvas.GetBottom(firstRect);
      var bottom2 = Canvas.GetBottom(secondrect);

      double y1 = 0.0;
      double y2 = 0.0;

      double x1 = 0.0;
      double x2 = 0.0;

      var startPoint = new Point();
      var endPoint = new Point();

      if (lineType == LineType.RightBttom)
      {
        startPoint = new Point(Canvas.GetLeft(firstRect) + firstRect.Width, canvasHeight - bottom1);
        endPoint = new Point(Canvas.GetLeft(secondrect) + secondrect.Width, canvasHeight - bottom2);

        y1 = bottom1;
        y2 = bottom2;

        x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
        x2 = Canvas.GetLeft(secondrect) + secondrect.Width;

        //Canvas.SetBottom(myPath, y1);
      }
      else if (lineType == LineType.LeftTop)
      {
        startPoint = new Point(Canvas.GetLeft(firstRect), canvasHeight - bottom1 - firstRect.Height);
        endPoint = new Point(Canvas.GetLeft(secondrect), canvasHeight - bottom2 - secondrect.Height);

        y1 = bottom1 + firstRect.Height;
        y2 = bottom2 + secondrect.Height;

        x1 = Canvas.GetLeft(firstRect);
        x2 = Canvas.GetLeft(secondrect);

        //Canvas.SetBottom(myPath, y1);
      }
      else if (lineType == LineType.RightTop)
      {
        startPoint = new Point(Canvas.GetLeft(firstRect) + firstRect.Width, canvasHeight - bottom1 - firstRect.Height);
        endPoint = new Point(Canvas.GetLeft(secondrect) + secondrect.Width, canvasHeight - bottom2 - secondrect.Height);

        y1 = bottom1 + firstRect.Height;
        y2 = bottom2 + secondrect.Height;

        x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
        x2 = Canvas.GetLeft(secondrect) + secondrect.Width;
      }
      else if (lineType == LineType.LeftBottom)
      {
        startPoint = new Point(Canvas.GetLeft(firstRect), canvasHeight - bottom1);
        endPoint = new Point(Canvas.GetLeft(secondrect), canvasHeight - bottom2);

        y1 = bottom1;
        y2 = bottom2;

        x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
        x2 = Canvas.GetLeft(secondrect) + secondrect.Width;
      }


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
        TimeFrame = timeFrame
      };

      ctksLines.Add(line);
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
            CreateLine(i, i + 1, LineType.LeftTop, timeFrame);

          if (currentCandle.Close < nextCandle.Close || (currentCandle.Open < nextCandle.Close))
            CreateLine(i, i + 1, LineType.RightBttom, timeFrame);
        }
        else
        {
          if (!nextCandle.IsGreen)
          {
            CreateLine(i, i + 1, LineType.RightTop, timeFrame);
            CreateLine(i, i + 1, LineType.LeftBottom, timeFrame);
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
      
      foreach (var line in ctksLines)
      {
        var actualLeft = Canvas.GetLeft(lastCandle) + lastCandle.Width / 2;
        var actual = GetPointOnLine(line.X1, line.Y1, line.X2, line.Y2, actualLeft);
        var value = GetValueFromCanvas(canvasHeight, actual);

        var intersection = new CtksIntersection()
        {
          Line = line,
          Value = value,
          TimeFrame = line.TimeFrame,
          Id = intersectionId
        };

        ctksIntersections.Add(intersection);
        intersectionId++;
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
      IntersectionsVisible = true;

      var lastCandle = canvas.Children.OfType<Rectangle>().Last();

      var inter = intersections ?? ctksIntersections;

      var maxCanvasValue = GetValueFromCanvas(canvasHeight, canvasHeight);
      var minCanvasValue = GetValueFromCanvas(canvasHeight, 0);

      foreach (var intersection in inter.Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue))
      {
        var circle = new Ellipse();
        var size = 3;
        circle.Width = size;
        circle.Height = size;
        circle.Fill = Brushes.Red;

        var actualLeft = Canvas.GetLeft(lastCandle) + lastCandle.Width / 2;
        var line = intersection.Line;

        var actual = GetCanvasValue(canvasHeight, intersection.Value);

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


        //canvas.Children.Add(circle);
        canvas.Children.Add(target);
        canvas.Children.Add(text);

        renderedIntersections.Add(new RenderedIntersection()
        {
          Line = target,
          Text = text,
          //Mark = circle
        });
      }
    }

    #endregion

    #region GetValueFromCanvas

    private decimal GetValueFromCanvas(double canvasHeight, double value)
    {
      canvasHeight = canvasHeight * 0.75;

      var logMaxValue = Math.Log10((double)layout.MaxValue);
      var logMinValue = Math.Log10((double)layout.MinValue);

      var logRange = logMaxValue - logMinValue;

      var valued = Math.Pow(10, (value * logRange / canvasHeight) + logMinValue);

      if ((double)decimal.MaxValue < valued)
      {
        return decimal.MaxValue;
      }

      return (decimal)valued;
    }

    #endregion

    #region GetCanvasValue

    private double GetCanvasValue(double canvasHeight, decimal value)
    {
      canvasHeight = canvasHeight * 0.75;

      var logValue = Math.Log10((double)value);
      var logMaxValue = Math.Log10((double)layout.MaxValue);
      var logMinValue = Math.Log10((double)layout.MinValue);

      var logRange = logMaxValue - logMinValue;
      double diffrence = logValue - logMinValue;

      return diffrence * canvasHeight / logRange;
    }

    #endregion

    #region RenderLines

    public void RenderLines()
    {
      foreach (var ctksLine in ctksLines)
      {
        var x3 = canvasWidth * 1.5;
        var y3 = GetPointOnLine(ctksLine.X1, ctksLine.Y1, ctksLine.X2, ctksLine.Y2, x3);

        PathFigure pathFigure = new PathFigure();
        LineSegment segment = new LineSegment();
        Path myPath = new Path();

        myPath.Stroke = Brushes.Yellow;
        myPath.StrokeThickness = 1;


        pathFigure.StartPoint = ctksLine.StartPoint;
        segment.Point = ctksLine.EndPoint;

        LineSegment lastSegment = new LineSegment();
        lastSegment.Point = new Point(x3, canvasHeight - y3);

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
    }

    #endregion

    public void CrateCtks(IList<Candle> candles, Action createChart)
    {
      Candles = null;
      canvas.Children.Clear();
      ctksIntersections.Clear();
      ctksLines.Clear();

      createChart();

      CreateLines(candles, timeFrame);
      AddIntersections();
      RenderIntersections();

      Candles = candles;
    }
  }
}
