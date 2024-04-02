using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using CTKS_Chart.Views.Prompts;
using Logger;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.Prompt;

namespace CTKS_Chart.Strategy
{
  public enum StrategyPosition
  {
    Bullish,
    Bearish,
    Neutral
  }

  public abstract class StrategyViewModel : Strategy
  {
    public override IList<Position> ClosedBuyPositions { get; set; } = new ObservableCollection<Position>();
    public override IList<Position> ClosedSellPositions { get; set; } = new ObservableCollection<Position>();

    public override IList<Position> OpenSellPositions { get; set; } = new ObservableCollection<Position>();
    public override IList<Position> OpenBuyPositions { get; set; } = new ObservableCollection<Position>();

    #region ActualPositions

    private IList<Position> actualPositions = new ObservableCollection<Position>();

    public override IList<Position> ActualPositions
    {
      get { return actualPositions; }
      set
      {
        if (value != actualPositions)
        {
          actualPositions = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalExpectedProfit

    public decimal TotalExpectedProfit
    {
      get { return ActualPositions.Sum(x => x.ExpectedProfit); }
    }

    #endregion

    #region AvrageBuyPrice

    public decimal AvrageBuyPrice
    {
      get
      {
        var positions = ActualPositions.ToList();

        var filled = ActualPositions.Sum(x => x.OpositPositions.Sum(y => y.Profit));
        var value = positions.Sum(x => x.OpositPositions.Sum(y => y.PositionSize)) - filled;
        var native = positions.Sum(x => x.OpositPositions.Sum(y => y.PositionSizeNative));

        return Math.Round(TotalNativeAsset > 0 && native > 0 ? value / native : 0, Asset.PriceRound);
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
  }

  public abstract class Strategy : ViewModel
  {
    protected decimal LeftSize = 0;
    private SemaphoreSlim sellLock = new SemaphoreSlim(1, 1);
    private SemaphoreSlim buyLock = new SemaphoreSlim(1, 1);

    public Strategy()
    {
      Budget = StartingBudget;
#if DEBUG
      var multi = 1;
      var newss = new List<KeyValuePair<TimeFrame, decimal>>();

      StartingBudget = 10000;
      StartingBudget *= multi;
      Budget = StartingBudget;
      //MaxBuyPrice = (decimal)0.0005;
      //MinSellPrice = (decimal)8.5;

      MaxAutomaticBudget = 5000;
      AutomaticBudget = 5000;

      PositionSizeMapping = new Dictionary<TimeFrame, decimal>()
      {
        { TimeFrame.M12, 600},
        { TimeFrame.M6, 500},
        { TimeFrame.M3, 400},
        { TimeFrame.M1, 300},
        { TimeFrame.W2, 200},
        { TimeFrame.W1, 100},
      };

      foreach (var data in StrategyData.PositionSizeMapping)
      {
        newss.Add(new KeyValuePair<TimeFrame, decimal>(data.Key, data.Value * multi));
      }

      PositionSizeMapping = newss;
      ScaleSize = 0;
      StrategyPosition = StrategyPosition.Neutral;
      //499
#endif
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
    TimeFrame minTimeframe = TimeFrame.D1;
    public Dictionary<TimeFrame, decimal> PositionWeight { get; } = new Dictionary<TimeFrame, decimal>()
    {
      {TimeFrame.M12, 7},
      {TimeFrame.M6, 6},
      {TimeFrame.M3, 5},
      {TimeFrame.M1, 3},
      {TimeFrame.W2, 2},
      {TimeFrame.W1, 1},
    };

    public List<InnerStrategy> InnerStrategies { get; set; } = new List<InnerStrategy>();



    #region StrategyPosition

    private StrategyPosition strategyPosition = StrategyPosition.Bullish;

    public StrategyPosition StrategyPosition
    {
      get { return strategyPosition; }
      set
      {
        if (value != strategyPosition)
        {
          strategyPosition = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion




    #region StrategyData

    private StrategyData strategyData = new StrategyData()
    {
      MinBuyPrice = (decimal)1,
      PositionSizeMapping = new Dictionary<TimeFrame, decimal>()
      {
        { TimeFrame.M12, 60},
        { TimeFrame.M6, 50},
        { TimeFrame.M3, 40},
        { TimeFrame.M1, 30},
        { TimeFrame.W2, 20},
        { TimeFrame.W1, 10},
      },
      StartingBudget = 1000,
      AutomaticPositionSize = (decimal)0.35,
      ScaleSize = 0
    };

    protected bool wasStrategyDataLoaded;

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
          RaisePropertyChanged(nameof(StrategyViewModel.TotalBuy));
          RaisePropertyChanged(nameof(StrategyViewModel.TotalSell));
          RaisePropertyChanged(nameof(PositionSizeMapping));
          RaisePropertyChanged(nameof(ScaleSize));
          RaisePropertyChanged(nameof(MaxBuyPrice));
          RaisePropertyChanged(nameof(MinSellPrice));
          RaisePropertyChanged(nameof(AutoATHPriceAsMaxBuy));
          RaisePropertyChanged(nameof(AutomaticBudget));
          RaisePropertyChanged(nameof(MaxAutomaticBudget));
          RaisePropertyChanged(nameof(AutomaticPositionSize));
          RaisePropertyChanged(nameof(AutomaticPositionSizeValue));
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

    #region AutomaticBudget

    public decimal AutomaticBudget
    {
      get { return StrategyData.AutomaticBudget; }
      set
      {
        if (value != StrategyData.AutomaticBudget)
        {
          StrategyData.AutomaticBudget = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AutomaticPositionSizeValue

    public decimal AutomaticPositionSizeValue
    {
      get { return GetPositionSize(TimeFrame.W1, positionSide: PositionSide.Neutral) * (decimal)AutomaticPositionSize; }
    }

    #endregion

    #region AutomaticPositionSize

    public decimal AutomaticPositionSize
    {
      get { return StrategyData.AutomaticPositionSize; }
      set
      {
        if (value != StrategyData.AutomaticPositionSize)
        {
          StrategyData.AutomaticPositionSize = value;

          Task.Run(async () =>
          {
            try
            {
              await buyLock.WaitAsync();

              var openedBuy = OpenBuyPositions
                .Where(x => x.IsAutomatic)
                .ToList();


              VSynchronizationContext.InvokeOnDispatcher(async () =>
              {
                foreach (var buyPosition in openedBuy)
                {
                  await OnCancelPosition(buyPosition);
                }
              });
            }
            finally
            {
              buyLock.Release();
            }
          });

          RaisePropertyChanged();
          RaisePropertyChanged(nameof(AutomaticPositionSizeValue));
        }
      }
    }

    #endregion

    #region MaxAutomaticBudget

    public decimal MaxAutomaticBudget
    {
      get { return StrategyData.MaxAutomaticBudget; }
      set
      {
        if (value != StrategyData.MaxAutomaticBudget)
        {
          StrategyData.MaxAutomaticBudget = value;
          RaisePropertyChanged();

          Task.Run(async () =>
          {
            try
            {
              await buyLock.WaitAsync();

              var openedBuy = OpenBuyPositions
                .Where(x => x.IsAutomatic)
                .ToList();
              VSynchronizationContext.InvokeOnDispatcher(async () =>
              {
                foreach (var buyPosition in openedBuy)
                {
                  await OnCancelPosition(buyPosition);
                }
              });
            }
            finally
            {
              buyLock.Release();
            }
          });
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

    #region MaxBuyPrice

    public decimal? MaxBuyPrice
    {
      get
      {
        return StrategyData.MaxBuyPrice;
      }
      set
      {
        if (value != StrategyData.MaxBuyPrice)
        {
          StrategyData.MaxBuyPrice = value;
          RaisePropertyChanged();

          if (wasStrategyDataLoaded)
          {
            SaveStrategyData();
          }
        }
      }
    }

    #endregion

    #region MinSellPrice

    public decimal? MinSellPrice
    {
      get
      {
        return StrategyData.MinSellPrice;
      }
      set
      {
        if (value != StrategyData.MinSellPrice)
        {
          StrategyData.MinSellPrice = value;
          RaisePropertyChanged();

          if (wasStrategyDataLoaded)
          {
            RecreateAllManualSell();
          }
        }
      }
    }

    #endregion

    #region AutoATHPriceAsMaxBuy

    public bool AutoATHPriceAsMaxBuy
    {
      get
      {
        return StrategyData.AutoATHPriceAsMaxBuy;
      }
      set
      {
        if (value != StrategyData.AutoATHPriceAsMaxBuy)
        {
          StrategyData.AutoATHPriceAsMaxBuy = value;
          RaisePropertyChanged();

          if (wasStrategyDataLoaded)
          {
            SaveStrategyData();
          }
        }
      }
    }

    #endregion

    public decimal MaxTotalValue { get; set; }

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
          RaisePropertyChanged(nameof(AutomaticPositionSizeValue));
        }
      }
    }

    #endregion

    #region MinSellProfitMapping

    public Dictionary<TimeFrame, double> MinSellProfitMapping { get; set; } = new Dictionary<TimeFrame, double>()
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

    public Dictionary<TimeFrame, double> MinBuyMapping { get; set; } = new Dictionary<TimeFrame, double>()
    {
      {TimeFrame.M12, 0.005},
      {TimeFrame.M6,  0.005},
      {TimeFrame.M3,  0.005},
      {TimeFrame.M1,  0.005},
      {TimeFrame.W2,  0.005},
      {TimeFrame.W1,  0.005},
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

    public virtual IList<Position> ClosedBuyPositions { get; set; } = new List<Position>();
    public virtual IList<Position> ClosedSellPositions { get; set; } = new List<Position>();

    public virtual IList<Position> OpenSellPositions { get; set; } = new List<Position>();
    public virtual IList<Position> OpenBuyPositions { get; set; } = new List<Position>();

    #region ActualPositions

    private IList<Position> actualPositions = new List<Position>();

    public virtual IList<Position> ActualPositions
    {
      get { return actualPositions; }
      set
      {
        if (value != actualPositions)
        {
          actualPositions = value;
          RaisePropertyChanged();
        }
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

    #region ActualPositionProfit

    private decimal totalActualProfit;

    public decimal TotalActualProfit
    {
      get { return totalActualProfit; }
      set
      {
        if (value != totalActualProfit)
        {
          totalActualProfit = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    public bool DisableOnBuy { get; set; }

    public bool EnableManualPositions { get; set; } = true;

    public bool EnableStopLoss { get; set; }

    #region Methods

    #region GetMaxBuy

    private decimal GetMaxBuy(decimal low, TimeFrame timeFrame)
    {
      var max = low * (decimal)(1 - MinBuyMapping[timeFrame]);

      return max;
    }

    #endregion

    #region CreatePositions

    Candle lastCandle = null;

    public async void CreatePositions(Candle actualCandle)
    {
      try
      {
        await buyLock.WaitAsync();
        lastCandle = actualCandle;
        decimal lastSell = decimal.MaxValue;



        if (ClosedSellPositions.Any() && ActualPositions.Any())
        {
          lastSell = ClosedSellPositions.Last().Price * (decimal)(1 - MinBuyMapping[TimeFrame.W1]);
        }

#if DEBUG
        foreach (var innerStrategy in InnerStrategies)
        {
          lastSell = innerStrategy.Calculate(actualCandle);
        }
#endif

        var minBuy = actualCandle.Close * (1 - MinBuyPrice);
        decimal maxBuy = MaxBuyPrice ?? decimal.MaxValue;

        if (MaxBuyPrice == null && lastSell < decimal.MaxValue)
        {
          maxBuy = lastSell;
        }

        var openedBuy = OpenBuyPositions
          .Where(x => !x.IsAutomatic)
          .Where(x => x.Price != x.Intersection.Value ||
                      x.Price < minBuy ||
                      x.Price > maxBuy)
          .ToList();

        foreach (var buyPosition in openedBuy)
        {
          await OnCancelPosition(buyPosition);
        }

        if (StrategyData.MaxAutomaticBudget > 0)
        {
          var automaticOpenedBuy = OpenBuyPositions
            .Where(x => x.IsAutomatic)
            .Where(x => x.Price != x.Intersection.Value ||
                        x.Price < minBuy)
            .ToList();


          foreach (var buyPosition in automaticOpenedBuy)
          {
            await OnCancelPosition(buyPosition);
          }
        }

        if (EnableStopLoss)
          await CalculateActualProfits(actualCandle);
        else
        {
          var stopped = OpenBuyPositions.Where(x => x.StopLoss).ToList();
          stopped.ForEach(x => x.StopLoss = false);

          await RecreateSellPositions(actualCandle, stopped.SelectMany(x => x.OpositPositions.Where(y => y.State == PositionState.Open)));
        }

        var openedSell = OpenSellPositions
          .Where(x => !x.IsAutomatic)
          .Where(x => !x.OpositPositions[0].StopLoss)
          .Where(x => x.Price != x.Intersection.Value || x.Price < MinSellPrice)
          .ToList();

        if (openedSell.Any())
          await RecreateSellPositions(actualCandle, openedSell);


        if (StrategyData.MaxAutomaticBudget > 0)
        {
          var openedAutoSell = OpenSellPositions
          .Where(x => x.IsAutomatic)
          .Where(x => x.Price != x.Intersection.Value)
          .Where(x => !x.OpositPositions[0].StopLoss)
          .ToList();

          if (openedAutoSell.Any())
            await RecreateSellPositions(actualCandle, openedAutoSell);
        }


        var inter = Intersections
                    .Where(x => x.TimeFrame >= minTimeframe)
                    .Where(x => x.IsEnabled)
                    .Where(x => x.Value < actualCandle.Close.Value &&
                                x.Value > minBuy &&
                                x.Value < lastSell &&
                                x.Value < GetMaxBuy(actualCandle.Close.Value, x.TimeFrame))
                      .OrderByDescending(x => x.Value)
                    .ToList();


        var nonAutomaticIntersections = inter.Where(x => x.Value < maxBuy);

        if (EnableManualPositions)
        {
          foreach (var intersection in nonAutomaticIntersections)
          {
            await CreateBuyPositionFromIntersection(intersection);
          }
        }


        if (StrategyData.MaxAutomaticBudget > 0)
        {
          var autoIntersections = inter;

          foreach (var intersection in autoIntersections)
          {
            await CreateBuyPositionFromIntersection(intersection, true);
          }
        }


#if RELEASE
        var newTotalNativeAsset = TotalNativeAsset;

        var sum = OpenSellPositions
          .Sum(x => x.OriginalPositionSizeNative);


        if (Math.Round(sum, Asset.NativeRound) != Math.Round(newTotalNativeAsset, Asset.NativeRound))
        {
          var missingSell = ClosedBuyPositions.Where(x => x.OpositPositions.Count == 0 && x.State == PositionState.Filled);

          foreach (var missing in missingSell)
          {
            LeftSize = missing.OriginalPositionSize;
            Logger.Log(MessageType.Warning, $"Recreating failed sell position for buy {missing.ShortId}", true);
            await CreateSellPositionForBuy(missing, Intersections.OrderBy(x => x.Value)
              .Where(x => x.Value > actualCandle.Close.Value));
          }

        }

#endif

        RaisePropertyChanged(nameof(StrategyViewModel.TotalExpectedProfit));
      }
      finally
      {
        buyLock.Release();
      }
    }

    #endregion

    #region CreatePositionFromIntersection

    private async Task CreateBuyPositionFromIntersection(CtksIntersection intersection, bool automatic = false)
    {
      var positionsOnIntersesction =
        AllOpenedPositions
        .Where(x => x.IsAutomatic == automatic)
        .Where(x => x.Intersection.IsSame(intersection) &&
                    x.Intersection.TimeFrame == intersection.TimeFrame)
        .ToList();

      var maxPOsitionOnIntersection = GetPositionSize(intersection.TimeFrame, automatic, PositionSide.Buy);

      var sum = positionsOnIntersesction.Sum(x => x.PositionSize);

      var existing =
        ActualPositions
          .Where(x => x.IsAutomatic == automatic)
        .Where(x => x.Intersection.IsSame(intersection) &&
                    x.Intersection.TimeFrame == intersection.TimeFrame)
        .Sum(x => x.OpositPositions.Sum(y => y.PositionSize));

      var leftSize = maxPOsitionOnIntersection - sum;

      if (existing > 0)
      {
        leftSize = leftSize - existing;
      }

      if (leftSize > MinPositionValue)
      {
        var validPositions =
          OpenBuyPositions
            .Where(x => x.IsAutomatic == automatic)
            .Where(x => intersection.Value > x.Price)
            .OrderByDescending(x => x.Price)
            .ToList();

        var openAuto = OpenBuyPositions.Where(x => x.IsAutomatic).Sum(x => x.PositionSize);
        var filledAuto = ActualPositions.Where(x => x.IsAutomatic).Sum(x => x.OpositPositions.Sum(y => y.PositionSize));

        var automaticSize = openAuto + filledAuto;
        var automaticBudget = MaxAutomaticBudget - automaticSize;

        AutomaticBudget = automaticBudget;

        if (AutomaticBudget > Budget && automatic)
        {
          validPositions.AddRange(
            OpenBuyPositions
              .Where(x => !x.IsAutomatic)
              .Where(x => intersection.Value > x.Price)
              .OrderByDescending(x => x.Price)
              .ToList());
        }

        var stack = new Stack<Position>(validPositions);
        var openBuy = stack.Sum(x => x.PositionSize);


        while ((GetBudget(automatic) < leftSize && GetBudget(automatic) + openBuy > leftSize))
        {
          var openLow = stack.Pop();

          if (openLow != null &&
              intersection.Value > openLow.Price &&
              !openLow.Intersection.IsSame(intersection))
          {
            Logger?.Log(MessageType.Warning, $"Cancelling buyPosition {openLow.Intersection.Value} in order to create another {intersection.Value}", simpleMessage: true);

            var result = await OnCancelPosition(openLow);

            if (automatic && result)
            {
              AutomaticBudget += openLow.OriginalPositionSize;
            }
          }
          else
          {
            break;
          }
        }

        if (GetBudget(automatic) > leftSize)
        {
          await CreateBuyPosition(leftSize, intersection, automatic);
        }
      }
    }

    #endregion

    #region RecreateAllManualSell

    private async void RecreateAllManualSell()
    {
      var openedSell = OpenSellPositions
        .Where(x => !x.IsAutomatic).ToList();

      await RecreateSellPositions(lastCandle, openedSell);
    }

    #endregion

    #region GetBudget

    private decimal GetBudget(bool automatic = false)
    {
      return automatic ? Math.Min(AutomaticBudget, Budget) : Budget;
    }

    #endregion

    #region RecreateSellPositions

    public async Task RecreateSellPositions(Candle actualCandle, IEnumerable<Position> positionsToCancel)
    {
      var removedBu = new HashSet<Position>();

      var sellPositions = positionsToCancel
        .Where(x => x.OpositPositions.Count > 0)
        .OrderByDescending(x => x.OpositPositions[0].Price).ToList();

      var sumf = OpenSellPositions
        .Sum(x => x.OriginalPositionSizeNative);

      var asdd = Math.Round(sumf, 0);

      foreach (var sellPosition in sellPositions)
      {
        await OnCancelPosition(sellPosition, removedBu);
      }


      foreach (var opened in removedBu)
      {
        await CreateSellPositionForBuy(opened,
          Intersections.OrderBy(x => x.Value)
          .Where(x => x.Value > actualCandle.Close.Value));
      }
    }

    #endregion

    #region GetPositionSize

    private decimal GetPositionSize(TimeFrame timeFrame, bool automatic = false, PositionSide positionSide = PositionSide.Neutral)
    {
      decimal positionSize = 0;

      if (automatic)
      {
        positionSize = AutomaticPositionSizeValue;
      }
      else
      {
        positionSize = PositionSizeMapping.Single(x => x.Key == timeFrame).Value;

        if (positionSide == PositionSide.Buy && StrategyPosition == StrategyPosition.Bearish)
        {
          positionSize = PositionSizeMapping.Single(x => x.Key == TimeFrame.W1).Value;
        }
        else if (positionSide == PositionSide.Sell && StrategyPosition == StrategyPosition.Bullish)
        {
          positionSize = PositionSizeMapping.Single(x => x.Key == TimeFrame.W1).Value;
        }
      }


      return positionSize;
    }

    #endregion

    #region ValidatePositions

    public virtual void ValidatePositions(Candle candle)
    {
      var openedBuy = OpenBuyPositions.ToList();

      var assetsValue = TotalNativeAsset * candle.Close.Value;
      var openPositions = openedBuy.Sum(x => x.PositionSize);

      TotalValue = assetsValue + openPositions + Budget;
      TotalNativeAssetValue = TotalNativeAsset * candle.Close.Value;

      if (MaxTotalValue < TotalValue)
      {
        MaxTotalValue = TotalValue;
      }

      RaisePropertyChanged(nameof(StrategyViewModel.AvrageBuyPrice));
      RaisePropertyChanged(nameof(AllClosedPositions));
    }

    #endregion

    #region CreateSellPositionForBuy

    protected async Task CreateSellPositionForBuy(Position position, IEnumerable<CtksIntersection> ctksIntersections, decimal minForcePrice = 0)
    {
      if (position.PositionSize > 0)
      {
        await CreateSell(position, ctksIntersections.ToList(), minForcePrice);

        var sumOposite = position.OpositPositions.Sum(x => x.OriginalPositionSizeNative);
        if (sumOposite != position.OriginalPositionSizeNative)
        {
          throw new Exception("Postion asset value does not mach sell order !!");
        }
      }
    }

    #endregion

    #region CloseBuy

    public async Task CloseBuy(Position position, decimal minForcePrice = 0)
    {
      try
      {
        await buyLock.WaitAsync();

        if (position.State != PositionState.Filled)
        {
          var ordered = Intersections.OrderBy(x => x.Value).ToList();

          //var inter = Intersections.SingleOrDefault(x => x.IsSame(buyPosition.Intersection));

          //if (inter != null)
          //  inter.IsEnabled = false;

          TotalNativeAsset += position.PositionSizeNative;
          LeftSize += position.PositionSizeNative;

          await CreateSellPositionForBuy(position, ordered, minForcePrice);

          position.State = PositionState.Filled;

          ClosedBuyPositions.Add(position);
          OpenBuyPositions.Remove(position);
          Budget -= position.Fees ?? 0;

          if (position.IsAutomatic)
            AutomaticBudget -= position.Fees ?? 0;

          RaisePropertyChanged(nameof(StrategyViewModel.TotalBuy));

          if (DisableOnBuy)
          {
            position.Intersection.IsEnabled = false;
          }

          ActualPositions.Add(position);
          RaisePropertyChanged(nameof(AllCompletedPositions));
          RaisePropertyChanged(nameof(StrategyViewModel.TotalExpectedProfit));
          SaveState();
        }
      }
      finally
      {
        buyLock.Release();
      }
    }

    #endregion

    #region CloseSell

    protected async void CloseSell(Position position)
    {
      try
      {
        await sellLock.WaitAsync();

        var newTotalNativeAsset = TotalNativeAsset - position.OriginalPositionSizeNative;

        var sum = OpenSellPositions
          .Where(x => x.Id != position.Id)
          .Sum(x => x.OriginalPositionSizeNative);


        if (Math.Round(sum, Asset.NativeRound) != Math.Round(newTotalNativeAsset, Asset.NativeRound))
        {
          HandleError($"Native asset value does not mach sell order !! " +
                      $"{Math.Round(sum, Asset.NativeRound)} != {Math.Round(newTotalNativeAsset, Asset.NativeRound)},\n" +
                      $"position: {position.ShortId}, {position.Price},{position.OriginalPositionSizeNative}\n" +
                      $"Left size: {LeftSize}");

        }


        var finalSize = position.Price * position.OriginalPositionSizeNative;
        var profit = finalSize - position.OriginalPositionSize; ;
        if (position.OpositPositions[0].StopLoss)
        {
          profit = finalSize - position.OpositPositions[0].OriginalPositionSize;
        }

        position.Profit = profit;
        position.PositionSize = 0;
        position.PositionSizeNative = 0;

        TotalProfit += position.Profit;
        Budget += finalSize;
        //Dont put sum, this should be real value if there is an error, find it
        TotalNativeAsset = newTotalNativeAsset;

        Budget -= position.Fees ?? 0;

        if (position.IsAutomatic)
        {
          AutomaticBudget += finalSize;
          AutomaticBudget -= position.Fees ?? 0;
        }

        Scale(position.Profit);
        position.State = PositionState.Filled;

        ClosedSellPositions.Add(position);
        OpenSellPositions.Remove(position);

        if (position.OpositPositions.Count > 0)
        {
          var originalBuy = position.OpositPositions.Single();

          originalBuy.RaiseNotify(nameof(Position.TotalProfit));
          originalBuy.RaiseNotify(nameof(Position.TotalFees));
          originalBuy.RaiseNotify(nameof(Position.FinalProfit));
          originalBuy.RaiseNotify(nameof(Position.ExpectedProfit));

          if (originalBuy.OpositPositions.Sum(x => x.PositionSize) == 0)
          {
            originalBuy.State = PositionState.Completed;

            ActualPositions.Remove(originalBuy);

            RaisePropertyChanged(nameof(AllCompletedPositions));
            RaisePropertyChanged(nameof(StrategyViewModel.TotalExpectedProfit));
          }
        }

        RaisePropertyChanged(nameof(StrategyViewModel.TotalSell));
        SaveState();
      }
      finally
      {
        sellLock.Release();
      }

    }

    #endregion

    #region CreateSell

    private async Task CreateSell(Position buyPosition, IList<CtksIntersection> ctksIntersections, decimal minForcePrice = 0)
    {
      try
      {
        await sellLock.WaitAsync();

        var minPrice = Math.Max(buyPosition.Price * (decimal)(1.0 + MinSellProfitMapping[buyPosition.TimeFrame]), buyPosition.IsAutomatic ? 0 : MinSellPrice ?? 0);

        IList<CtksIntersection> nextLines = null;

        if (buyPosition.StopLoss)
        {
          nextLines = ctksIntersections
            .Where(x => x.TimeFrame >= minTimeframe)
            .OrderBy(x => x.Value)
            .ToList();
        }
        else
        {
          nextLines = ctksIntersections
            .Where(x => x.TimeFrame >= minTimeframe)
            .Where(x => x.Value > minPrice && x.Value > minForcePrice)
            .OrderBy(x => x.Value)
            .ToList();

          if (nextLines.Count == 0)
          {
            nextLines = ctksIntersections
              .Where(x => x.Value > minPrice && x.Value > minForcePrice)
              .OrderBy(x => x.Value)
              .ToList();
          }
        }

        int i = 0;
        List<Position> createdPositions = new List<Position>();

        while (buyPosition.PositionSize > 0 && nextLines.Count > 0)
        {
          foreach (var nextLine in nextLines)
          {
            var leftPositionSize = buyPosition.PositionSize;

            if (leftPositionSize < 0)
            {
              buyPosition.PositionSize = 0;
            }

            var ctksIntersection = nextLine;
            var forcePositionSize = i > 0 || nextLine.Value > buyPosition.Price * (decimal)1.5;


            var positionSize = buyPosition.PositionSize;

            var maxPOsitionOnIntersection = (decimal)GetPositionSize(ctksIntersection.TimeFrame, buyPosition.IsAutomatic, PositionSide.Sell);

            var positionsOnIntersesction = OpenSellPositions
              .Where(x => x.Intersection.IsSame(ctksIntersection))
              .Sum(x => x.PositionSize);

            leftPositionSize = (decimal)maxPOsitionOnIntersection - positionsOnIntersesction;


            if (!forcePositionSize && MinPositionValue > leftPositionSize)
            {
              continue;
            }

            if (buyPosition.PositionSize > leftPositionSize)
            {
              positionSize = leftPositionSize;
            }

            if (leftPositionSize <= 0 && !forcePositionSize)
              continue;

            if (forcePositionSize)
            {
              if (buyPosition.PositionSize > maxPOsitionOnIntersection)
                positionSize = maxPOsitionOnIntersection;
              else
                positionSize = buyPosition.PositionSize;
            }

            decimal roundedNativeSize = 0;

            if (buyPosition.StopLoss)
            {
              var leftNative = buyPosition.OriginalPositionSizeNative - buyPosition.OpositPositions.Where(x => x.State == PositionState.Filled).Sum(x => x.OriginalPositionSizeNative);
              positionSize = leftNative * nextLine.Value;
              roundedNativeSize = leftNative;
            }
            else
            {
              roundedNativeSize = Math.Round(positionSize / buyPosition.Price, Asset.NativeRound);

              if (buyPosition.PositionSizeNative < roundedNativeSize)
              {
                roundedNativeSize = buyPosition.PositionSizeNative;
              }

              if (buyPosition.PositionSize - positionSize == 0 && buyPosition.PositionSizeNative - roundedNativeSize > 0)
              {
                roundedNativeSize = buyPosition.PositionSizeNative;
              }

              positionSize = buyPosition.Price * roundedNativeSize;
              if (positionSize <= 0)
              {
                continue;
              }

              var leftSize = buyPosition.PositionSize - positionSize;

              if (MinPositionValue > leftSize && leftSize > 0)
              {
                roundedNativeSize = roundedNativeSize + (buyPosition.PositionSizeNative - roundedNativeSize);

              }

              positionSize = buyPosition.Price * roundedNativeSize;
              if (positionSize <= 0)
              {
                continue;
              }
            }

            var newPosition = new Position(positionSize, ctksIntersection.Value, roundedNativeSize)
            {
              Side = PositionSide.Sell,
              TimeFrame = ctksIntersection.TimeFrame,
              Intersection = ctksIntersection,
              State = PositionState.Open,
              IsAutomatic = buyPosition.IsAutomatic
            };

            newPosition.OpositPositions.Add(buyPosition);

            buyPosition.PositionSize -= positionSize;
            buyPosition.PositionSizeNative -= roundedNativeSize;
            buyPosition.OpositPositions.Add(newPosition);


            createdPositions.Add(newPosition);

            if (buyPosition.StopLoss)
            {
              buyPosition.PositionSize = 0;
            }

            if (buyPosition.PositionSize <= 0)
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
              {
                HandleError("Left native size is less than 0 !!");
              }
            }
            else
            {
              HandleError("Sell order was not created!, trying again");

              await Task.Delay(1000);
            }
          }
        }

        SaveState();
      }
      finally
      {
        sellLock.Release();
      }
    }

    #endregion

    #region CreateBuyPosition

    private async Task CreateBuyPosition(decimal positionSize, CtksIntersection intersection, bool automatic = false)
    {
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
        IsAutomatic = automatic
      };

      var id = await CreatePosition(newPosition);

      if (id > 0)
      {
        newPosition.Id = id;

        Budget -= newPosition.PositionSize;

        if (automatic)
        {
          AutomaticBudget -= newPosition.PositionSize;
        }

        OpenBuyPositions.Add(newPosition);

        onCreatePositionSub.OnNext(newPosition);
      }

      SaveState();
    }

    #endregion

    #region OnCancelPosition

    public async Task<bool> OnCancelPosition(Position position, HashSet<Position> removed = null, bool force = false)
    {
      var cancled = await CancelPosition(position);

      if (cancled || force)
      {
        if (position.Side == PositionSide.Buy)
        {
          OpenBuyPositions.Remove(position);
          Budget += position.PositionSize;

          if (position.IsAutomatic)
          {
            AutomaticBudget += position.PositionSize;
          }

          position.RaiseNotify(nameof(Position.ExpectedProfit));
        }
        else
        {
          if (position.OpositPositions.Count > 0)
          {
            var buy = position.OpositPositions[0];
            buy.OpositPositions.Remove(position);
            buy.PositionSize += position.OriginalPositionSize;
            buy.PositionSizeNative += position.OriginalPositionSizeNative;

            if (removed != null)
              removed.Add(buy);
          }

          LeftSize += position.OriginalPositionSizeNative;

          OpenSellPositions.Remove(position);
        }
      }

      SaveState();

      return cancled;
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

    #region UpdateIntersections

    public void UpdateIntersections(List<CtksIntersection> ctksIntersections)
    {
      foreach (var postion in AllOpenedPositions)
      {
        var inter = postion.Intersection;
        var found = ctksIntersections.SingleOrDefault(y => y.IsSame(inter));

        if (found != null)
        {
          found.IsEnabled = inter.IsEnabled;

          postion.Intersection = found;
        }
        else
          postion.Intersection = new CtksIntersection();
      }


      foreach (var postion in ActualPositions)
      {
        var inter = postion.Intersection;
        var found = ctksIntersections.SingleOrDefault(y => y.IsSame(inter));

        if (found != null)
        {
          found.IsEnabled = inter.IsEnabled;
          postion.Intersection = found;
        }
        else
          postion.Intersection = new CtksIntersection();
      }

    }

    #endregion

    private async Task CalculateActualProfits(Candle actual)
    {
      foreach (var position in ActualPositions)
      {
        var filledSells = position.OpositPositions.Where(x => x.State == PositionState.Filled).ToList();

        var realizedProfit = filledSells.Sum(x => x.OriginalPositionSize + x.Profit);
        var leftSize = position.OpositPositions.Where(x => x.State == PositionState.Open).Sum(x => x.PositionSizeNative);
        var fees = position.Fees ?? 0 + filledSells.Sum(x => x.Fees ?? 0);

        var profit = (realizedProfit + (leftSize * actual.Close.Value)) - position.OriginalPositionSize - fees;
        position.ActualProfit = profit;
        var perc = profit * 100 / position.OriginalPositionSize;

        if (perc < (decimal)-55 && !position.StopLoss)
        {
          position.StopLoss = true;
          await RecreateSellPositions(actual, position.OpositPositions.Where(x => x.State == PositionState.Open));
        }
      }


    }

    private void HandleError(string message)
    {
#if RELEASE
      Logger.Log(MessageType.Error, message);
#else
      throw new Exception(message);
#endif
    }

    protected abstract Task<bool> CancelPosition(Position position);
    protected abstract Task<long> CreatePosition(Position position);
    public abstract void SaveState();
    public abstract void SaveStrategyData();
    public abstract void LoadState();
    public abstract Task RefreshState();
    public abstract bool IsPositionFilled(Candle candle, Position position);

    #region Reset

    public async Task Reset(Candle actualCandle)
    {

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
        {
          await CreateSellPositionForBuy(opened, Intersections.OrderBy(x => x.Value).Where(x => x.Value > actualCandle.Close * (decimal)1.01));
        }
        else
          ClosedBuyPositions.Remove(opened);
      }


      SaveState();

    }

    #endregion

    #endregion
  }
}