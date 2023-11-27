using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using VCore.Standard;

namespace CTKS_Chart
{
  public class Position : ViewModel
  {
    public Position()
    {
      
    }

    public Position(decimal positionSize, decimal price, decimal positionSizeNative)
    {
      PositionSize = positionSize;
      OriginalPositionSize = positionSize;
      OriginalPositionSizeNative = positionSizeNative;
      PositionSizeNative = positionSizeNative;
      Price = price;
    }


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
        }
      }
    }

    #endregion

    public decimal PositionSizeNative { get; set; }
    public decimal Price { get; set; }
    public CtksIntersection Intersection { get; set; }
    public PositionSide Side { get; set; }
    public long Id { get; set; }

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
    public decimal OriginalPositionSize { get;  set; }
    public decimal OriginalPositionSizeNative { get;  set; }
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