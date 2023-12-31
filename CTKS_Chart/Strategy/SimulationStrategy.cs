using System.Threading.Tasks;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Strategy
{
  public class SimulationStrategy : Strategy
  {
    public override Task RefreshState()
    {
      return Task.CompletedTask;
    }

    public override bool IsPositionFilled(Candle candle, Position position)
    {
      if (position.Side == PositionSide.Buy)
      {
        return candle.Low.Value <= position.Price;
      }
      else
      {
        return candle.High.Value >= position.Price;
      }
    }

    protected override Task<bool> CancelPosition(Position position)
    {
      return Task.FromResult(true);
    }

    private long actual = 1;
    protected override Task<long> CreatePosition(Position position)
    {
      return Task.FromResult((long)actual++);
    }


    string path = "State";

    public override void SaveState()
    {
    
    }

    public override void LoadState()
    {
     
    }

  }
}