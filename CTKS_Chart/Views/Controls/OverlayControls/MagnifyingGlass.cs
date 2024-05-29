using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CTKS_Chart.Trading;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Views.Controls
{
  public class MagnifyingGlass : SelectableTool
  {
    public Brush Background { get; set; }
    public Border Tooltip { get; set; }
    public Chart Chart { get; set; }


    public override void Render(Point mousePoint, decimal representedPrice, DateTime representedDate, int assetPriceRound)
    {
      base.Render(mousePoint, representedPrice, representedDate, assetPriceRound);

      var brush = DrawingHelper.GetBrushFromHex("#45037ffc");
    
      if (StartPoint != null && EndPoint == null)
      {
        var startPoint = StartPoint.Value;

        var diffX = Math.Abs(startPoint.X - mousePoint.X);
        var diffY = Math.Abs(startPoint.Y - mousePoint.Y);

        if (startPoint.X > mousePoint.X)
        {
          Canvas.SetLeft(UIElement, mousePoint.X);
        }

        if (startPoint.Y > mousePoint.Y)
        {
          Canvas.SetTop(UIElement, mousePoint.Y);
        }


        Background = brush;
        UIElement.Background = Background;

        UIElement.Width = diffX;
        UIElement.Height = diffY;
      }
    }

    public override void OnEnd()
    {
      base.OnEnd();

      Chart.DrawingViewModel.EnableAutoLock = false;
      Chart.DrawingViewModel.SetLock(false);

      Chart.DrawingViewModel.SetMaxValue(Math.Max(StartPrice, EndPrice));
      Chart.DrawingViewModel.SetMinValue(Math.Min(StartPrice, EndPrice));

      Chart.DrawingViewModel.SetMaxUnix(Math.Max(StartDate.DateTimeToUnixSeconds(), EndDate.DateTimeToUnixSeconds()));
      Chart.DrawingViewModel.SetMinUnix(Math.Min(StartDate.DateTimeToUnixSeconds(), EndDate.DateTimeToUnixSeconds()));

      Chart.DrawingViewModel.RenderOverlay();

      IsEnabled = false;
      Chart.DrawingViewModel.EnableAutoLock = true;
    }
  }
}
