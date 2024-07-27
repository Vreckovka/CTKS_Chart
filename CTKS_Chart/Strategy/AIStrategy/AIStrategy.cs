using CTKS_Chart.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VNeuralNetwork;

namespace CTKS_Chart.Strategy.AIStrategy
{

  public class AIStrategy : BaseSimulationStrategy<AIPosition>
  {
    public IndicatorData IndicatorData { get; set; }
    public AIBot AIBot { get; set; }

    private decimal PositionSize
    {
      get
      {
        return TotalValue * 0.10m;
      }
    }

    public AIStrategy()
    {
      StartingBudget = 1000;
    }

    public AIStrategy(AIBot bot): this()
    {
      AIBot = bot;
    }

    public override async void CreatePositions(Candle actualCandle, Candle dailyCandle)
    {
      IndicatorData = dailyCandle.IndicatorData;


      var output = AIBot.Update(actualCandle,dailyCandle, this, PositionSize);

      var perc = (decimal)output[0];
      var price = actualCandle.Close * perc;
      var weight = PositionSize * (decimal)Math.Abs((decimal)output[1]);

      if (price != null)
      {
        var limits = GetMaxAndMinBuy(actualCandle, dailyCandle);

        var lastSell = limits.Item1;
        var minBuy = limits.Item2;
        var maxBuy = limits.Item3;

        var inter = Intersections
                   .Where(x => x.IsEnabled)
                   .Where(x => x.Value < actualCandle.Close.Value &&
                               x.Value > minBuy &&
                               x.Value < lastSell)
                   .ToList();

        var intersection = inter.OrderBy(x => Math.Abs(x.Value - price.Value)).FirstOrDefault();

        if (intersection != null)
        {
          var positionsOnIntersesction =
               AllOpenedPositions
               .Where(x => x.Intersection.IsSame(intersection) &&
                           x.Intersection.TimeFrame == intersection.TimeFrame)
               .ToList();

          var existing =
       ActualPositions
       .Any(x => x.Intersection.IsSame(intersection) &&
                   x.Intersection.TimeFrame == intersection.TimeFrame);

          if (price > 0 && positionsOnIntersesction.Count == 0 && !existing)
          {
            await CreateBuyPositionFromIntersection(intersection, weight);
          }
          else
          {
            foreach (var position in positionsOnIntersesction)
            {
              await CancelPosition(position);
            }
          }
        }
      }
    }

    private async Task CreateBuyPositionFromIntersection(
      CtksIntersection intersection,
      decimal leftSize)
    {

      if (GetBudget() > leftSize && leftSize > MinPositionValue)
      {
        await CreateBuyPosition(leftSize, intersection, false);
      }
    }

    protected override async Task CreateSellPositionForBuy(AIPosition buyPosition, decimal minForcePrice = 0)
    {
      var output = AIBot.Update(lastCandle, lastDailyCandle, this, 0, PositionSide.Sell, buyPosition);

      var price = buyPosition.Price * (1 + (decimal)Math.Abs(output[0]));

      var ctksIntersection = Intersections.OrderBy(x => Math.Abs(x.Value - price)).FirstOrDefault();

      var positionSize = buyPosition.PositionSize;

      var roundedNativeSize = Math.Round(positionSize / buyPosition.Price, Asset.NativeRound);

      var newPosition = new AIPosition()
      {
        PositionSize = positionSize,
        OriginalPositionSize = positionSize,
        Price = ctksIntersection.Value,
        OriginalPositionSizeNative = roundedNativeSize,
        PositionSizeNative = roundedNativeSize,
        Side = PositionSide.Sell,
        TimeFrame = ctksIntersection.TimeFrame,
        Intersection = ctksIntersection,
        State = PositionState.Open,
        IsAutomatic = buyPosition.IsAutomatic
      };

      await PlaceSellPositions(new List<AIPosition>() { newPosition }, buyPosition);
    }

    public override Task CloseBuy(AIPosition TPosition, decimal minForcePrice = 0)
    {
      AIBot.NeuralNetwork.AddFitness(1);

      return base.CloseBuy(TPosition, minForcePrice);

    }

    protected override void CloseSell(AIPosition position)
    {
      var finalSize = position.Price * position.OriginalPositionSizeNative;
      var profit = finalSize - position.OriginalPositionSize; ;

      AIBot.NeuralNetwork.AddFitness((float)profit);

      base.CloseSell(position);
    }
  }
}
