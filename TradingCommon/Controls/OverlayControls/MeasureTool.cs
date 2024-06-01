using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Views.Controls
{

  public class MeasureTool : SelectableTool
  {
    public Brush Background { get; set; }
    public Border Tooltip { get; set; }

    public override void Render(Point mousePoint, decimal representedPrice, DateTime represnetedDate, int assetPriceRound)
    {
      var green = DrawingHelper.GetBrushFromHex("#45aaf542");
      var red = DrawingHelper.GetBrushFromHex("#45f54242");

      if (UIElement == null)
      {
        Tooltip = new Border() { Padding = new Thickness(5) };

        StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Vertical };

        var text = new TextBlock() { FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };

        text.Inlines.Add(new Run());
        text.Inlines.Add(new LineBreak());
        text.Inlines.Add(new Run());

        text.Inlines.Add(new LineBreak());
        text.Inlines.Add(new Run());

        stackPanel.Children.Add(text);

        Tooltip.Child = stackPanel;

        overlay.Children.Add(Tooltip);
      }

      base.Render(mousePoint, representedPrice, represnetedDate, assetPriceRound);

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
          Canvas.SetTop(Tooltip, mousePoint.Y - Tooltip.ActualHeight - 5);

          Background = green;
        }
        else
        {
          Canvas.SetTop(Tooltip, mousePoint.Y + 5);

          Background = red;
        }

        Canvas.SetLeft(Tooltip, Canvas.GetLeft(UIElement) + ((UIElement.ActualWidth / 2) - (Tooltip.ActualWidth / 2)));


        UIElement.Background = Background;
        Tooltip.Background = Background;

        if (Tooltip.Child is StackPanel stackPanel)
        {
          var text = (TextBlock)stackPanel.Children[0];

          ((Run)text.Inlines.ToList()[0]).Text = $"{StartPrice} - {representedPrice}";
          ((Run)text.Inlines.ToList()[2]).Text = $"{representedPrice - StartPrice} ({((StartPrice - representedPrice) / StartPrice * 100 * -1).ToString("N2")}%)";

          ((Run)text.Inlines.ToList()[4]).Text = $"{(represnetedDate - StartDate).ToString(@"dd\.hh\:mm\:ss")}";
        }

        UIElement.Width = diffX;
        UIElement.Height = diffY;
      }
    }

    public override void Clear()
    {
      base.Clear();
      overlay.Children.Remove(Tooltip);

      Tooltip = null;
    }  
  }
}
