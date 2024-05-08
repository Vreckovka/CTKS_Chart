using KMeans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VCore.Standard.Helpers;
using Dbscan;
using Point = System.Windows.Point;
using DbscanImplementation;

namespace CTKS_Chart.Trading
{
  public class Ctks
  {
    private readonly CtksLayout layout;
    private readonly TimeFrame timeFrame;

    private double canvasHeight;
    private double canvasWidth;
    private readonly Asset asset;

    long minUnix;
    long maxUnix;
    long unixDiff;

    public Ctks(CtksLayout layout, TimeFrame timeFrame, double canvasHeight, double canvasWidth, Asset asset)
    {
      this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
      this.timeFrame = timeFrame;

      this.canvasHeight = canvasHeight;
      this.canvasWidth = canvasWidth;
      this.asset = asset ?? throw new ArgumentNullException(nameof(asset));


    }

    public List<CtksLine> ctksLines = new List<CtksLine>();
    public List<CtksIntersection> Intersections { get; private set; } = new List<CtksIntersection>();

    public bool IntersectionsVisible { get; set; }
    public IList<Candle> Candles { get; set; } = new List<Candle>();


    #region CreateLines

    public void CreateLines(IList<Candle> candles)
    {
      for (int i = 0; i < candles.Count - 2; i++)
      {
        var currentCandle = candles[i];
        var nextCandle = candles[i + 1];

        SetupLine(currentCandle, nextCandle);
      }
    }

    #endregion

    #region SetupLine

    private void SetupLine(Candle firstCandle, Candle secondCandle)
    {
      if (firstCandle.IsGreen)
      {
        if (secondCandle.IsGreen)
        {
          CreateLine(firstCandle, secondCandle, LineType.LeftTop, timeFrame);
          CreateLine(firstCandle, secondCandle, LineType.RightBottom, timeFrame);
        }
        else
        {
          if (firstCandle.Open < secondCandle.Close)
            CreateLine(firstCandle, secondCandle, LineType.RightBottom, timeFrame);
          else if (firstCandle.Open > secondCandle.Close)
            CreateLine(firstCandle, secondCandle, LineType.LeftBottom, timeFrame);
        }
      }
      else
      {
        if (!secondCandle.IsGreen)
        {
          CreateLine(firstCandle, secondCandle, LineType.RightTop, timeFrame);
          CreateLine(firstCandle, secondCandle, LineType.LeftBottom, timeFrame);
        }
        else
        {
          if (firstCandle.Open > secondCandle.Close)
            CreateLine(firstCandle, secondCandle, LineType.RightTop, timeFrame);
          else if (firstCandle.Open < secondCandle.Close)
            CreateLine(firstCandle, secondCandle, LineType.LeftTop, timeFrame);
        }
      }
    }

    #endregion

    #region CreateLine

    public CtksLine CreateLine(
      Candle firstCandle,
      Candle secondCandle,
      LineType lineType,
      TimeFrame timeFrame)
    {
      decimal price1 = 0;
      decimal price2 = 0;
      long unix1 = 0;
      long unix2 = 0;

      switch (lineType)
      {
        case LineType.LeftBottom:
        case LineType.RightBottom:
          price1 = firstCandle.IsGreen ? firstCandle.Open.Value : firstCandle.Close.Value;
          price2 = secondCandle.IsGreen ? secondCandle.Open.Value : secondCandle.Close.Value;
          break;
        case LineType.LeftTop:
        case LineType.RightTop:
          price1 = firstCandle.IsGreen ? firstCandle.Close.Value : firstCandle.Open.Value;
          price2 = secondCandle.IsGreen ? secondCandle.Close.Value : secondCandle.Open.Value;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(lineType), lineType, null);
      }

      //more than 2 to accomodate margin
      var unix_diff = (long)(unixDiff / 2.195);

      switch (lineType)
      {
        case LineType.LeftBottom:
        case LineType.LeftTop:
          unix1 = firstCandle.UnixTime - unix_diff;
          unix2 = secondCandle.UnixTime - unix_diff;
          break;
        case LineType.RightBottom:
        case LineType.RightTop:
          unix1 = firstCandle.UnixTime + unix_diff;
          unix2 = secondCandle.UnixTime + unix_diff;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(lineType), lineType, null);
      }

      var y1 = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, price1, layout.MaxValue, layout.MinValue);
      var y2 = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, price2, layout.MaxValue, layout.MinValue);

      var x1 = TradingHelper.GetCanvasValueLinear(canvasWidth, unix1, maxUnix, minUnix);
      var x2 = TradingHelper.GetCanvasValueLinear(canvasWidth, unix2, maxUnix, minUnix);

      var startPoint = new Point(x1, y1);
      var endPoint = new Point(x2, y2);

      var line = new CtksLine()
      {
        StartPoint = startPoint,
        EndPoint = endPoint,
        TimeFrame = timeFrame,
        LineType = lineType,
        FirstPoint = new CtksLinePoint()
        {
          Price = price1,
          UnixTime = unix1
        },
        SecondPoint = new CtksLinePoint()
        {
          Price = price2,
          UnixTime = unix2
        }
      };

      ctksLines.Add(line);

      return line;
    }

    #endregion

    #region AddIntersections

    public void AddIntersections()
    {
      var lastCandle = Candles.Last();
      var highestClose = Candles.Max(x => x.High);
      var lowestClose = Candles.Min(x => x.Low);

      var newCtksIntersections = new List<CtksIntersection>();

      foreach (var line in ctksLines)
      {
        var actualLeft = TradingHelper.GetCanvasValueLinear(canvasWidth, lastCandle.UnixTime, maxUnix, minUnix);
        var actual = TradingHelper.GetPointOnLine(line.StartPoint.X, line.StartPoint.Y, line.EndPoint.X, line.EndPoint.Y, actualLeft);
        var value = Math.Round(TradingHelper.GetValueFromCanvas(canvasHeight, canvasHeight - actual, layout.MaxValue, layout.MinValue), asset.PriceRound);


        if (value > lowestClose * (decimal)0.99 && value < highestClose * 100)
        {
          var intersection = new CtksIntersection()
          {
            Value = value,
            TimeFrame = line.TimeFrame,
            Line = line
          };

          newCtksIntersections.Add(intersection);
        }
      }

      Intersections = newCtksIntersections;
      Intersections.AddRange(CreateClusters(newCtksIntersections));
    }

    #endregion

    #region CrateCtks

    public void CrateCtks(IList<Candle> candles)
    {
      Candles = null;
      Intersections.Clear();
      ctksLines.Clear();

      if (candles.Count > 1)
      {
        minUnix = candles.First().UnixTime;
        maxUnix = candles.Last().UnixTime;
        unixDiff = candles[1].UnixTime - candles[0].UnixTime;
      }
      else
        return;

      Candles = candles;

      CreateLines(candles);
      AddIntersections();
    }

    #endregion

    //dbscan
    private IEnumerable<CtksIntersection> CreateClusters(IEnumerable<CtksIntersection> intersections)
    {
      var division = 5;
      var clusterIntersections = new List<CtksIntersection>();

      //if (intersections.Count() > division * 2)
      //{
      //  List<SimplePoint> points = intersections.Select(x => new SimplePoint(new decimal[] { 0, x.Value }) { Intersection = x }).ToList();
      //  KMeansClustering cl = new KMeansClustering(points.ToArray(), (int)(intersections.Count() / division));

      //  Cluster[] clusters = cl.Compute();

      //  clusterIntersections = clusters.Select(x => new CtksIntersection()
      //  {
      //    Value = Math.Round(x.Centroid.Components[1], asset.PriceRound),
      //    TimeFrame = timeFrame,
      //    Cluster = new CtksCluster()
      //    {
      //      Value = Math.Round(x.Centroid.Components[1], asset.PriceRound),
      //      Intersections = x.Points.Select(x => x.Intersection)
      //    }
      //  }).ToList();
      //}



      //199 165
      var clusterscc = Dbscan.Dbscan.CalculateClusters(
                        intersections.Select(x => new SimplePoint(0, x.Value)
                        {
                          Intersection = x
                        }),
                        epsilon: 0.0015m,
                        minimumPointsPerCluster: 2);

      foreach (var cluster in clusterscc.Clusters)
      {
        var intersectionValues = cluster.Objects.Select(y => y.Point.Y);
        var median = GetMedian(intersectionValues.ToArray());
        //var average = intersectionValues.Average();

        var value = Math.Round(median, asset.PriceRound);

        var newCluster = new CtksCluster()
        {
          Value = value,
          Intersections = cluster.Objects.Select(x => x.Intersection)
        };

        var ctksIntersection = new CtksIntersection()
        {
          TimeFrame = timeFrame,
          Value = value,
          Cluster = newCluster
        };

        clusterIntersections.Add(ctksIntersection);
      }

      return clusterIntersections.DistinctBy(x => x.Value);
    }


    #region GetMedian

    public static decimal GetMedian(decimal[] sourceNumbers)
    {
      //Framework 2.0 version of this method. there is an easier way in F4        
      if (sourceNumbers == null || sourceNumbers.Length == 0)
        throw new System.Exception("Median of empty array not defined.");

      //make sure the list is sorted, but use a new array
      decimal[] sortedPNumbers = (decimal[])sourceNumbers.Clone();
      Array.Sort(sortedPNumbers);

      //get the median
      int size = sortedPNumbers.Length;
      int mid = size / 2;
      decimal median = (size % 2 != 0) ? sortedPNumbers[mid] : (sortedPNumbers[mid] + sortedPNumbers[mid - 1]) / 2;
      return median;
    }

    #endregion

    public class SimplePoint : DataVec, IPointData
    {
      public SimplePoint(decimal[] data)
      {
        Components = data;
      }

      public SimplePoint(decimal x, decimal y) => Point = new Dbscan.Point(x, y);
      public Dbscan.Point Point { get; }

      public CtksIntersection Intersection { get; set; }
    }
  }
}


