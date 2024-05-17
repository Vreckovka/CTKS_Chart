using System.Windows.Media;
using CTKS_Chart.Trading;
using VCore.Standard;

namespace CTKS_Chart.ViewModels
{
  public class RenderedItem<TViewModel> : ViewModel<TViewModel>
  {

    public RenderedItem(TViewModel model) : base(model)
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
  public class RenderedIntesection : RenderedItem<CtksIntersection>
  {
    public RenderedIntesection(CtksIntersection model) : base(model)
    {
    }
  }

  public class DrawingRenderedLabel : RenderedItem<string>
  {
    public DrawingRenderedLabel(string model) : base(model)
    {
    }

    #region Price

    private decimal price;

    public decimal Price
    {
      get { return price; }
      set
      {
        if (value != price)
        {
          price = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public string Tag { get; set; }
  }
}