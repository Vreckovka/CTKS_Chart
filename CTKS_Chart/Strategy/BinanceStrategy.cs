using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Sockets;
using CTKS_Chart.Binance;
using Logger;
using VCore;
using VCore.WPF;

namespace CTKS_Chart
{

  public class BinanceStrategy : Strategy
  {
    private readonly BinanceBroker binanceBroker;
    private readonly ILogger logger;
    private readonly bool isLive;
    string path = "State";

    public BinanceStrategy(BinanceBroker binanceBroker, ILogger logger, bool isLive)
    {
      this.binanceBroker = binanceBroker ?? throw new ArgumentNullException(nameof(binanceBroker));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.isLive = isLive;
      Logger = this.logger;

      Subscribe();
    }

    public override bool IsPositionFilled(Candle candle, Position position)
    {
      return false;
    }

    private async void Subscribe()
    {
      await binanceBroker.SubscribeUserStream(OnOrderUpdate);
    }

    #region OnOrderUpdate

    private SemaphoreSlim orderLock = new SemaphoreSlim(1, 1);
    private async void OnOrderUpdate(DataEvent<BinanceStreamOrderUpdate> data)
    {
      try
      {
        await orderLock.WaitAsync();
        var orderUpdate = data.Data;

        if (Asset != null && data.Data.Symbol == Asset.Symbol)
        {
          var message = $"{orderUpdate.UpdateTime} Order update {orderUpdate.Status} " +
                        $"{orderUpdate.Side} {orderUpdate.Price.ToString($"N{Asset.PriceRound}")} " +
                        $"{orderUpdate.QuantityFilled.ToString($"N{Asset.NativeRound}")}";

          if (orderUpdate.Status == OrderStatus.Filled)
          {
            if (orderUpdate.Side == OrderSide.Buy)
              logger.Log(MessageType.Success, message, simpleMessage: true);
            else
              logger.Log(MessageType.Success2, message, simpleMessage: true);
          }
          else
          {
            logger.Log(MessageType.Inform, message, simpleMessage: true);
          }


          if (orderUpdate.RejectReason != OrderRejectReason.None)
          {
            logger.Log(MessageType.Error, $"Order update REJECTED {orderUpdate.Id} {orderUpdate.Status} {orderUpdate.UpdateTime} {orderUpdate.RejectReason}", simpleMessage: true);
          }

          VSynchronizationContext.InvokeOnDispatcher(async () =>
          {
            if (orderUpdate.Status == OrderStatus.Filled)
            {
              var existingPosition = AllOpenedPositions.SingleOrDefault(x => x.Id == orderUpdate.Id);

              if (existingPosition != null && existingPosition.State != PositionState.Filled)
              {
                var fees = await GetFees(orderUpdate);

                existingPosition.Fees = fees;

                if (existingPosition.Side == PositionSide.Sell)
                  CloseSell(existingPosition);
                else
                  await CloseBuy(existingPosition);
              }
            }
          });
        }
      }
      finally
      {
        orderLock.Release();
      }
    }

    #endregion

    #region RefreshState

    public override async Task RefreshState()
    {
      var closedOrders = (await binanceBroker.GetClosedOrders(Asset.Symbol)).ToList();

      foreach (var closed in closedOrders)
      {
        var order = AllOpenedPositions.SingleOrDefault(x => x.Id == long.Parse(closed.Id));

        if (order != null)
        {
          if (order.State == PositionState.Open)
          {
            if (closed.Status == CryptoExchange.Net.CommonObjects.CommonOrderStatus.Filled)
            {
              if (order.Side == PositionSide.Buy)
                await CloseBuy(order);
              else
                CloseSell(order);
            }
            else if (closed.Status == CryptoExchange.Net.CommonObjects.CommonOrderStatus.Canceled)
            {
              await OnCancelPosition(order, force: true);
            }
          }
        }
      }
    }

    #endregion

    #region CancelPosition

    protected override Task<bool> CancelPosition(Position position)
    {
      if (isLive)
      {
        return binanceBroker.Close(Asset.Symbol, position.Id);
      }
      else
      {
        return Task.FromResult(false);
      }
    }

    #endregion

    #region CreatePosition

    protected override Task<long> CreatePosition(Position position)
    {
      if (isLive)
      {
        if (position.Side == PositionSide.Buy)
          return binanceBroker.Buy(Asset.Symbol, position.PositionSizeNative, position.Price);
        else
          return binanceBroker.Sell(Asset.Symbol, position.PositionSizeNative, position.Price);
      }
      else
      {
        return Task.FromResult(0L);
      }
    }

    #endregion

    #region SaveState

    public override void SaveState()
    {
      var openBuy = JsonSerializer.Serialize(OpenBuyPositions.Select(x => new PositionDto(x)));
      var openSell = JsonSerializer.Serialize(OpenSellPositions.Select(x => new PositionDto(x)));
      var cloedSell = JsonSerializer.Serialize(ClosedSellPositions.Select(x => new PositionDto(x)));
      var closedBuy = JsonSerializer.Serialize(ClosedBuyPositions.Select(x => new PositionDto(x)));
      var data = JsonSerializer.Serialize(StrategyData);

      Path.Combine(path, "openBuy.json").EnsureDirectoryExists();

      File.WriteAllText(Path.Combine(path, "openBuy.json"), openBuy);
      File.WriteAllText(Path.Combine(path, "openSell.json"), openSell);
      File.WriteAllText(Path.Combine(path, "cloedSell.json"), cloedSell);
      File.WriteAllText(Path.Combine(path, "closedBuy.json"), closedBuy);
      File.WriteAllText(Path.Combine(path, "data.json"), data);
    }

    #endregion

    #region LoadState

    public override void LoadState()
    {
      if (File.Exists(Path.Combine(path, "openBuy.json")))
      {
        var openBuys = JsonSerializer.Deserialize<IEnumerable<PositionDto>>(File.ReadAllText(Path.Combine(path, "openBuy.json")));
        var openSells = JsonSerializer.Deserialize<IEnumerable<PositionDto>>(File.ReadAllText(Path.Combine(path, "openSell.json")));
        var closedSells = JsonSerializer.Deserialize<IEnumerable<PositionDto>>(File.ReadAllText(Path.Combine(path, "cloedSell.json")));
        var closedBuys = JsonSerializer.Deserialize<IEnumerable<PositionDto>>(File.ReadAllText(Path.Combine(path, "closedBuy.json")));

        foreach (var position in openBuys)
        {
          OpenBuyPositions.Add(position);
        }

        foreach (var position in openSells)
        {
          OpenSellPositions.Add(position);
        }

        foreach (var position in closedSells)
        {
          ClosedSellPositions.Add(position);
        }

        var sells = OpenSellPositions.Concat(ClosedSellPositions).ToArray();

        foreach (var closedBuy in closedBuys)
        {
          foreach (var op in closedBuy.OpositePositions)
          {
            var pos = sells.SingleOrDefault(x => x.Id == op);

            if (pos != null)
            {
              pos.OpositPositions.Add(closedBuy);

              closedBuy.OpositPositions.Add(pos);
            }
          }

          ClosedBuyPositions.Add(closedBuy);
        }


        if (File.Exists(Path.Combine(path, "data.json")))
        {
          StrategyData = JsonSerializer.Deserialize<StrategyData>(File.ReadAllText(Path.Combine(path, "data.json")));
        }
        else
        {
          var map = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<TimeFrame, decimal>>>(File.ReadAllText(Path.Combine(path, "map.json")));

          PositionSizeMapping = map.ToDictionary(x => x.Key, x => x.Value);
          Budget = (decimal)double.Parse(File.ReadAllText(Path.Combine(path, "budget.json")));
          TotalProfit = decimal.Parse(File.ReadAllText(Path.Combine(path, "totalProfit.json")));
          StartingBudget = decimal.Parse(File.ReadAllText(Path.Combine(path, "startBug.json")));
          TotalNativeAsset = decimal.Parse(File.ReadAllText(Path.Combine(path, "native.json")));
        }
      }
    }

    #endregion

    private async Task<decimal?> GetFees(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
    {
      try
      {
        var fees = binanceStreamOrderUpdate.Fee;
        var asset = binanceStreamOrderUpdate.FeeAsset;

        var ticker = await binanceBroker.GetTicker(asset + "USDT");

        if (ticker != null)
        {
          return fees * ticker;
        }

        return null;
      }
      catch (Exception ex)
      {
        logger.Log(ex);

        return null;
      }
    }
  }
}