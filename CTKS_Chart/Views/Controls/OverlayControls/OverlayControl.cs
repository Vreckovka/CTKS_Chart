using System;
using System.Windows;
using System.Windows.Controls;
using VCore.Standard;

namespace CTKS_Chart.Views.Controls
{
  public abstract class OverlayControl : ViewModel
  {
    public Canvas Overlay { get; set; }
    public Border UIElement { get; set; }
    public abstract void Render(
      Point mousePoint, 
      decimal representedPrice, 
      DateTime represnetedDate,
      int assetPriceRound);

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

          if (!value)
          {
            Clear();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsEnabled

    private bool isEnabled;

    public bool IsEnabled
    {
      get { return isEnabled; }
      set
      {
        if (value != isEnabled)
        {
          isEnabled = value;

          if (!value)
            IsVisible = false;

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Clear

    public virtual void Clear()
    {
      Overlay.Children.Remove(UIElement);
      UIElement = null;
      IsVisible = false;
    } 

    #endregion
  }
}
