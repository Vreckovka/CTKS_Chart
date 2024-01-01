﻿using System;
using System.Collections.Generic;
using System.Linq;
using CTKS_Chart.Trading;
using VCore.Standard;

namespace CTKS_Chart.Strategy
{
  //THERE IS POSITION_DTO ALSO NEED CHANGE!!!!
  public class Position : ViewModel
  {
    //Json serialize
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

    //THERE IS POSITION_DTO ALSO NEED CHANGE!!!!
    public decimal? Fees { get; set; }
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

    //THERE IS POSITION_DTO ALSO NEED CHANGE!!!!
    public DateTime? FilledDate { get; set; }
    public DateTime? CreatedDate { get; set; }

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

    #region ActualProfit

    private decimal actualProfit;

    public decimal ActualProfit
    {
      get { return actualProfit; }
      set
      {
        if (value != actualProfit)
        {
          actualProfit = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public decimal TotalProfit
    {
      get
      {
        return OpositPositions != null ? OpositPositions.Sum(x => x.Profit) : 0;
      }
    }

    public decimal TotalFees
    {
      get
      {
        return OpositPositions != null ?  OpositPositions.Where(x => x.Fees != null).Sum(x => x.Fees.Value) + (Fees != null ? Fees.Value : 0) : 0;
      }
    }

    public decimal FinalProfit
    {
      get
      {
        return TotalProfit - TotalFees;
      }
    }

    public decimal ExpectedProfit
    {
      get
      {
        if(OpositPositions != null)
        {
          var totalValue = OpositPositions.Sum(x => x.OriginalPositionSizeNative * x.Price);
          var totalFees = OpositPositions.Sum(x => x.OriginalPositionSizeNative * x.Price * (decimal)0.001) + Fees ?? 0;

          return totalValue - totalFees - OriginalPositionSize;
        }

        return 0;
      }
    }

    public void RaiseNotify(string name)
    {
      RaisePropertyChanged(name);
    }
  }
}