using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using CTKS_Chart.Trading;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Strategy
{
  public abstract class StrategyViewModel : Strategy
  {
    public override IList<Position> ClosedBuyPositions { get; set; } = new ObservableCollection<Position>();
    public override IList<Position> ClosedSellPositions { get; set; } = new ObservableCollection<Position>();

    public override IList<Position> OpenSellPositions { get; set; } = new ObservableCollection<Position>();
    public override IList<Position> OpenBuyPositions { get; set; } = new ObservableCollection<Position>();

    #region ActualPositions

    private IList<Position> actualPositions = new ObservableCollection<Position>();

    public override IList<Position> ActualPositions
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

    #region TotalSell

    public decimal TotalSell
    {
      get { return AllClosedPositions.Where(x => x.Side == PositionSide.Sell).Sum(x => x.OriginalPositionSize + x.Profit); }
    }

    #endregion
  }
}