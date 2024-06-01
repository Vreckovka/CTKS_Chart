using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using System.Collections.Generic;
using System.Windows.Input;
using TradingManager.Providers;
using TradingManager.Views;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;

namespace TradingManager.ViewModels
{

  public class TradingViewDataViewModel : ViewModel
  {
    private readonly IWindowManager windowManager;

    public TradingViewDataViewModel(IWindowManager windowManager)
    {
      this.windowManager = windowManager ?? throw new System.ArgumentNullException(nameof(windowManager));
    }

    #region Name

    private string name;

    public string Name
    {
      get { return name; }
      set
      {
        if (value != name)
        {
          name = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Path

    private string path;

    public string Path
    {
      get { return path; }
      set
      {
        if (value != path)
        {
          path = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TimeFrame

    private TimeFrame timeFrame;

    public TimeFrame TimeFrame
    {
      get { return timeFrame; }
      set
      {
        if (value != timeFrame)
        {
          timeFrame = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsOutDated

    private bool isOutDated;

    public bool IsOutDated
    {
      get { return isOutDated; }
      set
      {
        if (value != isOutDated)
        {
          isOutDated = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public TradingViewSymbol TradingViewSymbol { get; set; }

    public IList<Candle> Candles { get; set; }

    #region OpenChart

    protected ActionCommand openChart;


    public ICommand OpenChart
    {
      get
      {
        return openChart ??= new ActionCommand(OnOpenChart).DisposeWith(this);
      }
    }

    private void OnOpenChart()
    {
      this.windowManager.ShowPrompt<ChartView>(null);
    }

    #endregion
  }

  public class ChartViewModel : BasePromptViewModel
  {
    public IList<Candle> Candles { get; set; }

   // public DrawingViewModel DrawingViewModel { get; set; }
  }
}
