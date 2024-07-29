using CTKS_Chart.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using VNeuralNetwork;

namespace CTKS_Chart.Strategy.AIStrategy
{
  public class AIBuyBot : AIObject
  {
   

    public AIBuyBot(NeuralNetwork neuralNetwork) : base(neuralNetwork)
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
        intersections,
        positionSize
        );

      return NeuralNetwork.FeedForward(inputs);
    }

    public float[] GetInputs(
      Candle actualCandle,
      Candle dailyCandle,
      AIStrategy strategy,
      IList<CtksIntersection> intersections,
      decimal positionSize)
    {
      float[] inputs = new float[NeuralNetwork.Layers[0]];

      int index = 0;

      AddInput((float)strategy.TotalValue, ref index, ref inputs);
      AddInput((float)strategy.Budget, ref index, ref inputs);
      AddInput((float)strategy.OpenBuyPositions.Sum(x => x.PositionSize), ref index, ref inputs);

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


      AddIntersectionInputs(intersections, 20, ref inputs);

      return inputs;
    }

    private void AddIntersectionInputs(
      IList<CtksIntersection> intersections,
      int starterIndex,
      ref float[] inputs)
    {
      for (int i = starterIndex; i < starterIndex + intersections.Count; i++)
      {
        inputs[i] = LogTransform(intersections[i - starterIndex].Value);
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
