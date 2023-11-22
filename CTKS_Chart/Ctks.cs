using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveChart.Annotations;

namespace CTKS_Chart
{
  public enum LineType
  {
    LeftBottom,
    RightBttom,
    LeftTop,
    RightTop
  }

  public class CtksIntersection
  {
    public CtksLine Line { get; set; }
    public double Value { get; set; }
  }

  public class CtksLine
  {
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }

    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }

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
    private readonly Func<Canvas, double, double> getCanvasValue;

    public Ctks(Canvas canvas, [NotNull] Func<Canvas, double, double> getCanvasValue)
    {
      this.canvas = canvas;
      this.getCanvasValue = getCanvasValue ?? throw new ArgumentNullException(nameof(getCanvasValue));
    }

    public List<CtksLine> ctksLines = new List<CtksLine>();
    public List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();

    public List<Path> renderedLines = new List<Path>();
    public List<RenderedIntersection> renderedIntersections = new List<RenderedIntersection>();

    public bool LinesVisible { get; set; }
    public bool IntersectionsVisible { get; set; }

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

    public void CreateLine(int candleIndex, int secondIndex, LineType lineType)
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
        startPoint = new Point(Canvas.GetLeft(firstRect) + firstRect.Width, canvas.ActualHeight - bottom1);
        endPoint = new Point(Canvas.GetLeft(secondrect) + secondrect.Width, canvas.ActualHeight - bottom2);

        y1 = bottom1;
        y2 = bottom2;

        x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
        x2 = Canvas.GetLeft(secondrect) + secondrect.Width;

        //Canvas.SetBottom(myPath, y1);
      }
      else if (lineType == LineType.LeftTop)
      {
        startPoint = new Point(Canvas.GetLeft(firstRect), canvas.ActualHeight - bottom1 - firstRect.Height);
        endPoint = new Point(Canvas.GetLeft(secondrect), canvas.ActualHeight - bottom2 - secondrect.Height);

        y1 = bottom1 + firstRect.Height;
        y2 = bottom2 + secondrect.Height;

        x1 = Canvas.GetLeft(firstRect);
        x2 = Canvas.GetLeft(secondrect);

        //Canvas.SetBottom(myPath, y1);
      }
      else if (lineType == LineType.RightTop)
      {
        startPoint = new Point(Canvas.GetLeft(firstRect) + firstRect.Width, canvas.ActualHeight - bottom1 - firstRect.Height);
        endPoint = new Point(Canvas.GetLeft(secondrect) + secondrect.Width, canvas.ActualHeight - bottom2 - secondrect.Height);

        y1 = bottom1 + firstRect.Height;
        y2 = bottom2 + secondrect.Height;

        x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
        x2 = Canvas.GetLeft(secondrect) + secondrect.Width;
      }
      else if (lineType == LineType.LeftBottom)
      {
        startPoint = new Point(Canvas.GetLeft(firstRect), canvas.ActualHeight - bottom1);
        endPoint = new Point(Canvas.GetLeft(secondrect), canvas.ActualHeight - bottom2);

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
        EndPoint = endPoint
      };

      ctksLines.Add(line);
    }

    #endregion

    #region CreateLines

    public void CreateLines(IList<Candle> candles)
    {
      for (int i = 0; i < candles.Count - 2; i++)
      {
        var currentCandle = candles[i];
        var nextCandle = candles[i + 1];

        if (currentCandle.IsGreen)
        {
          if (nextCandle.IsGreen)
            CreateLine(i, i + 1, LineType.LeftTop);

          if (currentCandle.Close < nextCandle.Close || (currentCandle.Open < nextCandle.Close))
            CreateLine(i, i + 1, LineType.RightBttom);
        }
        else
        {
          if (!nextCandle.IsGreen)
          {
            CreateLine(i, i + 1, LineType.RightTop);
            CreateLine(i, i + 1, LineType.LeftBottom);
          }
        }
      }
    }

    #endregion

    #region AddIntersections

    public void AddIntersections(Rectangle lastCandle)
    {
      foreach (var line in ctksLines)
      {
        var actualLeft = Canvas.GetLeft(lastCandle) + lastCandle.Width / 2;
        var actual = GetPointOnLine(line.X1, line.Y1, line.X2, line.Y2, actualLeft);
        var value = getCanvasValue(canvas, actual);

        var intersection = new CtksIntersection()
        {
          Line = line,
          Value = value
        };

        ctksIntersections.Add(intersection);
      }
    }

    #endregion


    public void ClearRenderedIntersections()
    {
      foreach (var intersection in renderedIntersections)
      {
        canvas.Children.Remove(intersection.Line);
        canvas.Children.Remove(intersection.Text);
        canvas.Children.Remove(intersection.Mark);
      }

      renderedIntersections.Clear();
    }

    public void ClearRenderedLines()
    {
      foreach (var line in renderedLines)
      {
        canvas.Children.Remove(line);
      }

      renderedLines.Clear();
    }

    #region RenderIntersections

    public void RenderIntersections(Rectangle lastCandle, double? max = null)
    {
      foreach (var intersection in ctksIntersections)
      {
        var circle = new Ellipse();
        var size = 3;
        circle.Width = size;
        circle.Height = size;
        circle.Fill = Brushes.Red;

        var actualLeft = Canvas.GetLeft(lastCandle) + lastCandle.Width / 2;
        var line = intersection.Line;

        var actual = GetPointOnLine(line.X1, line.Y1, line.X2, line.Y2, actualLeft);

        Canvas.SetLeft(circle, actualLeft - size / 2.0);
        Canvas.SetBottom(circle, actual - size / 2.0);

        var target = new Line();
        target.Stroke = Brushes.Gray;
        target.StrokeThickness = 2;
        target.X1 = 150;
        target.X2 = canvas.ActualWidth;
        target.StrokeDashArray = new DoubleCollection() { 1, 1 };

        var lineY = canvas.ActualHeight - actual;

        target.Y1 = lineY;
        target.Y2 = lineY;

        var text = new TextBlock();


        if (intersection.Value > max)
        {
          return;
        }

        text.Text = intersection.Value.ToString("N2");
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
        var x3 = canvas.ActualWidth * 1.5;
        var y3 = GetPointOnLine(ctksLine.X1, ctksLine.Y1, ctksLine.X2, ctksLine.Y2, x3);

        PathFigure pathFigure = new PathFigure();
        LineSegment segment = new LineSegment();
        Path myPath = new Path();

        myPath.Stroke = Brushes.Yellow;
        myPath.StrokeThickness = 1;


        pathFigure.StartPoint = ctksLine.StartPoint;
        segment.Point = ctksLine.EndPoint;

        LineSegment lastSegment = new LineSegment();
        lastSegment.Point = new Point(x3, canvas.ActualHeight - y3);

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

  }
}
