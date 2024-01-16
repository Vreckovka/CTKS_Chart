namespace CTKS_Chart.Trading
{
  public class CtksIntersection
  {
    public decimal Value { get; set; }
    public TimeFrame TimeFrame { get; set; }
    public CtksLine Line { get; set; }

    public bool IsSame(CtksIntersection other)
    {
      if (Line != null && other.Line != null)
      {
        return Line.IsSame(other.Line) && TimeFrame == other.TimeFrame;
      }
      else
        return Value == other.Value && TimeFrame == other.TimeFrame;
    }
  }
}
