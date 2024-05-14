using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CTKS_Chart.Trading;

namespace CTKS_Chart.Views.Controls
{
  public class VerticalRuler : Ruler
  {
    public override RulerMode Mode => RulerMode.Vertical;

    protected override void FrameworkElement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      Overlay.Height = e.NewSize.Height;

      base.FrameworkElement_SizeChanged(sender, e);
    }

    protected override void RenderValues()
    {
      base.RenderValues();
      var fontSize = 10;
      var padding = 8;

      if (ValuesToRender.Count > 0 && ValuesToRender.Count < 30)
      {
        var max = ValuesToRender
                 .Select(x => DrawingHelper.GetFormattedText($"*{Math.Round(x.Model.Value, AssetPriceRound)}*".ToString(),
                 x.SelectedBrush, fontSize))
                 .Max(x => x.Width);

        Width = max + (padding * 2);

        foreach (var intersection in ValuesToRender)
        {
          padding = 8;

          var pricePositionY = TradingHelper.GetCanvasValue(Overlay.ActualHeight, intersection.Model.Value, MaxValue, MinValue);

          var text = Math.Round(intersection.Model.Value, AssetPriceRound).ToString();

          if (intersection.Model.IsCluster)
          {
            text = $"*{text}*";
          }

          var formattedText = DrawingHelper.GetFormattedText(text, intersection.SelectedBrush, fontSize);

          var price = new TextBlock()
          {
            Text = text,
            FontSize = fontSize,
            Foreground = intersection.SelectedBrush,
            FontWeight = intersection.Model.IsCluster ? FontWeights.Bold : FontWeights.Normal
          };

          pricePositionY = pricePositionY + (formattedText.Height / 2);

          if (intersection.Model.IsCluster)
          {
            padding -= 5;
          }

          Overlay.Children.Add(price);
          Labels.Add(price);

          Canvas.SetLeft(price, padding);
          Canvas.SetTop(price, Overlay.ActualHeight - pricePositionY);
        }
      }

      if ((ValuesToRender.Count < 5 || ValuesToRender.Count >= 30) && Overlay.ActualHeight > 0)
      {
        var count = 12;

        var step = Overlay.ActualHeight / count;
        var actualStep = step;

        for (int i = 0; i < count - 1; i++)
        {
          var yPrice = TradingHelper.GetValueFromCanvas(Overlay.ActualHeight, actualStep, MaxValue, MinValue);
          var y = Overlay.ActualHeight - TradingHelper.GetCanvasValue(Overlay.ActualHeight, yPrice, MaxValue, MinValue);

          var brush = DrawingHelper.GetBrushFromHex("#45ffffff");
          var label = Math.Round(yPrice, AssetPriceRound).ToString();

          var formattedText = DrawingHelper.GetFormattedText(label, brush, fontSize);

          var price = new TextBlock()
          {
            Text = label,
            FontSize = fontSize,
            Foreground = brush,
          };

          Overlay.Children.Add(price);
          Labels.Add(price);

          Canvas.SetLeft(price, padding);
          Canvas.SetTop(price, y - (formattedText.Height / 2));

          actualStep += step;
        }
      }
    }

    TextBlock priceTextBlock;

    public override void RenderLabel(Point mousePoint, decimal price, DateTime dateTime, int assetPriceRound)
    {
      var priceText = Math.Round(price, assetPriceRound).ToString();

      var fontSize = 11;
      var brush = Brushes.White;

      if (labelBorder == null)
      {
        labelBorder = new Border()
        {
          Background = DrawingHelper.GetBrushFromHex("#3b3d40"),
          Padding = new Thickness(0, 2, 0, 2),
          CornerRadius = new CornerRadius(2, 2, 2, 2),
          IsHitTestVisible = false,
        };

        priceTextBlock = new TextBlock()
        {
          Text = priceText,
          FontSize = fontSize,
          Foreground = brush,
          FontWeight = FontWeights.Bold,
          TextAlignment = TextAlignment.Center
        };

        var formattedText = DrawingHelper.GetFormattedText(priceText, brush, fontSize);
        labelBorder.Child = priceTextBlock;

        Canvas.SetTop(labelBorder, mousePoint.Y - ((formattedText.Height / 2) +
          labelBorder.Padding.Bottom));

        Panel.SetZIndex(labelBorder, 100);
        Overlay.Children.Add(labelBorder);

      }
      else
      {
        priceTextBlock.Text = priceText;

        Canvas.SetTop(labelBorder, mousePoint.Y - (labelBorder.ActualHeight / 2));
      }

      labelBorder.Width = Overlay.ActualWidth;
    }

    public override void ClearLabel()
    {
      Overlay.Children.Remove(labelBorder);

      labelBorder = null;
      priceTextBlock = null;
    }
  }
}
