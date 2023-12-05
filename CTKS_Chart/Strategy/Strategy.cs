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
  public abstract class Strategy : ViewModel
  {
    public Strategy()
    {
      budget = StartingBudget;
    }

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
    public double ScaleSize { get; set; } = 3;

    public ILogger Logger { get; set; }

    #region Positions

    public ObservableCollection<Position> ClosedBuyPositions { get; set; } = new ObservableCollection<Position>();
    public ObservableCollection<Position> ClosedSellPositions { get; set; } = new ObservableCollection<Position>();

    public ObservableCollection<Position> OpenSellPositions { get; set; } = new ObservableCollection<Position>();
    public ObservableCollection<Position> OpenBuyPositions { get; set; } = new ObservableCollection<Position>();


    #endregion

    #region PositionSizeMapping

    private Dictionary<TimeFrame, decimal> positionSizeMapping = new Dictionary<TimeFrame, decimal>()
    {
      {TimeFrame.M12, 200},
      {TimeFrame.M6, 100},
      { TimeFrame.M3, 50},
      { TimeFrame.M1, 30},
      { TimeFrame.W2, 20},
      { TimeFrame.W1, 10},
    };

    public Dictionary<TimeFrame, decimal> PositionSizeMapping
    {
      get { return positionSizeMapping; }
      set
      {
        if (value != positionSizeMapping)
        {
          positionSizeMapping = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinSellProfitMapping


    public Dictionary<TimeFrame, double> MinSellProfitMapping { get; } = new Dictionary<TimeFrame, double>()
    {
      {TimeFrame.M12, 0.01},
      {TimeFrame.M6,  0.01},
      {TimeFrame.M3,  0.01},
      {TimeFrame.M1,  0.01},
      {TimeFrame.W2,  0.01},
      {TimeFrame.W1,  0.01},
    };

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

    #region TotalProfit

    private decimal totalProfit;

    public decimal TotalProfit
    {
      get { return totalProfit; }
      set
      {
        if (value != totalProfit)
        {
          totalProfit = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion



    #region StartingBudget

    private decimal startingBudget = 1000;

    public decimal StartingBudget
    {
      get { return startingBudget; }
      protected set
      {
        if (value != startingBudget)
        {
          startingBudget = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public List<CtksIntersection> Intersections { get; set; } = new List<CtksIntersection>();

    #region Budget

    private decimal budget;

    public decimal Budget
    {
      get { return budget; }
      set
      {
        if (value != budget)
        {
          budget = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalNativeAsset

    private decimal totalNativeAsset;

    public decimal TotalNativeAsset
    {
      get { return totalNativeAsset; }
      set
      {
        if (value != totalNativeAsset)
        {
          totalNativeAsset = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    #region TotalBuy

    private decimal totalBuy;

    public decimal TotalBuy
    {
      get { return totalBuy; }
      set
      {
        if (value != totalBuy)
        {
          totalBuy = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalSell

    private decimal totalSell;

    public decimal TotalSell
    {
      get { return totalSell; }
      set
      {
        if (value != totalSell)
        {
          totalSell = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AvrageBuyPrice

    public decimal AvrageBuyPrice
    {
      get
      {
        var closed = ClosedBuyPositions.Where(x => x.State == PositionState.Filled).ToList();
        var asd = closed.Sum(x => x.OriginalPositionSize);
        var asdddd = closed.Sum(x => x.OriginalPositionSizeNative);
        return TotalNativeAsset > 0 ? asd / asdddd : 0;
      }
    }

    #endregion

    #region GetMinBuy

    private decimal GetMinBuy(decimal low, TimeFrame timeFrame)
    {
      return low * (decimal)(1 - MinSellProfitMapping[timeFrame]);
      return low;
    }

    #endregion

    #region CreatePositions

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    public async void CreatePositions(Candle actualCandle)
    {
      try
      {
        await semaphoreSlim.WaitAsync();
        var minPrice = actualCandle.Close * (decimal)0.75;

        var openedBuy = OpenBuyPositions.Where(x => !Intersections.Any(y => y.Value == x.Intersection.Value)).ToList();
        var openedSell = OpenSellPositions.Where(x => !Intersections.Any(y => y.Value == x.Intersection.Value)).ToList();
        var ordered = Intersections.OrderBy(x => x.Value).ToList();


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
          await CreateSellPositionForBuy(opened, ordered);
        }

        var inter = Intersections
          .Where(x => x.Value < actualCandle.Close.Value && x.Value > minPrice && x.Value < GetMinBuy(actualCandle.Close.Value, x.TimeFrame))
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
            return;
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
      return PositionSizeMapping[timeFrame];
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
            if (position.Side == PositionSide.Buy)
            {
              await CloseBuy(position);

              var closedWithinCandle = position.OpositPositions.Where(x => x.Price < candle.Close.Value);

              foreach (var closedWithin in closedWithinCandle)
              {
                CloseSell(closedWithin);
              }
            }
            else
            {
              CloseSell(position);
            }
          }


          if (position.Price < candle.Close.Value * (decimal)0.75 && position.State == PositionState.Open)
          {
            await OnCancelPosition(position);
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
      if(position.PositionSize > 0)
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

    private SemaphoreSlim buySemaphore = new SemaphoreSlim(1, 1);
    public async Task CloseBuy(Position position)
    {
      try
      {
        await buySemaphore.WaitAsync();

        if (position.State != PositionState.Filled)
        {
          var ordered = Intersections.OrderBy(x => x.Value).ToList();

          TotalBuy += position.PositionSize;
          TotalNativeAsset += position.PositionSizeNative;
          LeftSize += position.PositionSizeNative;

          await CreateSellPositionForBuy(position, ordered);

          position.State = PositionState.Filled;

          ClosedBuyPositions.Add(position);
          OpenBuyPositions.Remove(position);
          SaveState();
        }
      }
      finally
      {
        buySemaphore.Release();
      }
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

    protected decimal LeftSize = 0;

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

    #region RenderPositions

    public void RenderPositions(Canvas canvas, Func<double, decimal, double> getCanvasValue, List<CtksIntersection> ctksIntersections, decimal maxValue, decimal minValue)
    {
      var renderedPositions = new List<CtksIntersection>();
      var valid = ctksIntersections.Where(x => x.Value > minValue && x.Value < maxValue);

      foreach (var intersection in valid)
      {
        var target = new Line();

        var positionsOnIntersesction = AllOpenedPositions
          .Where(x => x.Intersection?.Value == intersection.Value)
          .ToList();

        var firstPositionsOnIntersesction = positionsOnIntersesction.FirstOrDefault();
        var sum = positionsOnIntersesction.Sum(x => x.PositionSize);

        if (firstPositionsOnIntersesction != null && !renderedPositions.Contains(intersection))
        {
          var stroke = firstPositionsOnIntersesction.Side == PositionSide.Buy ? Brushes.Green : Brushes.Red;
          target.Stroke = stroke;

          var frame = intersection.TimeFrame;

          switch (frame)
          {
            case TimeFrame.Null:
              target.StrokeThickness = 1;
              break;
            case TimeFrame.M12:
              target.StrokeThickness = 4;
              break;
            case TimeFrame.M6:
              target.StrokeThickness = 2;
              break;
            case TimeFrame.M3:
              target.StrokeThickness = 1;
              break;
            default:
              target.StrokeThickness = 1;
              break;
          }

          target.X1 = 150;
          target.X2 = canvas.ActualWidth;
          target.StrokeDashArray = new DoubleCollection() { 1, 1 };

          var actual = getCanvasValue(canvas.ActualHeight, intersection.Value);

          var lineY = canvas.ActualHeight - actual;

          target.Y1 = lineY;
          target.Y2 = lineY;

          Panel.SetZIndex(target, 110);

          var text = new TextBlock();
          text.Text = sum.ToString("N4");
          text.Foreground = stroke;

          Panel.SetZIndex(text, 110);
          Canvas.SetLeft(text, 50);
          Canvas.SetBottom(text, actual);

          canvas.Children.Add(text);
          canvas.Children.Add(target);

          renderedPositions.Add(intersection);
        }
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
      TotalSell += position.OriginalPositionSize;
      Budget += finalSize;
      TotalNativeAsset -= position.OriginalPositionSizeNative;

      Scale(position.Profit);

      var sum = OpenSellPositions.ToList().Sum(x => x.OriginalPositionSizeNative);

      if (Math.Round(sum) != Math.Round(TotalNativeAsset))
      {
        throw new Exception("Native asset value does not mach sell order !!");
      }

      if (position.OpositPositions.Count > 0)
      {
        var originalBuy = position.OpositPositions.Single();

        if (originalBuy.OpositPositions.Sum(x => x.PositionSize) == 0)
        {
          originalBuy.State = PositionState.Completed;
        }
      }

      position.State = PositionState.Filled;

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
      var nextMaxValue = PositionSizeMapping[TimeFrame.M12] * size;

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

    #region OnCancelPosition

    private async Task OnCancelPosition(Position position, HashSet<Position> removed = null)
    {
      var cancled = await CancelPosition(position);

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
          buy.PositionSize += position.PositionSize;
          buy.PositionSizeNative += position.PositionSizeNative;
          LeftSize += position.PositionSizeNative;

          OpenSellPositions.Remove(position);

          if (removed != null)
            removed.Add(buy);
        }
      }

      SaveState();
    }

    #endregion

    protected abstract Task<bool> CancelPosition(Position position);
    protected abstract Task<long> CreatePosition(Position position);
    public abstract void SaveState();
    public abstract void LoadState();
    public abstract void RefreshState();
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
  }
}