using System.Collections.Generic;
using System.Windows.Controls;
using VCore.Standard;

namespace CTKS_Chart.Trading
{
  public class Layout : ViewModel
  {
    public string Title { get; set; }
    public IList<Candle> AllCandles { get; set; }

    public decimal MaxValue { get; set; }
    public decimal MinValue { get; set; }
    public TimeFrame TimeFrame { get; set; }

    public string DataLocation { get; set; }

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
  }

  public class CtksLayout : Layout
  {
    public Ctks Ctks { get; set; }
  }
}