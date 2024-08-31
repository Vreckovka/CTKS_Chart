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
    public static bool DebugFlag = false;
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

        bool isDebug = false;

#if DEBUG
        isDebug = true;
#endif

        if (DebugFlag || isDebug)
        {
          var existingData = GetExistingData(symbol, timeFrame);

          if (existingData != null)
          {
            if (maxDate != null)
              return existingData.Where(x => x.OpenTime <= maxDate).ToList();

            return existingData;
          }
        }


        var list = new List<Candle>();

        var lines = File.ReadAllLines(path);

        if (!lines.Any())
        {
          return new List<Candle>();
        }

        var header = lines[0].Split(",");

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

            if (header.Length >= 23)
            {
              indicatorData = new IndicatorData()
              {
                RangeFilter = GetRangeFilter(data, header),
                BBWP = GetBBWP(data, header),
                IchimokuCloud = GetIchimoku(data, header),
                ADX = GetADX(data, header),
                AO = GetAwesomeOscilator(data, header),
                ATR = GetATR(data, header),
                MACD = GetMACD(data, header),
                MFI = GetMFI(data, header),
                VI = GetVortexIndicator(data, header)
              };
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
              FilePath = path,
              TimeFrame = timeFrame
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
          var existingSymbol = LoadedData.TryGetValue(symbol, out var symbolData);

          if (existingSymbol && symbolData.Any(x => x.Key == timeFrame))
            LoadedData[symbol][timeFrame] = list;
          else
          {
            var data = new Dictionary<TimeFrame, List<Candle>>();

            if (existingSymbol)
            {
              LoadedData[symbol].Add(timeFrame, list);
            }
            else
            {
              data.Add(timeFrame, list);
              LoadedData.Add(symbol, data);
            }
          }
        }

        return list.DistinctBy(x => x.UnixTime).ToList();
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

    #region Indicators

    #region GetRangeFilter

    private static RangeFilterData GetRangeFilter(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "Range Filter") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var rangeFilter);
        decimal.TryParse(data[startIndex + 1], out var highTarget);
        decimal.TryParse(data[startIndex + 2], out var lowTarget);
        decimal.TryParse(data[startIndex + 3], out var upward);


        var rangeData = new RangeFilterData();

        rangeData.RangeFilter = rangeFilter;
        rangeData.HighTarget = highTarget;
        rangeData.LowTarget = lowTarget;
        rangeData.Upward = upward != 0;

        return rangeData;
      }

      return null;
    }

    #endregion

    #region GetBBWP

    private static BBWPData GetBBWP(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "BBWP") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var bbwp);
        decimal.TryParse(data[startIndex + 1], out var hi);
        decimal.TryParse(data[startIndex + 2], out var low);
        decimal.TryParse(data[startIndex + 3], out var ma1);
        decimal.TryParse(data[startIndex + 4], out var ma2);

        var bbwpData = new BBWPData();

        bbwpData.BBWP = bbwp;
        bbwpData.MA1 = ma1;
        bbwpData.MA2 = ma2;

        return bbwpData;
      }

      return null;
    }

    #endregion

    #region GetIchimoku

    private static IchimokuCloud GetIchimoku(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "Conversion Line") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var conversionLine);
        decimal.TryParse(data[startIndex + 1], out var baseLine);
        decimal.TryParse(data[startIndex + 2], out var a);
        decimal.TryParse(data[startIndex + 3], out var b);
        decimal.TryParse(data[startIndex + 4], out var upper);
        decimal.TryParse(data[startIndex + 5], out var lower);

        var indicatorData = new IchimokuCloud()
        {
          ConversionLine = conversionLine,
          BaseLine = baseLine,
          UpperCloud = upper,
          LowerCloud = lower,
          LeadingSpanA = a,
          LeadingSpanB = b
        };

        return indicatorData;
      }

      return null;
    }

    #endregion

    #region GetStochRSI

    private static StochRSI GetStochRSI(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "K") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var k);
        decimal.TryParse(data[startIndex + 1], out var d);

        var indicatorData = new StochRSI()
        {
          K = k,
          D = d,
        };

        return indicatorData;
      }

      return null;
    }

    #endregion

    #region GetRSI

    private static RSIData GetRSI(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "RSI") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var rsi);
        decimal.TryParse(data[startIndex + 1], out var ma);

        var indicatorData = new RSIData()
        {
          RSI = rsi,
          RSIMA = ma,
        };

        return indicatorData;
      }

      return null;
    }

    #endregion 


    public static ATRData GetATR(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "Line") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var line);

        var indicatorData = new ATRData()
        {
          Line = line,
        };

        return indicatorData;
      }

      return null;
    }

    public static VortexIndicatorData GetVortexIndicator(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "VI +") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var plus);
        decimal.TryParse(data[startIndex + 1], out var minus);

        var indicatorData = new VortexIndicatorData()
        {
          VIPlus = plus,
          VIMinus = minus
        };

        return indicatorData;
      }

      return null;
    }

    public static ADXData GetADX(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "ADX") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var adx);

        var indicatorData = new ADXData()
        {
          ADX = adx,
        };

        return indicatorData;
      }

      return null;
    }

    public static MFIData GetMFI(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "MF") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var mf);

        var indicatorData = new MFIData()
        {
          MF = mf,
        };

        return indicatorData;
      }

      return null;
    }

    public static AwesomeOsiclatorData GetAwesomeOscilator(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "Plot") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var plot);

        var indicatorData = new AwesomeOsiclatorData()
        {
          Plot = plot,
        };

        return indicatorData;
      }

      return null;
    }

    public static MACDData GetMACD(string[] data, string[] header)
    {
      var startIndex = header.IndexOf(x => x == "MACD") ?? -1;

      if (startIndex >= 0)
      {
        decimal.TryParse(data[startIndex], out var macd);
        decimal.TryParse(data[startIndex + 1], out var ema);
        decimal.TryParse(data[startIndex + 2], out var histogram);

        var indicatorData = new MACDData()
        {
          MACD = macd,
          EMA = ema,
          Histogram = histogram
        };

        return indicatorData;
      }

      return null;
    }

    #endregion
  }
}
