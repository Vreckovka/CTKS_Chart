using System.Windows;

namespace CTKS_Chart.Trading
{
  public class ChartCandle 
  {
    public Candle Candle { get; set; }
    public Rect Body { get; set; }
    public Rect? TopWick { get; set; }
    public Rect? BottomWick { get; set; }

  }
}