using System.Collections.Generic;
using VCore.Standard;

namespace CTKS_Chart
{
  public class Position : ViewModel
  {
    public Position(double positionSize, double price)
    {
      PositionSize = positionSize;
      OriginalPositionSize = positionSize;
      PositionSizeNative = positionSize / price;
      Price = price;
    }

    #region ProfitValue

    public double ProfitValue
    {
      get
      {
        return Profit / 100 * PositionSize;
      }
    }

    #endregion

    #region Profit

    private double profit;

    public double Profit
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

    public double PositionSizeNative { get;  }
    public double Price { get;  }
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

    public double OriginalPositionSize { get; }
    public IList<Position> OpositPositions { get; set; } = new List<Position>();

    #region PositionSize

    private double positionSize;

    public double PositionSize
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