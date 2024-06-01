using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using CTKS_Chart.Trading;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Strategy
{
  public abstract class StrategyViewModel<TPosition> : BaseStrategy<TPosition>
    where TPosition : Position, new()
  {
    public override IList<TPosition> ClosedBuyPositions { get; set; } = new ObservableCollection<TPosition>();
    public override IList<TPosition> ClosedSellPositions { get; set; } = new ObservableCollection<TPosition>();

    public override IList<TPosition> OpenSellPositions { get; set; } = new ObservableCollection<TPosition>();
    public override IList<TPosition> OpenBuyPositions { get; set; } = new ObservableCollection<TPosition>();

    #region ActualPositions

    private IList<TPosition> actualPositions = new ObservableCollection<TPosition>();

    public override IList<TPosition> ActualPositions
    {
      get { return actualPositions; }
      set
      {
        if (value != actualPositions)
        {
          actualPositions = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalExpectedProfit

    public decimal TotalExpectedProfit
    {
      get { return ActualPositions.Sum(x => x.ExpectedProfit); }
    }

    #endregion

    #region AvrageBuyPrice

    public decimal AvrageBuyPrice
    {
      get
      {
        var positions = ActualPositions.ToList();

        var filled = ActualPositions.Sum(x => x.OpositPositions.Sum(y => y.Profit));
        var value = positions.Sum(x => x.OpositPositions.Sum(y => y.PositionSize)) - filled;
        var native = positions.Sum(x => x.OpositPositions.Sum(y => y.PositionSizeNative));

        return Math.Round(TotalNativeAsset > 0 && native > 0 ? value / native : 0, Asset.PriceRound);
      }
    }

    #endregion

    #region TotalBuy

    public decimal TotalBuy
    {
      get { return AllClosedPositions.Where(x => x.Side == PositionSide.Buy).Sum(x => x.OriginalPositionSize); }
    }

    #endregion

    #region TotalFees

    public decimal TotalFees
    {
      get { return AllClosedPositions.Sum(x => x.Fees ?? 0); }
    }

    #endregion

    #region TotalSell

    public decimal TotalSell
    {
      get { return AllClosedPositions.Where(x => x.Side == PositionSide.Sell).Sum(x => x.OriginalPositionSize + x.Profit); }
    }

    #endregion

    #region Turnover

    public decimal Turnover
    {
      get { return TotalBuy + TotalSell; }
    }

    #endregion

    #region AbosoluteGainValue

    public decimal AbosoluteGainValue
    {
      get { return TotalValue - StartingBudget; }
    }

    #endregion

    #region AbosoluteGain

    public decimal AbosoluteGain
    {
      get { return TotalValue > 0 ? AbosoluteGainValue / TotalValue * 100 : 0; }
    }

    #endregion
  }
}