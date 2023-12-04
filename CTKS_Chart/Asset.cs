namespace CTKS_Chart
{
  public class Asset
  {
    public string Symbol { get; set; }
    public int NativeRound { get; set; }
    public int PriceRound { get; set; }

    public decimal StartLowPrice { get; set; }

    public decimal StartMaxPrice { get; set; }
  }
}