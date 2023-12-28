using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Logger;
using VCore.Standard;
using VCore.Standard.Helpers;

namespace CTKS_Chart
{
  public class StrategyData
  {
    public double ScaleSize { get; set; }
    public decimal StartingBudget { get; set; }
    public decimal Budget { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalNativeAsset { get; set; }
    public decimal MinBuyPrice { get; set; }
    public IEnumerable<KeyValuePair<TimeFrame, decimal>> PositionSizeMapping { get; set; }
  }


  public abstract class Strategy : ViewModel
  {
    protected decimal LeftSize = 0;

    public Strategy()
    {
      Budget = StartingBudget;
    }

    #region Properties

    private Subject<Position> onCreatePositionSub = new Subject<Position>();
    public IObservable<Position> OnCreatePosition
    {
      get
      {
        return onCreatePositionSub.AsObservable();
      }
    }

    public Asset Asset { get; set; }
    public decimal MinPositionValue { get; set; } = 6;

    public ILogger Logger { get; set; }


    #region StrategyData

    private StrategyData strategyData = new StrategyData()
    {
      MinBuyPrice = (decimal)0.25,
      PositionSizeMapping = new Dictionary<TimeFrame, decimal>()
      {
        { TimeFrame.M12, 70},
        { TimeFrame.M6, 60},
        { TimeFrame.M3, 50},
        { TimeFrame.M1, 30},
        { TimeFrame.W2, 20},
        { TimeFrame.W1, 10},
      },
      StartingBudget = 1000,
      ScaleSize = 1.5
    };

    public StrategyData StrategyData
    {
      get { return strategyData; }
      set
      {
        if (value != strategyData)
        {
          strategyData = value;
          RaisePropertyChanged();

          RaisePropertyChanged(nameof(StartingBudget));
          RaisePropertyChanged(nameof(TotalProfit));
          RaisePropertyChanged(nameof(MinBuyPrice));
          RaisePropertyChanged(nameof(Budget));
          RaisePropertyChanged(nameof(TotalNativeAsset));
          RaisePropertyChanged(nameof(TotalBuy));
          RaisePropertyChanged(nameof(TotalSell));
          RaisePropertyChanged(nameof(PositionSizeMapping));
          RaisePropertyChanged(nameof(ScaleSize));
        }
      }
    }

    #endregion

    #region ScaleSize

    public double ScaleSize
    {
      get { return StrategyData.ScaleSize; }
      set
      {
        if (value != StrategyData.ScaleSize)
        {
          StrategyData.ScaleSize = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region StartingBudget

    public decimal StartingBudget
    {
      get { return StrategyData.StartingBudget; }
      protected set
      {
        if (value != StrategyData.StartingBudget)
        {
          StrategyData.StartingBudget = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalProfit

    public decimal TotalProfit
    {
      get { return StrategyData.TotalProfit; }
      set
      {
        if (value != StrategyData.TotalProfit)
        {
          StrategyData.TotalProfit = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinBuyPrice

    public decimal MinBuyPrice
    {
      get { return StrategyData.MinBuyPrice; }
      set
      {
        if (value != StrategyData.MinBuyPrice)
        {
          StrategyData.MinBuyPrice = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Budget

    public decimal Budget
    {
      get { return StrategyData.Budget; }
      set
      {
        if (value != StrategyData.Budget)
        {
          StrategyData.Budget = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalNativeAsset

    public decimal TotalNativeAsset
    {
      get { return StrategyData.TotalNativeAsset; }
      set
      {
        if (value != StrategyData.TotalNativeAsset)
        {
          StrategyData.TotalNativeAsset = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalBuy

    public decimal TotalBuy
    {
      get { return AllClosedPositions.Where(x => x.Side == PositionSide.Buy).Sum(x => x.OriginalPositionSize); }
    }

    #endregion

    #region TotalSell

    public decimal TotalSell
    {
      get { return AllClosedPositions.Where(x => x.Side == PositionSide.Sell).Sum(x => x.OriginalPositionSize + x.Profit); }
    }

    #endregion

    #region PositionSizeMapping

    public IEnumerable<KeyValuePair<TimeFrame, decimal>> PositionSizeMapping
    {
      get { return StrategyData.PositionSizeMapping; }
      set
      {
        if (value != StrategyData.PositionSizeMapping)
        {
          StrategyData.PositionSizeMapping = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinSellProfitMapping

    public Dictionary<TimeFrame, double> MinSellProfitMapping { get; } = new Dictionary<TimeFrame, double>()
    {
      {TimeFrame.M12, 0.005},
      {TimeFrame.M6,  0.005},
      {TimeFrame.M3,  0.005},
      {TimeFrame.M1,  0.005},
      {TimeFrame.W2,  0.005},
      {TimeFrame.W1,  0.005},
    };

    #endregion

    #region MinBuyMapping

    public Dictionary<TimeFrame, double> MinBuyMapping { get; } = new Dictionary<TimeFrame, double>()
    {
      {TimeFrame.M12, 0.01},
      {TimeFrame.M6,  0.01},
      {TimeFrame.M3,  0.01},
      {TimeFrame.M1,  0.01},
      {TimeFrame.W2,  0.01},
      {TimeFrame.W1,  0.01},
    };

    #endregion

    #region Calculated Properties

    #region TotalNativeAssetValue

    private decimal totalNativeAssetValue;

    public decimal TotalNativeAssetValue
    {
      get { return totalNativeAssetValue; }
      set
      {
        if (value != totalNativeAssetValue)
        {
          totalNativeAssetValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalValue

    private decimal totalValue;

    public decimal TotalValue
    {
      get { return totalValue; }
      set
      {
        if (value != totalValue)
        {
          totalValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Positions

    public ObservableCollection<Position> ClosedBuyPositions { get; set; } = new ObservableCollection<Position>();
    public ObservableCollection<Position> ClosedSellPositions { get; set; } = new ObservableCollection<Position>();

    public ObservableCollection<Position> OpenSellPositions { get; set; } = new ObservableCollection<Position>();
    public ObservableCollection<Position> OpenBuyPositions { get; set; } = new ObservableCollection<Position>();

    #region ActualPositions

    public IEnumerable<Position> ActualPositions
    {
      get
      {
        return ClosedBuyPositions.Where(x => x.State == PositionState.Filled);
      }
    }

    #endregion

    #region AllCompletedPositions

    public IEnumerable<Position> AllCompletedPositions
    {
      get
      {
        return ClosedBuyPositions.Where(x => x.State == PositionState.Completed);
      }
    }

    #endregion

    #region AllClosedPositions

    public IEnumerable<Position> AllClosedPositions
    {
      get
      {
        return ClosedBuyPositions.Concat(ClosedSellPositions);
      }
    }

    #endregion

    #region AllOpenedPositions

    public IEnumerable<Position> AllOpenedPositions
    {
      get
      {
        return OpenBuyPositions.Concat(OpenSellPositions);
      }
    }

    #endregion

    #endregion

    public List<CtksIntersection> Intersections { get; set; } = new List<CtksIntersection>();

    #region AvrageBuyPrice

    public decimal AvrageBuyPrice
    {
      get
      {
        var closed = ClosedBuyPositions.Where(x => x.State == PositionState.Filled).ToList();
        var asd = closed.Sum(x => x.OriginalPositionSize);
        var asdddd = closed.Sum(x => x.OriginalPositionSizeNative);
        return TotalNativeAsset > 0 && asdddd > 0 ? asd / asdddd : 0;
      }
    }

    #endregion

    #endregion

    #region Methods

    #region GetMinBuy

    private decimal GetMinBuy(decimal low, TimeFrame timeFrame)
    {
      var min = low * (decimal)(1 - MinBuyMapping[timeFrame]);

      return min;
    }

    #endregion

    #region CreatePositions

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    public async void CreatePositions(Candle actualCandle)
    {
      try
      {
        await semaphoreSlim.WaitAsync();

        var minBuy = actualCandle.Close * (1 - MinBuyPrice);

        var openedBuy = OpenBuyPositions
          .Where(x => !Intersections.Any(y => y.Value == x.Intersection.Value) || x.Price < minBuy)
          .ToList();

        var openedSell = OpenSellPositions.Where(x => !Intersections.Any(y => y.Value == x.Intersection.Value)).ToList();

        foreach (var buyPosition in openedBuy)
        {
          await OnCancelPosition(buyPosition);
        }

        var removedBu = new HashSet<Position>();

        foreach (var sellPosition in openedSell)
        {
          await OnCancelPosition(sellPosition, removedBu);
        }

        foreach (var opened in removedBu)
        {
          await CreateSellPositionForBuy(opened, Intersections.OrderBy(x => x.Value).Where(x => x.Value > actualCandle.Close.Value));
        }

        var inter = Intersections
          .Where(x => x.Value < actualCandle.Close.Value &&
                      x.Value > minBuy &&
                      x.Value < GetMinBuy(actualCandle.Close.Value, x.TimeFrame))
          .OrderByDescending(x => x.Value)
          .ToList();

        foreach (var intersection in inter)
        {
          var positionsOnIntersesction = AllOpenedPositions
            .Where(x => x.Intersection?.Value == intersection.Value)
            .ToList();

          var maxPOsitionOnIntersection = GetPositionSize(intersection.TimeFrame);
          var sum = positionsOnIntersesction.Sum(x => x.PositionSize);

          var existing = AllClosedPositions
            .Where(x => x.Intersection.Value == intersection.Value)
            .Where(x => x.State == PositionState.Filled)
            .Sum(x => x.OpositPositions.Sum(y => y.PositionSize));

          var leftSize = maxPOsitionOnIntersection - sum;

          if (existing > 0)
          {
            leftSize = leftSize - existing;
          }

          if (Budget < leftSize)
          {
            if (Budget - 1 > MinPositionValue)
            {
              leftSize = Budget - 1;
            }
            else
            {
              break;
            }
          }


          if (leftSize > MinPositionValue)
          {
            await CreateBuyPosition(leftSize, intersection);
          }
        }
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region GetPositionSize

    private decimal GetPositionSize(TimeFrame timeFrame)
    {
      return PositionSizeMapping.Single(x => x.Key == timeFrame).Value;
    }

    #endregion

    #region ValidatePositions

    public async void ValidatePositions(Candle candle)
    {
      for (int i = 0; i < 2; i++)
      {
        var allPositions = AllOpenedPositions.Where(x => x.State == PositionState.Open).ToList();

        foreach (var position in allPositions)
        {
          if (IsPositionFilled(candle, position))
          {
            position.Fees = position.OriginalPositionSize * (decimal)0.001;

            if (position.Side == PositionSide.Buy)
            {
              await CloseBuy(position);
            }
            else
            {
              CloseSell(position);
            }
          }
        }
      }

      var openedBuy = OpenBuyPositions.ToList();

      var assetsValue = TotalNativeAsset * candle.Close.Value;
      var openPositions = openedBuy.Sum(x => x.PositionSize);

      TotalValue = assetsValue + openPositions + Budget;
      TotalNativeAssetValue = TotalNativeAsset * candle.Close.Value;
      RaisePropertyChanged(nameof(AvrageBuyPrice));

      RaisePropertyChanged(nameof(AllClosedPositions));
    }

    #endregion

    #region CreateSellPositionForBuy

    protected async Task CreateSellPositionForBuy(Position position, IEnumerable<CtksIntersection> ctksIntersections)
    {
      if (position.PositionSize > 0)
      {
        await CreateSell(position, ctksIntersections);

        var sumOposite = position.OpositPositions.Sum(x => x.OriginalPositionSizeNative);
        if (sumOposite != position.OriginalPositionSizeNative)
        {
          throw new Exception("Postion asset value does not mach sell order !!");
        }
      }
    }

    #endregion

    #region CloseBuy

    public async Task CloseBuy(Position position)
    {
      try
      {
        await semaphoreSlim.WaitAsync();

        if (position.State != PositionState.Filled)
        {
          var ordered = Intersections.OrderBy(x => x.Value).ToList();

          TotalNativeAsset += position.PositionSizeNative;
          LeftSize += position.PositionSizeNative;

          await CreateSellPositionForBuy(position, ordered);

          position.State = PositionState.Filled;

          ClosedBuyPositions.Add(position);
          OpenBuyPositions.Remove(position);
          // Budget -= position.Fees ?? 0;

          if (TotalValue / 3 < PositionSizeMapping.Single(x => x.Key == TimeFrame.M12).Value)
          {
            Scale(-1 * TotalValue * (decimal)0.01);
          }

          RaisePropertyChanged(nameof(TotalBuy));
          RaisePropertyChanged(nameof(ActualPositions));
          RaisePropertyChanged(nameof(AllCompletedPositions));

          SaveState();
        }
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region CloseSell

    protected void CloseSell(Position position)
    {
      ClosedSellPositions.Add(position);
      OpenSellPositions.Remove(position);

      var finalSize = position.Price * position.OriginalPositionSizeNative;

      position.Profit = finalSize - position.OriginalPositionSize;
      position.PositionSize = 0;
      position.PositionSizeNative = 0;

      TotalProfit += position.Profit;
      Budget += finalSize;
      TotalNativeAsset -= position.OriginalPositionSizeNative;

      //Budget -= position.Fees ?? 0;

      Scale(position.Profit);

      var sum = OpenSellPositions.ToList().Sum(x => x.OriginalPositionSizeNative);

      if (Math.Round(sum, Asset.NativeRound) != Math.Round(TotalNativeAsset, Asset.NativeRound))
      {
        throw new Exception($"Native asset value does not mach sell order !! {Math.Round(sum, Asset.NativeRound)} != {Math.Round(TotalNativeAsset, Asset.NativeRound)}");
      }

      if (position.OpositPositions.Count > 0)
      {
        var originalBuy = position.OpositPositions.Single();

        originalBuy.RaiseNotify(nameof(Position.TotalProfit));

        if (originalBuy.OpositPositions.Sum(x => x.PositionSize) == 0)
        {
          originalBuy.State = PositionState.Completed;
        }
      }

      position.State = PositionState.Filled;

      RaisePropertyChanged(nameof(TotalSell));
      RaisePropertyChanged(nameof(ActualPositions));

      SaveState();

    }

    #endregion

    #region CreateSell

    private async Task CreateSell(Position position, IEnumerable<CtksIntersection> ctksIntersections)
    {
      var minPrice = position.Price * (decimal)(1.0 + MinSellProfitMapping[position.TimeFrame]);
      var nextLines = ctksIntersections.Where(x => x.Value > minPrice).ToList();

      int i = 0;
      List<Position> createdPositions = new List<Position>();

      while (position.PositionSize > 0 && nextLines.Count > 0)
      {
        foreach (var nextLine in nextLines)
        {
          var leftPositionSize = position.PositionSize;

          if (leftPositionSize < 0)
          {
            position.PositionSize = 0;
          }

          var ctksIntersection = nextLine;
          var forcePositionSize = i > 0 || nextLine.Value > position.Price * (decimal)1.5;


          var positionSize = position.PositionSize;

          var maxPOsitionOnIntersection = (decimal)GetPositionSize(ctksIntersection.TimeFrame);

          var positionsOnIntersesction = OpenSellPositions
            .Where(x => x.Intersection?.Value == ctksIntersection.Value)
            .Sum(x => x.PositionSize);

          leftPositionSize = (decimal)maxPOsitionOnIntersection - positionsOnIntersesction;


          if (!forcePositionSize && MinPositionValue > leftPositionSize)
          {
            continue;
          }

          if (position.PositionSize > leftPositionSize)
          {
            positionSize = leftPositionSize;
          }

          if (leftPositionSize <= 0 && !forcePositionSize)
            continue;

          if (forcePositionSize)
          {
            if (position.PositionSize > maxPOsitionOnIntersection)
              positionSize = maxPOsitionOnIntersection;
            else
              positionSize = position.PositionSize;
          }

          var roundedNativeSize = Math.Round(positionSize / position.Price, Asset.NativeRound);

          if (position.PositionSizeNative < roundedNativeSize)
          {
            roundedNativeSize = position.PositionSizeNative;
          }

          if (position.PositionSize - positionSize == 0 && position.PositionSizeNative - roundedNativeSize > 0)
          {
            roundedNativeSize = position.PositionSizeNative;
          }

          positionSize = position.Price * roundedNativeSize;
          if (positionSize <= 0)
          {
            continue;
          }

          var leftSize = position.PositionSize - positionSize;

          if (MinPositionValue > leftSize && leftSize > 0)
          {
            roundedNativeSize = roundedNativeSize + (position.PositionSizeNative - roundedNativeSize);

          }

          positionSize = position.Price * roundedNativeSize;
          if (positionSize <= 0)
          {
            continue;
          }

          var newPosition = new Position(positionSize, ctksIntersection.Value, roundedNativeSize)
          {
            Side = PositionSide.Sell,
            TimeFrame = ctksIntersection.TimeFrame,
            Intersection = ctksIntersection,
            State = PositionState.Open,
          };

          newPosition.OpositPositions.Add(position);

          position.PositionSize -= positionSize;
          position.PositionSizeNative -= roundedNativeSize;
          position.OpositPositions.Add(newPosition);


          createdPositions.Add(newPosition);



          if (position.PositionSize <= 0)
            break;
        }

        i++;
      }

      foreach (var sell in createdPositions)
      {
        long id = 0;

        while (id == 0)
        {
          id = await CreatePosition(sell);


          if (id > 0)
          {
            sell.Id = id;

            onCreatePositionSub.OnNext(sell);
            OpenSellPositions.Add(sell);

            LeftSize -= sell.PositionSizeNative;

            if (LeftSize < 0)
              throw new Exception("Left native size is less than 0 !!");
          }
          else
          {
            Logger?.Log(MessageType.Error, "Sell order was not created!, trying again");
            await Task.Delay(1000);
          }
        }
      }

      SaveState();
    }

    #endregion

    #region CreateBuyPosition

    private async Task CreateBuyPosition(decimal positionSize, CtksIntersection intersection)
    {

      var existingBuys = ClosedBuyPositions
      .Where(x => x.State == PositionState.Filled)
      .Where(x => x.Intersection.Value == intersection.Value)
      .ToList();


      if (existingBuys.Any())
      {
        var leftSize = existingBuys.Sum(x => x.OpositPositions.Sum(y => y.OriginalPositionSize - y.PositionSize));

        if (positionSize > leftSize)
        {
          positionSize = leftSize;
        }
      }

      var roundedNativeSize = Math.Round(positionSize / intersection.Value, Asset.NativeRound);
      positionSize = roundedNativeSize * intersection.Value;

      if (positionSize == 0)
        return;


      var newPosition = new Position(positionSize, intersection.Value, roundedNativeSize)
      {
        TimeFrame = intersection.TimeFrame,
        Intersection = intersection,
        State = PositionState.Open,
        Side = PositionSide.Buy,
      };

      var id = await CreatePosition(newPosition);

      if (id > 0)
      {
        newPosition.Id = id;

        Budget -= newPosition.PositionSize;
        OpenBuyPositions.Add(newPosition);

        onCreatePositionSub.OnNext(newPosition);

      }

      SaveState();
    }

    #endregion

    #region OnCancelPosition

    public async Task OnCancelPosition(Position position, HashSet<Position> removed = null, bool force = false)
    {
      var cancled = force;

      if (!cancled)
        cancled = await CancelPosition(position);

      if (cancled)
      {
        if (position.Side == PositionSide.Buy)
        {
          OpenBuyPositions.Remove(position);
          Budget += position.PositionSize;
        }
        else
        {
          var buy = position.OpositPositions[0];
          buy.OpositPositions.Remove(position);
          buy.PositionSize += position.OriginalPositionSize;
          buy.PositionSizeNative += position.OriginalPositionSizeNative;
          LeftSize += position.OriginalPositionSizeNative;

          OpenSellPositions.Remove(position);

          if (removed != null)
            removed.Add(buy);
        }
      }

      SaveState();
    }

    #endregion

    #region Scale

    private void Scale(decimal profit)
    {
      var perc = ((profit * (decimal)100.0 / (TotalProfit + StartingBudget))) / (decimal)100.0;
      var map = PositionSizeMapping.ToList();

      var size = (1 + (perc * (decimal)1 * (decimal)ScaleSize));
      var maxValue = (TotalProfit + StartingBudget);
      var nextMaxValue = PositionSizeMapping.Single(x => x.Key == TimeFrame.M12).Value * size;

      var newList = new Dictionary<TimeFrame, decimal>();

      if (nextMaxValue < maxValue)
      {
        foreach (var mapping in map)
        {
          newList.Add(mapping.Key, mapping.Value * size);
        }

        PositionSizeMapping = newList;

        SaveState();
      }
    }

    #endregion


    protected abstract Task<bool> CancelPosition(Position position);
    protected abstract Task<long> CreatePosition(Position position);
    public abstract void SaveState();
    public abstract void LoadState();
    public abstract Task RefreshState();
    public virtual bool IsPositionFilled(Candle candle, Position position)
    {
      if (position.Side == PositionSide.Buy)
      {
        return candle.Close.Value <= position.Price;
      }
      else
      {
        return candle.Close.Value >= position.Price;
      }
    }

    #region Reset

    public async Task Reset(Candle actualCandle)
    {
      try
      {
        await semaphoreSlim.WaitAsync();
        var asd = AllOpenedPositions.ToList();

        foreach (var open in asd)
        {
          await OnCancelPosition(open, force: true);
        }


        LeftSize = TotalNativeAsset;
        var fakeSize = LeftSize;

        var removedBu = new List<Position>();

        var buys = ClosedBuyPositions
          .Where(x => x.State == PositionState.Filled &&
                      !x.OpositPositions.Any())
          .DistinctBy(x => x.Id)
          .ToList();



        if (buys.Count > 0)
        {
          removedBu = new List<Position>(buys);
        }

        for (int i = 0; i < removedBu.Count; i++)
        {
          var opened = removedBu[i];

          fakeSize -= opened.PositionSizeNative;
          opened.OriginalPositionSize = opened.PositionSize;
          opened.OriginalPositionSizeNative = opened.PositionSizeNative;

          if (fakeSize < 0)
          {
            opened.OriginalPositionSizeNative = LeftSize;
            opened.PositionSizeNative = LeftSize;
            opened.OriginalPositionSize = opened.Price * LeftSize;
            opened.PositionSize = opened.Price * LeftSize;
          }

          if (i == removedBu.Count - 1 && LeftSize > 0)
          {
            opened.OriginalPositionSizeNative += LeftSize - opened.OriginalPositionSizeNative;
            opened.PositionSizeNative += LeftSize - opened.PositionSizeNative;
            opened.OriginalPositionSize = opened.Price * opened.OriginalPositionSizeNative;
            opened.PositionSize = opened.Price * opened.PositionSizeNative;
          }

          if (LeftSize > 0)
            await CreateSellPositionForBuy(opened, Intersections.OrderBy(x => x.Value).Where(x => x.Value > actualCandle.Close * (decimal)1.01));
          else
            ClosedBuyPositions.Remove(opened);
        }


        SaveState();
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #endregion
  }
}