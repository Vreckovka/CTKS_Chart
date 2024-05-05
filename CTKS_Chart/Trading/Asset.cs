using System;

namespace CTKS_Chart.Trading
{
  public class Asset
  {
    public string Symbol { get; set; }
    public int NativeRound { get; set; }
    public int PriceRound { get; set; }
    public decimal StartLowPrice { get; set; }
    public decimal StartMaxPrice { get; set; }
    public long StartMaxUnix { get; set; }
    public long StartMinUnix { get; set; }

    public TimeSpan RunTime { get; set; }
    public long RunTimeTicks { get; set; }

    public string DataPath { get; set; }
    public string DataSymbol { get; set; }
    public TimeFrame[] TimeFrames { get; set; }

    public string IndicatorDataPath { get; set; }
  }
}