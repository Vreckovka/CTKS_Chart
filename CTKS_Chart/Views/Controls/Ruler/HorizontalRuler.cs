using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CTKS_Chart.Trading;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Views.Controls
{
  public class HorizontalRuler : Ruler
  {
    TextBlock dateTextBlock;

    private void HorizontalRuler_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      throw new NotImplementedException();
    }

    public override RulerMode Mode => RulerMode.Horizontal;

    protected override void FrameworkElement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      Overlay.Width = e.NewSize.Width;

      base.FrameworkElement_SizeChanged(sender, e);
    }

    protected override void RenderValues()
    {
      if (Visibility != Visibility.Visible || MaxValue == 0 || MinValue == 0)
      {
        return;
      }

      base.RenderValues();
      var fontSize = 10;

      var diff = (long)(MaxValue - MinValue);
      var count = 9;

      var step = diff / count;
      long actualStep = (long)MinValue + (step / 2);


      for (int i = 0; i < count; i++)
      {
        var utcDate = DateTimeHelper.UnixTimeStampToUtcDateTime(actualStep);
        var x = TradingHelper.GetCanvasValueLinear(Overlay.ActualWidth, actualStep, (long)MaxValue, (long)MinValue);

        string label;

        //Month
        if (step >= 2628000)
        {
          label = utcDate.ToString("MM");
        }
        else if (step > 350000)
        {
          label = utcDate.ToString("dd.MM");
        }
        else if (step > 50000)
        {
          label = utcDate.ToString("dd.MM HH");
        }
        else if (step > 35000)
        {
          label = utcDate.ToString("dd HH:mm");
        }
        else
        {
          label = utcDate.ToString("HH:mm:ss");
        }

        var brush = DrawingHelper.GetBrushFromHex("#45ffffff");
        var formattedText = DrawingHelper.GetFormattedText(label, brush, fontSize);

        var dateText = new TextBlock()
        {
          Text = label,
          FontSize = fontSize,
          Foreground = brush,
          VerticalAlignment = VerticalAlignment.Center,
          TextAlignment = TextAlignment.Center
        };


        x = x - (formattedText.Width / 2);
        var y = Overlay.ActualHeight - ((Overlay.ActualHeight / 2) + 5);

        var newPoint = new Point(x, y);
        RenderedLabel existingLabel = Labels.SingleOrDefault(x => x.Order == i);

        if (existingLabel == null)
        {
          var border = new Border();
          border.Child = dateText;
          border.Height = ActualHeight;

          Overlay.Children.Add(border);

          var newLabel = new RenderedLabel()
          {
            Border = border,
            Position = newPoint,
            TextBlock = dateText,
            Order = i
          };

          existingLabel = newLabel;

          Labels.Add(existingLabel);
        }
       

        if (existingLabel.Position.X != x)
          Canvas.SetLeft(existingLabel.Border, x);


        existingLabel.Position = newPoint;
        existingLabel.TextBlock.Text = dateText.Text;
  
        actualStep += step;
      }
    }

    #region RenderLabel

    public override void RenderLabel(Point mousePoint, decimal price, DateTime date, int assetPriceRound)
    {
      var fontSize = 11;
      var brush = Brushes.White;
      var dateText = date.ToString("dd.MM.yyy HH:mm:ss");

      if (labelBorder == null)
      {
        var formattedTextDate = DrawingHelper.GetFormattedText(dateText, brush, fontSize);
        dateTextBlock = new TextBlock()
        {
          Text = dateText,
          FontSize = fontSize,
          Foreground = brush,
          FontWeight = FontWeights.Bold,
          TextAlignment = TextAlignment.Center,
          VerticalAlignment = VerticalAlignment.Center
        };

        labelBorder = new Border()
        {
          Background = DrawingHelper.GetBrushFromHex("#3b3d40"),
          Padding = new Thickness(4, 0, 4, 0),
          CornerRadius = new CornerRadius(2, 2, 2, 2),
          IsHitTestVisible = false,
        };

        Panel.SetZIndex(labelBorder, 100);

        labelBorder.Child = dateTextBlock;

        Overlay.Children.Add(labelBorder);
        Canvas.SetLeft(labelBorder, mousePoint.X - (formattedTextDate.Width / 2));
      }
      else
      {
        dateTextBlock.Text = dateText;
        Canvas.SetLeft(labelBorder, mousePoint.X - (dateTextBlock.ActualWidth / 2));
      }

      labelBorder.Height = Overlay.ActualHeight;
    }

    #endregion

    public override void ClearLabel()
    {
      Overlay.Children.Remove(labelBorder);

      labelBorder = null;
      dateTextBlock = null;
    }
  }
}
