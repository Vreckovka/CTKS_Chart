using System.Windows;

namespace CTKS_Chart.Trading
{
  public class CtksLine
  {
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }

    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }

    public TimeFrame TimeFrame { get; set; }

    public bool IsSame(CtksLine other)
    {
      return StartPoint == other.StartPoint && EndPoint == other.EndPoint;
    }
  }
}