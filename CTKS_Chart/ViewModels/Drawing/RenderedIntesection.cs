using System.Windows.Media;
using CTKS_Chart.Trading;
using VCore.Standard;

namespace CTKS_Chart.ViewModels
{
  public class RenderedIntesection : ViewModel<CtksIntersection>
  {
    public RenderedIntesection(CtksIntersection model) : base(model)
    {
    }

    #region SelectedBrush

    private Brush selectedBrush;

    public Brush SelectedBrush
    {
      get { return selectedBrush; }
      set
      {
        if (value != selectedBrush)
        {
          selectedBrush = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}