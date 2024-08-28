﻿using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VNeuralNetwork;

namespace CTKS_Chart.Strategy.AIStrategy
{

  public class AIStrategy : BaseSimulationStrategy<AIPosition>
  {
    public IList<IndicatorData> IndicatorDatas { get; set; }
    public AIBot BuyAIBot { get; set; }
    public AIBot SellAIBot { get; set; }
    public float OriginalFitness { get; set; }

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
        return (TotalValue * 2m) / SimulationAIPromptViewModel.TakeIntersections;
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

    public override async void CreatePositions(Candle actualCandle, IList<Candle> indicatorCandles)
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

        if (dailyCandle == null || dailyCandle.CloseTime < dailyCandle.CloseTime)
        {
          lastDailyCandles.Add(dailyCandle);
        }

        indicatorsCandles = indicatorCandles;
        lastCandle = actualCandle;

        IndicatorDatas = indicatorCandles.Select(x => x.IndicatorData).ToList();

        var highest = Intersections[3].Value;

        var maxBuy = (decimal)Math.Max((double)(actualCandle.Close.Value * 0.995m), (double)highest);

        await CheckPositions(actualCandle, 0, maxBuy);

        var inter = Intersections
                    .Where(x => x.IsEnabled)
                    .Where(x => x.Value < actualCandle.Close.Value &&
                                x.Value < maxBuy)
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

        var toOpen = indexes.Where(x => x.prob > 0.75);
        var toClose = indexes.Where(x => x.prob < 0.25);


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
      if (GetBudget() > leftSize && leftSize > MinPositionValue)
      {
        await CreateBuyPosition(leftSize, intersection, false);
      }
    }

    #endregion

    #region CreateSellPositionForBuy

    protected override async Task CreateSellPositionForBuy(AIPosition buyPosition, decimal minForcePrice = 0)
    {
      var inter = Intersections
        .Where(x => x.Value > buyPosition.Price * 1.005m)
        .OrderBy(x => x.Value)
        .Take(SimulationAIPromptViewModel.TakeIntersections)
        .ToList();

      var output = SellAIBot.Update(
        lastCandle,
        this,
        inter,
        GetLastPrices(takeLastDailyCandles),
        (float)(buyPosition.PositionSize / PositionSize));


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
