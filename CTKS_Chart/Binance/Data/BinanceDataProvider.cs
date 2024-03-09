using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Binance.Net.Enums;
using CryptoExchange.Net.CommonObjects;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Binance.Data
{
  public class BinanceDataProvider
  {
    private readonly BinanceBroker binanceBroker;

    public BinanceDataProvider(BinanceBroker binanceBroker)
    {
      this.binanceBroker = binanceBroker ?? throw new ArgumentNullException(nameof(binanceBroker));
    }

    #region GetKlines

    public async void GetKlines(
      string symbol,
      TimeSpan klineInterval,
      CancellationToken? cancellationToken = null,
      Action<List<Kline>> onPartDownloaded = null,
      int limit = 1000)
    {

      if (cancellationToken != null && cancellationToken.Value.IsCancellationRequested)
      {
        Console.WriteLine("Task was cancelled before it got started.");

        cancellationToken.Value.ThrowIfCancellationRequested();
      }

      var fileName = $"{symbol}-{klineInterval.TotalMinutes}-generated.csv";

      if(File.Exists(fileName))
      {
        File.Delete(fileName);
      }

      List<Candle> candles = new List<Candle>();
      var firstValues = (await binanceBroker.GetCandles(symbol, klineInterval)).OrderByDescending(x => x.CloseTime).ToList();
      candles.AddRange(firstValues);

      DateTime? lastCloseTime = null;
      DateTime?  firstDate = candles.Last().CloseTime;

      while (true)
      {
        firstDate = candles.Last().CloseTime;

        if(lastCloseTime == firstDate)
        {
          break;
        }

        var lastValues = (await binanceBroker.GetCandles(symbol, klineInterval, endTime: firstDate)).OrderByDescending(x => x.CloseTime);

        candles.AddRange(lastValues);

        lastCloseTime = firstDate;
      }

      using (StreamWriter w = File.AppendText(fileName))
      {
        foreach (var value in candles.OrderBy(x => x.CloseTime))
        {
          w.WriteLine($"{value.UnixTime},{value.Open},{value.High},{value.Low},{value.Close}");
        }
       
      }
    }

    #endregion
  }
}
