using System.Linq;
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
      if (position.Side == PositionSide.Buy && candle.Low <= position.Price)
      {
        return true;
      }
      else if (position.Side == PositionSide.Sell && candle.High >= position.Price)
        return true;

      return false;
    }

    Candle lastCandle = null;
    public override async void ValidatePositions(Candle candle)
    {
      lastCandle = candle;
      var allPositions = AllOpenedPositions
        .Where(x => x.State == PositionState.Open)
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
            CloseSell(position);
          }
        }
      }

      var openSells = AllOpenedPositions.Where(x => x.State == PositionState.Open && x.Side == PositionSide.Sell && x.Price < candle.Low).ToList();

      foreach (var openSell in openSells)
      {
        CloseSell(openSell);
      }


      base.ValidatePositions(candle);
    }

    protected override Task<bool> CancelPosition(Position position)
    {
      return Task.FromResult(true);
    }

    private long actual = 1;
    protected override Task<long> CreatePosition(Position position)
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