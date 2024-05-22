using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Sockets;
using CTKS_Chart.Binance;
using CTKS_Chart.Strategy.Futures;
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using Logger;
using VCore;
using VCore.Standard.Helpers;
using VCore.WPF;

namespace CTKS_Chart.Strategy
{
  public abstract class BinanceStrategy<TPosition> : StrategyViewModel<TPosition>
      where TPosition : Position, new()
  {
    protected readonly BinanceBroker binanceBroker;
    private readonly ILogger logger;

    string path = Path.Combine(Settings.DataPath, "State");
    protected SemaphoreSlim orderUpdateLock = new SemaphoreSlim(1, 1);

    public BinanceStrategy(BinanceBroker binanceBroker, ILogger logger)
    {
      this.binanceBroker = binanceBroker ?? throw new ArgumentNullException(nameof(binanceBroker));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      Logger = this.logger;


    }

    public override async void SubscribeToChanges()
    {
#if RELEASE
      await binanceBroker.SubscribeUserStream(OnOrderUpdate);
#endif
    }

    public override bool IsPositionFilled(Candle candle, TPosition position)
    {
      return false;
    }


    #region OnOrderUpdate

    private async void OnOrderUpdate(DataEvent<BinanceStreamOrderUpdate> data)
    {
      try
      {
        await orderUpdateLock.WaitAsync();

        var orderUpdate = data.Data;

        if (Asset != null && data.Data.Symbol == Asset.Symbol)
        {
          var message = $"{orderUpdate.UpdateTime} Order update {orderUpdate.Status} " +
                        $"{orderUpdate.Side} {orderUpdate.Price.ToString($"N{Asset.PriceRound}")} " +
                        $"{orderUpdate.QuantityFilled.ToString($"N{Asset.NativeRound}")}";

          if (orderUpdate.Status == OrderStatus.Filled)
          {
            if (orderUpdate.Side == OrderSide.Buy)
              Logger.Log(MessageType.Success, message, simpleMessage: true);
            else
              Logger.Log(MessageType.Success2, message, simpleMessage: true);
          }
          else
          {
            Logger.Log(MessageType.Inform, message, simpleMessage: true);
          }


          if (orderUpdate.RejectReason != OrderRejectReason.None)
          {
            Logger.Log(MessageType.Error, $"Order update REJECTED {orderUpdate.Id} {orderUpdate.Status} {orderUpdate.UpdateTime} {orderUpdate.RejectReason}", simpleMessage: true);
          }


          VSynchronizationContext.InvokeOnDispatcher(async () =>
          {
            if (orderUpdate.Status == OrderStatus.Filled)
            {
              var existingPosition = AllOpenedPositions.SingleOrDefault(x => x.Id == orderUpdate.Id);

              if (existingPosition != null && existingPosition.State != PositionState.Filled)
              {
                var fees = await GetFees(orderUpdate.Fee, orderUpdate.FeeAsset);

                existingPosition.Fees = fees;
                existingPosition.FilledDate = orderUpdate.UpdateTime;

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
        orderUpdateLock.Release();
      }
    }

    #endregion

    #region RefreshState

    public override async Task RefreshState()
    {
      try
      {
        await orderUpdateLock.WaitAsync();

        var closedOrders = (await binanceBroker.GetClosedOrders(Asset.Symbol)).ToList();

        foreach (var closed in closedOrders)
        {
          var postion = AllOpenedPositions.SingleOrDefault(x => x.Id == long.Parse(closed.Id));

          if (postion != null)
          {
            if (postion.State == PositionState.Open)
            {
              if (closed.Status == CryptoExchange.Net.CommonObjects.CommonOrderStatus.Filled)
              {
                var trade = (await binanceBroker.GetClosedTrades(closed.Id, Asset.Symbol))?.FirstOrDefault();

                if (trade != null)
                {
                  postion.Fees = await GetFees(trade.Fee, trade.FeeAsset);
                  postion.FilledDate = trade.Timestamp;
                }
                else
                  postion.FilledDate = closed.Timestamp;

                if (postion.Side == PositionSide.Buy)
                  await CloseBuy(postion);
                else
                  CloseSell(postion);
              }
              else if (closed.Status == CryptoExchange.Net.CommonObjects.CommonOrderStatus.Canceled)
              {
                await CancelPosition(postion, force: true);
              }
            }
          }
        }


        var valids = ActualPositions
          .Where(x => x.OpositPositions.All(y => y.State == PositionState.Filled)).ToList();

        foreach (var valid in valids)
        {
          valid.State = PositionState.Filled;
          ActualPositions.Remove(valid);
        }
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
      finally
      {
        orderUpdateLock.Release();
      }
    }

    #endregion

    #region FetchMissingInfo

    public async Task FetchMissingInfo()
    {
      try
      {
        await orderUpdateLock.WaitAsync();

        var tradeList = (await binanceBroker.GetAccountTradeList(Asset.Symbol, new DateTime(2023, 11, 1))).ToList();
        var misingInfoList = AllClosedPositions.Where(x => x.Fees == null || x.FilledDate == null);

        foreach (var missingInfoPosition in misingInfoList)
        {
          var trade = tradeList.FirstOrDefault(x => x.OrderId == missingInfoPosition.Id);

          if (trade != null)
          {
            missingInfoPosition.FilledDate = trade.Timestamp;

            if (missingInfoPosition.Fees == null)
              missingInfoPosition.Fees = await GetFees(trade.Fee, trade.FeeAsset);
          }
        }

        var openOrders = (await binanceBroker.GetOpenOrders(Asset.Symbol)).ToList();
        misingInfoList = AllOpenedPositions.Where(x => x.CreatedDate == null);

        foreach (var missingInfoPosition in misingInfoList)
        {
          var trade = openOrders.FirstOrDefault(x => x.Id == missingInfoPosition.Id.ToString());

          if (trade != null)
          {
            missingInfoPosition.CreatedDate = trade.Timestamp;
          }
        }


        RaisePropertyChanged(nameof(OpenBuyPositions));
        RaisePropertyChanged(nameof(ClosedBuyPositions));

        RaisePropertyChanged(nameof(ClosedSellPositions));
        RaisePropertyChanged(nameof(OpenSellPositions));

        RaisePropertyChanged(nameof(AllCompletedPositions));

        SaveState();
      }
      finally
      {
        orderUpdateLock.Release();
      }
    }

    #endregion

    #region SaveState

    private object saveLock = new object();
    public override void SaveState()
    {
      lock (saveLock)
      {
        var options = new JsonSerializerOptions()
        {
          WriteIndented = true
        };

        var openBuy = JsonSerializer.Serialize(OpenBuyPositions.Select(x => new PositionDto<TPosition>(x)), options);
        var openSell = JsonSerializer.Serialize(OpenSellPositions.Select(x => new PositionDto<TPosition>(x)), options);
        var cloedSell = JsonSerializer.Serialize(ClosedSellPositions.Select(x => new PositionDto<TPosition>(x)), options);
        var closedBuy = JsonSerializer.Serialize(ClosedBuyPositions.Select(x => new PositionDto<TPosition>(x)), options);

        Path.Combine(path, "openBuy.json").EnsureDirectoryExists();

        File.WriteAllText(Path.Combine(path, "openBuy.json"), openBuy);
        File.WriteAllText(Path.Combine(path, "openSell.json"), openSell);
        File.WriteAllText(Path.Combine(path, "cloedSell.json"), cloedSell);
        File.WriteAllText(Path.Combine(path, "closedBuy.json"), closedBuy);

        SaveStrategyData();
      }
    }

    #endregion

    #region SaveStrategyData

    public override void SaveStrategyData()
    {
      var options = new JsonSerializerOptions()
      {
        WriteIndented = true
      };

      var data = JsonSerializer.Serialize(StrategyData, options);

      File.WriteAllText(Path.Combine(path, "data.json"), data);
    }

    #endregion

    #region LoadState

    public override void LoadState()
    {
      if (File.Exists(Path.Combine(path, "openBuy.json")))
      {
        var openBuys = JsonSerializer.Deserialize<IEnumerable<PositionDto<TPosition>>>(File.ReadAllText(Path.Combine(path, "openBuy.json")));
        var openSells = JsonSerializer.Deserialize<IEnumerable<PositionDto<TPosition>>>(File.ReadAllText(Path.Combine(path, "openSell.json")));
        var closedSells = JsonSerializer.Deserialize<IEnumerable<PositionDto<TPosition>>>(File.ReadAllText(Path.Combine(path, "cloedSell.json")));
        var closedBuys = JsonSerializer.Deserialize<IEnumerable<PositionDto<TPosition>>>(File.ReadAllText(Path.Combine(path, "closedBuy.json")));

        foreach (var position in openBuys)
        {
          OpenBuyPositions.Add(position.GetPosition());
        }

        foreach (var position in openSells)
        {
          OpenSellPositions.Add(position.GetPosition());
        }

        foreach (var position in closedSells)
        {
          ClosedSellPositions.Add(position.GetPosition());
        }

        var sells = OpenSellPositions.Concat(ClosedSellPositions).ToArray();

        foreach (var closedBuy in closedBuys)
        {
          var normalPos = closedBuy.GetPosition();

          foreach (var op in closedBuy.OpositePositions)
          {
            Position pos = null;

            //Is Debug
            if (op == 1)
            {
              pos = sells.FirstOrDefault(x => x.Id == op);
            }
            else
            {
              pos = sells.SingleOrDefault(x => x.Id == op);
            }


            if (pos != null)
            {
              pos.OpositPositions.Add(normalPos);

              normalPos.OpositPositions.Add(pos);
            }
          }

          ClosedBuyPositions.Add(normalPos);
        }
      }

      if (File.Exists(Path.Combine(path, "data.json")))
      {
        StrategyData = JsonSerializer.Deserialize<StrategyData>(File.ReadAllText(Path.Combine(path, "data.json")));
        wasStrategyDataLoaded = true;
      }

      ActualPositions = new ObservableCollection<TPosition>(ClosedBuyPositions.Where(x => x.State == PositionState.Filled));

      RaisePropertyChanged(nameof(TotalBuy));
      RaisePropertyChanged(nameof(TotalSell));
      RaisePropertyChanged(nameof(AllCompletedPositions));
      RaisePropertyChanged(nameof(TotalExpectedProfit));
    }

    #endregion

    #region GetFees

    protected async Task<decimal?> GetFees(decimal? fee, string feeAsset)
    {
      try
      {
        var ticker = await binanceBroker.GetTicker(feeAsset + "USDT");

        return fee * ticker;
      }
      catch (Exception ex)
      {
        logger.Log(ex);

        return null;
      }
    }

    #endregion
  }

  public class BinanceFuturesStrategy : BinanceStrategy<FuturesPosition>
  {
    public BinanceFuturesStrategy(BinanceBroker binanceBroker, ILogger logger) : base(binanceBroker, logger)
    {
    }

    #region CancelPosition

    protected override async Task<bool> PlaceCancelPosition(FuturesPosition position)
    {
      return await binanceBroker.Cancel(Asset.Symbol, position.Id);
    }

    #endregion

    #region CreatePosition

    protected override async Task<long> PlaceCreatePosition(FuturesPosition position)
    {
      if (position.Side == PositionSide.Buy)
        return await binanceBroker.Buy(Asset.Symbol, position);
      else
        return await binanceBroker.Sell(Asset.Symbol, position);
    }

    #endregion
  }




  public class BinanceSpotStrategy : BinanceStrategy<Position>
  {
    public BinanceSpotStrategy(BinanceBroker binanceBroker, ILogger logger) : base(binanceBroker, logger)
    {
    }

    #region CancelPosition

    protected override async Task<bool> PlaceCancelPosition(Position position)
    {

#if RELEASE
      return await binanceBroker.Cancel(Asset.Symbol, position.Id);
#else
      return await Task.FromResult(true);
#endif
    }

    #endregion

    #region CreatePosition

    private static int FakeId = 1;
    protected override async Task<long> PlaceCreatePosition(Position position)
    {

#if RELEASE
      if (position.Side == PositionSide.Buy)
        return await binanceBroker.Buy(Asset.Symbol, position);
      else
        return await binanceBroker.Sell(Asset.Symbol, position);
#else
      return await Task.FromResult(FakeId++);
#endif

    }

    #endregion

  }
}