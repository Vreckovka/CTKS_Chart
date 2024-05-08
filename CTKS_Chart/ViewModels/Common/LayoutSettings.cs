using System.Collections.Generic;
using Binance.Net.Enums;

namespace CTKS_Chart.ViewModels
{
  public class LayoutSettings
  {
    public KlineInterval LayoutInterval { get; set; }
    public IEnumerable<ColorSetting> ColorSettings { get; set; }
    public DrawingSettings DrawingSettings { get; set; }
  }
}