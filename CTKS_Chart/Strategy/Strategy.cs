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
    public ObservableCollection<Position> BuyPositions { get; set; } = new ObservableCollection<Position>();
    public ObservableCollection<Position> SellPositions { get; set; } = new ObservableCollection<Position>();

    public IEnumerable<Position> AllPositions
    {
      get
      {
        return BuyPositions.Concat(SellPositions);
      }
    }

    #region TotalProfit

    private double totalProfit;

    public double TotalProfit
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

    #region Budget

    private double budget = 1000;

    public double Budget
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

    private double totalNativeAsset;

    public double TotalNativeAsset
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

    #region TotalValue

    private double totalValue;

    public double TotalValue
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

    private double totalBuy;

    public double TotalBuy
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

    private double totalSell;

    public double TotalSell
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

    private double minDiff = 0.01;

    public void CreatePositions(double low, List<CtksIntersection> ctksIntersections)
    {
      var minPrice = low * 0.75;
      var maxPrice = low * (1 - minDiff) ;

      foreach (var intersection in ctksIntersections.Where(x => x.Value < low && x.Value > minPrice && x.Value < maxPrice))
      {
        var positionsOnIntersesction = AllPositions
          .Where(x => x.Intersection?.Id == intersection.Id && x.State == PositionState.Open)
          .ToList();

        var maxPOsitionOnIntersection = GetPositionSize(intersection.TimeFrame);
        var sum = positionsOnIntersesction.Sum(x => x.PositionSize);

        var existing = BuyPositions
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
          var newPosition = new Position(leftSize, intersection.Value)
          {
            TimeFrame = intersection.TimeFrame,
            Intersection = intersection,
            State = PositionState.Open,
            Side = PositionSide.Buy,
          };

          Budget -= newPosition.PositionSize;
          BuyPositions.Add(newPosition);
        }
      }
    }

    private double GetPositionSize(TimeFrame timeFrame)
    {
      return timeFrame == TimeFrame.M12 ? 250 : timeFrame == TimeFrame.M6 ? 50 : 25;
    }

    public void ValidatePositions(double high, double low, IEnumerable<CtksIntersection> ctksIntersections)
    {
      var ordered = ctksIntersections.OrderBy(x => x.Value).ToList();
      var allPositions = AllPositions.Where(x => x.State == PositionState.Open).ToList();

      foreach (var position in allPositions)
      {
        if (low <= position.Price && position.Price < high)
        {
          position.State = PositionState.Filled;

          if (position.Side == PositionSide.Buy)
          {
            var minPrice = position.Price * (1 + minDiff);
            var nextLines = ordered.Where(x => x.Value > minPrice);

            foreach (var nextLine in nextLines)
            {
              var positionSize = position.PositionSize;
              var maxPOsitionOnIntersection = GetPositionSize(nextLine.TimeFrame);

              var positionsOnIntersesction = SellPositions
                .Where(x => x.Intersection?.Id == nextLine.Id && x.State == PositionState.Open)
                .Sum(x => x.PositionSize);

              var leftPositionSize = maxPOsitionOnIntersection - positionsOnIntersesction;

              if (position.PositionSize > leftPositionSize)
              {
                positionSize = leftPositionSize;
              }

              if (leftPositionSize <= 0)
                continue;

              var newPosition = new Position(positionSize, nextLine.Value)
              {
                Side = PositionSide.Sell,
                TimeFrame = nextLine.TimeFrame,
                Intersection = nextLine,
                State = PositionState.Open,
              };

              newPosition.OpositPositions.Add(position);

              position.PositionSize -= positionSize;
              position.OpositPositions.Add(newPosition);

              TotalBuy += newPosition.PositionSize;
              TotalNativeAsset += newPosition.PositionSizeNative;
              SellPositions.Add(newPosition);

              if (position.PositionSize <= 0)
                break;
            }
          }
          else
          {
            var originalBuy = position.OpositPositions.Single();

            originalBuy.PositionSize += position.PositionSize;
            position.Profit += (position.Price * 100.0 / originalBuy.Price) - 100;

            TotalProfit += SellPositions.Where(x => x.State == PositionState.Filled).Sum(x => x.ProfitValue);
            TotalSell += position.PositionSize;
            Budget += position.PositionSize + position.ProfitValue;
            TotalNativeAsset -= position.PositionSizeNative;

            if (originalBuy.PositionSize == originalBuy.OriginalPositionSize)
            {
              originalBuy.State = PositionState.Completed;
            }

            position.PositionSize = 0;
          }
        }

        if (position.Price < low * 0.75)
        {
          position.State = PositionState.Completed;
          Budget += position.PositionSize;
        }
      }


      var actualBuy = SellPositions.Where(x => x.State == PositionState.Open).Sum(x => x.PositionSizeNative) * low;
      var openPositions = BuyPositions.Where(x => x.State == PositionState.Open).Sum(x => x.PositionSize);

      TotalValue = actualBuy + openPositions + Budget;
    }

    #region RenderPositions

    public void RenderPositions(Canvas canvas, Func<double, double, double> getCanvasValue, List<CtksIntersection> ctksIntersections, double maxValue, double minValue)
    {
      var renderedPositions = new List<CtksIntersection>();
      var valid = ctksIntersections.Where(x => x.Value > minValue && x.Value < maxValue);

      foreach (var intersection in valid)
      {
        var target = new Line();

        var positionsOnIntersesction = AllPositions
          .Where(x => x.Intersection?.Id == intersection.Id && x.State == PositionState.Open)
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
  }
}