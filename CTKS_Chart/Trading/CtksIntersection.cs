namespace CTKS_Chart.Trading
{
  public class CtksIntersection
  {
    public decimal Value { get; set; }
    public TimeFrame TimeFrame { get; set; }
    public CtksLine Line { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsSame(CtksIntersection other)
    {
      if (Line != null && other.Line != null)
      {
        var result = Line.IsSame(other.Line);

        return result;
      }
      else
        return Value == other.Value && TimeFrame == other.TimeFrame;
    }
  }
}
