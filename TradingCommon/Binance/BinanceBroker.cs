using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Models.Spot.Socket;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Sockets;
using CTKS_Chart.Strategy.Futures;
using CTKS_Chart.Trading;
using Logger;
using Position = CTKS_Chart.Strategy.Position;
using PositionSide = CTKS_Chart.Strategy.PositionSide;


namespace CTKS_Chart.Binance
{
  public class BinanceBroker
  {
    private readonly ILogger logger;
    private string apiKey = "pEUT5muif0EINAO9rwNPH7f2TcGl22YT13x8vdKjn1UazTmwISyAjijCSghjrq4K";
    private string apiSecret = "5h71ZEWdjFShRIHX7Q14dGrSfASJYECPfcL06DmLzf2qcVKnYSv6SusSsYww6vbR";


    private string liveApiKey = "PKtvAWr3JlNCRxGck10RB1b1QFHrb0GF2ixUTjnHSB475Y80w11MQ7WX8v89XQgc";
    private string liveApiSecret = "I94eM3iFvLh55wic66iADGfO2EnEyTXSOyy1iKV6YDAdGsNriEYTROIZvqLG5ZI7";

    private BinanceSocketClient socketClient;

    public BinanceBroker(ILogger logger)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      BinanceRestClient.SetDefaultOptions((options) =>
      {
        options.ApiCredentials = new ApiCredentials(liveApiKey, liveApiSecret);
        options.Environment = BinanceEnvironment.Live;
      });

      //BinanceRestClient.SetDefaultOptions((options) =>
      //{
      //  options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
      //  options.Environment = BinanceEnvironment.Testnet;
      //});

      socketClient = new BinanceSocketClient();
      }

    #region GetCandles

    public async Task<IEnumerable<Candle>> GetCandles(
      string symbol,
      TimeSpan interval,
      DateTime? startTime = null,
      DateTime? endTime = null,
      int? limit = null)
    {
      var list = new List<Candle>();

      using (var client = new BinanceRestClient())
      {
        var klineData = await client.SpotApi.CommonSpotClient.GetKlinesAsync(symbol, interval, startTime, endTime, limit);

        foreach (var kline in klineData.Data)
        {
          var newCandle = new Candle()
          {
            Close = kline.ClosePrice,
            High = kline.HighPrice,
            Low = kline.LowPrice,
            Open = kline.OpenPrice,
            OpenTime = kline.OpenTime,
            CloseTime = kline.OpenTime.AddMinutes(interval.TotalMinutes),
          };

          newCandle.UnixTime = ((DateTimeOffset)newCandle.OpenTime).ToUnixTimeSeconds();

          list.Add(newCandle);
        }
      }

      return list;
    }

    #endregion

    #region GetTicker

    public async Task<decimal?> GetTicker(string symbol)
    {
      using (var client = new BinanceRestClient())
      {
        var ticker = await client.SpotApi.CommonSpotClient.GetTickerAsync(symbol);

        return ticker.Data.LastPrice;
      }
    }

    #endregion

    #region GetClosedOrders

    public async Task<IEnumerable<Order>> GetClosedOrders(string symbol)
    {
      using (var client = new BinanceRestClient())
      {
        return (await client.SpotApi.CommonSpotClient.GetClosedOrdersAsync(symbol)).Data;
      }
    }

    #endregion

    #region GetClosedTrades

    public async Task<IEnumerable<UserTrade>> GetClosedTrades(string orderId, string symbol)
    {
      using (var client = new BinanceRestClient())
      {
        return (await client.SpotApi.CommonSpotClient.GetOrderTradesAsync(orderId, symbol)).Data;
      }
    }

    #endregion

    #region GetOpenOrders

    public async Task<IEnumerable<Order>> GetOpenOrders(string symbol)
    {
      using (var client = new BinanceRestClient())
      {
        return (await client.SpotApi.CommonSpotClient.GetOpenOrdersAsync(symbol)).Data;
      }
    }

    #endregion

    #region GetPositionSide

    private OrderSide GetPositionSide(PositionSide positionSide)
    {
      switch (positionSide)
      {
        case PositionSide.Buy:
          return OrderSide.Buy;
        case PositionSide.Sell:
          return OrderSide.Sell;
        default:
          throw new ArgumentOutOfRangeException(nameof(positionSide), positionSide, null);
      }
    }

    #endregion

    #region Buy

    private SemaphoreSlim createPositionLock = new SemaphoreSlim(1, 1);
    public async Task<long> Buy(string symbol, Position position)
    {
      try
      {
        await createPositionLock.WaitAsync();
        using (var client = new BinanceRestClient())
        {

          var result = await client.SpotApi.Trading.PlaceOrderAsync(symbol,
            OrderSide.Buy,
            SpotOrderType.Limit,
            position.PositionSizeNative,
            price: position.Price,
            timeInForce: TimeInForce.GoodTillCanceled);

          if (result.Success)
          {
            position.CreatedDate = result.Data.CreateTime;
            position.Id = result.Data.Id;
            return result.Data.Id;
          }
          else
            LogError(result.Error?.Message);
        }
      }
      finally
      {
        createPositionLock.Release();
      }

      return 0;
    }

    #endregion

    #region Sell

    public async Task<long> Sell(string symbol, Position position)
    {

      try
      {
        await createPositionLock.WaitAsync();
        using (var client = new BinanceRestClient())
        {

          var result = await client.SpotApi.Trading.PlaceOrderAsync(symbol,
            OrderSide.Sell,
            SpotOrderType.Limit,
            position.PositionSizeNative,
            price: position.Price,
            timeInForce: TimeInForce.GoodTillCanceled);

          if (result.Success)
          {
            position.CreatedDate = result.Data.CreateTime;
            position.Id = result.Data.Id;

            return result.Data.Id;
          }

          else
            LogError(result.Error?.Message);
        }
      }
      finally
      {
        createPositionLock.Release();
      }

      return 0;
    }

    #endregion

    #region Cancel

    public async Task<bool> Cancel(string symbol, long positionId)
    {

      try
      {
        await createPositionLock.WaitAsync();
        using (var client = new BinanceRestClient())
        {
          var result = await client.SpotApi.Trading.CancelOrderAsync(symbol, positionId);

          if (!result.Success)
            LogError(result.Error?.Message);

          return result.Success;
        }
      }
      finally
      {
        createPositionLock.Release();
      }

    }

    #endregion

    #region PlaceLong

    public async Task<long> PlaceLong(string symbol, FuturesPosition futuresPosition)
    {
      try
      {
        await createPositionLock.WaitAsync();
        using (var client = new BinanceRestClient())
        {

          var result = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol,
            OrderSide.Buy,
            FuturesOrderType.Limit,
            futuresPosition.PositionSize,
            price: futuresPosition.Price,
            stopPrice: futuresPosition.StopLoss.Price,
            timeInForce: TimeInForce.GoodTillCanceled);

          if (result.Success)
          {
            futuresPosition.CreatedDate = result.Data.UpdateTime;
            futuresPosition.Id = result.Data.Id;
            return result.Data.Id;
          }
          else
            LogError(result.Error?.Message);
        }
      }
      finally
      {
        createPositionLock.Release();
      }

      return 0;
    }

    #endregion

    #region SubscribeToKlineInterval

    private SemaphoreSlim subscribeToKlineIntervaLock = new SemaphoreSlim(1, 1);
    private int subId;
    public async Task SubscribeToKlineInterval(string symbol, Action<IBinanceStreamKline> onKlineUpdate, KlineInterval klineInterval)
    {
      try
      {
        await subscribeToKlineIntervaLock.WaitAsync();

        await socketClient.UnsubscribeAsync(subId);

        var sub = await socketClient.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(symbol, klineInterval, (x) =>
       {
         onKlineUpdate(x.Data.Data);
       });


        if (!sub.Success)
        {
          throw new Exception("Kline subscibe failed!");
        }

        subId = sub.Data.Id;
      }
      finally
      {
        subscribeToKlineIntervaLock.Release();
      }
    }

    #endregion

    #region SubscribeUserStream

    private string key;
    Action<DataEvent<BinanceStreamOrderUpdate>> onUpdate;
    private SerialDisposable serialDisposable = new SerialDisposable();
    public async Task SubscribeUserStream(Action<DataEvent<BinanceStreamOrderUpdate>> data)
    {
      using (var client = new BinanceRestClient())
      {
        var startOkay = await client.SpotApi.Account.StartUserStreamAsync();
        if (!startOkay.Success)
        {

          return;
        }

        key = startOkay.Data;
        onUpdate = data;

        var subOkay = await socketClient.SpotApi.Account.SubscribeToUserDataUpdatesAsync(key, data, null, null, null);

        if (!subOkay.Success)
        {
          LogError(subOkay.Error?.Message);
        }

        logger.Log(MessageType.Inform2, $"{DateTime.UtcNow} Subscribe to stream success", simpleMessage: true);

        serialDisposable.Disposable?.Dispose();
        serialDisposable.Disposable = Observable.Interval(TimeSpan.FromMinutes(30)).Subscribe((x) =>
         {
           RefreshStream();
         });
      }
    }

    #endregion

    #region RefreshStream

    public async void RefreshStream()
    {
      using (var client = new BinanceRestClient())
      {
        ;
        var result = await client.SpotApi.Account.KeepAliveUserStreamAsync(key);

        if (result.Success)
        {
          logger.Log(MessageType.Inform2, $"{DateTime.UtcNow} Subscribe token refreshed", simpleMessage: true);
        }
        else
        {
          logger.Log(MessageType.Error, $"{DateTime.UtcNow} Subscribe token refreshed FAILED", simpleMessage: true);

          if (onUpdate != null)
          {
            await SubscribeUserStream(onUpdate);
          }
        }
      }
    }

    #endregion

    #region GetAccountTradeList

    public async Task<IEnumerable<BinanceTrade>> GetAccountTradeList(string symbol, DateTime? pStartTime = null)
    {
      using (var client = new BinanceRestClient())
      {
        return (await client.SpotApi.Trading.GetUserTradesAsync(symbol, startTime: pStartTime)).Data;
      }
    }

    #endregion

    #region GetTimeSpanFromInterval

    public TimeSpan GetTimeSpanFromInterval(KlineInterval klineInterval)
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

    #region LogError

    private void LogError(string message)
    {
      var logToFile = message.Contains("1000ms ahead of the server's time.") ? false : true;

      logger.Log(MessageType.Error, message, logToFile);
    }

    #endregion
  }
}
