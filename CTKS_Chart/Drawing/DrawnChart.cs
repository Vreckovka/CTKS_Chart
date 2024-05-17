using System.Collections.Generic;
using CTKS_Chart.Trading;

namespace CTKS_Chart.ViewModels
{
  public class DrawnChart
  {
    public IEnumerable<ChartCandle> Candles { get; set; }
  }
}