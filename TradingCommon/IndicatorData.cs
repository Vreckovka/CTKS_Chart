namespace CTKS_Chart.Trading
{
  public abstract class Indicator
  {
    public abstract decimal[] GetData();
  }

  public class RangeFilterData : Indicator
  {
    public decimal RangeFilter { get; set; }
    public decimal HighTarget { get; set; }
    public decimal LowTarget { get; set; }
    public bool Upward { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { RangeFilter, HighTarget, LowTarget, Upward ? 1 : -1 };
    }
  }

  public class IchimokuCloud : Indicator
  {
    public decimal ConversionLine { get; set; }
    public decimal BaseLine { get; set; }
    public decimal UpperCloud { get; set; }
    public decimal LowerCloud { get; set; }
    public decimal LeadingSpanA { get; set; }
    public decimal LeadingSpanB { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { ConversionLine, BaseLine, UpperCloud, LowerCloud, LeadingSpanA, LeadingSpanB };
    }
  }

  public class StochRSI : Indicator
  {
    public decimal K { get; set; }
    public decimal D { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { K / 100.0m, D / 100.0m  };
    }
  }

  public class BBWPData : Indicator
  {
    public decimal BBWP { get; set; }
    public decimal ExtremeHi { get; set; }
    public decimal ExtremeLo { get; set; }
    public decimal MA1 { get; set; }
    public decimal MA2 { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { BBWP / 100.0m, ExtremeHi / 100.0m, ExtremeLo / 100.0m, MA1 / 100.0m, MA2 / 100.0m };
    }
  }

  public class RSIData : Indicator
  {
    public decimal RSI { get; set; }
    public decimal RSIMA { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { RSI / 1000.0m, RSIMA / 100.0m};
    }
  }

  public class IndicatorData
  {
    public RangeFilterData RangeFilter { get; set; }
    public BBWPData BBWP { get; set; }
    public IchimokuCloud IchimokuCloud { get; set; }
    public StochRSI StochRSI { get; set; }
    public RSIData RSI { get; set; }
  }
}