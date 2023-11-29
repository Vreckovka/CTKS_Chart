using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CTKS_Chart.Binance;
using VCore;

namespace CTKS_Chart
{
  public class BinanceStrategy : Strategy
  {
    private readonly BinanceBroker binanceBroker;


    public BinanceStrategy(BinanceBroker binanceBroker)
    {
      this.binanceBroker = binanceBroker ?? throw new ArgumentNullException(nameof(binanceBroker));
    }

    protected override Task<bool> CancelPosition(Position position)
    {
      return binanceBroker.Close(Asset.Symbol, position.Id);
    }

    protected override Task<long> CreatePosition(Position position)
    {
      if (position.Side == PositionSide.Buy)
        return binanceBroker.Buy(Asset.Symbol, position.PositionSizeNative, position.Price);
      else
        return binanceBroker.Sell(Asset.Symbol, position.PositionSizeNative, position.Price);
    }

    string path = "State";

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