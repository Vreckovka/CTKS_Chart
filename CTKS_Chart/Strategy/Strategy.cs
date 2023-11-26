using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VCore.Standard;
using VCore.Standard.Helpers;

namespace CTKS_Chart
{
  public class Strategy : ViewModel
  {
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

    public const decimal Multiplicator = (decimal)1;
    public ObservableCollection<Position> ClosedBuyPositions { get; set; } = new ObservableCollection<Position>();
    public ObservableCollection<Position> ClosedSellPositions { get; set; } = new ObservableCollection<Position>();

    public ObservableCollection<Position> OpenSellPositions { get; set; } = new ObservableCollection<Position>();
    public ObservableCollection<Position> OpenBuyPositions { get; set; } = new ObservableCollection<Position>();

    public Dictionary<TimeFrame, decimal> PositionSizeMapping { get; set; } = new Dictionary<TimeFrame, decimal>()
    {
      {TimeFrame.M12, 250},
      {TimeFrame.M6, 150},
      {TimeFrame.M3, 75},
      {TimeFrame.M1, 50},
      {TimeFrame.W2, 30},
      {TimeFrame.W1, 20},
    };

    public Dictionary<TimeFrame, double> MinSellProfitMapping { get; set; } = new Dictionary<TimeFrame, double>()
    {
      {TimeFrame.M12, 0.01},
      {TimeFrame.M6,  0.01},
      {TimeFrame.M3,  0.01},
      {TimeFrame.M1,  0.01},
      {TimeFrame.W2,  0.01},
      {TimeFrame.W1,  0.01},
    };

    public IEnumerable<Position> AllClosedPositions
    {
      get
      {
        return ClosedBuyPositions.Concat(ClosedSellPositions);
      }
    }

    public IEnumerable<Position> AllOpenedPositions
    {
      get
      {
        return OpenBuyPositions.Concat(OpenSellPositions);
      }
    }

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

    public static decimal StartingBudget
    {
      get
      {
        return 2000 * Multiplicator;
      }
    }

    #region Budget

    private decimal budget = StartingBudget;

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

    private decimal GetMinBuy(decimal low, TimeFrame timeFrame)
    {
      return low * (decimal)(1 - MinSellProfitMapping[timeFrame]);
      return low;
    }

    #region CreatePositions

    private Dictionary<Position, decimal> keyValuePairs = new Dictionary<Position, decimal>();
    public void CreatePositions(Candle actualCandle, List<CtksIntersection> ctksIntersections)
    {
      var minPrice = actualCandle.Low * (decimal)0.75;

      var openedBuy = OpenBuyPositions.Where(x => !ctksIntersections.Any(y => y.Value == x.Intersection.Value)).ToList();
      var openedSell = OpenSellPositions.Where(x => !ctksIntersections.Any(y => y.Value == x.Intersection.Value)).ToList();
      var ordered = ctksIntersections.OrderBy(x => x.Value).ToList();


      foreach (var buyPosition in openedBuy)
      {
        OpenBuyPositions.Remove(buyPosition);
        Budget += buyPosition.PositionSize;
      }

      decimal removed = 0;
      var removedBu = new HashSet<Position>();
      keyValuePairs = new Dictionary<Position, decimal>();
      created = 0;

      foreach (var sellPosition in openedSell)
      {
        var buy = sellPosition.OpositPositions[0];
        buy.OpositPositions.Remove(sellPosition);
        buy.PositionSize += sellPosition.PositionSize;
        buy.PositionSizeNative += sellPosition.PositionSizeNative;
        LeftSize += sellPosition.PositionSizeNative;

        OpenSellPositions.Remove(sellPosition);
        removedBu.Add(buy);

        removed += sellPosition.PositionSizeNative;

        if (keyValuePairs.ContainsKey(buy))
        {
          keyValuePairs[buy] += sellPosition.PositionSizeNative;
        }
        else
          keyValuePairs.Add(buy, sellPosition.PositionSizeNative);
      }


      foreach (var opened in removedBu)
      {
        CreateSellPositionForBuy(opened, ordered, actualCandle);
      }

      var sum1 = OpenSellPositions.ToList().Sum(x => x.OriginalPositionSizeNative);
      var summ = keyValuePairs.Sum(x => x.Value);

      foreach (var ke in ClosedBuyPositions)
      {
        var sumOposite = ke.OpositPositions.Sum(x => x.OriginalPositionSizeNative);
        if (sumOposite != ke.OriginalPositionSizeNative)
        {
          throw new Exception("Postion asset value does not mach sell order !!");
        }
      }

      //if (Math.Round(sum1) != Math.Round(TotalNativeAsset))
      //{
      //  throw new Exception("Native asset value does not mach sell order !!");
      //}

      foreach (var intersection in
        ctksIntersections
          .Where(x => x.Value < actualCandle.Close.Value && x.Value > minPrice && x.Value < GetMinBuy(actualCandle.Close.Value, x.TimeFrame)))
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
          CreateBuyPosition(leftSize, intersection);
        }
      }
    }

    #endregion

    #region GetPositionSize

    private decimal GetPositionSize(TimeFrame timeFrame)
    {
      return PositionSizeMapping[timeFrame] * Multiplicator;
    }

    #endregion

    #region ValidatePositions

    public void ValidatePositions(Candle candle, IEnumerable<CtksIntersection> ctksIntersections)
    {
      var ordered = ctksIntersections.OrderBy(x => x.Value).ToList();

      for (int i = 0; i < 2; i++)
      {
        var allPositions = AllOpenedPositions.ToList();

        foreach (var position in allPositions)
        {
          if (candle.Low.Value <= position.Price && position.Price < candle.High.Value)
          {
            position.State = PositionState.Filled;

            if (position.Side == PositionSide.Buy)
            {
              TotalBuy += position.PositionSize;
              TotalNativeAsset += position.PositionSizeNative;
              LeftSize += position.PositionSizeNative;

              CreateSellPositionForBuy(position, ordered, candle);

              ClosedBuyPositions.Add(position);
              OpenBuyPositions.Remove(position);

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

          if (position.Price < candle.Low.Value * (decimal)0.75 && position.State == PositionState.Open)
          {
            if (position.Side == PositionSide.Buy)
            {
              OpenBuyPositions.Remove(position);
            }
            else
            {
              OpenSellPositions.Remove(position);
            }

            Budget += position.PositionSize;
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

    private decimal created;
    private void CreateSellPositionForBuy(Position position, IEnumerable<CtksIntersection> ctksIntersections, Candle actualCandle)
    {
      CreateSell(position, ctksIntersections, actualCandle);

      var sumOposite = position.OpositPositions.Sum(x => x.OriginalPositionSizeNative);
      if (sumOposite != position.OriginalPositionSizeNative)
      {
        throw new Exception("Postion asset value does not mach sell order !!");
      }

    }

    #endregion

    #region CreateSell

    private void CreateSell(Position position, IEnumerable<CtksIntersection> ctksIntersections, Candle actualCandle)
    {
      var minPrice = position.Price * (decimal)(1.0 + MinSellProfitMapping[position.TimeFrame]);
      var nextLines = ctksIntersections.Where(x => x.Value > minPrice && x.Value > actualCandle.Close.Value).ToList();

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


          if (MinPositionValue > leftPositionSize)
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

          created += newPosition.PositionSizeNative;

          createdPositions.Add(newPosition);

         

          if (position.PositionSize <= 0)
            break;
        }

        i++;
      }

      foreach (var sell in createdPositions)
      {
        onCreatePositionSub.OnNext(sell);
        OpenSellPositions.Add(sell);

        LeftSize -= sell.PositionSizeNative;

        if (LeftSize < 0)
          ;
      }
    }

    #endregion

    private decimal LeftSize = 0;

    private void CreateBuyPosition(decimal positionSize, CtksIntersection intersection)
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

      Budget -= newPosition.PositionSize;
      OpenBuyPositions.Add(newPosition);

      onCreatePositionSub.OnNext(newPosition);
    }

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

    private void CloseSell(Position position)
    {
      ClosedSellPositions.Add(position);
      OpenSellPositions.Remove(position);

      var originalBuy = position.OpositPositions.Single();

      var finalSize = position.Price * position.OriginalPositionSizeNative;

      position.Profit = finalSize - position.OriginalPositionSize;
      position.PositionSize = 0;
      position.PositionSizeNative = 0;

      TotalProfit = ClosedSellPositions.Where(x => x.State == PositionState.Filled).Sum(x => x.Profit);
      TotalSell += position.OriginalPositionSize;
      Budget += finalSize;
      TotalNativeAsset -= position.OriginalPositionSizeNative;

      var sum = OpenSellPositions.ToList().Sum(x => x.OriginalPositionSizeNative);

      if (Math.Round(sum) != Math.Round(TotalNativeAsset))
      {
        throw new Exception("Native asset value does not mach sell order !!");
      }

      if (originalBuy.OpositPositions.Sum(x => x.PositionSize) == 0)
      {
        originalBuy.State = PositionState.Completed;
      }
    }

    #endregion
  }
}