using CTKS_Chart.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using VNeuralNetwork;

namespace CTKS_Chart.Strategy.AIStrategy
{
  public class AIBot : AIObject
  {
   

    public AIBot(NeuralNetwork neuralNetwork) : base(neuralNetwork)
    {
    }

    public float[] Update(
      Candle actualCandle,
       Candle dailyCandle,
      AIStrategy strategy,
      decimal positionSize,
      IList<CtksIntersection> intersections)
    {
      float[] inputs = GetInputs(
        actualCandle,
        dailyCandle, 
        strategy,
        positionSize,
        intersections);

      return NeuralNetwork.FeedForward(inputs);
    }

    public float[] GetInputs(
      Candle actualCandle,
      Candle dailyCandle,
      AIStrategy strategy,
       decimal positionSize,
      IList<CtksIntersection> intersections)
    {
      float[] inputs = new float[NeuralNetwork.Layers[0].inputCount];

      int index = 0;

      var openSize = (float)strategy.OpenBuyPositions.Sum(x => x.PositionSize);
      var openCount = (float)strategy.OpenBuyPositions.Count;

      AddInput((float)strategy.TotalValue, ref index, ref inputs);
      AddInput((float)strategy.Budget, ref index, ref inputs);
      AddInput(openSize > 0 ? openSize : -1 , ref index, ref inputs);
      AddInput(openCount > 0 ? openCount : -1, ref index, ref inputs);

      AddInput((float)strategy.TotalActualProfit, ref index, ref inputs);

      AddInput(LogTransform(actualCandle.Open), ref index, ref inputs);
      AddInput(LogTransform(actualCandle.Close), ref index, ref inputs);
      AddInput(LogTransform(actualCandle.High), ref index, ref inputs);
      AddInput(LogTransform(actualCandle.Low), ref index, ref inputs);

      AddInput(LogTransform(strategy.IndicatorData.RangeFilterData.RangeFilter), ref index, ref inputs);
      AddInput(LogTransform(strategy.IndicatorData.RangeFilterData.HighTarget), ref index, ref inputs);
      AddInput(LogTransform(strategy.IndicatorData.RangeFilterData.LowTarget), ref index, ref inputs);
      AddInput(strategy.IndicatorData.RangeFilterData.Upward ? 1 : -1, ref index, ref inputs);
      AddInput((float)strategy.IndicatorData.BBWP, ref index, ref inputs);

      AddInput((float)positionSize, ref index, ref inputs);

      AddInput(LogTransform(dailyCandle.Open), ref index, ref inputs);
      AddInput(LogTransform(dailyCandle.Close), ref index, ref inputs);
      AddInput(LogTransform(dailyCandle.High), ref index, ref inputs);
      AddInput(LogTransform(dailyCandle.Low), ref index, ref inputs);

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
        AddInput(LogTransform(intersections[i].Value), ref index, ref inputs);
      }
    }

    private float LogTransform(decimal? data)
    {
      return (float)Math.Log((float)data + 1);
    }

    private void AddInput(float input, ref int index, ref float[] inputs)
    {
      inputs[index] = input;
      index++;
    }
  }
}
