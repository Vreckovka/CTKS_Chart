using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VCore.Standard;
using VCore.Standard.Helpers;

namespace CTKS_Chart
{
  public class Strategy : ViewModel
  {
    public const decimal Multiplicator = 1;
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
      {TimeFrame.W2, 20},
      {TimeFrame.W1, 10},
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

    private decimal GetMinBuy(decimal low, TimeFrame timeFrame)
    {
      return low * (decimal)(1 - MinSellProfitMapping[timeFrame]);
      return low;
    }


    public void CreatePositions(decimal low, decimal actualPrice, List<CtksIntersection> ctksIntersections)
    {
      var minPrice = low * (decimal)0.70;

      var openedBuy = OpenBuyPositions.Where(x => !ctksIntersections.Any(y => y.Id == x.Intersection.Id)).ToList();
      var openedSell = OpenSellPositions.Where(x => !ctksIntersections.Any(y => y.Id == x.Intersection.Id)).ToList();
      var ordered = ctksIntersections.OrderBy(x => x.Value).ToList();

      foreach (var buyPosition in openedBuy)
      {
        OpenBuyPositions.Remove(buyPosition);
        Budget += buyPosition.PositionSize;
      }

      foreach (var sellPosition in openedSell)
      {
        var buy = sellPosition.OpositPositions[0];
        buy.OpositPositions.Remove(sellPosition);
        buy.PositionSize += sellPosition.PositionSize;

        OpenSellPositions.Remove(sellPosition);
      }

      foreach (var opened in ClosedBuyPositions.Where(x => x.State == PositionState.Filled))
      {
        CreateSellPositionForBuy(opened, ordered, actualPrice);
      }


      foreach (var intersection in ctksIntersections.Where(x => x.Value < actualPrice && x.Value > minPrice && x.Value < GetMinBuy(actualPrice, x.TimeFrame)))
      {
        var positionsOnIntersesction = AllOpenedPositions
          .Where(x => x.Intersection?.Id == intersection.Id)
          .ToList();

        var maxPOsitionOnIntersection = GetPositionSize(intersection.TimeFrame);
        var sum = positionsOnIntersesction.Sum(x => x.PositionSize);

        var existing = AllClosedPositions
          .Where(x => x.Intersection.Id == intersection.Id)
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

        if (leftSize > 0)
        {
          var newPosition = new Position(leftSize, intersection.Value, leftSize / intersection.Value)
          {
            TimeFrame = intersection.TimeFrame,
            Intersection = intersection,
            State = PositionState.Open,
            Side = PositionSide.Buy,
          };

          Budget -= newPosition.PositionSize;
          OpenBuyPositions.Add(newPosition);
        }
      }
    }

    private decimal GetPositionSize(TimeFrame timeFrame)
    {
      return PositionSizeMapping[timeFrame] * Multiplicator;
    }

    public void ValidatePositions(decimal high, decimal low, decimal actualPrice, IEnumerable<CtksIntersection> ctksIntersections)
    {
      var ordered = ctksIntersections.OrderBy(x => x.Value).ToList();

      for (int i = 0; i < 2; i++)
      {
        var allPositions = AllOpenedPositions.ToList();

        foreach (var position in allPositions)
        {
          if (low <= position.Price && position.Price < high)
          {
            position.State = PositionState.Filled;

            if (position.Side == PositionSide.Buy)
            {
              TotalBuy += position.PositionSize;
              TotalNativeAsset += position.PositionSizeNative;

              CreateSellPositionForBuy(position, ordered, actualPrice);

              ClosedBuyPositions.Add(position);
              OpenBuyPositions.Remove(position);

              var closedWithinCandle = position.OpositPositions.Where(x => x.Price < actualPrice);

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

          if (position.Price < low * (decimal)0.75)
          {
            position.State = PositionState.Completed;

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

      var assetsValue = TotalNativeAsset * actualPrice;
      var openPositions = openedBuy.Sum(x => x.PositionSize);

      TotalValue = assetsValue + openPositions + Budget;
      TotalNativeAssetValue = TotalNativeAsset * actualPrice;

      RaisePropertyChanged(nameof(AllClosedPositions));
    }

    #region CreateSellPositionForBuy

    private void CreateSellPositionForBuy(Position position, IEnumerable<CtksIntersection> ctksIntersections, decimal actualPrice)
    {
      var minPrice = position.Price * (decimal)(1.0 + MinSellProfitMapping[position.TimeFrame]);
      var nextLines = ctksIntersections.Where(x => x.Value > minPrice && x.Value > actualPrice).ToList();

      int i = 0;
      while (position.PositionSize > 0)
      {
        foreach (var nextLine in nextLines)
        {
          var leftPositionSize = position.PositionSize;

          if (leftPositionSize < 0)
          {
            position.PositionSize = 0;
          }

          CreateSell(nextLine, position, i > 0);

          if (position.PositionSize <= 0)
            break;

        }

        i++;
      }

      var sumOposite = position.OpositPositions.Sum(x => x.PositionSizeNative);
      if (Math.Round(sumOposite, 10) != Math.Round(position.PositionSizeNative, 10))
      {
        throw new Exception("Postion asset value does not mach sell order !!");
      }
    }

    #endregion

    #region CreateSell

    private void CreateSell(CtksIntersection ctksIntersection, Position position, bool forcePositionSize = false)
    {
      var positionSize = position.PositionSize;

      var maxPOsitionOnIntersection = (decimal)GetPositionSize(ctksIntersection.TimeFrame);

      var positionsOnIntersesction = OpenSellPositions
        .Where(x => x.Intersection?.Id == ctksIntersection.Id)
        .Sum(x => x.PositionSize);

      var leftPositionSize = (decimal)maxPOsitionOnIntersection - positionsOnIntersesction;

      if (position.PositionSize > leftPositionSize)
      {
        positionSize = leftPositionSize;
      }

      if (leftPositionSize <= 0 && !forcePositionSize)
        return;

      if (forcePositionSize)
      {
        if (position.PositionSize > maxPOsitionOnIntersection)
          positionSize = maxPOsitionOnIntersection;
        else
          positionSize = position.PositionSize;
      }

      if (positionSize <= 0)
      {
        return;
      }

      var newPosition = new Position(positionSize, ctksIntersection.Value, positionSize / position.Price)
      {
        Side = PositionSide.Sell,
        TimeFrame = ctksIntersection.TimeFrame,
        Intersection = ctksIntersection,
        State = PositionState.Open,
      };

      newPosition.OpositPositions.Add(position);

      position.PositionSize -= positionSize;
      position.OpositPositions.Add(newPosition);



      OpenSellPositions.Add(newPosition);
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
          .Where(x => x.Intersection?.Id == intersection.Id)
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

    private void CloseSell(Position position)
    {
      ClosedSellPositions.Add(position);
      OpenSellPositions.Remove(position);

      var originalBuy = position.OpositPositions.Single();

      position.Profit += (position.Price * (decimal)100.0 / originalBuy.Price) - 100;

      TotalProfit = ClosedSellPositions.Where(x => x.State == PositionState.Filled).Sum(x => x.ProfitValue);
      TotalSell += position.PositionSize;
      Budget += position.PositionSize + position.ProfitValue;
      TotalNativeAsset -= position.PositionSizeNative;



      var sum = OpenSellPositions.ToList().Sum(x => x.PositionSizeNative);

      if (Math.Round(sum) != Math.Round(TotalNativeAsset))
      {
        throw new Exception("Native asset value does not mach sell order !!");
      }

      if (originalBuy.PositionSize == originalBuy.OriginalPositionSize)
      {
        originalBuy.State = PositionState.Completed;
      }

      position.PositionSize = 0;
    }
  }
}