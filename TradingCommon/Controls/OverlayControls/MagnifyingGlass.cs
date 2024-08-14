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

    public long OriginalMaxUnix { get; set; }
    public long OriginalMinUnix { get; set; }
    public decimal OriginalMaxValue { get; set; }
    public decimal OriginalMinValue { get; set; }

    public override void OnEnd()
    {
      base.OnEnd();

      OriginalMaxUnix = Chart.DrawingViewModel.MaxUnix;
      OriginalMinUnix = Chart.DrawingViewModel.MinUnix;
      OriginalMaxValue = Chart.DrawingViewModel.MaxValue;
      OriginalMinValue = Chart.DrawingViewModel.MinValue;

      Chart.DrawingViewModel.EnableAutoLock = false;
      Chart.DrawingViewModel.SetLock(false);

      Chart.DrawingViewModel.SetMaxValue(Math.Max(StartPrice, EndPrice));
      Chart.DrawingViewModel.SetMinValue(Math.Min(StartPrice, EndPrice));

      Chart.DrawingViewModel.SetMaxUnix(Math.Max(StartDate.DateTimeToUnixSeconds(), EndDate.DateTimeToUnixSeconds()));
      Chart.DrawingViewModel.SetMinUnix(Math.Min(StartDate.DateTimeToUnixSeconds(), EndDate.DateTimeToUnixSeconds()));

      Chart.DrawingViewModel.Render();

      IsEnabled = false;
      Chart.DrawingViewModel.EnableAutoLock = true;
    }
  }
}
