﻿using System;

namespace CTKS_Chart.Trading
{

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

    public int FileLineIndex { get; set; }
    public string FilePath { get; set; }

    public bool IsGreen
    {
      get { return Close >= Open; }
    }

    public long UnixTime { get; set; }
    public long UnixDiff { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }

    public TimeFrame TimeFrame { get; set; }

    public IndicatorData IndicatorData { get; set; }

    public decimal Gain
    {
      get
      {
        return (Close.Value - Open.Value) / Close.Value * 100;
      }
    }
  }
}