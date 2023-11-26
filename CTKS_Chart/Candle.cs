using System;


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
  public DateTime Time { get; set; }
}