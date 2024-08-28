﻿using CTKS_Chart.Trading;
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

      decimal minPrice = lastPrices.Min() * 0.25m;
      decimal maxPrice = lastPrices.Max() * 3m;

      AddInput((float)(strategy.Budget / strategy.TotalValue), ref index, ref inputs);
      AddInput((float)(strategy.OpenBuyPositions.Sum(x => x.PositionSize) / strategy.TotalValue), ref index, ref inputs);
      AddInput((float)(strategy.DrawdawnFromMaxTotalValue / 100.0m), ref index, ref inputs);

      AddInput(AddNormalizedInput(actualCandle.Open, minPrice, maxPrice), ref index, ref inputs);
      AddInput(AddNormalizedInput(actualCandle.Close, minPrice, maxPrice), ref index, ref inputs);
      AddInput(AddNormalizedInput(actualCandle.High, minPrice, maxPrice), ref index, ref inputs);
      AddInput(AddNormalizedInput(actualCandle.Low, minPrice, maxPrice), ref index, ref inputs);

      foreach(var indicatorData in strategy.IndicatorDatas)
      {
        AddIndicator(indicatorData.RangeFilter, ref index, ref inputs, minPrice, maxPrice, true);
        AddIndicator(indicatorData.BBWP, ref index, ref inputs, minPrice, maxPrice);
        //AddIndicator(indicatorData.IchimokuCloud, ref index, ref inputs, minPrice, maxPrice, true);
        AddIndicator(indicatorData.StochRSI, ref index, ref inputs, minPrice, maxPrice);
        AddIndicator(indicatorData.RSI, ref index, ref inputs, minPrice, maxPrice);
      }

      foreach (var extraInput in extraInputs)
      {
        AddInput(extraInput, ref index, ref inputs);
      }

      //AddIntersectionInputs(intersections, minPrice, maxPrice, ref index, ref inputs);

      return inputs;
    }

    private void AddIntersectionInputs(
      IList<CtksIntersection> intersections,
      decimal minPrice,
      decimal maxPrice,
      ref int index,
      ref float[] inputs)
    {
      for (int i = 0; i < intersections.Count; i++)
      {
        var value = AddNormalizedInput(intersections[i].Value, minPrice, maxPrice);
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

    protected float AddNormalizedInput(decimal? price, decimal minPrice, decimal maxPrice)
    {
      if (price != null)
      {
        decimal normalized = (price.Value - minPrice) / (maxPrice - minPrice);

        return (float)normalized;
      }

      return -1;
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
      decimal minPrice,
      decimal maxPrice,
      bool normlized = false
      )
    {
      var values = indicatorData.GetData();
      int indexF = 0;

      foreach (var value in values)
      {
        if (normlized)
        {
          if (indicatorData is RangeFilterData filterData && indexF == 3)
          {
            AddInput((float)value, ref index, ref inputs);
          }
          else
          {
            AddInput(AddNormalizedInput(value, minPrice, maxPrice), ref index, ref inputs);
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
