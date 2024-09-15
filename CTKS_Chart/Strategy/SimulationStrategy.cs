using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;

namespace CTKS_Chart.Strategy
{
  public class SimulationStrategy : BaseSimulationStrategy<Position>
  {

  }

  public class BaseSimulationStrategy<TPosition> : BaseStrategy<TPosition> where TPosition : Position, new()
  {
    public override Task RefreshState()
    {
      return Task.CompletedTask;
    }

    public override void SubscribeToChanges()
    {
    }

    public override bool IsPositionFilled(Candle candle, TPosition position)
    {
      if (position.Side == PositionSide.Buy && candle.Low <= position.Price)
      {
        return true;
      }
      else if (position.Side == PositionSide.Sell && candle.High >= position.Price)
        return true;

      return false;
    }

    public bool IsPositionFilled(Candle candle, PositionSide side, decimal price)
    {
      if (side == PositionSide.Buy && candle.Low <= price)
      {
        return true;
      }
      else if (side == PositionSide.Sell && candle.High >= price)
        return true;

      return false;
    }

    public override async Task ValidatePositions(Candle candle)
    {
      await ValidateSimulationPosition(candle);
      await base.ValidatePositions(candle);
    }


    protected virtual async Task ValidateSimulationPosition(Candle candle)
    {
      var allPositions = AllOpenedPositions
        .OrderByDescending(x => x.Price)
        .ToList();

      foreach (var position in allPositions)
      {
        if (IsPositionFilled(candle, position))
        {
          position.Fees = position.OriginalPositionSize * (decimal)0.001;
          position.FilledDate = candle.OpenTime;

          if (position.Side == PositionSide.Buy)
          {
            await CloseBuy(position, candle.Close.Value);
          }
          else
          {
            await CloseSell(position);
          }
        }
      }

      var openSells = AllOpenedPositions.Where(x => x.State == PositionState.Open && x.Side == PositionSide.Sell && x.Price < candle.Low).ToList();

      foreach (var openSell in openSells)
      {
        await CloseSell(openSell);
      }
    }
    protected override Task<bool> PlaceCancelPosition(TPosition position)
    {
      return Task.FromResult(true);
    }

    private long actual = 1;
    protected override Task<long> PlaceCreatePosition(TPosition position)
    {
      position.CreatedDate = lastCandle.CloseTime;
      return Task.FromResult((long)actual++);
    }


    string path = "State";

    public override void SaveState()
    {

    }

    public override void SaveStrategyData()
    {
      return;
    }

    public override void LoadState()
    {

    }

  }
}