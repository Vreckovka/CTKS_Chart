using CTKS_Chart.Trading;
using System.Collections.ObjectModel;
using TradingManager.Providers;
using VCore.Standard;

namespace TradingManager.ViewModels
{
  public class TradingViewFolderDataViewModel : ViewModel
  {
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
    public ObservableCollection<TradingViewDataViewModel> Files { get; set; } = new ObservableCollection<TradingViewDataViewModel>();
  }

  public class TradingViewDataViewModel : ViewModel
  {
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
  }
}
