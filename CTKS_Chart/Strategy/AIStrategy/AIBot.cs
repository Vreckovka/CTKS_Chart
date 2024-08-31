using CTKS_Chart.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using VCore.Standard.Helpers;
using VNeuralNetwork;

namespace CTKS_Chart.Strategy.AIStrategy
{
  public class AIBot : AIObject
  {
    public AIBot(INeuralNetwork neuralNetwork) : base(neuralNetwork)
    {

    }

    public float[] Update(
      Candle actualCandle,
      AIStrategy strategy,
       IList<CtksIntersection> intersections,
      IList<decimal> lastPrices,
      params float[] extraInputs)
    {
      float[] inputs = GetInputs(
        actualCandle,
        strategy,
        intersections,
        lastPrices,
        extraInputs);

      return NeuralNetwork.FeedForward(inputs);
    }

    public virtual float[] GetInputs(
      Candle actualCandle,
      AIStrategy strategy,
      IList<CtksIntersection> intersections,
      IList<decimal> lastPrices,
      params float[] extraInputs)
    {
      var inputs = new float[NeuralNetwork.InputCount];
      var index = 0;

      AddInput((float)(strategy.Budget / strategy.TotalValue), ref index, ref inputs);
      AddInput((float)(strategy.OpenBuyPositions.Sum(x => x.PositionSize) / strategy.TotalValue), ref index, ref inputs);
      AddInput((float)(strategy.DrawdawnFromMaxTotalValue / 100.0m), ref index, ref inputs);

      AddInput(NormalizeZScore(actualCandle.Open, lastPrices), ref index, ref inputs);
      AddInput(NormalizeZScore(actualCandle.Close, lastPrices), ref index, ref inputs);
      AddInput(NormalizeZScore(actualCandle.High, lastPrices), ref index, ref inputs);
      AddInput(NormalizeZScore(actualCandle.Low, lastPrices), ref index, ref inputs);

      foreach(var indicatorData in strategy.IndicatorDatas.Take(2))
      {
        AddIndicator(indicatorData.RangeFilter, ref index, ref inputs, lastPrices);
        AddIndicator(indicatorData.IchimokuCloud, ref index, ref inputs, lastPrices);
        AddIndicator(indicatorData.ATR, ref index, ref inputs, lastPrices);

        AddIndicator(indicatorData.BBWP, ref index, ref inputs);
        AddIndicator(indicatorData.VI, ref index, ref inputs);
        AddIndicator(indicatorData.ADX, ref index, ref inputs);
        AddIndicator(indicatorData.MFI, ref index, ref inputs);
        AddIndicator(indicatorData.AO, ref index, ref inputs);
        AddIndicator(indicatorData.MACD, ref index, ref inputs);
      }

      foreach (var indicatorData in strategy.IndicatorDatas.Skip(2))
      {
        AddIndicator(indicatorData.VI, ref index, ref inputs);
        AddIndicator(indicatorData.ADX, ref index, ref inputs);
        AddIndicator(indicatorData.MFI, ref index, ref inputs);
        AddIndicator(indicatorData.AO, ref index, ref inputs);
        AddIndicator(indicatorData.MACD, ref index, ref inputs);
      }

      foreach (var extraInput in extraInputs)
      {
        AddInput(extraInput, ref index, ref inputs);
      }

      AddIntersectionInputs(intersections, ref index, ref inputs);

      return inputs;
    }

    private void AddIntersectionInputs(
      IList<CtksIntersection> intersections,
      ref int index,
      ref float[] inputs)
    {
      for (int i = 0; i < intersections.Count; i++)
      {
        var value = NormalizeZScore(intersections[i].Value, intersections.Select(x => x.Value));
        var weight = AddNormalizedInput((double)(int)intersections[i].TimeFrame, 1, 7);

        AddInput(value, ref index, ref inputs);
        //AddInput(weight, ref index, ref inputs);
      }
    }

    protected void AddInput(float input, ref int index, ref float[] inputs)
    {
      inputs[index] = input;
      index++;
    }

    //protected float AddNormalizedInput(decimal? price, decimal minPrice, decimal maxPrice)
    //{
    //  if (price != null)
    //  {
    //    decimal normalized = (price.Value - minPrice) / (maxPrice - minPrice);

    //    return (float)normalized;
    //  }

    //  return -1;
    //}

    public float NormalizeZScore(decimal? value, IEnumerable<decimal> historicalValues)
    {
      decimal mean = CalculateMean(historicalValues);
      decimal stdDev = CalculateStdDev(historicalValues, mean);

      // Avoid division by zero in case of very small standard deviation
      if (stdDev == 0) return 0;

      return (float)((value - mean) / stdDev);
    }

    public decimal CalculateStdDev(IEnumerable<decimal> values, decimal mean)
    {
      decimal sumSquaredDiffs = values.Sum(value => (value - mean) * (value - mean));
      return (decimal)Math.Sqrt((double)(sumSquaredDiffs / values.Count()));
    }

    public decimal CalculateMean(IEnumerable<decimal> values)
    {
      return values.Sum() / values.Count();
    }


    protected float AddNormalizedInput(double value, double min, double max)
    {
      var normalized = (value - min) / (max - min);

      return (float)normalized;
    }

    private void AddIndicator(
      Indicator indicatorData,
      ref int index,
      ref float[] inputs,
      IEnumerable<decimal> historicalValues = null)
    {
      var values = indicatorData.GetData();
      int indexF = 0;

      foreach (var value in values)
      {
        if (historicalValues != null)
        {
          if (indicatorData is RangeFilterData filterData && indexF == 3)
          {
            AddInput((float)value, ref index, ref inputs);
          }
          else
          {
            AddInput(NormalizeZScore(value, historicalValues), ref index, ref inputs);
          }
        }
        else
        {
          AddInput((float)value, ref index, ref inputs);
        }

        indexF++;
      }
    }


  }
}
