using System;
using System.Windows;
using System.Windows.Controls;
using VCore.Standard;

namespace CTKS_Chart.Views.Controls
{
  public abstract class OverlayControl : ViewModel
  {
    protected Canvas overlay;

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
      overlay.Children.Remove(UIElement);
      UIElement = null;
      IsVisible = false;
    }

    #endregion

    public Border UIElement { get; set; }

    public abstract void Render(
      Point mousePoint,
      decimal representedPrice,
      DateTime representedDate,
      int assetPriceRound);

    public virtual void OnMouseLeftClick(Point point, decimal representedPrice, DateTime representedDate)
    {
    }

    public void SetOverlay(Canvas overlay)
    {
      this.overlay = overlay;
    }
  }
}
