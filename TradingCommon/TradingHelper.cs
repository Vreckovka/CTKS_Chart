using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Trading
{
  public static class TradingViewHelper
  {
    #region ParseTradingView

    public static List<Candle> ParseTradingView(string path, DateTime? maxDate = null, int skip = 0, int cut = 0, bool addNotClosedCandle = false, int? indexCut = null)
    {
      var list = new List<Candle>();

      var file = File.ReadAllText(path);

      var lines = file.Split("\n").Skip(1 + skip).ToArray();
      CultureInfo.CurrentCulture = new CultureInfo("en-US");
      int index = 0;

      TimeSpan? dateDiff = null;

      foreach (var line in lines.TakeLast(lines.Length - cut))
      {
        var data = line.Split(",");

        if (data.Length < 4)
        {
          continue;
        }

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

        if (dateDiff == null && list.Count > 1)
        {
          dateDiff = list[1].OpenTime - list[0].OpenTime;

          list[0].CloseTime = list[0].OpenTime.AddMinutes(dateDiff.Value.TotalMinutes);
          list[1].CloseTime = list[1].OpenTime.AddMinutes(dateDiff.Value.TotalMinutes);
        }

        if (!isOverDate)
        {
          IndicatorData indicatorData = new IndicatorData();

          if (data.Length > 8)
          {
            decimal.TryParse(data[5], out var rangeFilter);
            decimal.TryParse(data[6], out var highTarget);
            decimal.TryParse(data[7], out var lowTarget);
            decimal.TryParse(data[8], out var upward);
            decimal.TryParse(data[9], out var bbwp);

            indicatorData.RangeFilter = rangeFilter;
            indicatorData.HighTarget = highTarget;
            indicatorData.LowTarget = lowTarget;
            indicatorData.Upward = upward != 0;
            indicatorData.BBWP = bbwp;
          }

          var newCandle = new Candle()
          {
            Close = closeParsed,
            Open = openParsed,
            High = highParsed,
            Low = lowParsed,
            OpenTime = dateTime,
            UnixTime = unixTimestamp,
            IndicatorData = indicatorData
          };

          if(dateDiff != null)
          {
            newCandle.CloseTime = newCandle.OpenTime.AddMinutes(dateDiff.Value.TotalMinutes);
          }

          list.Add(newCandle);
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

    #region IsOutDated

    public static bool IsOutDated(TimeFrame timeFrame, IList<Candle> innerCandles)
    {
      var last = innerCandles.Last();
      if (DateTime.UtcNow > TradingViewHelper.GetNextTime(last.OpenTime, timeFrame))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    #endregion



  }
}
