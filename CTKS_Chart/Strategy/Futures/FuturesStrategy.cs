using CTKS_Chart.Trading;
using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTKS_Chart.Strategy.Futures
{
  public class FuturesPosition : Position
  {

  }

  public class FuturesStrategy : SimulationStrategy
  {
    public async override void CreatePositions(Candle actualCandle)
    {
      base.CreatePositions(actualCandle);
      CalculatePositions();

      var validIntersections = Intersections;

      var inter = validIntersections
                   .Where(x => x.IsEnabled)
                   .Where(x => x.Value < actualCandle.Close.Value)
                   .ToList();

      var intersection = inter.FirstOrDefault();

      await CreateBuyPositionFromIntersection(intersection, true);
    }

    protected async override Task CreateBuyPositionFromIntersection(CtksIntersection intersection, bool automatic = false)
    {
      var leftSize = GetPositionSize(intersection.TimeFrame);

      if (GetBudget() > leftSize)
      {
        await CreateBuyPosition(leftSize, intersection, automatic);
      }
    }

    private void CalculatePositions()
    {
      var actualPosition = ClosedBuyPositions.LastOrDefault();

      if(actualPosition != null)
      {

      }
    }
  }
}

