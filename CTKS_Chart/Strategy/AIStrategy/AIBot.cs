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

    decimal mean;
    decimal stdDev;

    public virtual float[] GetInputs(
      Candle actualCandle,
      AIStrategy strategy,
      IList<CtksIntersection> intersections,
      IList<decimal> lastPrices,
      params float[] extraInputs)
    {
      var inputs = new float[NeuralNetwork.InputCount];
      var index = 0;

      mean = MathHelper.CalculateMean(lastPrices);
      stdDev = MathHelper.CalculateStdDev(lastPrices, mean);

      AddInput((float)(strategy.Budget / strategy.TotalValue), ref index, ref inputs);
      AddInput((float)(strategy.OpenBuyPositions.Sum(x => x.PositionSize) / strategy.TotalValue), ref index, ref inputs);
      AddInput((float)(strategy.DrawdawnFromMaxTotalValue / 100.0m), ref index, ref inputs);

      foreach (var indicatorData in strategy.IndicatorDatas)
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
        var value = GetNormalizedInput(intersections[i].Value);

        AddInput(value, ref index, ref inputs);
      }
    }

    protected void AddInput(float input, ref int index, ref float[] inputs)
    {
      inputs[index] = input;
      index++;
    }

    protected float GetNormalizedInput(decimal? price)
    {
      if (price != null)
      {
       return (float)MathHelper.NormalizeAndClampZScore(price.Value, mean, stdDev);
      }

      return 0;
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
            AddInput(GetNormalizedInput(value), ref index, ref inputs);
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
