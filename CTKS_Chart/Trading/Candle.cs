using System;
using System.Windows;

namespace CTKS_Chart.Trading
{
  public class ChartCandle 
  {
    public Candle Candle { get; set; }
    public Rect Body { get; set; }
    public Rect TopWick { get; set; }
    public Rect BottomWick { get; set; }

  }

  public class Candle
  {
    public static int ID = 0;
    public Candle()
    {
      Id = ID++;
    }

    public int Id { get; set; }
    public decimal? Close { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }

    public bool IsGreen
    {
      get { return Close > Open; }
    }

    public float UnixTime { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }
  }
}