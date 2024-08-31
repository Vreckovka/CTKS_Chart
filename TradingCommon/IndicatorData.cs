using System;
using VCore.Standard.Helpers;

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
      return new decimal[] { RangeFilter, HighTarget, LowTarget, Upward ? 1 : 0 };
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
      return new decimal[] { K / 100.0m, D / 100.0m };
    }
  }

  public class BBWPData : Indicator
  {
    public decimal BBWP { get; set; }
    public decimal MA1 { get; set; }
    public decimal MA2 { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { BBWP / 100.0m, MA1 / 100.0m, MA2 / 100.0m };
    }
  }

  public class RSIData : Indicator
  {
    public decimal RSI { get; set; }
    public decimal RSIMA { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { RSI / 1000.0m, RSIMA / 100.0m };
    }
  }

  public class ATRData : Indicator
  {
    public decimal Line { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { Line };
    }
  }

  public class VortexIndicatorData : Indicator
  {
    public decimal VIPlus { get; set; }
    public decimal VIMinus { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { MathHelper.NormalizedValue(VIPlus, 0, 1.5m), MathHelper.NormalizedValue(VIMinus, 0, 1.5m) };
    }
  }

  public class ADXData : Indicator
  {
    public decimal ADX { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { ADX / 100.0m };
    }
  }

  public class MFIData : Indicator
  {
    public decimal MF { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { MF / 100.0m };
    }
  }

  public class AwesomeOsiclatorData : Indicator
  {
    public decimal Plot { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { MathHelper.NormalizedValue(Plot, -1, 1) };
    }
  }

  public class MACDData : Indicator
  {
    public decimal Histogram { get; set; }
    public decimal MACD { get; set; }
    public decimal EMA { get; set; }

    public override decimal[] GetData()
    {
      return new decimal[] { MathHelper.NormalizedValue(Histogram, -1, 1), MathHelper.NormalizedValue(MACD, -1, 1), MathHelper.NormalizedValue(EMA, -1, 1) };
    }
  }

  public class HighTimeFrameIndicatorData
  {
    public VortexIndicatorData VI { get; set; } = new VortexIndicatorData();
    public ADXData ADX { get; set; } = new ADXData();
    public MFIData MFI { get; set; } = new MFIData();
    public AwesomeOsiclatorData AO { get; set; } = new AwesomeOsiclatorData();
    public MACDData MACD { get; set; } = new MACDData();

    public int NumberOfInputs
    {
      get
      {
        return
          VI.GetData().Length +
          ADX.GetData().Length +
          MFI.GetData().Length +
          AO.GetData().Length +
          MACD.GetData().Length;
      }
    }
  }

  public class IndicatorData
  {
    public RangeFilterData RangeFilter { get; set; } = new RangeFilterData();
    public IchimokuCloud IchimokuCloud { get; set; } = new IchimokuCloud();
    public ATRData ATR { get; set; } = new ATRData();

    public BBWPData BBWP { get; set; } = new BBWPData();
    public VortexIndicatorData VI { get; set; } = new VortexIndicatorData();
    public ADXData ADX { get; set; } = new ADXData();
    public MFIData MFI { get; set; } = new MFIData();
    public AwesomeOsiclatorData AO { get; set; } = new AwesomeOsiclatorData();
    public MACDData MACD { get; set; } = new MACDData();

    public int NumberOfInputs
    {
      get
      {
        return
          RangeFilter.GetData().Length +
          IchimokuCloud.GetData().Length +
          ATR.GetData().Length +

          BBWP.GetData().Length +
          VI.GetData().Length +
          ADX.GetData().Length +
          MFI.GetData().Length +
          AO.GetData().Length +
          MACD.GetData().Length;
      }
    }
  }
}