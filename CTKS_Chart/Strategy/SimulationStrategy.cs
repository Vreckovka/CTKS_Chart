using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VCore.Standard.Helpers;

namespace CTKS_Chart
{
  public class PositionDto : Position
  {
    public PositionDto()
    {

    }

    public PositionDto(Position position)
    {
      PositionSize = position.PositionSize;
      PositionSizeNative = position.PositionSizeNative;
      TimeFrame = position.TimeFrame;
      OriginalPositionSize = position.OriginalPositionSize;
      OriginalPositionSizeNative = position.OriginalPositionSizeNative;
      Id = position.Id;
      Price = position.Price;
      Profit = position.Profit;
      State = position.State;
      Side = position.Side;
      OpositePositions = position.OpositPositions.Select(x => x.Id).ToArray();
      Intersection = position.Intersection;
    }


    public long[] OpositePositions { get; set; }
  }

  public class SimulationStrategy : Strategy
  {
    public override void LoadState()
    {
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

    public override void SaveState()
    {
      
    }
  }
}