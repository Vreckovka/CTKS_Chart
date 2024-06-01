namespace CTKS_Chart.Strategy.Futures
{
  public class FuturesPosition : Position
  {
    public FuncionalPosition TakeProfit { get; set; }
    public FuncionalPosition StopLoss { get; set; }
    public decimal PnL { get; set; }
    public decimal Margin { get; set; } = 10;

    public decimal MarginSize
    {
      get
      {
        return Margin * OriginalPositionSize;
      }
    }
  }
}

