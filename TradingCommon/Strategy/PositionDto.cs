using System;
using System.Linq;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Strategy
{
  public class PositionDto<TPosition>
    where TPosition : Position, new()
  {
    public PositionDto()
    {

    }

    public PositionDto(TPosition position)
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
      Fees = position.Fees;
      FilledDate = position.FilledDate;
      CreatedDate = position.CreatedDate;
      IsAutomatic = position.IsAutomatic;
    }

    public TPosition GetPosition()
    {
      return new TPosition()
      {
        PositionSize = PositionSize,
        PositionSizeNative = PositionSizeNative,
        TimeFrame = TimeFrame,
        OriginalPositionSize = OriginalPositionSize,
        OriginalPositionSizeNative = OriginalPositionSizeNative,
        Id = Id,
        Price = Price,
        Profit = Profit,
        State = State,
        Side = Side,
        Intersection = Intersection,
        Fees = Fees,
        FilledDate = FilledDate,
        CreatedDate = CreatedDate,
        IsAutomatic = IsAutomatic
      };
    }



    public long Id { get; set; }
    public PositionSide Side { get; set; }
    public PositionState State { get; set; }
    public decimal Price { get; set; }
    public decimal PositionSize { get; set; }
    public decimal PositionSizeNative { get; set; }
    public bool IsAutomatic { get; set; }

    public decimal OriginalPositionSize { get; set; }
    public decimal OriginalPositionSizeNative { get; set; }


    public DateTime? CreatedDate { get; set; }
    public TimeFrame TimeFrame { get; set; }
    public CtksIntersection Intersection { get; set; }
    
    
    public DateTime? FilledDate { get; set; }
    public decimal Profit { get; set; }
    public decimal? Fees { get; set; }

    public long[] OpositePositions { get; set; }
  }
}