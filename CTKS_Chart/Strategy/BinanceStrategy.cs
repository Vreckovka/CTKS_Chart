using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Sockets;
using CTKS_Chart.Binance;
using VCore;
using VCore.WPF;

namespace CTKS_Chart
{
  public class BinanceStrategy : Strategy
  {
    private readonly BinanceBroker binanceBroker;
    string path = "State";

    public BinanceStrategy(BinanceBroker binanceBroker) 
    {
      this.binanceBroker = binanceBroker ?? throw new ArgumentNullException(nameof(binanceBroker));

      //Observable.Interval(TimeSpan.FromMinutes(5)).Subscribe(x =>
      //{
      //  RefreshState();
      //});

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

    public override async void RefreshState()
    {
      var closedOrders = await binanceBroker.GetClosedOrders(Asset.Symbol);

      VSynchronizationContext.InvokeOnDispatcher(async () =>
      {
        foreach (var closed in closedOrders)
        {
          var order = AllOpenedPositions.SingleOrDefault(x => x.Id == long.Parse(closed.Id));

          if (order != null)
          {
            if (order.State == PositionState.Filled || order.State == PositionState.Completed)
            {
              if (order.Side == PositionSide.Buy)
              {
                OpenBuyPositions.Remove(order);
                ClosedBuyPositions.Add(order);
              }
              else
              {
                OpenSellPositions.Remove(order);
                ClosedSellPositions.Add(order);
              }
            }
            else if (order.State == PositionState.Open)
            {
              if (closed.Status == CryptoExchange.Net.CommonObjects.CommonOrderStatus.Filled)
              {
                if (order.Side == PositionSide.Buy)
                  await CloseBuy(order);
                else
                  CloseSell(order);
              }
            }
          }
        }
      });
    }

    #region OnOrderUpdate

    private SemaphoreSlim orderLock = new SemaphoreSlim(1, 1);
    private async void OnOrderUpdate(DataEvent<BinanceStreamOrderUpdate> data)
    {
      try
      {
        await orderLock.WaitAsync();

        var orderUpdate = data.Data;
        if (orderUpdate.RejectReason != OrderRejectReason.None || orderUpdate.ExecutionType != ExecutionType.New)
          // Order got rejected, no need to show
          return;

        if (orderUpdate.Status == OrderStatus.Filled)
        {
          var existingPosition = AllOpenedPositions.SingleOrDefault(x => x.Id == orderUpdate.Id);

          if (existingPosition != null && existingPosition.State != PositionState.Filled)
          {
            if (existingPosition.Side == PositionSide.Sell)
              CloseSell(existingPosition);
            else
              await CloseBuy(existingPosition);
          }
        }

      }
      finally
      {
        orderLock.Release();
      }
    }

    #endregion

    #region CancelPosition

    protected override Task<bool> CancelPosition(Position position)
    {
      return binanceBroker.Close(Asset.Symbol, position.Id);
    }

    #endregion

    #region CreatePosition

    protected override Task<long> CreatePosition(Position position)
    {
      if (position.Side == PositionSide.Buy)
        return binanceBroker.Buy(Asset.Symbol, position.PositionSizeNative, position.Price);
      else
        return binanceBroker.Sell(Asset.Symbol, position.PositionSizeNative, position.Price);
    }

    #endregion

  

    public override void SaveState()
    {
      if (OpenBuyPositions.Count > 0)
      {
        var openBuy = JsonSerializer.Serialize(OpenBuyPositions.Select(x => new PositionDto(x)));
        var openSell = JsonSerializer.Serialize(OpenSellPositions.Select(x => new PositionDto(x)));
        var cloedSell = JsonSerializer.Serialize(ClosedSellPositions.Select(x => new PositionDto(x)));
        var closedBuy = JsonSerializer.Serialize(ClosedBuyPositions.Select(x => new PositionDto(x)));
        var map = JsonSerializer.Serialize(PositionSizeMapping.ToList());

        Path.Combine(path, "openBuy.json").EnsureDirectoryExists();

        File.WriteAllText(Path.Combine(path, "openBuy.json"), openBuy);
        File.WriteAllText(Path.Combine(path, "openSell.json"), openSell);
        File.WriteAllText(Path.Combine(path, "cloedSell.json"), cloedSell);
        File.WriteAllText(Path.Combine(path, "closedBuy.json"), closedBuy);
        File.WriteAllText(Path.Combine(path, "budget.json"), Budget.ToString());
        File.WriteAllText(Path.Combine(path, "totalProfit.json"), TotalProfit.ToString());
        File.WriteAllText(Path.Combine(path, "map.json"), map);
        File.WriteAllText(Path.Combine(path, "startBug.json"), StartingBudget.ToString());
        File.WriteAllText(Path.Combine(path, "native.json"), TotalNativeAsset.ToString());
        File.WriteAllText(Path.Combine(path, "totalBuy.json"), TotalBuy.ToString());
        File.WriteAllText(Path.Combine(path, "totalSell.json"), TotalSell.ToString());
      }
    }

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
            var pos = sells.Single(x => x.Id == op);
            pos.OpositPositions.Add(closedBuy);

            closedBuy.OpositPositions.Add(pos);
          }

          ClosedBuyPositions.Add(closedBuy);
        }



        var map = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<TimeFrame, decimal>>>(File.ReadAllText(Path.Combine(path, "map.json")));

        var data = File.ReadAllText(Path.Combine(path, "budget.json"));
        PositionSizeMapping = map.ToDictionary(x => x.Key, x => x.Value);
        Budget = (decimal)double.Parse(File.ReadAllText(Path.Combine(path, "budget.json")));
        TotalProfit = decimal.Parse(File.ReadAllText(Path.Combine(path, "totalProfit.json")));
        StartingBudget = decimal.Parse(File.ReadAllText(Path.Combine(path, "startBug.json")));
        TotalNativeAsset = decimal.Parse(File.ReadAllText(Path.Combine(path, "native.json")));
        TotalBuy = decimal.Parse(File.ReadAllText(Path.Combine(path, "totalBuy.json")));
        TotalSell = decimal.Parse(File.ReadAllText(Path.Combine(path, "totalSell.json")));
      }
    }
  }
}