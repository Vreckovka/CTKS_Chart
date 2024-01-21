using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
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

    public static double GetValueFromCanvas(double canvasHeight, double value, decimal maxValue, decimal minValue)
    {
      canvasHeight = canvasHeight * 0.75;

      var logMaxValue = Math.Log10((double)maxValue);
      var logMinValue = Math.Log10((double)minValue);

      var logRange = logMaxValue - logMinValue;

      return Math.Pow(10, (value * logRange / canvasHeight) + logMinValue);
    }

    #endregion

    #region GetCanvasValue

    public static double GetCanvasValue(double canvasHeight, decimal value, decimal maxValue, decimal minValue)
    {
      canvasHeight = canvasHeight * 0.75;

      var logValue = Math.Log10((double)value);
      var logMaxValue = Math.Log10((double)maxValue);
      var logMinValue = Math.Log10((double)minValue);

      var logRange = logMaxValue - logMinValue;
      double diffrence = logValue - logMinValue;

      return diffrence * canvasHeight / logRange;
    }

    #endregion

    #region GetBrushFromHex

    public static SolidColorBrush GetBrushFromHex(string hex)
    {
      return (SolidColorBrush)new BrushConverter().ConvertFrom(hex);
    }

    #endregion

    #region ParseTradingView

    public static List<Candle> ParseTradingView(string path, DateTime? maxDate = null, int skip = 0, int cut = 0, bool addNotClosedCandle = false, int? indexCut = null)
    {
      var list = new List<Candle>();

      var file = File.ReadAllText(path);

      var lines = file.Split("\n").Skip(1 + skip).ToArray();
      CultureInfo.CurrentCulture = new CultureInfo("en-US");
      int index = 0;

      foreach (var line in lines.TakeLast(lines.Length - cut))
      {
        var data = line.Split(",");

        long.TryParse(data[0], out var unixTimestamp);
        decimal.TryParse(data[1], out var openParsed);
        decimal.TryParse(data[2], out var highParsed);
        decimal.TryParse(data[3], out var lowParsed);
        decimal.TryParse(data[4], out var closeParsed);


        var dateTime = DateTimeHelper.UnixTimeStampToUtcDateTime(unixTimestamp);

        var isOverDate = dateTime > maxDate;

        if (isOverDate && addNotClosedCandle)
        {
          isOverDate = false;
          addNotClosedCandle = false;
        }

        if (indexCut == index)
        {
          isOverDate = true;
        }

        if (!isOverDate)
        {
          list.Add(new Candle()
          {
            Close = closeParsed,
            Open = openParsed,
            High = highParsed,
            Low = lowParsed,
            CloseTime = dateTime,
            UnixTime = unixTimestamp
          });
        }
        else
        {
          break;
        }

        index++;
      }

      return list;
    }

    #endregion

    #region GetNextTime

    public static DateTime GetNextTime(DateTime date, TimeFrame timeFrame)
    {
      switch (timeFrame)
      {
        case TimeFrame.Null:
          break;
        case TimeFrame.M12:
          return date.AddMonths(12);
        case TimeFrame.M6:
          return date.AddMonths(6);
        case TimeFrame.M3:
          return date.AddMonths(3);
        case TimeFrame.M1:
          return date.AddMonths(1);
        case TimeFrame.W2:
          return date.AddDays(14);
        case TimeFrame.W1:
          return date.AddDays(7);
        case TimeFrame.D1:
          return date.AddDays(1);
      }

      return DateTime.MinValue;
    }

    #endregion
  }
}
