using CTKS_Chart.Trading;
using System;
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
      PositionSide positionSide = PositionSide.Buy,
      AIPosition aIPosition = null)
    {
      float[] inputs = GetInputs(actualCandle,dailyCandle, strategy, positionSide, aIPosition, positionSize);

      return NeuralNetwork.FeedForward(inputs);
    }

    public float[] GetInputs(
      Candle actualCandle,
      Candle dailyCandle,
      AIStrategy strategy,
      PositionSide positionSide,
      AIPosition aIPosition,
      decimal positionSize)
    {
      float[] inputs = new float[NeuralNetwork.Layers[0]];

      inputs[0] = (float)strategy.TotalValue;
      inputs[1] = (float)strategy.Budget;
      inputs[2] = (float)strategy.OpenBuyPositions.Sum(x => x.PositionSize);

      inputs[3] = (float)strategy.TotalActualProfit;

      inputs[4] = (float)actualCandle.Open;
      inputs[5] = (float)actualCandle.Close;
      inputs[6] = (float)actualCandle.High;
      inputs[7] = (float)actualCandle.Low;


      inputs[8] = (float)strategy.IndicatorData.RangeFilterData.RangeFilter;
      inputs[9] = (float)strategy.IndicatorData.RangeFilterData.HighTarget;
      inputs[10] = (float)strategy.IndicatorData.RangeFilterData.LowTarget;
      inputs[11] = strategy.IndicatorData.RangeFilterData.Upward ? 1 : -1;

      inputs[12] = (float)strategy.IndicatorData.BBWP;

      inputs[13] = positionSide == PositionSide.Sell ? -1 : 1;
      inputs[14] = (float)positionSize;

      if (aIPosition != null)
      {
        inputs[15] = (float)aIPosition.Price;
      }

      inputs[16] = (float)dailyCandle.Open;
      inputs[17] = (float)dailyCandle.Close;
      inputs[18] = (float)dailyCandle.High;
      inputs[19] = (float)dailyCandle.Low;

      return inputs;
    }
  }
}
