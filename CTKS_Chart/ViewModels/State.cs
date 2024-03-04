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
    public decimal ValueToNative { get; set; }
    public decimal ValueToBTC { get; set; }
    public decimal? TotalAutoProfit { get; set; }
    public decimal? TotalManualProfit { get; set; }
    public decimal? ActualValue { get; set; }
    public decimal? ActualAutoValue { get; set; }
  }
}