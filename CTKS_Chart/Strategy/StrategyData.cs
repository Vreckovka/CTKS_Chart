using System.Collections.Generic;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Strategy
{
  public class StrategyData
  {
    public double ScaleSize { get; set; }
    public decimal StartingBudget { get; set; }
    public decimal Budget { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalNativeAsset { get; set; }
    public IEnumerable<KeyValuePair<TimeFrame, decimal>> PositionSizeMapping { get; set; }
    public decimal MinBuyPrice { get; set; }
    public decimal? MaxBuyPrice { get; set; }
    public decimal? MinSellPrice { get; set; }
    public decimal AutomaticBudget { get; set; }
    public decimal MaxAutomaticBudget { get; set; }
    public decimal AutomaticPositionSize { get; set; } = (decimal)0.5;
    public bool EnableManualPositions { get; set; } = true;
    public bool EnableAutoPositions { get; set; } = true;
    public bool EnableRangeFilterStrategy { get; set; }
  }
}