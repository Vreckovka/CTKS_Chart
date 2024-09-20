using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Prompts;
using VCore.WPF.ViewModels.Prompt;

namespace CTKS_Chart.ViewModels
{
  public class PositionSizeViewModel<TPosition> : BasePromptViewModel where TPosition : Position, new() 
  {
    private readonly BaseStrategy<TPosition> strategy;
    private readonly Candle actual;
    private readonly IWindowManager windowManager;

    public PositionSizeViewModel(BaseStrategy<TPosition> strategy, Candle actual, IWindowManager windowManager)
    {
      this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
      this.actual = actual ?? throw new ArgumentNullException(nameof(actual));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

      PositionSizeMapping = strategy.PositionSizeMapping;

      CancelVisibility = System.Windows.Visibility.Visible;
      CanExecuteOkCommand = () => { return IsDirty; };
      CalculatePositionSum();
    }

    public bool IsDirty { get; set; }

    #region ScaleSize

    public double ScaleSize
    {
      get { return strategy.ScaleSize; }
      set
      {
        if (value != strategy.ScaleSize)
        {
          strategy.ScaleSize = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PositionSizeMapping

    private IEnumerable<KeyValuePair<TimeFrame, decimal>> positionSizeMapping;

    public IEnumerable<KeyValuePair<TimeFrame, decimal>> PositionSizeMapping
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

    #region MinPrice

    private decimal minPrice;

    public decimal MinPrice
    {
      get { return minPrice; }
      set
      {
        if (value != minPrice)
        {
          minPrice = value;

          CalculatePositions();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MaxDrawdown

    private double maxDrawdown;

    public double MaxDrawdown
    {
      get { return maxDrawdown; }
      set
      {
        if (value != maxDrawdown)
        {
          maxDrawdown = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CalculatePositions

    public void CalculatePositions()
    {
      var minPrice = MinPrice;

      var intersections = strategy
        .Intersections
        .Where(x => x.Value < actual.Close &&
                    x.Value > minPrice).ToList();

      decimal i = 0;

      foreach (var inter in intersections)
      {
        i += strategy.PositionWeight[inter.TimeFrame];
      }

      if (i > 0)
      {
        var avaibleBudget = strategy.OpenBuyPositions.Sum(x => x.OriginalPositionSize) + strategy.Budget;
        var maxI = avaibleBudget / i;

        PositionSizeMapping = strategy.PositionWeight.Select(x => new KeyValuePair<TimeFrame, decimal>(x.Key, x.Value * maxI));
        maxDrawdown = 100 - (double)(MinPrice * 100 / actual.Close.Value);

        RaisePropertyChanged(nameof(MaxDrawdown));

        IsDirty = true;
        okCommand.RaiseCanExecuteChanged();
      }

    }

    #endregion

    #region CalculatePositionSum

    private void CalculatePositionSum()
    {
      var intersections = strategy
        .Intersections
        .Where(x => x.Value < actual.Close).ToList();

      decimal i = 0;
      decimal leftBudget = 0;

      var avaibleBudget = strategy.OpenBuyPositions.Sum(x => x.OriginalPositionSize) + strategy.Budget;

      foreach (var inter in intersections)
      {
        leftBudget += strategy.PositionSizeMapping.Single(x => x.Key == inter.TimeFrame).Value;

        if (leftBudget > avaibleBudget)
        {
          break;
        }

        i += strategy.PositionWeight[inter.TimeFrame];
      }

      decimal y = 0;

      foreach (var inter in intersections)
      {
        y += strategy.PositionWeight[inter.TimeFrame];

        if (y > i)
        {
          break;
        }
        else
        {
          minPrice = inter.Value;
        }
      }

      maxDrawdown = 100 - (double)(MinPrice * 100 / actual.Close.Value);
    }

    #endregion

    protected override void OnOkCommand()
    {
      base.OnOkCommand();

    
    }
  }
}
