namespace CloudComputing.Domains
{
  public class ServerRunData
  {
    public int Generation { get; set; }
    public string BuyGenomes { get; set; }
    public string SellGenomes { get; set; }

    public int AgentCount { get; set; }
    public int Minutes { get; set; }
    public double Split { get; set; }
    public bool IsRandom { get; set; }

    public string Symbol { get; set; }
  }
}
