using System;

public class Candle
{
  public decimal Close { get; set; }
  public decimal Open { get; set; }
  public decimal High { get; set; }
  public decimal Low { get; set; }

  public bool IsGreen
  {
    get { return Close > Open; }
  }

  public float UnixTime { get; set; }
  public DateTime Time { get; set; }
}