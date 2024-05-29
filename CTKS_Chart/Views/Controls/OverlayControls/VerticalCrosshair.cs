using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Views.Controls
{
  public class VerticalCrosshair : OverlayControl
  {
    public override void Render(
      Point mousePoint,
      decimal representedPrice,
      DateTime representedDate,
      int assetPriceRound)
    {

      if (UIElement != null)
      {
        Canvas.SetTop(UIElement, mousePoint.Y);
      }
      else
      {
        var gray = DrawingHelper.GetBrushFromHex("#45ffffff");

        UIElement = new Border();
        UIElement.Child = new Line()
        {
          X1 = 0,
          X2 = overlay.ActualWidth,
          Y1 = 0,
          Y2 = 0,
          Stroke = gray,
          StrokeThickness = 1,
          StrokeDashArray = new DoubleCollection() { 5 },
          IsHitTestVisible = false
        };

        Canvas.SetTop(UIElement, mousePoint.Y);
        overlay.Children.Add(UIElement);
      };
    }
  }
}
