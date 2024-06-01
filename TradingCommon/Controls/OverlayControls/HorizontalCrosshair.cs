using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Views.Controls
{
  public class HorizontalCrosshair : OverlayControl
  {
    #region Render

    public override void Render(Point mousePoint, decimal representedPrice, DateTime representedDate, int assetPriceRound)
    {
      if (UIElement == null)
      {
        var gray = DrawingHelper.GetBrushFromHex("#45ffffff");
        UIElement = new Border();

        UIElement.Child = new Line()
        {
          X1 = 0,
          X2 = 0,
          Y1 = 0,
          Y2 = overlay.ActualHeight,
          Stroke = gray,
          StrokeThickness = 1,
          StrokeDashArray = new DoubleCollection() { 5 },
          IsHitTestVisible = false
        };

        Canvas.SetLeft(UIElement, mousePoint.X);

        overlay.Children.Add(UIElement);
      }
      else
      {
        Canvas.SetLeft(UIElement, mousePoint.X);
      }
    }

    #endregion
  }
}
