using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VCore;
using VCore.Standard.Helpers;

namespace CTKS_Chart
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