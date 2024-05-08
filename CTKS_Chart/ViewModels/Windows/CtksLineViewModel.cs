using CTKS_Chart.Trading;
using VCore.Standard;

namespace CTKS_Chart.ViewModels
{
  public class CtksLineViewModel : SelectableViewModel<CtksLine>
  {
    public CtksLineViewModel(CtksLine model) : base(model)
    {
    }

    #region IsVisible

    private bool isVisible;
    public bool IsVisible
    {
      get { return isVisible; }
      set
      {
        if (value != isVisible)
        {
          isVisible = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}