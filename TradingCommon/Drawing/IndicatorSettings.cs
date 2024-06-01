using CTKS_Chart.Trading;
using VCore.Standard;

namespace CTKS_Chart.ViewModels
{
  public class IndicatorSettings : ViewModel
  {
    #region Show

    private bool show = true;

    public bool Show
    {
      get { return show; }
      set
      {
        if (value != show)
        {
          show = value;
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

    #region IndicatorType

    private IndicatorType indicatorType;

    public IndicatorType IndicatorType
    {
      get { return indicatorType; }
      set
      {
        if (value != indicatorType)
        {
          indicatorType = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}