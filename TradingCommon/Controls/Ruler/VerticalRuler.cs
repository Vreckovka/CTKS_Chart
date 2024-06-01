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

    #region RenderValues

    protected override void RenderValues()
    {
      if (Visibility != Visibility.Visible && MaxValue > 0 && MinValue > 0)
      {
        return;
      }

      base.RenderValues();
      var fontSize = 10;
      var padding = 8;
      int maxIntersections = 30;

      List<RenderedLabel> notFound = new List<RenderedLabel>();
      List<RenderedLabel> labelsToRender = new List<RenderedLabel>();

      if (ValuesToRender.Count > 0 && ValuesToRender.Count < maxIntersections)
      {
        foreach (var intersection in ValuesToRender)
        {
          padding = 8;

          var pricePositionY = TradingHelper.GetCanvasValue(Overlay.ActualHeight, intersection.Model.Value, MaxValue, MinValue);

          var text = Math.Round(intersection.Model.Value, AssetPriceRound).ToString();

          if (intersection.Model.IsCluster)
          {
            if (intersection.Model.Tag == CTKS_Chart.Trading.Tag.GlobalCluster)
            {
              text = $"G* {text} G*";
            }
            else
              text = $"*{text}*";
          }
          else if (intersection.Model.IntersectionType == IntersectionType.RangeFilter)
          {
            if (intersection.Model.Tag == CTKS_Chart.Trading.Tag.RangeFilterLow)
            {
              text = $"RFL {text}";
            }
            else if (intersection.Model.Tag == CTKS_Chart.Trading.Tag.RangeFilterHigh)
            {
              text = $"RFH {text}";
            }
            else
              text = $"RF {text}";
          }


          var formattedText = DrawingHelper.GetFormattedText(text, intersection.Brush, fontSize);

          var price = new TextBlock()
          {
            Text = text,
            FontSize = fontSize,
            Foreground = intersection.Brush,
            FontWeight = intersection.Model.IsCluster ? FontWeights.Bold : FontWeights.Normal,
            TextAlignment = TextAlignment.Center,
            Width = ActualWidth
          };

          pricePositionY = pricePositionY + (formattedText.Height / 2);


          var newff = new RenderedLabel()
          {
            TextBlock = price,
            Price = intersection.Model.Value,
            Position = new Point(padding, Overlay.ActualHeight - pricePositionY),
            Intersection = intersection.Model
          };

          labelsToRender.Add(newff);

        }
      }
      else
      {
        notFound.AddRange(Values.Where(y => y.Intersection != null).ToList());
      }

      if ((ValuesToRender.Count <= 5 || ValuesToRender.Count >= maxIntersections) && Overlay.ActualHeight > 0)
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

          var newff = new RenderedLabel() { TextBlock = price, Position = new Point(padding, y - (formattedText.Height / 2)), Order = i, Price = yPrice };

          labelsToRender.Add(newff);
          actualStep += step;
        }
      }
      else
      {
        notFound.AddRange(Values.Where(y => y.Intersection == null).ToList());
      }

      labelsToRender = labelsToRender.Where(x => x.Position.Y > 0).ToList();

      notFound.AddRange(Values.Where(y => y.Price > MaxValue || y.Price < MinValue).ToList());
      notFound.AddRange(Values.Where(y => !labelsToRender.Any(x => x.TextBlock.Text == y.TextBlock.Text)).ToList());

      foreach (var label in notFound)
      {
        Overlay.Children.Remove(label.TextBlock);
        Values.Remove(label);
      }

      foreach (var label in labelsToRender)
      {
        RenderedLabel existing = null;

        if (label.Intersection != null)
          existing = Values.FirstOrDefault(x => x.TextBlock.Text == label.TextBlock.Text);
        else if (label.Order != null && Values.Count > label.Order.Value)
          existing = Values[label.Order.Value];

        var x = label.Position.X;
        var y = label.Position.Y;

        if (existing == null)
        {
          existing = label;
          Overlay.Children.Add(existing.TextBlock);
          Values.Add(existing);

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

    #endregion

    #region RenderLabels

    protected override void RenderLabels()
    {
      var notFound = Labels.Where(y => y.Price > MaxValue || y.Price < MinValue).ToList();
      notFound.AddRange(Labels.Where(x => !LabelsToRender.Any(y => y.Tag == x.Label.Tag)));

      foreach (var label in notFound)
      {
        Overlay.Children.Remove(label.Border);
        Labels.Remove(label);
      }

      foreach (var label in LabelsToRender)
      {
        var priceText = label.Model;

        var fontSize = 11;
        var brush = Brushes.White;

        var existingLabel = Labels.FirstOrDefault(x => x.Label.Tag == label.Tag);
        var pricePositionY = Overlay.ActualHeight - TradingHelper.GetCanvasValue(Overlay.ActualHeight, label.Price, MaxValue, MinValue);

        if (existingLabel == null)
        {
          existingLabel = new RenderedLabel()
          {
            Price = label.Price,
            Label = label
          };

          existingLabel.Border = new Border()
          {
            Background = label.Brush,
            Padding = new Thickness(0, 2, 0, 2),
            CornerRadius = new CornerRadius(2, 2, 2, 2),
            IsHitTestVisible = false,
          };

          existingLabel.TextBlock = new TextBlock()
          {
            Text = priceText,
            FontSize = fontSize,
            Foreground = brush,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
          };

          var formattedText = DrawingHelper.GetFormattedText(priceText, brush, fontSize);
          existingLabel.Border.Child = existingLabel.TextBlock;

          Canvas.SetTop(existingLabel.Border, pricePositionY - ((formattedText.Height / 2) + existingLabel.Border.Padding.Bottom));

          Panel.SetZIndex(existingLabel.Border, 1);
          Overlay.Children.Add(existingLabel.Border);

          Labels.Add(existingLabel);

        }
        else
        {
          existingLabel.TextBlock.Text = priceText;
          existingLabel.Border.Background = label.Brush;

          Canvas.SetTop(existingLabel.Border, pricePositionY - (existingLabel.Border.ActualHeight / 2));
        }


        if (existingLabel.Label?.Tag == "actual_price")
        {
          Panel.SetZIndex(existingLabel.Border, 2);
        }

        existingLabel.Border.Width = ActualWidth;
      }
    }

    #endregion

    #region RenderLabel

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

    #endregion

    #region ClearLabel

    public override void ClearLabel()
    {
      Overlay.Children.Remove(labelBorder);

      labelBorder = null;
      priceTextBlock = null;
    }

    #endregion
  }
}
