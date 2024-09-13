using System;
using System.Collections.Generic;

namespace CloudComputing.Domains
{

  public class ClientData
  {
    public IList<RunData> GenomeData { get; set; } = new List<RunData>();
    public decimal Average { get; set; }
    public string Symbol { get; set; }
  }

  public class RunData
  {
    public uint BuyGenomeId { get; set; }
    public uint SellGenomeId { get; set; }

    public decimal Drawdawn { get; set; } 
    public decimal Fitness { get; set; }
    public decimal OriginalFitness { get; set; }
    public decimal NumberOfTrades { get; set; }
    public decimal TotalValue { get; set; } 
  }
}
