using System;

namespace CTKS_Chart.ViewModels
{
  public class State
  {
    public decimal TotalValue { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalNative { get; set; }
    public decimal TotalNativeValue { get; set; }
    public DateTime Date { get; set; }
    public decimal AthPrice { get; set; }
    public decimal? ClosePrice { get; set; }
  }
}