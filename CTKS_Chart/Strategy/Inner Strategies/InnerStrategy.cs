using System;
using System.Collections.Generic;
using System.Linq;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Strategy
{
  public abstract class InnerStrategy
  {
    public abstract decimal Calculate(Candle actual);
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

    public override decimal Calculate(Candle newCandle)
    {
      if (candles == null)
      {
        candles = TradingHelper.ParseTradingView(path);
      }

      var actualCandle = candles.FirstOrDefault(x => x.CloseTime > newCandle.CloseTime && x.OpenTime < newCandle.OpenTime);

      decimal lastSell = decimal.MaxValue;

      if (strategy.ClosedSellPositions.Any() && strategy.ActualPositions.Any())
      {
        lastSell = strategy.ClosedSellPositions.Last().Price;
      }


      if (actualCandle != null && actualCandle.IndicatorData.RangeFilter > 0 && lastCandle != actualCandle)
      {
        var bullish = actualCandle.IndicatorData.Upward;
        var bbwp = (double)actualCandle.IndicatorData.BBWP / 100.0;

        bool wasChange = false;

        //strategy.DisableOnBuy = !bullish;

        //if (bullish)
        //{
        //  strategy.Intersections.ForEach(x => x.IsEnabled = true);
        //}

        if (bbwp == 0)
        {
          return decimal.MaxValue;
        }

        var change = bullish ? bbwp : 1 + bbwp;
        var minValue = 0.0075;
        var maxValue = 0.05;

        var list = this.strategy.MinBuyMapping.ToList();
        foreach (var buy in list)
        {
          var newValue = buy.Value * change;

          if (newValue < minValue)
          {
            newValue = minValue;
          }
          else if (newValue > maxValue)
          {
            newValue = maxValue;
          }

          wasChange = strategy.MinBuyMapping[buy.Key] != newValue;
          strategy.MinBuyMapping[buy.Key] = newValue;
        }

        list = this.strategy.MinSellProfitMapping.ToList();
        change = bullish ? 1 + bbwp : bbwp;

        foreach (var sell in list)
        {
          var newValue = sell.Value * change;

          if (newValue < minValue)
          {
            newValue = minValue;
          }
          else if (newValue > maxValue)
          {
            newValue = maxValue;
          }

          wasChange = strategy.MinBuyMapping[sell.Key] != newValue;
          strategy.MinSellProfitMapping[sell.Key] = newValue;
        }

        var positionSizes = strategy.PositionSizeMapping.ToList();

        if (originalMapping == null)
        {
          originalMapping = positionSizes;
        }

        var newList = new Dictionary<TimeFrame, decimal>();

        foreach (var positionSize in positionSizes)
        {
          newList.Add(positionSize.Key, positionSize.Value);

          var value = bullish ? bbwp + 1 : bbwp;
          var newValue = positionSize.Value * (decimal)value;

          var maxPositionValue = originalMapping.SingleOrDefault(x => x.Key == positionSize.Key).Value ;
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
        }

        strategy.PositionSizeMapping = newList;

        lastCandle = actualCandle;

        if (!bullish)
        {
          lastSell *= (1 - (decimal)0.01);
        }


      }

      return lastSell;
    }
  }
}