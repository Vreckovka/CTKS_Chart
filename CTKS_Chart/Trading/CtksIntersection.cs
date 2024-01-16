namespace CTKS_Chart.Trading
{
  public class CtksIntersection
  {
    public decimal Value { get; set; }
    public TimeFrame TimeFrame { get; set; }
    public CtksLine Line { get; set; }

    public bool IsSame(CtksIntersection other)
    {
      //return Value == other.Value;
      return Line.IsSame(other.Line);
    }
  }
}
