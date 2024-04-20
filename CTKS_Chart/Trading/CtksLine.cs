using System.Windows;

namespace CTKS_Chart.Trading
{
  public class CtksLinePoint
  {
    public decimal Price { get; set; }
    public long UnixTime { get; set; }
  }
  public class CtksLine
  {
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }

    public TimeFrame TimeFrame { get; set; }

    public int? FirstIndex { get; set; }
    public int? SecondIndex { get; set; }

    public LineType LineType { get; set; }

    public CtksLinePoint FirstPoint { get; set; }
    public CtksLinePoint SecondPoint { get; set; }

    public bool IsSame(CtksLine other)
    {
      return
        FirstIndex == other.FirstIndex &&
        LineType == other.LineType &&
        SecondIndex == other.SecondIndex &&
        TimeFrame == other.TimeFrame;
    }
  }
}