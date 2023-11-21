using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

  public class Ctks
  {
    private Canvas canvas;
    public Ctks(Canvas canvas)
    {
      this.canvas = canvas;
    }


    public void CreateLine(int candleIndex, int secondIndex, LineType lineType, Func<double, double> getCanvasValue)
    {
      var candles = canvas.Children.OfType<Rectangle>().ToList();
      var firstRect = candles[candleIndex];
      var secondrect = candles[secondIndex];

      var bottom1 = Canvas.GetBottom(firstRect);
      var bottom2 = Canvas.GetBottom(secondrect);

      PathFigure myPathFigure = new PathFigure();
      LineSegment myLineSegment = new LineSegment();
      Path myPath = new Path();

      myPath.Stroke = Brushes.Yellow;
      myPath.StrokeThickness = 1;

      double y1 = 0.0;
      double y2 = 0.0;

      double x1 = 0.0;
      double x2 = 0.0;

      if (lineType == LineType.RightBttom)
      {
        myPathFigure.StartPoint = new Point(Canvas.GetLeft(firstRect) + firstRect.Width, canvas.ActualHeight - bottom1);
        myLineSegment.Point = new Point(Canvas.GetLeft(secondrect) + secondrect.Width, canvas.ActualHeight - bottom2);

        y1 = bottom1;
        y2 = bottom2;

        x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
        x2 = Canvas.GetLeft(secondrect) + secondrect.Width;

        Canvas.SetBottom(myPath, y1);
      }
      else if (lineType == LineType.LeftTop)
      {
        myPathFigure.StartPoint = new Point(Canvas.GetLeft(firstRect), canvas.ActualHeight - bottom1 - firstRect.Height);
        myLineSegment.Point = new Point(Canvas.GetLeft(secondrect), canvas.ActualHeight - bottom2 - secondrect.Height);

        y1 = bottom1 + firstRect.Height;
        y2 = bottom2 + secondrect.Height;

        x1 = Canvas.GetLeft(firstRect);
        x2 = Canvas.GetLeft(secondrect);

        Canvas.SetBottom(myPath, y1);
      }
      else if (lineType == LineType.RightTop)
      {
        myPathFigure.StartPoint = new Point(Canvas.GetLeft(firstRect) + firstRect.Width, canvas.ActualHeight - bottom1 - firstRect.Height);
        myLineSegment.Point = new Point(Canvas.GetLeft(secondrect) + secondrect.Width, canvas.ActualHeight - bottom2 - secondrect.Height);

        y1 = bottom1 + firstRect.Height;
        y2 = bottom2 + secondrect.Height;

        x1 = Canvas.GetLeft(firstRect) + firstRect.Width;
        x2 = Canvas.GetLeft(secondrect) + secondrect.Width;
      }

      var x3 = canvas.ActualWidth * 1.5;
      var y3 = GetPointOnLine(x1, y1, x2, y2, x3);

      LineSegment lastSegment = new LineSegment();
      lastSegment.Point = new Point(x3, canvas.ActualHeight - y3);

      if (lineType == LineType.RightTop)
        Canvas.SetBottom(myPath, canvas.ActualHeight - lastSegment.Point.Y);


      AddIntersections(x1, y1, x2, y2, getCanvasValue);

      PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();
      myPathSegmentCollection.Add(myLineSegment);
      myPathSegmentCollection.Add(lastSegment);

      myPathFigure.Segments = myPathSegmentCollection;

      PathFigureCollection myPathFigureCollection = new PathFigureCollection();
      myPathFigureCollection.Add(myPathFigure);

      PathGeometry myPathGeometry = new PathGeometry();
      myPathGeometry.Figures = myPathFigureCollection;
      myPath.Data = myPathGeometry;


      canvas.Children.Add(myPath);

    }

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

    private void AddIntersections(double x1, double y1, double x2, double y2, Func<double, double> getCanvasValue)
    {
      var candles = canvas.Children.OfType<Rectangle>().ToList();

      var circle = new Ellipse();
      var size = 5;
      circle.Width = size;
      circle.Height = size;
      circle.Fill = Brushes.Red;

      var lastCandl = candles[candles.Count - 1];
      var actualLeft = Canvas.GetLeft(lastCandl) + lastCandl.Width / 2;
      var actual = GetPointOnLine(x1, y1, x2, y2, actualLeft);

      Canvas.SetLeft(circle, actualLeft - size / 2.0);
      Canvas.SetBottom(circle, actual - size / 2.0);


      var line = new Line();
      line.Stroke = Brushes.Gray;
      line.StrokeThickness = 5;
      line.X1 = 150;
      line.X2 = canvas.ActualWidth;
      line.StrokeDashArray = new DoubleCollection() { 1, 1 };

      

      var lineY = canvas.ActualHeight - actual;

      line.Y1 = lineY;
      line.Y2 = lineY;

      var text = new TextBlock();
      text.Text = getCanvasValue(actual).ToString("N2") + "";
      text.Foreground = Brushes.White;
      Panel.SetZIndex(text, 99);

      Canvas.SetLeft(text, 0);
      Canvas.SetBottom(text, actual);

      canvas.Children.Add(circle);
      canvas.Children.Add(line);
      canvas.Children.Add(text);
    }
  }
}
