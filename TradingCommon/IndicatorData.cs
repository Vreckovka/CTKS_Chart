namespace CTKS_Chart.Trading
{
  public class RangeFilterData
  {
    public decimal RangeFilter { get; set; }
    public decimal HighTarget { get; set; }
    public decimal LowTarget { get; set; }
    public bool Upward { get; set; }
  }

  public class IndicatorData
  {
    public RangeFilterData RangeFilterData { get; set; }
    public decimal BBWP { get; set; }
  }
}