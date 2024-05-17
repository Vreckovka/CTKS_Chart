using System.Collections.Generic;
using Binance.Net.Enums;

namespace CTKS_Chart.ViewModels
{
  public class LayoutSettings
  {
    public KlineInterval LayoutInterval { get; set; }
    public IEnumerable<ColorSetting> ColorSettings { get; set; }
    public DrawingSettings DrawingSettings { get; set; }

    public decimal StartLowPrice { get; set; }
    public decimal StartMaxPrice { get; set; }
    public long StartMaxUnix { get; set; }
    public long StartMinUnix { get; set; }

  }
}