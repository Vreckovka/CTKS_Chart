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

    public Brush Brush { get; set; }

    #region SelectedHex

    private string selectedHex;

    public string SelectedHex
    {
      get { return selectedHex; }
      set
      {
        if (value != selectedHex)
        {
          selectedHex = value;
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

    public decimal Min { get; set; }
    public decimal Max { get; set; }
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