using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Binance.Net.Enums;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Trading
{
  public static class TradingHelper
  {
    #region GetTimeSpanFromInterval

    public static TimeSpan GetTimeSpanFromInterval(KlineInterval klineInterval)
    {
      switch (klineInterval)
      {
        case KlineInterval.OneSecond:
          return TimeSpan.FromSeconds(1);
        case KlineInterval.OneMinute:
          return TimeSpan.FromMinutes(1);
        case KlineInterval.ThreeMinutes:
          return TimeSpan.FromMinutes(3);
        case KlineInterval.FiveMinutes:
          return TimeSpan.FromMinutes(5);
        case KlineInterval.FifteenMinutes:
          return TimeSpan.FromMinutes(15);
        case KlineInterval.ThirtyMinutes:
          return TimeSpan.FromMinutes(30);
        case KlineInterval.OneHour:
          return TimeSpan.FromHours(1);
        case KlineInterval.TwoHour:
          return TimeSpan.FromHours(2);
        case KlineInterval.FourHour:
          return TimeSpan.FromHours(4);
        case KlineInterval.SixHour:
          return TimeSpan.FromHours(6);
        case KlineInterval.EightHour:
          return TimeSpan.FromHours(8);
        case KlineInterval.TwelveHour:
          return TimeSpan.FromHours(12);
        case KlineInterval.OneDay:
          return TimeSpan.FromDays(1);
        case KlineInterval.ThreeDay:
          return TimeSpan.FromDays(3);
        case KlineInterval.OneWeek:
          return TimeSpan.FromDays(7);
        case KlineInterval.OneMonth:
          return TimeSpan.FromDays(30);
        default:
          throw new ArgumentOutOfRangeException(nameof(klineInterval), klineInterval, null);
      }
    }

    #endregion

    #region GetValueFromCanvas

    public static decimal GetValueFromCanvas(double canvasHeight, double value, decimal maxValue, decimal minValue)
    {
      var logMaxValue = Math.Log10((double)maxValue);
      var logMinValue = Math.Log10((double)minValue);

      var logRange = logMaxValue - logMinValue;

      var valued = Math.Pow(10, (value * logRange / canvasHeight) + logMinValue);

      if ((double)decimal.MaxValue < valued)
      {
        return decimal.MaxValue;
      }

      return (decimal)valued;
    }

    #endregion

    #region GetValueFromCanvas

    public static long GetValueFromCanvasLinear(double canvasHeight, double value, long maxValue, long minValue)
    {
      var logMaxValue = (double)maxValue;
      var logMinValue = (double)minValue;

      var logRange = logMaxValue - logMinValue;

      var valued = (value * logRange / canvasHeight) + logMinValue;

      if ((long)long.MaxValue < valued)
      {
        return long.MaxValue;
      }

      return (long)valued;
    }

    #endregion

    #region GetCanvasValue

    public static double GetCanvasValue(double canvasHeight, decimal value, decimal maxValue, decimal minValue)
    {
      var logValue = Math.Log10((double)value);
      var logMaxValue = Math.Log10((double)maxValue);
      var logMinValue = Math.Log10((double)minValue);

      var logRange = logMaxValue - logMinValue;
      double difference = logValue - logMinValue;

      var result = difference * canvasHeight / logRange;

      return result;
    }

    #endregion

    #region GetCanvasValueLinear

    public static double GetCanvasValueLinear(double canvasHeight, long value, long maxValue, long minValue)
    {
      var logValue = value;
      var logMaxValue = maxValue;
      var logMinValue = minValue;

      var logRange = logMaxValue - logMinValue;
      double difference = logValue - logMinValue;

      var result = difference * canvasHeight / logRange;

      return result;
    }

    #endregion

    #region GetPointOnLine

    public static double GetPointOnLine(double x1, double y1, double x2, double y2, double x3)
    {
      double deltaY = y2 - y1;
      double deltaX = x2 - x1;

      var slope = deltaY / deltaX;
      //https://www.mathsisfun.com/algebra/line-equation-point-slope.html
      //y − y1 = m(x − x1)
      //x = x1 + ((y -y1) / m)
      //y = m(x − x1) + y1
      return (slope * (x3 - x1)) + y1;
    }

    #endregion

    public static Candle GetActualEqivalentCandle(string symbol, TimeFrame timeFrame, Candle actualCandle)
    {
      if (actualCandle == null)
        return null;

      if (TradingViewHelper.LoadedData[symbol].TryGetValue(timeFrame, out var candles))
      {
        candles = candles.Where(x => x.IndicatorData?.RangeFilter?.RangeFilter > 0).ToList();


        var equivalentDataCandles = candles
       .Where(x => IsWithinRange(x, actualCandle)).ToList();

        var equivalentDataCandle = equivalentDataCandles.FirstOrDefault();

        if (equivalentDataCandle != null)
        {
          return equivalentDataCandle;
        }

      }

      return null;
    }

    public static bool IsWithinRange(Candle interestCandle, Candle actualCandle)
    {
      return interestCandle.UnixTime <= actualCandle.UnixTime && interestCandle.CloseTime >= actualCandle.CloseTime;
    }
  }
}
