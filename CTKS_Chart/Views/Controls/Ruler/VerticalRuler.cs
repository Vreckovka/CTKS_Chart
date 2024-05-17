using System;
using System.Collections.Generic;
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
      if (Visibility != Visibility.Visible && MaxValue > 0 && MinValue > 0)
      {
        return;
      }



      base.RenderValues();
      var fontSize = 10;
      var padding = 8;


      List<RenderedLabel> labelsToRender = new List<RenderedLabel>();

      if (ValuesToRender.Count > 0 && ValuesToRender.Count < 30)
      {
        var max = ValuesToRender
                 .Select(x => DrawingHelper.GetFormattedText($"*{Math.Round(x.Model.Value, AssetPriceRound)}*".ToString(),
                 x.SelectedBrush, fontSize))
                 .Max(x => x.Width);

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
            FontWeight = intersection.Model.IsCluster ? FontWeights.Bold : FontWeights.Normal,
            TextAlignment = TextAlignment.Center,
            Width = ActualWidth
          };

          pricePositionY = pricePositionY + (formattedText.Height / 2);


          var newff = new RenderedLabel()
          {
            TextBlock = price,
            Position = new Point(padding, Overlay.ActualHeight - pricePositionY),
          };

          labelsToRender.Add(newff);

        }
      }

      if ((ValuesToRender.Count <= 5 || ValuesToRender.Count >= 30) && Overlay.ActualHeight > 0)
      {
        var count = 12;

        var step = Overlay.ActualHeight / count;
        var actualStep = step;
        var brush = DrawingHelper.GetBrushFromHex("#45ffffff");

        for (int i = 0; i < count - 1; i++)
        {
          var yPrice = TradingHelper.GetValueFromCanvas(Overlay.ActualHeight, actualStep, MaxValue, MinValue);
          var y = Overlay.ActualHeight - TradingHelper.GetCanvasValue(Overlay.ActualHeight, yPrice, MaxValue, MinValue);

          var label = Math.Round(yPrice, AssetPriceRound).ToString();

          var formattedText = DrawingHelper.GetFormattedText(label, brush, fontSize);

          var price = new TextBlock()
          {
            Text = label,
            FontSize = fontSize,
            Foreground = brush,
            TextAlignment = TextAlignment.Center,
            Width = ActualWidth
          };

          var newff = new RenderedLabel() { TextBlock = price, Position = new Point(padding, y - (formattedText.Height / 2)), Order = i };

          labelsToRender.Add(newff);
          actualStep += step;
        }
      }

      labelsToRender = labelsToRender.Where(x => x.Position.Y > 0).ToList();

      var isIntersections = labelsToRender.All(x => x.Order == null);
      List<RenderedLabel> notFound = new List<RenderedLabel>();

      if (isIntersections)
      {
        notFound = Labels
         .Where(x => !labelsToRender.Any(y => y.TextBlock.Text == x.TextBlock.Text))
         .ToList();

        notFound.AddRange(Labels.Where(x => x.Order != null));
      }
      else
      {
        notFound = Labels
        .Where(y => y.Order == null && (y.Price > MaxValue || y.Price < MinValue))
        .ToList();
      }

      foreach (var label in notFound)
      {
        Overlay.Children.Remove(label.TextBlock);
        Labels.Remove(label);
      }

      foreach (var label in labelsToRender)
      {
        RenderedLabel existing = null;

        if (isIntersections)
          existing = Labels.FirstOrDefault(x => x.TextBlock.Text == label.TextBlock.Text);
        else if(label.Order != null && Labels.Count > label.Order.Value)
          existing = Labels[label.Order.Value];

        var x = label.Position.X;
        var y = label.Position.Y;

        if (existing == null)
        {
          existing = label;
          Overlay.Children.Add(existing.TextBlock);
          Labels.Add(existing);

          Canvas.SetTop(existing.TextBlock, y);
        }

        if (existing.Position.Y != y)
          Canvas.SetTop(existing.TextBlock, y);


        existing.Position = new Point(x, y);
        existing.TextBlock.Foreground = label.TextBlock.Foreground;
        existing.TextBlock.Text = label.TextBlock.Text;
        existing.TextBlock.Width = ActualWidth;
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
