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
    public AIBuyBot BuyAIBot { get; set; }
    public AIBuyBot SellAIBot { get; set; }

    private decimal PositionSize
    {
      get
      {
        return TotalValue;
        //return 20m;
      }
    }

    public AIStrategy()
    {
      StartingBudget = 1000;
      Budget = StartingBudget;
    }

    public AIStrategy(AIBuyBot buyBot, AIBuyBot sellBot) : this()
    {
      BuyAIBot = buyBot;
      SellAIBot = sellBot;
    }

    public override async void CreatePositions(Candle actualCandle, Candle dailyCandle)
    {
      try
      {
        await buyLock.WaitAsync();

        IndicatorData = dailyCandle.IndicatorData;

        var maxBuy = actualCandle.Close.Value * 0.995m;

        var coeficient = MaxTotalValue / StartingBudget;
        var fitness = TotalNativeAssetValue / TotalValue * 10 * coeficient;

        BuyAIBot.NeuralNetwork.AddFitness((float)fitness * -1);
        SellAIBot.NeuralNetwork.AddFitness((float)fitness * -1);

        if (TotalNativeAssetValue == 0)
        {
          BuyAIBot.NeuralNetwork.AddFitness(-50);
        }

        if(MaxTotalValue * 0.7m > TotalValue)
        {
          BuyAIBot.NeuralNetwork.AddFitness((float)(TotalValue - MaxTotalValue));
          SellAIBot.NeuralNetwork.AddFitness((float)(TotalValue - MaxTotalValue));
        }

        await CheckPositions(actualCandle, 0, maxBuy);

        int take = 15;

        var inter = Intersections
                    .Where(x => x.IsEnabled)
                    .Where(x => x.Value < actualCandle.Close.Value &&
                                x.Value < maxBuy)
                    .OrderByDescending(x => x.Value)
                    .Take(take)
                    .ToList();


        var output = BuyAIBot.Update(
          actualCandle,
          dailyCandle,
          this,
          PositionSize,
          inter);


        var indexes = output
          .Take(take)
          .Select((v, i) => new { prob = v, index = i });


        var toOpen = indexes.Where(x => x.prob > 0.75);
        var toClose = indexes.Where(x => x.prob < -0.75);


        foreach (var prob in toOpen)
        {
          if (prob.index < inter.Count)
          {
            var intersection = inter[prob.index];
            var weight = (decimal)(Math.Pow(Math.E, output[prob.index + take]) / Math.E);
            var size = weight * PositionSize;

            var positionsOnIntersesction =
              AllOpenedPositions.Concat(ActualPositions)
              .Where(x => x.Intersection.IsSame(intersection) &&
                          x.Intersection.TimeFrame == intersection.TimeFrame
                          && x.Side == PositionSide.Buy)
              .ToList();


            if (positionsOnIntersesction.Count == 0)
            {
              await CreateBuyPositionFromIntersection(intersection, size);
            }
          }
        }

        foreach (var prob in toClose)
        {
          if (prob.index < inter.Count)
          {
            var intersection = inter[prob.index];

            var position =
             AllOpenedPositions
             .Where(x => x.Intersection.IsSame(intersection) &&
                         x.Intersection.TimeFrame == intersection.TimeFrame
                         && x.Side == PositionSide.Buy)
             .FirstOrDefault();

            if (position != null)
              await CancelPosition(position);
          }
        }
      }
      finally
      {
        buyLock.Release();
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
      var inter = Intersections
        .Where(x => x.Value > buyPosition.Price * 1.005m)
        .OrderBy(x => x.Value)
        .Take(15)
        .ToList();

      var output = SellAIBot.Update(
        lastCandle,
        lastDailyCandle,
        this,
        buyPosition.PositionSize,
        inter);


      var positionSize = buyPosition.PositionSize;

      var roundedNativeSize = Math.Round(positionSize / buyPosition.Price, Asset.NativeRound);

      var index = output
        .Select((v, i) => new { prob = v, index = i })
        .OrderByDescending(x => x.prob)
        .FirstOrDefault()?.index;

      CtksIntersection intersection = null;

      if (index < inter.Count)
        intersection = inter[index.Value];


      if (intersection != null)
      {
        var newPosition = new AIPosition()
        {
          PositionSize = positionSize,
          OriginalPositionSize = positionSize,
          Price = intersection.Value,
          OriginalPositionSizeNative = roundedNativeSize,
          PositionSizeNative = roundedNativeSize,
          Side = PositionSide.Sell,
          TimeFrame = intersection.TimeFrame,
          Intersection = intersection,
          State = PositionState.Open,
          IsAutomatic = buyPosition.IsAutomatic
        };


        await PlaceSellPositions(new List<AIPosition>() { newPosition }, buyPosition);
      }
    }

    public override Task CloseBuy(AIPosition TPosition, decimal minForcePrice = 0)
    {
      return base.CloseBuy(TPosition, minForcePrice);
    }

    protected override void CloseSell(AIPosition position)
    {
      var finalSize = position.Price * position.OriginalPositionSizeNative;
      var profit = finalSize - position.OriginalPositionSize;

      if (profit < 0)
        throw new Exception("Negative profit!");

      BuyAIBot.NeuralNetwork.AddFitness((float)profit);
      SellAIBot.NeuralNetwork.AddFitness((float)profit);

      var coeficient = TotalValue / StartingBudget;

      BuyAIBot.NeuralNetwork.AddFitness(1 * (float)coeficient);
      SellAIBot.NeuralNetwork.AddFitness(1 * (float)coeficient);

      base.CloseSell(position);
    }
  }
}
