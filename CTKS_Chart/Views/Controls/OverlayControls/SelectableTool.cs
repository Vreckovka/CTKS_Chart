using System;
using System.Windows;
using System.Windows.Controls;

namespace CTKS_Chart.Views.Controls
{
  public abstract class SelectableTool : OverlayControl
  {
    public Point? StartPoint { get; set; }
    public Point? EndPoint { get; set; }
    public decimal StartPrice { get; set; }
    public DateTime StartDate { get; set; }

    public decimal EndPrice { get; set; }
    public DateTime EndDate { get; set; }

    public override void Render(Point mousePoint, decimal representedPrice, DateTime representedDate, int assetPriceRound)
    {
      if (UIElement == null)
      {
        StartPoint = mousePoint;
        StartPrice = representedPrice;
        StartDate = representedDate;

        UIElement = new Border();

        Canvas.SetLeft(UIElement, mousePoint.X);
        Canvas.SetTop(UIElement, mousePoint.Y);

        overlay.Children.Add(UIElement);
      }
    }

    public override void OnMouseLeftClick(Point mousePoint, decimal representedPrice, DateTime representedDate)
    {
      if (IsEnabled)
      {
        if (IsVisible)
        {
          if (StartPoint != null && EndPoint == null)
          {
            EndPoint = mousePoint;
            EndPrice = representedPrice;
            EndDate = representedDate;

            OnEnd();
          }
          else if (StartPoint != null && EndPoint != null)
          {
            IsEnabled = false;
          }
        }
        else
        {
          IsVisible = true;
        }
      }
    }

    public virtual void OnEnd()
    {

    }

    public override void Clear()
    {
      base.Clear();

      StartPoint = null;
      EndPoint = null;
    }
  }
}
