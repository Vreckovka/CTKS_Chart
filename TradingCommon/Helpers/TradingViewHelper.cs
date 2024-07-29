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
    public static object batton = new object();
    public static Dictionary<string, Dictionary<TimeFrame, List<Candle>>> LoadedData { get; } = new Dictionary<string, Dictionary<TimeFrame, List<Candle>>>();

    #region ParseTradingView

    public static List<Candle> ParseTradingView(
      TimeFrame timeFrame,
      string path,
      string symbol,
      DateTime? maxDate = null,
      int skip = 0,
      int cut = 0,
      bool addNotClosedCandle = false,
      int? indexCut = null,
      bool saveData = false)
    {

      lock (batton)
      {
#if DEBUG
        var existingData = GetExistingData(symbol, timeFrame);

        if (existingData != null)
        {
          if (maxDate != null)
            return existingData.Where(x => x.OpenTime <= maxDate).ToList();

          return existingData;
        }
#endif

        var list = new List<Candle>();

        var lines = File.ReadAllLines(path);

        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        int index = 0;

        TimeSpan? dateDiff = null;

        for (int i = 1; i < lines.Length - cut; i++)
        {
          var line = lines[i];

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


              var rangeData = new RangeFilterData();

              rangeData.RangeFilter = rangeFilter;
              rangeData.HighTarget = highTarget;
              rangeData.LowTarget = lowTarget;
              rangeData.Upward = upward != 0;

              indicatorData.RangeFilterData = rangeData;
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
              IndicatorData = indicatorData,
              FileLineIndex = i,
              FilePath = path
            };

            if (dateDiff != null)
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

        if (saveData)
        {
          if (LoadedData.TryGetValue(symbol, out var symbolData))
            LoadedData[symbol][timeFrame] = list;
          else
          {
            var data = new Dictionary<TimeFrame, List<Candle>>();
            data.Add(timeFrame, list);

            LoadedData.Add(symbol, data);
          }

        }

        return list; 
      }
    }

    #endregion

    private static List<Candle> GetExistingData(string symbol, TimeFrame timeFrame)
    {
      if (LoadedData.TryGetValue(symbol, out var symbolData))
      {
        symbolData.TryGetValue(timeFrame, out var candles);

        return candles;
      }

      return null;
    }

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
        case TimeFrame.H4:
          return date.AddHours(4);
        case TimeFrame.H12:
          return date.AddHours(12);
        case TimeFrame.m15:
          return date.AddMinutes(15);
        case TimeFrame.m1:
          return date.AddMinutes(1);
        default:
          throw new ArgumentOutOfRangeException($"{timeFrame} was not found in the GetNextTime() method");
      }

      return DateTime.MinValue;
    }

    #endregion

    #region IsOutDated

    public static bool IsOutDated(TimeFrame timeFrame, IList<Candle> innerCandles)
    {
      var last = innerCandles.Last();
      if (DateTime.UtcNow >= GetNextTime(last.OpenTime, timeFrame))
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
