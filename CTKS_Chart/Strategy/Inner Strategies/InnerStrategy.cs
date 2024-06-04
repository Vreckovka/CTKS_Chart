using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTKS_Chart.Trading;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Strategy
{
  public abstract class InnerStrategy
  {
    public abstract IEnumerable<CtksIntersection> Calculate(Candle actual, PositionSide positionSide);
  }


  public class RangeFilterStrategy<TPosition> : InnerStrategy where TPosition : Position, new()
  {
    private readonly string path;
    private readonly string symbol;
    private readonly string btcPath;
    private readonly BaseStrategy<TPosition> strategy;
    private List<Candle> BtcCandles;

    private IEnumerable<KeyValuePair<TimeFrame, decimal>> originalMapping;

    public RangeFilterStrategy(string path, string symbol, string btcPath, BaseStrategy<TPosition> strategy)
    {
      this.path = path ?? throw new ArgumentNullException(nameof(path));
      this.symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
      this.btcPath = btcPath ?? throw new ArgumentNullException(nameof(btcPath));
      this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    Candle lastCandle;

    public override IEnumerable<CtksIntersection> Calculate(Candle newCandle, PositionSide positionSide)
    {
      //if (BtcCandles == null)
      //{
      //  BtcCandles = TradingViewHelper.ParseTradingView(TimeFrame.D1, btcPath);
      //}

      var actualAssetCandle = TradingHelper.GetActualEqivalentCandle(symbol,TimeFrame.D1, newCandle);
      //var actualBtcCandle = BtcCandles.FirstOrDefault(x => x.CloseTime >= newCandle.CloseTime && x.OpenTime <= newCandle.OpenTime);

      //if (actualAssetCandle != null && actualAssetCandle.IndicatorData.RangeFilterData.RangeFilter > 0 && lastCandle != actualAssetCandle)
      //{
      //  if (actualAssetCandle != null && actualBtcCandle != null)
      //    //RangeBased(actualAssetCandle, actualBtcCandle);
      //  return Skip(actualAssetCandle, actualBtcCandle);
      //}

      if (actualAssetCandle != null)
        return Skip(actualAssetCandle, newCandle, positionSide);

      return strategy.Intersections;
    }


    private IEnumerable<CtksIntersection> Skip(
      Candle actualAssetCandle, 
      Candle actualCandle,
      PositionSide positionSide)
    {
      var intersections = strategy.Intersections;
      intersections.ForEach(x => x.IsEnabled = true);
      int nStep = 1;
      var bullish = actualAssetCandle.IndicatorData.RangeFilterData.Upward;

      if (positionSide == PositionSide.Buy)
      {
        if (!actualAssetCandle.IndicatorData.RangeFilterData.Upward)
        {
          nStep++;
        }
      }
      else if (positionSide == PositionSide.Sell)
      {
        if (actualAssetCandle.IndicatorData.RangeFilterData.Upward)
        {
          nStep++;
        }
      }

      var valid = intersections.Where((x, i) => i % nStep == 0);
      var removed = intersections.Where(y => !valid.Contains(y)).ToList();

      removed.ForEach(x => x.IsEnabled = false);


      return valid;
    }


    #region RangeBased

    private void RangeBased(Candle actualAssetCandle, Candle actualBtcCandle)
    {

      //var bullish = actualAssetCandle.IndicatorData.Upward;
      //var bullish = actualBtcCandle.IndicatorData.Upward;
      var bullish = actualBtcCandle.IndicatorData.RangeFilterData.Upward || actualAssetCandle.IndicatorData.RangeFilterData.Upward;
      //var bullish = actualBtcCandle.IndicatorData.Upward && actualAssetCandle.IndicatorData.Upward;

      var size = 0.025m;

      if (actualBtcCandle.IndicatorData.RangeFilterData.Upward && actualAssetCandle.IndicatorData.RangeFilterData.Upward)
      {
        size = 0.2m;
      }
      else if (!actualBtcCandle.IndicatorData.RangeFilterData.Upward && !actualAssetCandle.IndicatorData.RangeFilterData.Upward)
      {
        size = 0.2m;
      }

      var minValue = 0.0075m;
      var maxValue = 0.25m;

      var list = this.strategy.MinBuyMapping.ToList();

      foreach (var buy in list)
      {
        var newValue = buy.Value * (bullish ? 1m - size : 1m + size);

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
        var newValue = sell.Value * (bullish ? 1m + size : 1m - size);

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

      lastCandle = actualAssetCandle;


      strategy.StrategyPosition = bullish ? StrategyPosition.Bullish : StrategyPosition.Bearish;
    }

    #endregion
  }
}