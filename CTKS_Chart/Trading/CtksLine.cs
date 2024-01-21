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

    public int? FirstIndex { get; set; }
    public int? SecondIndex { get; set; }

    public LineType LineType { get; set; }

    public float FirstCandleUnixTime { get; set; }
    public float SecondCandleUnixTime { get; set; }

    public bool IsSame(CtksLine other)
    {
      return
        FirstCandleUnixTime == other.FirstCandleUnixTime &&
        LineType == other.LineType &&
        SecondCandleUnixTime == other.SecondCandleUnixTime &&
        TimeFrame == other.TimeFrame;
    }
  }
}