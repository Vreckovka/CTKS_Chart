using CTKS_Chart.Binance;
using CTKS_Chart.Trading;
using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTKS_Chart.Strategy.Futures
{
  public class FuncionalPosition : FuturesPosition
  {
    public FuturesPosition OriginalPosition { get; set; }
  }

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

 
  public class FuturesSimulationStrategy : BaseSimulationStrategy<FuturesPosition>
  {
    public RangeFilterData RangeFilterData { get; set; }

    public async override void CreatePositions(Candle actualCandle)
    {
      await CheckPositions(actualCandle, 0, decimal.MaxValue);
      CalculatePositions(actualCandle);

      var validIntersections = Intersections;

      var inter = validIntersections
                   .Where(x => x.IsEnabled)
                   .Where(x => x.Value < actualCandle.Close.Value * 0.95m)
                   .ToList();

      var intersection = inter.FirstOrDefault();

      var fistBuy = OpenBuyPositions.FirstOrDefault();
      if (intersection != fistBuy?.Intersection && fistBuy != null && intersection != null)
      {
        var diff = Math.Abs(((intersection.Value - fistBuy.Intersection.Value) / intersection.Value));

        if (diff > 0.05m)
        {
          OpenBuyPositions.Remove(fistBuy);
          Budget += fistBuy.PositionSize;
        }
      }

      RangeFilterData = TradingHelper.GetActualEqivalentCandle(TimeFrame.D1, actualCandle)?.IndicatorData.RangeFilterData;

      //if (RangeFilterData == null || RangeFilterData.Upward)
      {
        if (OpenBuyPositions.Count == 0 && intersection != null && ActualPositions.Count == 0)
          await CreateBuyPositionFromIntersection(intersection);
      }

    }

    protected async override Task CreateBuyPositionFromIntersection(CtksIntersection intersection, bool automatic = false)
    {
      var leftSize = GetPositionSize(intersection.TimeFrame);

      if (GetBudget() > leftSize)
      {
        await CreateBuyPosition(leftSize, intersection, automatic);
      }
    }

    protected override async Task CreateBuyPosition(decimal positionSize, CtksIntersection intersection, bool automatic = false)
    {
      var roundedNativeSize = Math.Round(positionSize / intersection.Value, Asset.NativeRound);
      positionSize = roundedNativeSize * intersection.Value;

      if (positionSize == 0)
        return;

      var newPosition = new FuturesPosition()
      {
        PositionSize = positionSize,
        OriginalPositionSize = positionSize,
        Price = intersection.Value,
        OriginalPositionSizeNative = roundedNativeSize,
        PositionSizeNative = roundedNativeSize,
        TimeFrame = intersection.TimeFrame,
        Intersection = intersection,
        State = PositionState.Open,
        Side = PositionSide.Buy,
        IsAutomatic = automatic,

      };

      var tp = GetTakeProfit(newPosition);
      var sl = GetStopLoss(newPosition);

      if (tp == null || sl == null)
      {
        return;
      }

      newPosition.TakeProfit = tp;
      newPosition.StopLoss = sl;

      var id = await PlaceCreatePosition(newPosition);

      if (id > 0)
      {
        newPosition.Id = id;

        Budget -= newPosition.PositionSize;

        if (automatic)
        {
          AutomaticBudget -= newPosition.PositionSize;
        }

        OpenBuyPositions.Add(newPosition);

        onCreatePositionSub.OnNext(newPosition);
      }

      SaveState();
    }

    private void CalculatePositions(Candle candle)
    {
      var actualPosition = ActualPositions.FirstOrDefault();

      if (actualPosition != null)
      {
        actualPosition.PnL = GetPnl(candle, actualPosition);
      }
    }

    private FuncionalPosition GetTakeProfit(FuturesPosition position)
    {
      var intersection = Intersections.OrderBy(x => x.Value).FirstOrDefault(x => x.Value > position.Price * (1 + 0.05m));

      if (intersection == null)
        return null;

      return new FuncionalPosition()
      {
        Side = PositionSide.Sell,
        Price = intersection.Value,
        Intersection = intersection,
        OriginalPosition = position
      };

    }

    private FuncionalPosition GetStopLoss(FuturesPosition position)
    {
      var intersection = Intersections.OrderByDescending(x => x.Value).FirstOrDefault(x => x.Value < position.Price * (1 - 0.05m));

      if (intersection == null)
        return null;

      return new FuncionalPosition()
      {
        Side = PositionSide.Buy,
        Price = intersection.Value,
        Intersection = intersection,
        OriginalPosition = position
      };
    }

    private decimal GetPnl(Candle candle, FuturesPosition position)
    {
      var actualSize = position.Margin * position.OriginalPositionSizeNative * candle.Close.Value;
      var marginSize = position.OriginalPositionSize + actualSize - position.MarginSize;

      var side = 1;

      if (marginSize - position.OriginalPositionSize < 0)
      {
        side = -1;
      }

      return Math.Abs((position.OriginalPositionSize - marginSize) / position.OriginalPositionSize) * side;
    }

    public override async Task CloseBuy(FuturesPosition position, decimal minForcePrice = 0)
    {
      position.State = PositionState.Filled;

      OpenSellPositions.Add(position.TakeProfit);
      OpenBuyPositions.Add(position.StopLoss);
      OpenBuyPositions.Remove(position);

      ActualPositions.Add(position);
    }

    public override async Task CheckPositions(Candle actualCandle, decimal minBuy, decimal maxBuy)
    {
      var positions = AllOpenedPositions
        .OfType<FuncionalPosition>()
       .Where(x => x.Price != x.Intersection.Value)
       .ToList();

      foreach (var position in positions)
      {
        await CancelPosition(position);
      }

      foreach (var position in positions)
      {
        if (position.Side == PositionSide.Sell)
        {
          var newPosition = GetTakeProfit(position.OriginalPosition);

          if (newPosition != null)
            position.OriginalPosition.TakeProfit = newPosition;
          else
            ;
        }
        else
        {
          var newPosition = GetStopLoss(position.OriginalPosition);
          if (newPosition != null)
            position.OriginalPosition.StopLoss = newPosition;
          else
            ;
        }
      }

      var positionss = AllOpenedPositions
        .Where(x => x.StopLoss != null)
      .Where(x => x.Price != x.Intersection.Value)
      .ToList();

      foreach (var position in positionss)
      {
        await CancelPosition(position);
        ActualPositions.Remove(position);
      }
    }

    public override async void ValidatePositions(Candle candle)
    {
      await ValidateSimulationPosition(candle, OpenBuyPositions.Where(x => x.StopLoss != null));

      var openedBuy = OpenBuyPositions.ToList();

      var assetsValue = TotalNativeAsset * candle.Close.Value;
      var openPositions = openedBuy.Sum(x => x.PositionSize);

      TotalValue = assetsValue + openPositions + Budget;
      TotalNativeAssetValue = TotalNativeAsset * candle.Close.Value;

      if (MaxTotalValue < TotalValue)
      {
        MaxTotalValue = TotalValue;
      }

      RaisePropertyChanged(nameof(StrategyViewModel<FuturesPosition>.AvrageBuyPrice));
      RaisePropertyChanged(nameof(AllClosedPositions));

      var position = ActualPositions.FirstOrDefault();

      if (position != null)
      {
        var sl = IsPositionFilled(candle, PositionSide.Buy, position.StopLoss.Price);
        var tp = IsPositionFilled(candle, PositionSide.Sell, position.TakeProfit.Price);

        if (sl || tp)
        {
          position.Fees = position.OriginalPositionSize * (decimal)0.001;
          position.FilledDate = candle.OpenTime;
          position.PnL = GetPnl(candle, position);

          if (sl)
          {
            position.StopLoss.FilledDate = candle.OpenTime;
          }
          else
          {
            position.TakeProfit.FilledDate = candle.OpenTime;
          }

          position.State = PositionState.Completed;
          Budget += position.OriginalPositionSize;
          Budget += position.PnL * position.OriginalPositionSize;

          ActualPositions.Remove(position);
          OpenBuyPositions.Remove(position.StopLoss);
          OpenSellPositions.Remove(position.TakeProfit);

          RaisePropertyChanged(nameof(AllCompletedPositions));
          RaisePropertyChanged(nameof(StrategyViewModel<FuturesPosition>.TotalExpectedProfit));
        }
      }
    }
  }
}

