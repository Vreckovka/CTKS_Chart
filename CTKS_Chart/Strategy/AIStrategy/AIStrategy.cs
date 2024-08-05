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
    public AIBot BuyAIBot { get; set; }
    public AIBot SellAIBot { get; set; }

    decimal Coeficient
    {
      get
      {
        return TotalValue / StartingBudget;
      }
    }


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

    public AIStrategy(AIBot buyBot, AIBot sellBot) : this()
    {
      BuyAIBot = buyBot;
      SellAIBot = sellBot;
    }

    public override async void CreatePositions(Candle actualCandle, Candle dailyCandle)
    {
      try
      {
        await buyLock.WaitAsync();

        if (BuyAIBot == null)
          return;

        IndicatorData = dailyCandle.IndicatorData;

        var maxBuy = actualCandle.Close.Value * 0.995m;

        //var fitness = GetNegativeValue((float)Math.Pow((double)DrawdawnFromMaxTotalValue , 2)) / 50;

        //AddFitness(fitness);

        //foreach (var actualPosition in ActualPositions.ToArray())
        //{
        //  actualPosition.ActualProfit = actualPosition.GetActualProfit(actualCandle);

        //  if(actualPosition.ActualProfitPerc < -12)
        //  {
        //    StopLossPosition(actualPosition, actualCandle);
        //  }
        //}

        countFromLastBuy++;
        countFromLastSell++;

        //AddFitness(GetNegativeValue((countFromLastBuy + countFromLastSell) / 10));

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
      catch(Exception ex)
      {

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

      var indexes = output
        .Select((v, i) => new { prob = v, index = i })
        .OrderByDescending(x => x.prob);


      AIPosition newPosition = null;

      foreach (var indexProb in indexes)
      {
        CtksIntersection intersection = null;

        if (indexProb.index < inter.Count)
          intersection = inter[indexProb.index];

        if (intersection != null)
        {
          newPosition = new AIPosition()
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

          break;
        }
      }

      if (newPosition != null)
        await PlaceSellPositions(new List<AIPosition>() { newPosition }, buyPosition);
      else
        throw new Exception("There is no line to place SELL position!!");
    }




    int countFromLastBuy;
    int countFromLastSell;

    public override Task CloseBuy(AIPosition TPosition, decimal minForcePrice = 0)
    {
      AddFitness(1);
      countFromLastBuy = 0;

      return base.CloseBuy(TPosition, minForcePrice);
    }

    protected override void CloseSell(AIPosition position)
    {
      countFromLastSell = 0;

      AddFitness(1);

      var finalSize = position.Price * position.OriginalPositionSizeNative;
      var profit = finalSize - position.OriginalPositionSize;

      var fitness = GetProfitFitness(profit);

      if (profit < 0)
        throw new Exception("Negative profit!");

      AddFitness(fitness);

      base.CloseSell(position);
    }

    private void StopLossPosition(AIPosition position, Candle actualCandle)
    {
      position.Profit = position.GetActualProfit(actualCandle);
      position.PositionSize = 0;
      position.PositionSizeNative = 0;

      TotalProfit += position.Profit;
      Budget += position.OriginalPositionSize + position.Profit;
      TotalNativeAsset -= position.OriginalPositionSizeNative;

      Budget -= position.Fees ?? 0;

      position.State = PositionState.Completed;

      OpenSellPositions.Remove((AIPosition)position.OpositPositions[0]);
      ActualPositions.Remove(position);

      var fitness = GetNegativeValue(GetProfitFitness(position.Profit));

      AddFitness(fitness);
    }

    private void AddFitness(float fitness)
    {
      SellAIBot.NeuralNetwork.AddFitness(fitness);
      BuyAIBot.NeuralNetwork.AddFitness(fitness);
    }

    private float GetProfitFitness(decimal profit)
    {
      var profitPerc = (profit / TotalValue * 100);

      return (float)profitPerc;
    }

    private float GetNegativeValue(float value)
    {
      if (value > 0)
        value *= -1;

      return value;
    }
  }
}
