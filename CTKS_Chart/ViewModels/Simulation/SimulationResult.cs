using System;
using System.Collections.Generic;

namespace CTKS_Chart.ViewModels
{
  public class SimulationResult
  {
    public SimulationResult()
    {
    }

    public decimal TotalValue { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalNativeValue { get; set; }
    public decimal TotalNative { get; set; }
    public TimeSpan RunTime { get; set; }
    public long RunTimeTicks { get; set; }
    public DateTime Date { get; set; }
    public string BotName { get; set; }

    public SimulationResultValue MaxValue { get; set; } = new SimulationResultValue();
    public SimulationResultValue LowAfterMaxValue { get; set; } = new SimulationResultValue(decimal.MaxValue);
    public IList<SimulationResultDataPoint> DataPoints { get; set; } = new List<SimulationResultDataPoint>();

    public decimal Drawdawn
    {
      get;
      set;
    }
  }
}
