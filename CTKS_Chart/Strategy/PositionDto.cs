using System.Linq;

namespace CTKS_Chart
{
  public class PositionDto : Position
  {
    public PositionDto()
    {

    }

    public PositionDto(Position position)
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
    }


    public long[] OpositePositions { get; set; }
  }
}