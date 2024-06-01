using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Binance.Net.Enums;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using LiveCharts.Wpf.Charts.Base;
using Microsoft.Expression.Interactivity.Core;
using VCore.Standard.Helpers;
using VCore.WPF.ItemsCollections;
using PositionSide = CTKS_Chart.Strategy.PositionSide;

namespace CTKS_Chart.ViewModels
{
  public class StrategyDrawingViewModel<TPosition, TStrategy> : DrawingViewModel
    where TPosition : Position, new()
    where TStrategy : BaseStrategy<TPosition>
  {
    DashStyle pricesDashStyle = new DashStyle(new List<double>() { 2 }, 5);
    public decimal chartDiff = 0.01m;
    public StrategyDrawingViewModel(TradingBot<TPosition, TStrategy> tradingBot, Layout layout)
    {
      TradingBot = tradingBot;
      Layout = layout;

      DrawingSettings = new DrawingSettings();
      DrawingSettings.RenderLayout = () => Render();
    }

    #region Properties

    public Layout Layout { get; }
    public TradingBot<TPosition, TStrategy> TradingBot { get; }

    #endregion

    #region Methods

    #region RenderOverlay

    protected decimal? lastAth;
    public void RenderOverlay(decimal? athPrice = null, Candle actual = null)
    {
      if (athPrice != null)
      {
        lastAth = athPrice;
      }

      Render(actual);
    }


    #endregion

    #region OnRender

    public override void OnRender(DrawnChart newChart, DrawingContext dc, DrawingGroup dGroup, WriteableBitmap writeableBmp)
    {
      try
      {
        if (TradingBot.Strategy != null)
          RenderIntersections(dc, TradingBot.Strategy.Intersections);

        var chartCandles = newChart.Candles.ToList();
        var lastCandle = ActualCandles.LastOrDefault();

        DrawClosedPositions(writeableBmp, TradingBot.Strategy.AllClosedPositions, chartCandles, CanvasHeight);

        var maxCanvasValue = MaxValue;
        var minCanvasValue = MinValue;
        var chartDiff = (MaxValue - MinValue) * 0.03m;

        maxCanvasValue = MaxValue - chartDiff;
        minCanvasValue = MinValue + chartDiff;

        if (lastCandle != null)
        {
          var lastPrice = lastCandle.Close;

          DrawActualPrice(dc, lastCandle, CanvasHeight);
        }

        if (TradingBot.Strategy is StrategyViewModel<TPosition> strategyViewModel)
        {
          decimal price = strategyViewModel.AvrageBuyPrice;

          if (DrawingSettings.ShowAveragePrice)
          {
            DrawAveragePrice(dc, strategyViewModel.AvrageBuyPrice, CanvasHeight);
          }
          else
          {
            RenderedLabels.Remove(RenderedLabels.SingleOrDefault(x => x.Tag == "average_price"));
          }
        }

        if (DrawingSettings.ShowATH)
        {
          DrawPriceToATH(dc, lastAth, CanvasHeight);
        }
        else
        {
          RenderedLabels.Remove(RenderedLabels.SingleOrDefault(x => x.Tag == "ath_price"));
        }


        DrawMaxBuyPrice(dc, TradingBot.Strategy.MaxBuyPrice, CanvasHeight);
        DrawMinSellPrice(dc, TradingBot.Strategy.MinSellPrice, CanvasHeight);

        if (IsActualCandleVisible && EnableAutoLock)
        {
          lockChart = true;
          RaisePropertyChanged(nameof(LockChart));
        }
      }
      catch (Exception ex) { }
    }

    #endregion

    #region RenderIntersections

    public void RenderIntersections(DrawingContext dc, IEnumerable<CtksIntersection> intersections, TimeFrame? timeFrame = null)
    {
      if (timeFrame != null)
      {
        intersections = intersections.Where(x => x.TimeFrame == timeFrame);
      }


      var removed = RenderedIntersections.Where(x => !TradingBot.Strategy.Intersections.Any(y => y == x.Model)).ToList();
      removed.AddRange(RenderedIntersections.Where(x => x.Model.Cluster != null)
              .Where(x => !(x.Max > MinValue &&
                          x.Min < MaxValue)));

      removed.ForEach(x => RenderedIntersections.Remove(x));

      if (DrawingSettings.ShowClusters)
      {

        DrawClusters(dc, intersections,
                            CanvasHeight,
                            CanvasWidth,
                            TradingBot.Strategy.AllOpenedPositions.ToList());
      }

      removed.AddRange(RenderedIntersections.Where(x => x.Model.Cluster == null)
        .Where(x => x.Model.Value < MinValue || x.Model.Value > MaxValue));

      removed.ForEach(x => RenderedIntersections.Remove(x));

      if (lastFilledPosition != TradingBot.Strategy.AllClosedPositions.Max(x => x.FilledDate))
      {
        RenderedIntersections.Clear();
      }

      lastFilledPosition = TradingBot.Strategy.AllClosedPositions.Max(x => x.FilledDate);


      if (DrawingSettings.ShowIntersections)
      {
        DrawIntersections(dc, intersections,
                        CanvasHeight,
                        CanvasWidth,
                        TradingBot.Strategy.AllOpenedPositions.ToList());
      }
      else
      {
        RenderedIntersections.Clear();
      }

    }

    #endregion

    #region DrawIntersections

    public void DrawIntersections(
      DrawingContext drawingContext,
      IEnumerable<CtksIntersection> intersections,
      double canvasHeight,
      double canvasWidth,
      IList<TPosition> allPositions = null,
      TimeFrame minTimeframe = 0
      )
    {
      var diff = (MaxValue - MinValue) * chartDiff;

      var maxCanvasValue = MaxValue;
      var minCanvasValue = MinValue;

      var validIntersection = intersections
        .Where(x => x.Value > minCanvasValue && x.Value < maxCanvasValue && minTimeframe <= x.TimeFrame)
        .ToList();

      foreach (var intersection in validIntersection)
      {
        var selectedBrush = GetIntersectionBrush(allPositions, intersection);

        var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, MaxValue, MinValue);

        var frame = intersection.TimeFrame;

        var lineY = canvasHeight - actual;


        FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush.Item2);

        Pen pen = new Pen(selectedBrush.Item2, 1);
        pen.DashStyle = DashStyles.Dash;
        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));

        var rendered = RenderedIntersections.SingleOrDefault(x => x.Model == intersection);

        if (rendered == null)
          RenderedIntersections.Add(new RenderedIntesection(intersection) { SelectedHex = selectedBrush.Item1, Brush = selectedBrush.Item2 });
        else
        {
          rendered.SelectedHex = selectedBrush.Item1;
          var brush = selectedBrush.Item2.Clone();
          brush.Opacity = 100;

          rendered.Brush = brush;
        }
      }
    }

    #endregion

    #region DrawClosedPositions

    public void DrawClosedPositions(
      WriteableBitmap drawingContext,
      IEnumerable<Position> positions,
      IList<ChartCandle> candles,
      double canvasHeight)
    {
      var minDate = DateTimeHelper.UnixTimeStampToUtcDateTime(MinUnix);
      var maxDate = DateTimeHelper.UnixTimeStampToUtcDateTime(MaxUnix);

      positions = positions.Where(x => x.FilledDate > minDate && x.FilledDate < maxDate).ToList();

      if (!DrawingSettings.ShowAutoPositions)
      {
        positions = positions.Where(x => !x.IsAutomatic);
      }

      if (!DrawingSettings.ShowManualPositions)
      {
        positions = positions.Where(x => x.IsAutomatic);
      }


      foreach (var position in positions)
      {
        var isActiveBuy = position.Side == PositionSide.Buy && position.State == PositionState.Filled;
        string selectedBrush = "#ffffff";

        if (position.Side == PositionSide.Buy)
        {
          if (position.IsAutomatic)
          {
            selectedBrush = ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_BUY].Brush;
          }
          else
          {
            if (position.State == PositionState.Filled)
            {
              selectedBrush = ColorScheme.ColorSettings[ColorPurpose.ACTIVE_BUY].Brush;
            }
            else
            {
              selectedBrush = ColorScheme.ColorSettings[ColorPurpose.FILLED_BUY].Brush;
            }
          }
        }
        else
        {
          if (position.IsAutomatic)
          {
            selectedBrush = ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_SELL].Brush;
          }
          else
          {
            selectedBrush = ColorScheme.ColorSettings[ColorPurpose.FILLED_SELL].Brush;
          }
        }

        //Alpha channel present
        if (selectedBrush.Length == 9)
        {
          selectedBrush = $"#aa{selectedBrush.Substring(3, selectedBrush.Length - 3)}";
        }

        var selectedColor = DrawingHelper.GetColorFromHex(selectedBrush);

        var actual = TradingHelper.GetCanvasValue(canvasHeight, position.Price, MaxValue, MinValue);
        var frame = position.Intersection.TimeFrame;

        var positionY = canvasHeight - actual;
        var candle = candles.FirstOrDefault(x => x.Candle.OpenTime <= position.FilledDate && x.Candle.CloseTime >= position.FilledDate);

        if (candle != null)
        {
          var fontSize = isActiveBuy ? 16 : 7;

          if (position.IsAutomatic)
          {
            fontSize = (int)(fontSize / 1.33);
          }

          var positionX = candle.Body.X;

          var point = new Point(positionX, positionY);

          if (point.X > 0 && point.X < CanvasWidth && point.Y > 0 && point.Y < CanvasHeight)
          {
            int size = fontSize;
            int width = fontSize / 2;

            if (position.Side == PositionSide.Buy)
            {
              drawingContext.DrawTriangle(
               (int)positionX - width,
               (int)positionY + size,
               (int)positionX + width,
               (int)positionY + size,
               (int)positionX,
               (int)positionY,
               selectedColor
             );

              drawingContext.FillTriangle(
                (int)positionX - width,
                (int)positionY + size,
                (int)positionX + width,
                (int)positionY + size,
                (int)positionX,
                (int)positionY,
                selectedColor
             );
            }
            else
            {
              drawingContext.DrawTriangle(
              (int)positionX - width,
              (int)positionY - size,
              (int)positionX + width,
              (int)positionY - size,
              (int)positionX,
              (int)positionY,
              selectedColor
            );

              drawingContext.FillTriangle(
                (int)positionX - size,
                (int)positionY - size,
                (int)positionX + size,
                (int)positionY - size,
                (int)positionX,
                (int)positionY,
                selectedColor
              );
            }
          }
        }
      }
    }

    #endregion

    #region DrawActualPrice

    public void DrawActualPrice(DrawingContext drawingContext, Candle lastCandle, double canvasHeight)
    {
      var brush = lastCandle.IsGreen ? ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush : ColorScheme.ColorSettings[ColorPurpose.RED].Brush;

      DrawPrice(drawingContext, lastCandle.Close, "actual_price", brush, canvasHeight);
    }

    #endregion

    #region DrawAveragePrice

    public void DrawAveragePrice(DrawingContext drawingContext, decimal price, double canvasHeight)
    {
      DrawPrice(drawingContext, price, "average_price", ColorScheme.ColorSettings[ColorPurpose.AVERAGE_BUY].Brush, canvasHeight);
    }

    #endregion

    #region DrawPriceToATH

    public void DrawPriceToATH(DrawingContext drawingContext, decimal? price, double canvasHeight)
    {
      DrawPrice(drawingContext, price, "ath_price", ColorScheme.ColorSettings[ColorPurpose.ATH].Brush, canvasHeight);
    }

    #endregion

    #region DrawMaxBuyPrice

    public void DrawMaxBuyPrice(DrawingContext drawingContext, decimal? price, double canvasHeight)
    {
      DrawPrice(drawingContext, price, "max_buy", ColorScheme.ColorSettings[ColorPurpose.MAX_BUY_PRICE].Brush, canvasHeight);
    }

    #endregion

    #region DrawMinSellPrice

    public void DrawMinSellPrice(DrawingContext drawingContext, decimal? price, double canvasHeight)
    {
      DrawPrice(drawingContext, price, "min_sell", ColorScheme.ColorSettings[ColorPurpose.MIN_SELL_PRICE].Brush, canvasHeight);
    }

    #endregion

    #region DrawClusters

    public void DrawClusters(
      DrawingContext drawingContext,
      IEnumerable<CtksIntersection> intersections,
      double canvasHeight,
      double canvasWidth,
      IList<TPosition> allPositions = null,
      TimeFrame minTimeframe = 0)
    {
      var diff = (MaxValue - MinValue) * chartDiff;

      var maxCanvasValue = MaxValue - diff;
      var minCanvasValue = MinValue + diff;

      var validIntersection = intersections
        .Where(x =>
                minTimeframe <= x.TimeFrame &&
                 x.Cluster != null &&
                 x.TimeFrame >= minTimeframe &&
                 x.Cluster.Intersections.Any()
                )
        .Select(x => new
        {
          minValue = x.Cluster.Intersections.Min(x => x.Value),
          maxValue = x.Cluster.Intersections.Max(x => x.Value),
          intersection = x
        })
        .Where(x => x.maxValue > minCanvasValue &&
                x.minValue < maxCanvasValue)
        .ToList();

      foreach (var actualIntersectionObject in validIntersection)
      {
        var intersection = actualIntersectionObject.intersection;

        var selectedBrush = GetIntersectionBrush(allPositions, intersection);
        var frame = intersection.TimeFrame;

        FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush.Item2);

        Pen pen = new Pen(selectedBrush.Item2, 1);
        pen.DashStyle = DashStyles.Dash;
        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        var maxValue = actualIntersectionObject.maxValue;
        var minValue = actualIntersectionObject.minValue;

        var max = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, maxValue, MaxValue, MinValue);
        var min = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, minValue, MaxValue, MinValue);

        var clusterRect = new Rect()
        {
          X = 0,
          Y = max,
          Height = min - max,
          Width = canvasWidth
        };


        if (canvasHeight < min)
        {
          clusterRect.Height = clusterRect.Height - (min - canvasHeight);
        }

        if (max < 0)
        {
          clusterRect.Y = 0;
          clusterRect.Height += max;
        }


        selectedBrush.Item2.Opacity = 0.20;
        drawingContext.DrawRectangle(selectedBrush.Item2, null, clusterRect);

        var rendered = RenderedIntersections.SingleOrDefault(x => x.Model == intersection);
        var clone = selectedBrush.Item2.Clone();

        clone.Opacity = 1;

        if (rendered == null)
          RenderedIntersections.Add(new RenderedIntesection(intersection)
          {
            SelectedHex = selectedBrush.Item1,
            Brush = clone,
            Min = minValue,
            Max =
            maxValue
          });
        else
        {
          rendered.SelectedHex = selectedBrush.Item1;
          var brush = selectedBrush.Item2.Clone();
          brush.Opacity = 100;

          rendered.Brush = brush;
        }


      }
    }

    #endregion

    #region GetIntersectionBrush

    private Tuple<string, Brush> GetIntersectionBrush(IList<TPosition> allPositions, CtksIntersection intersection)
    {
      string selectedHex = ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush;

      var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, MaxValue, MinValue);

      var lineY = canvasHeight - actual;

      if (!intersection.IsEnabled)
      {
        selectedHex = "#614c4c";
      }


      if (allPositions != null)
      {
        var positionsOnIntersesction = allPositions
        .Where(x => x.Intersection.IsSame(intersection))
        .ToList();

        var firstPositionsOnIntersesction = positionsOnIntersesction.FirstOrDefault();
        var isOnlyAuto = positionsOnIntersesction.All(x => x.IsAutomatic);
        var isCombined = positionsOnIntersesction.Any(x => x.IsAutomatic) && positionsOnIntersesction.Any(x => !x.IsAutomatic);

        if (firstPositionsOnIntersesction != null)
        {
          selectedHex =
            firstPositionsOnIntersesction.Side == PositionSide.Buy ?
                isOnlyAuto ? ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_BUY].Brush :
                isCombined ? ColorScheme.ColorSettings[ColorPurpose.COMBINED_BUY].Brush :
               ColorScheme.ColorSettings[ColorPurpose.BUY].Brush :

                isOnlyAuto ? ColorScheme.ColorSettings[ColorPurpose.AUTOMATIC_SELL].Brush :
                isCombined ? ColorScheme.ColorSettings[ColorPurpose.COMBINED_SELL].Brush :
                ColorScheme.ColorSettings[ColorPurpose.SELL].Brush;
        }
      }

      var selectedBrush = DrawingHelper.GetBrushFromHex(selectedHex);
      if (!intersection.IsEnabled)
      {
        selectedBrush.Opacity = 0.25;
      }

      return new Tuple<string, Brush>(selectedHex, selectedBrush);
    }

    #endregion

    #region DrawPrice

    public void DrawPrice(DrawingContext drawingContext, decimal? price, string tag, string brush, double canvasHeight)
    {
      if (price > 0 && price > MinValue && price < MaxValue)
      {
        var close = TradingHelper.GetCanvasValue(canvasHeight, price.Value, MaxValue, MinValue);
        var selectedBrush = DrawingHelper.GetBrushFromHex(brush);

        var lineY = canvasHeight - close;

        var pen = new Pen(selectedBrush, 1);

        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth, lineY));

        var text = price.Value.ToString($"N{TradingBot.Asset.PriceRound}");

        var existing = RenderedLabels.SingleOrDefault(x => x.Tag == tag);

        if (existing != null)
        {
          existing.Model = text;
          existing.SelectedHex = brush;
          existing.Brush = selectedBrush;
          existing.Price = price.Value;
        }
        else
        {
          RenderedLabels.Add(new DrawingRenderedLabel(text) { SelectedHex = brush, Price = price.Value, Tag = tag, Brush = selectedBrush });
        }
      }
      else
      {
        var existing = RenderedLabels.SingleOrDefault(x => x.Tag == tag);

        if (existing != null)
        {
          RenderedLabels.Remove(existing);
        }
      }
    }

    #endregion

    #endregion
  }
}