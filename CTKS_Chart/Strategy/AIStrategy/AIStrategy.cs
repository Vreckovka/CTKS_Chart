using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VNeuralNetwork;

namespace CTKS_Chart.Strategy.AIStrategy
{

  public class AIStrategy : BaseSimulationStrategy<AIPosition>
  {
    public IList<IndicatorData> IndicatorDatas { get; set; }
    public IList<IndicatorData> HighIndicatorDatas { get; set; }

    public AIBot BuyAIBot { get; set; }
    public AIBot SellAIBot { get; set; }
    public float OriginalFitness { get; set; }

    private decimal PositionSize
    {
      get
      {
        return (TotalValue * 3m) / SimulationAIPromptViewModel.TakeIntersections;
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

    #region CreatePositions

    List<CtksIntersection> lastIntersections = new List<CtksIntersection>();
    public List<Candle> lastDailyCandles = new List<Candle>();
    public int takeLastDailyCandles = 100;
    Candle lastDailyCandle = null;

    public override async Task CreatePositions(
      Candle actualCandle,
      IList<Candle> indicatorCandles)
    {
      try
      {
        await buyLock.WaitAsync();

        var dailyCandle = indicatorCandles.FirstOrDefault();

        if (indicatorCandles.Any(x => x == null))
        {
          Logger.Log(MessageType.Warning, $"No indicator candle {actualCandle.OpenTime.Date}, doing nothing...");
          return;
        }

        if (BuyAIBot == null)
          return;

        if (lastDailyCandle == null || dailyCandle.CloseTime > lastDailyCandle.CloseTime)
        {
          lastDailyCandles.Add(dailyCandle);
          CheckSellPositions(actualCandle);
        }

        indicatorsCandles = indicatorCandles;
        lastCandle = actualCandle;
        lastDailyCandle = dailyCandle;

        IndicatorDatas = indicatorCandles.Select(x => x.IndicatorData).ToList();

        await CheckPositions(actualCandle, 0, actualCandle.Close.Value);

        var inter = Intersections
                    //.Where(x => x.IsEnabled)
                    .Where(x => x.Value < actualCandle.Close.Value)
                    .Take(SimulationAIPromptViewModel.TakeIntersections)
                    .ToList();

        var notIns = lastIntersections.Where(p => !inter.Any(p2 => p2.IsSame(p)));

        foreach (var notIn in notIns)
        {
          await CancelPositionOnIntersection(notIn);
        }

        lastIntersections = inter;

        var output = BuyAIBot.Update(
          actualCandle,
          this,
          inter,
          GetLastPrices(takeLastDailyCandles));

        var indexes = output
        .Take(SimulationAIPromptViewModel.TakeIntersections)
        .Select((v, i) => new { prob = v, index = i });

        var toOpen = indexes.Where(x => x.prob > 0.70);
        var toClose = indexes.Where(x => x.prob < 0.30);

        foreach (var prob in toOpen)
        {
          if (prob.index < inter.Count)
          {
            var intersection = inter[prob.index];
            var weight = (decimal)output[prob.index + SimulationAIPromptViewModel.TakeIntersections];
            var size = weight * PositionSize;

            var positionsOnIntersesction =
              AllOpenedPositions.Concat(ActualPositions)
              .Where(x => x.Intersection.IsSame(intersection) &&
                          x.Intersection.TimeFrame == intersection.TimeFrame
                          && x.Side == PositionSide.Buy)
              .ToList();


            if (positionsOnIntersesction.Count == 0 && GetBudget() > size && size > MinPositionValue)
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

            await CancelPositionOnIntersection(intersection);
          }
        }
      }
      catch (Exception ex)
      {
        Logger?.Log(ex);
      }
      finally
      {
        buyLock.Release();
      }
    }

    #endregion

    private async void CheckSellPositions(Candle actualCandle)
    {
      var inter = Intersections
          .Where(x => x.Value > actualCandle.Close * 1.005m)
          .OrderBy(x => x.Value)
          .Take(SimulationAIPromptViewModel.TakeIntersections)
          .ToList();

      foreach (var position in ActualPositions)
      {
        var output = SellAIBot.Update(
          lastCandle,
          this,
          inter,
          GetLastPrices(takeLastDailyCandles),
          (float)(position.PositionSize / PositionSize));


        var indexes = output
          .Select((prob, index) => (prob, index))
          .OrderByDescending(x => x.prob);

        var sell = ((AIPosition)position.OpositPositions[0]);

        var better = indexes.OrderByDescending(x => x.prob).FirstOrDefault(x => x.prob > sell.Prob);

        if (better.prob > 0)
        {
          await CancelPosition(sell);
          await CreateSellPositionForBuy(position);
        }
      }
    }

    #region CancelPositionOnIntersection

    private async Task CancelPositionOnIntersection(CtksIntersection intersection)
    {
      var position =
       AllOpenedPositions
       .Where(x => x.Intersection.IsSame(intersection) &&
                   x.Intersection.TimeFrame == intersection.TimeFrame
                   && x.Side == PositionSide.Buy)
       .FirstOrDefault();

      if (position != null)
        await CancelPosition(position);
    }

    #endregion

    #region CreateBuyPositionFromIntersection

    private async Task CreateBuyPositionFromIntersection(
      CtksIntersection intersection,
      decimal leftSize)
    {
      await CreateBuyPosition(leftSize, intersection, false);
    }

    #endregion

    private (IEnumerable<(float prob, int index)> indexes, IList<CtksIntersection> inter) GetSellProbs(AIPosition buyPosition)
    {
      var inter = Intersections
      .Where(x => x.Value > lastCandle.Close * 1.005m)
      .OrderBy(x => x.Value)
      .Take(SimulationAIPromptViewModel.TakeIntersections)
      .ToList();

      var output = SellAIBot.Update(
        lastCandle,
        this,
        inter,
        GetLastPrices(takeLastDailyCandles),
        (float)(buyPosition.PositionSize / PositionSize));


      var indexes = output
        .Select((prob, index) => (prob, index))
        .OrderByDescending(x => x.prob);

      return (indexes, inter);
    }

    #region CreateSellPositionForBuy

    protected override async Task CreateSellPositionForBuy(AIPosition buyPosition, decimal minForcePrice = 0)
    {
      var result = GetSellProbs(buyPosition);

      var indexes = result.indexes;
      var inter = result.inter;

      AIPosition newPosition = null;

      var positionSize = buyPosition.PositionSize;

      var roundedNativeSize = Math.Round(positionSize / buyPosition.Price, Asset.NativeRound);

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
            IsAutomatic = buyPosition.IsAutomatic,
            Prob = indexProb.prob
          };

          break;
        }
      }

      if (newPosition == null)
      {
        newPosition = new AIPosition()
        {
          PositionSize = positionSize,
          OriginalPositionSize = positionSize,
          Price = inter[0].Value,
          OriginalPositionSizeNative = roundedNativeSize,
          PositionSizeNative = roundedNativeSize,
          Side = PositionSide.Sell,
          TimeFrame = inter[0].TimeFrame,
          Intersection = inter[0],
          State = PositionState.Open,
          IsAutomatic = buyPosition.IsAutomatic
        };
      }

      if (newPosition != null)
        await PlaceSellPositions(new List<AIPosition>() { newPosition }, buyPosition);
      else
        throw new Exception("There is no line to place SELL position!!");
    }

    #endregion

    #region AddFitness

    public void AddFitness(float fitness)
    {
      SellAIBot?.NeuralNetwork.AddFitness(fitness);
      BuyAIBot?.NeuralNetwork.AddFitness(fitness);
    }

    #endregion

    private List<decimal> GetLastPrices(int count)
    {
      return lastDailyCandles.TakeLast(count).Select(x => x.Close.Value).ToList();
    }
  }
}
