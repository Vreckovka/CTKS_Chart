using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

  public abstract class BaseStrategy<TPosition> : ViewModel where TPosition : Position, new()
  {
    protected decimal LeftSize = 0;
    protected SemaphoreSlim buyLock = new SemaphoreSlim(1, 1);

    public BaseStrategy()
    {
      Budget = StartingBudget;

#if RELEASE
   Budget = 0;
#endif

#if DEBUG
      var multi = 1;
      var newss = new List<KeyValuePair<TimeFrame, decimal>>();

      StartingBudget = 1000;
      StartingBudget *= multi;
      Budget = StartingBudget;

      StrategyData.MaxAutomaticBudget = StartingBudget * (decimal)0.35;
      StrategyData.AutomaticBudget = StartingBudget * (decimal)0.35;

      PositionSizeMapping = new Dictionary<TimeFrame, decimal>()
      {
        { TimeFrame.M12, 70},
        { TimeFrame.M6, 60},
        { TimeFrame.M3, 50},
        { TimeFrame.M1, 40},
        { TimeFrame.W2, 30},
        { TimeFrame.W1, 20},
      };

      foreach (var data in StrategyData.PositionSizeMapping)
      {
        newss.Add(new KeyValuePair<TimeFrame, decimal>(data.Key, data.Value * multi));
      }

      PositionSizeMapping = newss;
      ScaleSize = 0;
      StrategyPosition = StrategyPosition.Neutral;
#endif
    }

    #region Properties

    protected Subject<TPosition> onCreatePositionSub = new Subject<TPosition>();
    public IObservable<TPosition> OnCreatePosition
    {
      get
      {
        return onCreatePositionSub.AsObservable();
      }
    }

    public Asset Asset { get; set; }
    public decimal MinPositionValue { get; set; } = 6;
    public ILogger Logger { get; set; }

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

    private StrategyPosition strategyPosition = StrategyPosition.Neutral;

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
          RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.TotalBuy));
          RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.TotalFees));
          RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.TotalSell));
          RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.Turnover));
          RaisePropertyChanged(nameof(PositionSizeMapping));
          RaisePropertyChanged(nameof(ScaleSize));
          RaisePropertyChanged(nameof(MaxBuyPrice));
          RaisePropertyChanged(nameof(MinSellPrice));
          RaisePropertyChanged(nameof(AutomaticBudget));
          RaisePropertyChanged(nameof(MaxAutomaticBudget));
          RaisePropertyChanged(nameof(AutomaticPositionSize));
          RaisePropertyChanged(nameof(AutomaticPositionSizeValue));
          RaisePropertyChanged(nameof(EnableManualPositions));
          RaisePropertyChanged(nameof(EnableAutoPositions));
          RaisePropertyChanged(nameof(EnableRangeFilterStrategy));
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
      set
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
      get { return Math.Max(MinPositionValue, PositionSizeMapping.Single(x => x.Key == TimeFrame.W1).Value * (decimal)AutomaticPositionSize); }
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
                  await CancelPosition(buyPosition);
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
                  await CancelPosition(buyPosition);
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

    public decimal MaxTotalValue { get; set; }

    #region DrawdawnFromMaxTotalValue

    private decimal drawdawnFromMaxTotalValue;

    public decimal DrawdawnFromMaxTotalValue
    {
      get { return drawdawnFromMaxTotalValue; }
      set
      {
        if (value != drawdawnFromMaxTotalValue)
        {
          drawdawnFromMaxTotalValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MaxDrawdawnFromMaxTotalValue

    private decimal maxDrawdawnFromMaxTotalValue;

    public decimal MaxDrawdawnFromMaxTotalValue
    {
      get { return maxDrawdawnFromMaxTotalValue; }
      set
      {
        if (value != maxDrawdawnFromMaxTotalValue)
        {
          maxDrawdawnFromMaxTotalValue = value;
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

    #region BasePositionSizeMapping

    IEnumerable<KeyValuePair<TimeFrame, decimal>> basePositionSizeMapping;
    public IEnumerable<KeyValuePair<TimeFrame, decimal>> BasePositionSizeMapping
    {
      get { return basePositionSizeMapping; }
      set
      {
        if (value != basePositionSizeMapping)
        {
          basePositionSizeMapping = value;
          RaisePropertyChanged();
          RaisePropertyChanged(nameof(AutomaticPositionSizeValue));
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

    public Dictionary<TimeFrame, decimal> MinSellProfitMapping { get; set; } = new Dictionary<TimeFrame, decimal>()
    {
      {TimeFrame.M12, 0.005m},
      {TimeFrame.M6,  0.005m},
      {TimeFrame.M3,  0.005m},
      {TimeFrame.M1,  0.005m},
      {TimeFrame.W2,  0.005m},
      {TimeFrame.W1,  0.005m},
      {TimeFrame.D1,  0.005m},
    };

    #endregion

    #region MinBuyMapping

    public Dictionary<TimeFrame, decimal> MinBuyMapping { get; set; } = new Dictionary<TimeFrame, decimal>()
    {
      {TimeFrame.M12, 0.005m},
      {TimeFrame.M6,  0.005m},
      {TimeFrame.M3,  0.005m},
      {TimeFrame.M1,  0.005m},
      {TimeFrame.W2,  0.005m},
      {TimeFrame.W1,  0.005m},
      {TimeFrame.D1,  0.005m},
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

          RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.AbosoluteGain));
          RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.AbosoluteGainValue));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Positions

    public virtual ObservableCollection<TPosition> ClosedBuyPositions { get; set; } = new ObservableCollection<TPosition>();
    public virtual ObservableCollection<TPosition> ClosedSellPositions { get; set; } = new ObservableCollection<TPosition>();

    public virtual ObservableCollection<TPosition> OpenSellPositions { get; set; } = new ObservableCollection<TPosition>();
    public virtual ObservableCollection<TPosition> OpenBuyPositions { get; set; } = new ObservableCollection<TPosition>();

    #region ActualPositions

    private IList<TPosition> actualPositions = new List<TPosition>();

    public virtual IList<TPosition> ActualPositions
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

    public IEnumerable<TPosition> AllCompletedPositions
    {
      get
      {
        return ClosedBuyPositions.Where(x => x.State == PositionState.Completed);
      }
    }

    #endregion

    #region AllClosedPositions

    public IEnumerable<TPosition> AllClosedPositions
    {
      get
      {
        return ClosedBuyPositions.Concat(ClosedSellPositions);
      }
    }

    #endregion

    #region AllOpenedPositions

    public IEnumerable<TPosition> AllOpenedPositions
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

    #region EnableManualPositions

    public bool EnableManualPositions
    {
      get { return StrategyData.EnableManualPositions; }
      set
      {
        if (value != StrategyData.EnableManualPositions)
        {
          StrategyData.EnableManualPositions = value;


          if (wasStrategyDataLoaded)
          {
            SaveStrategyData();
          }

          RaisePropertyChanged();

          if (!StrategyData.EnableManualPositions)
          {
            VSynchronizationContext.InvokeOnDispatcher(async () =>
            {
              var manualPositions = OpenBuyPositions.Where(x => !x.IsAutomatic).ToList();

              foreach (var TPosition in manualPositions)
              {
                await CancelPosition(TPosition);
              }
            });

            AutomaticBudget = Budget;
            MaxAutomaticBudget = Budget;
          }
        }
      }
    }

    #endregion

    #region EnableAutoPositions

    public bool EnableAutoPositions
    {
      get { return StrategyData.EnableAutoPositions; }
      set
      {
        if (value != StrategyData.EnableAutoPositions)
        {
          StrategyData.EnableAutoPositions = value;

          if (wasStrategyDataLoaded)
          {
            SaveStrategyData();
          }

          RaisePropertyChanged();

          if (!StrategyData.EnableAutoPositions)
          {
            VSynchronizationContext.InvokeOnDispatcher(async () =>
            {
              try
              {
                await buyLock.WaitAsync();

                var manualPositions = OpenBuyPositions.Where(x => x.IsAutomatic).ToList();

                foreach (var TPosition in manualPositions)
                {
                  await CancelPosition(TPosition);
                }
              }
              finally
              {
                buyLock.Release();
              }
            });
          }
        }
      }
    }

    #endregion

    #region EnableRangeFilterStrategy

    public bool EnableRangeFilterStrategy
    {
      get { return StrategyData.EnableRangeFilterStrategy; }
      set
      {
        if (value != StrategyData.EnableRangeFilterStrategy)
        {
          StrategyData.EnableRangeFilterStrategy = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Methods

    #region GetMaxBuy

    private decimal GetMaxBuy(decimal low, TimeFrame timeFrame)
    {
      var max = low * (decimal)(1 - MinBuyMapping[timeFrame]);

      return max;
    }

    #endregion

    #region CreatePositions

    public Candle lastCandle = null;
    public IList<Candle> indicatorsCandles = new List<Candle>();

    public virtual async Task CreatePositions(Candle actualCandle, IList<Candle> indicatorCandles)
    {
      try
      {
        await buyLock.WaitAsync();

        var dailyCandle = indicatorCandles.FirstOrDefault();

        this.indicatorsCandles = indicatorCandles;
        lastCandle = actualCandle;

        var limits = GetMaxAndMinBuy(actualCandle, dailyCandle);

        var lastSell = limits.Item1;
        var minBuy = limits.Item2;
        var maxBuy = limits.Item3;

        await CheckPositions(actualCandle, minBuy, maxBuy);
        var validIntersections = Intersections;

      

        if (EnableRangeFilterStrategy)
        {
          if (dailyCandle != null)
          {
            MaxBuyPrice = dailyCandle.IndicatorData.RangeFilter.RangeFilter;
            maxBuy = dailyCandle.IndicatorData.RangeFilter.RangeFilter;

            StrategyPosition = dailyCandle.IndicatorData.RangeFilter.Upward ? StrategyPosition.Bullish : StrategyPosition.Bearish;
          }
        }

#if DEBUG
        foreach (var innerStrategy in InnerStrategies)
        {
          //validIntersections = innerStrategy.Calculate(actualCandle, dailyCandle, PositionSide.Buy).ToList();
        }
#endif

        //Intersections are already ordered by value
        var inter = validIntersections
                    .Where(x => x.IsEnabled)
                    .Where(x => x.Value < actualCandle.Close.Value &&
                                x.Value > minBuy &&
                                x.Value < lastSell)
                    .ToList();

        var nonAutomaticIntersections = inter.Where(x => x.Value < maxBuy &&
                                                         x.Value < GetMaxBuy(actualCandle.Close.Value, x.TimeFrame));

        if (EnableManualPositions)
        {
          foreach (var intersection in nonAutomaticIntersections)
          {
            await CreateBuyPositionFromIntersection(intersection);
          }
        }

        if (StrategyData.MaxAutomaticBudget > 0 && EnableAutoPositions)
        {
          var autoIntersections = inter.Where(
                                   x => x.Value < lastSell * 0.995m &&
                                   x.Value < actualCandle.Close.Value * 0.995m);

          if (dailyCandle != null)
          {
            if (!dailyCandle.IndicatorData.RangeFilter.Upward)
              autoIntersections = autoIntersections.Where(x => x.Value < dailyCandle.Open.Value);

            autoIntersections = autoIntersections.Where(x => x.Value < dailyCandle.IndicatorData.RangeFilter.HighTarget);
          }

          foreach (var intersection in autoIntersections)
          {
            await CreateBuyPositionFromIntersection(intersection, true);
          }
        }


        RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.TotalExpectedProfit));
      }
      finally
      {
        buyLock.Release();
      }
    }

    #endregion

    #region CheckPositions

    public virtual async Task CheckPositions(Candle actualCandle, decimal minBuy, decimal maxBuy)
    {
      var openedBuy = OpenBuyPositions
        .Where(x => !x.IsAutomatic)
        .Where(x => x.Price != x.Intersection.Value ||
                    x.Intersection.IsEnabled == false ||
                    x.Price < minBuy ||
                    x.Price > maxBuy)
        .ToList();

      foreach (var buyPosition in openedBuy)
      {
        await CancelPosition(buyPosition);
      }

      if (StrategyData.MaxAutomaticBudget > 0)
      {
        var automaticOpenedBuy = OpenBuyPositions
          .Where(x => x.IsAutomatic)
          .Where(x => x.Price != x.Intersection.Value ||
                      x.Intersection.IsEnabled == false ||
                      x.Price < minBuy)
          .ToList();


        foreach (var buyPosition in automaticOpenedBuy)
        {
          await CancelPosition(buyPosition);
        }
      }

      var openedSell = OpenSellPositions
        .Where(x => !x.IsAutomatic)
        .Where(x => x.Price != x.Intersection.Value ||
                   x.Intersection.IsEnabled == false ||
                    x.Price < MinSellPrice)
        .ToList();

      if (openedSell.Any())
        await RecreateSellPositions(actualCandle, openedSell);


      if (StrategyData.MaxAutomaticBudget > 0)
      {
        var openedAutoSell = OpenSellPositions
        .Where(x => x.IsAutomatic)
        .Where(x => x.Price != x.Intersection.Value
        || x.Intersection.IsEnabled == false
        )
        .ToList();

        if (openedAutoSell.Any())
          await RecreateSellPositions(actualCandle, openedAutoSell);
      }
    }

    #endregion

    #region GetMaxAndMinBuy

    protected Tuple<decimal, decimal, decimal> GetMaxAndMinBuy(Candle actualCandle, Candle actualDailyCandle)
    {
      decimal lastSell = decimal.MaxValue;

      if (ClosedSellPositions.Any() && ActualPositions.Any())
      {
        lastSell = ClosedSellPositions.Last().Price;
      }
      else
      {
        var data = actualDailyCandle;

        if (data != null)
        {
          lastSell = data.IndicatorData.RangeFilter.HighTarget;
        }
      }

      var minBuy = actualCandle.Close.Value * (1 - MinBuyPrice);
      decimal maxBuy = MaxBuyPrice ?? decimal.MaxValue;

      if (MaxBuyPrice == null && lastSell < decimal.MaxValue)
      {
        maxBuy = lastSell;
      }

      return new Tuple<decimal, decimal, decimal>(lastSell, minBuy, maxBuy);
    }

    #endregion

    #region CreatePositionFromIntersection

    protected virtual async Task CreateBuyPositionFromIntersection(CtksIntersection intersection, bool automatic = false)
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

      if (leftSize >= MinPositionValue)
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


        await CancelPositionsToCreate(intersection, automatic, leftSize, validPositions);

        if (GetBudget(automatic) > leftSize)
        {
          await CreateBuyPosition(leftSize, intersection, automatic);
        }
      }
    }

    #endregion

    #region CancelPositionsToCreate

    private async Task CancelPositionsToCreate(CtksIntersection intersection, bool automatic, decimal leftSize, List<TPosition> validPositions)
    {
      var stack = new Stack<TPosition>(validPositions);
      var openBuy = stack.Sum(x => x.PositionSize);

      while ((GetBudget(automatic) < leftSize && GetBudget(automatic) + openBuy > leftSize))
      {
        var openLow = stack.Pop();

        if (openLow != null &&
            intersection.Value > openLow.Price &&
            !openLow.Intersection.IsSame(intersection))
        {
          Logger?.Log(MessageType.Warning, $"Cancelling buyPosition {openLow.Intersection.Value} in order to create another {intersection.Value}", simpleMessage: true);

          var result = await CancelPosition(openLow);

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

    protected decimal GetBudget(bool automatic = false)
    {
      return automatic ? Math.Min(AutomaticBudget, Budget) : Budget;
    }

    #endregion

    #region RecreateSellPositions

    public async Task RecreateSellPositions(Candle actualCandle, IEnumerable<TPosition> positionsToCancel)
    {
      var removedBu = new HashSet<TPosition>();

      var sellPositions = positionsToCancel
        .Where(x => x.OpositPositions.Count > 0)
        .OrderByDescending(x => x.OpositPositions[0].Price).ToList();

      var sumf = OpenSellPositions
        .Sum(x => x.OriginalPositionSizeNative);

      var asdd = Math.Round(sumf, 0);

      foreach (var sellPosition in sellPositions)
      {
        await CancelPosition(sellPosition, removedBu);
      }


      foreach (var opened in removedBu)
      {
        await CreateSellPositionForBuy(opened);
      }
    }

    #endregion

    private IEnumerable<CtksIntersection> GetIntersectionsForSell(Candle actualCandle, Candle dailyCandle)
    {
      var intersections = Intersections;

      foreach (var innerStrategy in InnerStrategies)
      {
        var validIntersections = innerStrategy.Calculate(actualCandle, dailyCandle, PositionSide.Sell)
           .Where(x => x.Value > actualCandle.Close.Value).ToList();

        if (validIntersections.Any())
        {
          intersections = validIntersections;
        }
      }

      return intersections.OrderBy(x => x.Value)
           .Where(x => x.Value > actualCandle.Close.Value);
    }

    #region GetPositionSize

    protected decimal GetPositionSize(TimeFrame timeFrame, bool automatic = false, PositionSide positionSide = PositionSide.Neutral)
    {
      decimal positionSize = 0;

      if (timeFrame == TimeFrame.D1)
      {
        timeFrame = TimeFrame.W1;
      }

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

      DrawdawnFromMaxTotalValue = ((TotalValue - MaxTotalValue) / MaxTotalValue) * 100;

      if (DrawdawnFromMaxTotalValue < MaxDrawdawnFromMaxTotalValue)
      {
        MaxDrawdawnFromMaxTotalValue = DrawdawnFromMaxTotalValue;
      }

      RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.AvrageBuyPrice));
      RaisePropertyChanged(nameof(AllClosedPositions));
    }

    #endregion

    #region CreateSellPositionForBuy

    protected virtual async Task CreateSellPositionForBuy(TPosition TPosition, decimal minForcePrice = 0)
    {
      if (TPosition.PositionSize > 0)
      {
        var ctksIntersections = GetIntersectionsForSell(lastCandle, indicatorsCandles.FirstOrDefault());

        await CreateSell(TPosition, ctksIntersections.ToList(), minForcePrice);

        var sumOposite = TPosition.OpositPositions.Sum(x => x.OriginalPositionSizeNative);
        if (sumOposite != TPosition.OriginalPositionSizeNative)
        {
          throw new Exception("Postion asset value does not mach sell order !!");
        }
      }
    }

    #endregion

    #region CloseBuy

    public virtual async Task CloseBuy(TPosition TPosition, decimal minForcePrice = 0)
    {
      try
      {
        await buyLock.WaitAsync();

        if (TPosition.State != PositionState.Filled)
        {
          //var inter = Intersections.SingleOrDefault(x => x.IsSame(buyPosition.Intersection));

          //if (inter != null)
          //  inter.IsEnabled = false;

          TotalNativeAsset += TPosition.PositionSizeNative;
          LeftSize += TPosition.PositionSizeNative;

          await CreateSellPositionForBuy(TPosition, minForcePrice);

          TPosition.State = PositionState.Filled;

          ClosedBuyPositions.Add(TPosition);
          OpenBuyPositions.Remove(TPosition);
          Budget -= TPosition.Fees ?? 0;

          if (TPosition.IsAutomatic)
            AutomaticBudget -= TPosition.Fees ?? 0;

          if (DisableOnBuy)
          {
            TPosition.Intersection.IsEnabled = false;
          }


          ActualPositions.Add(TPosition);

          RaisePropertyChanged(nameof(AllCompletedPositions));
          RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.TotalExpectedProfit));
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

    protected virtual async void CloseSell(TPosition position)
    {
      try
      {
        await buyLock.WaitAsync();

        var newTotalNativeAsset = TotalNativeAsset - position.OriginalPositionSizeNative;

        var sum = OpenSellPositions
          .Where(x => x.Id != position.Id)
          .Sum(x => x.OriginalPositionSizeNative);


        if (Math.Round(sum, Asset.NativeRound) != Math.Round(newTotalNativeAsset, Asset.NativeRound))
        {
          HandleError($"Native asset value does not mach sell order !! " +
                      $"{Math.Round(sum, Asset.NativeRound)} != {Math.Round(newTotalNativeAsset, Asset.NativeRound)},\n" +
                      $"TPosition: {position.ShortId}, {position.Price},{position.OriginalPositionSizeNative}\n" +
                      $"Left size: {LeftSize}");

        }

        var finalSize = position.Price * position.OriginalPositionSizeNative;
        var profit = finalSize - position.OriginalPositionSize; 

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

        position.State = PositionState.Filled;

        ClosedSellPositions.Add(position);
        OpenSellPositions.Remove(position);

        if (position.OpositPositions.Count > 0)
        {
          var originalBuy = position.OpositPositions.Single();

          originalBuy.RaiseNotify(nameof(position.TotalProfit));
          originalBuy.RaiseNotify(nameof(position.TotalFees));
          originalBuy.RaiseNotify(nameof(position.FinalProfit));
          originalBuy.RaiseNotify(nameof(position.ExpectedProfit));

          if (originalBuy.OpositPositions.Sum(x => x.PositionSize) == 0)
          {
            originalBuy.State = PositionState.Completed;

            ActualPositions.Remove((TPosition)originalBuy);

            RaisePropertyChanged(nameof(AllCompletedPositions));
            RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.TotalExpectedProfit));
          }
        }

        RaisePropertyChanged(nameof(StrategyViewModel<TPosition>.TotalSell));

        SaveState();
      }
      finally
      {
        buyLock.Release();
      }

    }

    #endregion

    #region CreateSell

    protected virtual async Task CreateSell(TPosition buyPosition, IList<CtksIntersection> ctksIntersections, decimal minForcePrice = 0)
    {
      try
      {
        var minPrice = Math.Max(buyPosition.Price * (1.0m + MinSellProfitMapping[buyPosition.TimeFrame]), MinSellPrice ?? 0);

        if (buyPosition.IsAutomatic)
        {
          minPrice = buyPosition.Price * (decimal)1.005;
        }

        IList<CtksIntersection> nextLines = null;

        nextLines = ctksIntersections
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

        int i = 0;
        List<TPosition> createdPositions = new List<TPosition>();

        var buyPositionSize = buyPosition.PositionSize;
        var buyPositionSizeNative = buyPosition.PositionSizeNative;

        foreach (var nextLine in nextLines)
        {
          var leftPositionSize = buyPositionSize;

          if (leftPositionSize < 0)
          {
            buyPositionSize = 0;
          }

          var ctksIntersection = nextLine;
          var forcePositionSize = i > 0 || nextLine.Value > buyPosition.Price * (decimal)1.5;


          var positionSize = buyPositionSize;

          var maxPOsitionOnIntersection = (decimal)GetPositionSize(ctksIntersection.TimeFrame, buyPosition.IsAutomatic, PositionSide.Sell);

          var positionsOnIntersesction = OpenSellPositions
            .Where(x => x.Intersection.IsSame(ctksIntersection))
            .Sum(x => x.PositionSize);

          leftPositionSize = (decimal)maxPOsitionOnIntersection - positionsOnIntersesction;


          if (!forcePositionSize && MinPositionValue > leftPositionSize)
          {
            continue;
          }

          if (buyPositionSize > leftPositionSize)
          {
            positionSize = leftPositionSize;
          }

          if (leftPositionSize <= 0 && !forcePositionSize)
            continue;

          if (forcePositionSize)
          {
            if (buyPositionSize > maxPOsitionOnIntersection)
              positionSize = maxPOsitionOnIntersection;
            else
              positionSize = buyPositionSize;
          }



          decimal roundedNativeSize = 0;

          roundedNativeSize = Math.Round(positionSize / buyPosition.Price, Asset.NativeRound);

          if (buyPositionSizeNative < roundedNativeSize)
          {
            roundedNativeSize = buyPositionSizeNative;
          }

          if (buyPositionSize - positionSize == 0 && buyPositionSizeNative - roundedNativeSize > 0)
          {
            roundedNativeSize = buyPositionSizeNative;
          }

          positionSize = buyPosition.Price * roundedNativeSize;
          if (positionSize <= 0)
          {
            continue;
          }

          var leftSize = buyPositionSize - positionSize;

          if (MinPositionValue > leftSize && leftSize > 0)
          {
            roundedNativeSize = roundedNativeSize + (buyPositionSizeNative - roundedNativeSize);

          }

          positionSize = buyPosition.Price * roundedNativeSize;
          if (positionSize <= 0)
          {
            continue;
          }

          var newPosition = new TPosition()
          {
            PositionSize = positionSize,
            OriginalPositionSize = positionSize,
            Price = ctksIntersection.Value,
            OriginalPositionSizeNative = roundedNativeSize,
            PositionSizeNative = roundedNativeSize,
            Side = PositionSide.Sell,
            TimeFrame = ctksIntersection.TimeFrame,
            Intersection = ctksIntersection,
            State = PositionState.Open,
            IsAutomatic = buyPosition.IsAutomatic
          };

          createdPositions.Add(newPosition);

          buyPositionSize -= newPosition.OriginalPositionSize;
          buyPositionSizeNative -= newPosition.OriginalPositionSizeNative;

          if (buyPositionSize <= 0)
            break;
        }


        if (buyPositionSize > 0)
        {
          var lastCreated = createdPositions.LastOrDefault();

          if (lastCreated != null)
          {
            var positionSize = lastCreated.PositionSize + buyPosition.PositionSize;
            var roundedNativeSize = Math.Round(positionSize / buyPosition.Price, Asset.NativeRound);

            var TPosition = new TPosition()
            {
              PositionSize = positionSize,
              OriginalPositionSize = positionSize,
              Price = lastCreated.Price,
              OriginalPositionSizeNative = roundedNativeSize,
              PositionSizeNative = roundedNativeSize,
              Side = PositionSide.Sell,
              TimeFrame = lastCreated.TimeFrame,
              Intersection = lastCreated.Intersection,
              State = PositionState.Open,
              IsAutomatic = buyPosition.IsAutomatic
            };

            TPosition.OpositPositions.Add(buyPosition);

            buyPosition.PositionSize = 0;
            buyPosition.PositionSizeNative = 0;
            buyPosition.OpositPositions.Add(TPosition);
            buyPosition.OpositPositions.Add(lastCreated);

            createdPositions.Remove(lastCreated);
            createdPositions.Add(TPosition);
          }
          else
          {
            var positionSize = buyPosition.PositionSize;

            var ctksIntersection = nextLines.Last();
            var roundedNativeSize = Math.Round(positionSize / buyPosition.Price, Asset.NativeRound);

            var newPosition = new TPosition()
            {
              PositionSize = positionSize,
              OriginalPositionSize = positionSize,
              Price = ctksIntersection.Value,
              OriginalPositionSizeNative = roundedNativeSize,
              PositionSizeNative = roundedNativeSize,
              Side = PositionSide.Sell,
              TimeFrame = ctksIntersection.TimeFrame,
              Intersection = ctksIntersection,
              State = PositionState.Open,
              IsAutomatic = buyPosition.IsAutomatic
            };

            createdPositions.Add(newPosition);
          }
        }

        await PlaceSellPositions(createdPositions, buyPosition);
      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }

    }

    #endregion

    #region PlaceSellPositions

    protected async Task PlaceSellPositions(List<TPosition> createdPositions, TPosition buyPosition)
    {
      try
      {
        await buyLock.WaitAsync();

        foreach (var sell in createdPositions)
        {
          long id = 0;

          while (id == 0)
          {
            id = await PlaceCreatePosition(sell);

            if (id > 0)
            {
              sell.Id = id;
              onCreatePositionSub.OnNext(sell);
              OpenSellPositions.Add(sell);

              sell.OpositPositions.Add(buyPosition);

              buyPosition.PositionSize -= sell.OriginalPositionSize;
              buyPosition.PositionSizeNative -= sell.OriginalPositionSizeNative;
              buyPosition.OpositPositions.Add(sell);

              LeftSize -= sell.PositionSizeNative;

              SaveState();
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
      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }
      finally
      {
        buyLock.Release();
      }
    }

    #endregion

    #region CreateBuyPosition

    protected virtual async Task CreateBuyPosition(decimal positionSize, CtksIntersection intersection, bool automatic = false)
    {
      var roundedNativeSize = Math.Round(positionSize / intersection.Value, Asset.NativeRound);
      positionSize = roundedNativeSize * intersection.Value;

      if (positionSize == 0)
        return;


      var newPosition = new TPosition()
      {
        PositionSize = positionSize,
        OriginalPositionSize = positionSize,
        Price = intersection.Value,
        OriginalPositionSizeNative = roundedNativeSize,
        PositionSizeNative = roundedNativeSize,
        TimeFrame = intersection.TimeFrame,
        Intersection = intersection,
        State = PositionState.Open,
        Side = PositionSide.Buy,
        IsAutomatic = automatic
      };

      var id = await PlaceCreatePosition(newPosition);

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

    public async Task<bool> CancelPosition(TPosition position, HashSet<TPosition> removed = null, bool force = false)
    {
      var cancled = await PlaceCancelPosition(position);

      if (cancled || force)
      {
        RemovePosition(position, removed);
      }

      SaveState();

      return cancled;
    }

    #endregion

    public void RemovePosition(TPosition position, HashSet<TPosition> removed = null)
    {
      if (position.Side == PositionSide.Buy)
      {
        OpenBuyPositions.Remove(position);
        Budget += position.PositionSize;

        if (position.IsAutomatic)
        {
          AutomaticBudget += position.PositionSize;
        }

        position.RaiseNotify(nameof(position.ExpectedProfit));
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
            removed.Add((TPosition)buy);
        }

        LeftSize += position.OriginalPositionSizeNative;

        OpenSellPositions.Remove(position);
      }
    }

    #region UpdateIntersections

    public void UpdateIntersections(IEnumerable<CtksIntersection> ctksIntersections)
    {
      var positions = AllOpenedPositions.Where(x => x.Intersection.IntersectionType != IntersectionType.RangeFilter).ToList();

      foreach (var postion in positions)
      {
        var inter = postion.Intersection;

        var found = ctksIntersections.FirstOrDefault(y => y.IsSame(inter));

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


    private void HandleError(string message)
    {
#if RELEASE
      Logger.Log(MessageType.Error, message);
#else
      throw new Exception(message);
#endif
    }

    protected abstract Task<bool> PlaceCancelPosition(TPosition TPosition);
    protected abstract Task<long> PlaceCreatePosition(TPosition TPosition);
    public abstract void SaveState();
    public abstract void SaveStrategyData();
    public abstract void LoadState();
    public abstract Task RefreshState();
    public abstract bool IsPositionFilled(Candle candle, TPosition TPosition);
    public abstract void SubscribeToChanges();

    #region Reset

    public async Task Reset(Candle actualCandle)
    {
      try
      {
        await buyLock.WaitAsync();
        var asd = AllOpenedPositions.ToList();

        foreach (var open in asd)
        {
          await CancelPosition(open, force: true);
        }

        LeftSize = TotalNativeAsset;
        var fakeSize = LeftSize;

        var removedBu = new List<TPosition>();

        var buys = ClosedBuyPositions
          .Where(x => x.State == PositionState.Filled &&
                      !x.OpositPositions.Any())
          .DistinctBy(x => x.Id)
          .ToList();



        if (buys.Count > 0)
        {
          removedBu = new List<TPosition>(buys);
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
            await CreateSellPositionForBuy(opened);
          }
          else
            ClosedBuyPositions.Remove(opened);
        }


        SaveState();
      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }
      finally
      {
        buyLock.Release();
      }

    }

    #endregion

    #endregion
  }
}