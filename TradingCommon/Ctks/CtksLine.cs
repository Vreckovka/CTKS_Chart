

namespace CTKS_Chart.Trading
{
  public struct PositionPoint
  {
    public PositionPoint(double x, double y)
    {
      X = x;
      Y = y;
    }

    public double X { get; set; }
    public double Y { get; set; }
  }
  public class CtksLinePoint
  {
    public decimal Price { get; set; }
    public long UnixTime { get; set; }
  }
  public class CtksLine
  {
    public PositionPoint StartPoint { get; set; }
    public PositionPoint EndPoint { get; set; }

    public TimeFrame TimeFrame { get; set; }


    public LineType LineType { get; set; }

    public CtksLinePoint FirstPoint { get; set; }
    public CtksLinePoint SecondPoint { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsSame(CtksLine other)
    {
      return
        FirstPoint.Price == other.FirstPoint.Price &&
         SecondPoint.Price == other.SecondPoint.Price &&
        LineType == other.LineType &&
        TimeFrame == other.TimeFrame;
    }
  }
}