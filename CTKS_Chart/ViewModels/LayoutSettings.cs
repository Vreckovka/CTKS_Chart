using System.Collections.Generic;
using Binance.Net.Enums;

namespace CTKS_Chart.ViewModels
{
  public class LayoutSettings
  {
    public bool ShowClosedPositions { get; set; }
    public KlineInterval LayoutInterval { get; set; }
    public IEnumerable<ColorSetting> ColorSettings { get; set; }

    public bool ShowAveragePrice { get; set; }

    public bool ShowATH { get; set; }

  }
}