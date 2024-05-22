using System;

namespace CTKS_Chart.ViewModels
{
  public class SimulationResultDataPoint
  {
    public DateTime Date { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalNative { get; set; }
    public decimal TotalNativeValue { get; set; }
    public decimal Close { get; set; }
  }
}
