public class Candle
{
  public double Close { get; set; }
  public double Open { get; set; }
  public double High { get; set; }
  public double Low { get; set; }

  public bool IsGreen
  {
    get { return Close > Open; }
  }
}