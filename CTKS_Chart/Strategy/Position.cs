using System.Collections.Generic;
using VCore.Standard;

namespace CTKS_Chart
{
  public class Position : ViewModel
  {
    public Position(decimal positionSize, decimal price, decimal positionSizeNative)
    {
      PositionSize = positionSize;
      OriginalPositionSize = positionSize;
      PositionSizeNative = positionSizeNative;
      Price = price;
    }

    #region ProfitValue

    public decimal ProfitValue
    {
      get
      {
        return Profit /100 * (decimal)OriginalPositionSize;
      }
    }

    #endregion

    #region Profit

    private decimal profit;

    public decimal Profit
    {
      get { return profit; }
      set
      {
        if (value != profit)
        {
          profit = value;
          RaisePropertyChanged();
          RaisePropertyChanged(nameof(ProfitValue));
        }
      }
    }

    #endregion

    public decimal PositionSizeNative { get;  }
    public decimal Price { get;  }
    public CtksIntersection Intersection { get; set; }

    public PositionSide Side { get; set; }


    #region State

    private PositionState state;

    public PositionState State
    {
      get { return state; }
      set
      {
        if (value != state)
        {
          state = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public TimeFrame TimeFrame { get; set; }

    public decimal OriginalPositionSize { get; }
    public IList<Position> OpositPositions { get; set; } = new List<Position>();

    #region PositionSize

    private decimal positionSize;

    public decimal PositionSize
    {
      get { return positionSize; }
      set
      {
        if (value != positionSize)
        {
          positionSize = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}