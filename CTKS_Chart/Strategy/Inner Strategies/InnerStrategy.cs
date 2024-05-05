﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Strategy
{
  public abstract class InnerStrategy
  {
    public abstract void Calculate(Candle actual);
  }

  public class RangeFilterStrategy : InnerStrategy
  {
    private readonly string path;
    private readonly Strategy strategy;
    private List<Candle> candles;
    private IEnumerable<KeyValuePair<TimeFrame, decimal>> originalMapping;

    public RangeFilterStrategy(string path, Strategy strategy)
    {
      this.path = path ?? throw new ArgumentNullException(nameof(path));
      this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    Candle lastCandle;

    public override void Calculate(Candle newCandle)
    {
      if (candles == null)
      {
        candles = TradingViewHelper.ParseTradingView(path);
      }

      var actualCandle = candles.FirstOrDefault(x => x.CloseTime > newCandle.CloseTime && x.OpenTime < newCandle.OpenTime);

 
      if (actualCandle != null && actualCandle.IndicatorData.RangeFilter > 0 && lastCandle != actualCandle)
      {
        var bullish = actualCandle.IndicatorData.Upward;
        var bbwp = (double)actualCandle.IndicatorData.BBWP / 100.0;

        bool wasChange = false;




        var size = 0.1;

        var minValue = 0.0075;
        var maxValue = 0.25;

        var list = this.strategy.MinBuyMapping.ToList();
        foreach (var buy in list)
        {
          var newValue = buy.Value * (bullish ? 1 - size : 1 + size);

          if (newValue < minValue)
          {
            newValue = minValue;
          }
          else if (newValue > maxValue)
          {
            newValue = maxValue;
          }

          strategy.MinBuyMapping[buy.Key] = newValue;
        }

        list = this.strategy.MinSellProfitMapping.ToList();
       

        foreach (var sell in list)
        {      
          var newValue = sell.Value * (bullish ? 1 + size : 1 - size);

          if (newValue < minValue)
          {
            newValue = minValue;
          }
          else if (newValue > maxValue)
          {
            newValue = maxValue;
          }

          strategy.MinSellProfitMapping[sell.Key] = newValue;
        }

        var positionSizes = strategy.PositionSizeMapping.ToList();

        if (originalMapping == null)
        {
          strategy.BasePositionSizeMapping = strategy.PositionSizeMapping;
         
        }

        originalMapping = strategy.BasePositionSizeMapping;
        var newList = new Dictionary<TimeFrame, decimal>();

        foreach (var positionSize in positionSizes)
        {
          newList.Add(positionSize.Key, positionSize.Value);

          var newValue = positionSize.Value * (decimal)(bullish ? 1 + size : 1 - size);

          var maxPositionValue = originalMapping.SingleOrDefault(x => x.Key == positionSize.Key).Value * (decimal)1.25;
          var minPositionValue = originalMapping.SingleOrDefault(x => x.Key == positionSize.Key).Value / 5;

          if (newValue > maxPositionValue)
          {
            newValue = maxPositionValue;
          }
          else if (newValue < minPositionValue)
          {
            newValue = minPositionValue;
          }

          newList[positionSize.Key] = newValue;



          //31557

          //if (strategy.StrategyPosition == StrategyPosition.Bearish)
          //{
          //  var positionsToStop = strategy.ActualPositions
          //    .Where(x => x.TimeFrame == positionSize.Key)
          //    .Where(x => !x.IsAutomatic)
          //    .Where(x => x.OriginalPositionSize > originalMapping.SingleOrDefault(y => y.Key == positionSize.Key).Value);

          //  if (positionsToStop.Any())
          //    StopPositions(newCandle, positionsToStop);
          //}
        }

        if (bullish)
        {
          strategy.ScaleSize += 0.05;

          if (strategy.ScaleSize > 2)
          {
            strategy.ScaleSize = 2;
          }
        }
        else
        {
          strategy.ScaleSize -= 0.05;

          if (strategy.ScaleSize < -1)
          {
            strategy.ScaleSize = -1;
          }
        }

        strategy.PositionSizeMapping = newList;

        lastCandle = actualCandle;


        strategy.StrategyPosition = bullish ? StrategyPosition.Bullish : StrategyPosition.Bearish;

        //strategy.DisableOnBuy = !bullish;

        //if (bullish)
        //{
        //  strategy.Intersections.ForEach(x => x.IsEnabled = true);
        //}
      }
    }
  }
}