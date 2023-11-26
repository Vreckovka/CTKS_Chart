using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot.Socket;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Sockets;


namespace CTKS_Chart.Binance
{
  public class BinanceBroker
  {
    private string apiKey = "pEUT5muif0EINAO9rwNPH7f2TcGl22YT13x8vdKjn1UazTmwISyAjijCSghjrq4K";
    private string apiSecret = "5h71ZEWdjFShRIHX7Q14dGrSfASJYECPfcL06DmLzf2qcVKnYSv6SusSsYww6vbR";


    private string liveApiKey = "PKtvAWr3JlNCRxGck10RB1b1QFHrb0GF2ixUTjnHSB475Y80w11MQ7WX8v89XQgc";
    private string liveApiSecret = "I94eM3iFvLh55wic66iADGfO2EnEyTXSOyy1iKV6YDAdGsNriEYTROIZvqLG5ZI7";

    private BinanceSocketClient socketClient;

    public BinanceBroker()
    {
      //BinanceClient.SetDefaultOptions(new BinanceClientOptions()
      //{
      //  ApiCredentials = new ApiCredentials(apiKey, secret),
      //  LogVerbosity = LogVerbosity.Error,
      //  LogWriters = new List<TextWriter> { Console.Out },
      //  BaseAddress = "https://testnet.binance.vision/api"
      //});


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

    public async Task<IEnumerable<Candle>> GetCandles(string symbol, TimeSpan interval)
    {
      var list = new List<Candle>();

      using (var client = new BinanceRestClient())
      {
        var klineData = await client.SpotApi.CommonSpotClient.GetKlinesAsync(symbol, interval);

        foreach (var kline in klineData.Data)
        {
          list.Add(new Candle()
          {
            Close = kline.ClosePrice,
            High = kline.HighPrice,
            Low = kline.LowPrice,
            Open = kline.OpenPrice,
            Time = kline.OpenTime,
          });
        }
      }

      return list;
    }

    public async void GetOpenOrders(string symbol)
    {
      using (var client = new BinanceRestClient())
      {
        var orders = await client.SpotApi.CommonSpotClient.GetOpenOrdersAsync(symbol);
      }

      using (var client = new BinanceRestClient())
      {
        var ordersd = await client.SpotApi.CommonSpotClient.GetClosedOrdersAsync(symbol);
      }
    }

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


    private SemaphoreSlim createPositionLock = new SemaphoreSlim(1, 1);
    public async Task Buy(string symbol, decimal tradeAmount, decimal price)
    {

      try
      {
        await createPositionLock.WaitAsync();
        using (var client = new BinanceRestClient())
        {

          var result = await client.SpotApi.Trading.PlaceOrderAsync(symbol,
            OrderSide.Buy,
            SpotOrderType.Limit,
            tradeAmount,
            price: price,
            timeInForce: TimeInForce.GoodTillCanceled);
        }
      }
      catch (Exception ex)
      {
        ;
      }
      finally
      {
        createPositionLock.Release();
      }
    }

    public async Task Sell(string symbol, decimal tradeAmount, decimal price)
    {

      try
      {
        await createPositionLock.WaitAsync();
        using (var client = new BinanceRestClient())
        {

          var result = await client.SpotApi.Trading.PlaceOrderAsync(symbol,
            OrderSide.Sell,
            SpotOrderType.Limit,
            tradeAmount,
            price: price,
            timeInForce: TimeInForce.GoodTillCanceled);
        }
      }
      finally
      {
        createPositionLock.Release();
      }

    }


    private SemaphoreSlim subscribeToKlineIntervaLock = new SemaphoreSlim(1, 1);
    private int subId;
    public async Task SubscribeToKlineInterval(Action<IBinanceStreamKline> onKlineUpdate, KlineInterval klineInterval)
    {

      try
      {
        await subscribeToKlineIntervaLock.WaitAsync();

        await socketClient.UnsubscribeAsync(subId);
        var sub = await socketClient.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync("ADAUSDT", klineInterval, (x) =>
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

    public async Task SubscribeUserStream()
    {
      using (var client = new BinanceRestClient())
      {
        var startOkay = await client.SpotApi.Account.StartUserStreamAsync();
        if (!startOkay.Success)
        {

          return;
        }

        var subOkay = await socketClient.SpotApi.Account.SubscribeToUserDataUpdatesAsync(startOkay.Data, OnOrderUpdate, null, OnAccountUpdate, null);
        if (!subOkay.Success)
        {
          return;
        }




        var accountResult = await client.SpotApi.Account.GetAccountInfoAsync();

      }
    }

    private void OnAccountUpdate(DataEvent<BinanceStreamPositionsUpdate> data)
    {
      Debug.WriteLine(data);
    }

    private object orderLock = new object();
    private void OnOrderUpdate(DataEvent<BinanceStreamOrderUpdate> data)
    {
      var orderUpdate = data.Data;

      //var symbol = AllPrices.SingleOrDefault(a => a.Symbol == orderUpdate.Symbol);
      //if (symbol == null)
      //  return;

      lock (orderLock)
      {
        //var order = symbol.Orders.SingleOrDefault(o => o.Id == orderUpdate.Id);
        //if (order == null)
        {
          if (orderUpdate.RejectReason != OrderRejectReason.None || orderUpdate.ExecutionType != ExecutionType.New)
            // Order got rejected, no need to show
            return;

          Application.Current.Dispatcher.Invoke(() =>
          {
            //symbol.AddOrder(new OrderViewModel()
            //{
            //  ExecutedQuantity = orderUpdate.QuoteQuantityFilled,
            //  Id = orderUpdate.Id,
            //  OriginalQuantity = orderUpdate.Quantity,
            //  Price = orderUpdate.Price,
            //  Side = orderUpdate.Side,
            //  Status = orderUpdate.Status,
            //  Symbol = orderUpdate.Symbol,
            //  Time = orderUpdate.CreateTime,
            //  Type = orderUpdate.Type
            //});
          });
        }
        // else
        {
          //   order.ExecutedQuantity = orderUpdate.QuantityFilled;
          //   order.Status = orderUpdate.Status;
        }
      }
    }
  }
}
