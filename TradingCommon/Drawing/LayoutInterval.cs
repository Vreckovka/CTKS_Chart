using Binance.Net.Enums;

namespace CTKS_Chart.ViewModels
{
  public class LayoutInterval
  {
    public string Title { get; set; }
    public KlineInterval Interval { get; set; }
    public bool IsFavorite { get; set; }
  }
}