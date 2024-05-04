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
        //Simulation is giving significant less value without Value == other.Value;
        var result =
          Line.IsSame(other.Line)
          && Value == other.Value;

        return result;
      }
      else
        return Value == other.Value && TimeFrame == other.TimeFrame;
    }
  }
}
