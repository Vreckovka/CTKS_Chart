using System;
using System.Collections.Generic;

namespace CloudComputing.Domains
{
  public class ClientData
  {
    public IList<RunData> GenomeData { get; set; } = new List<RunData>();
    public decimal Average { get; set; }

    public string BuyGenomes { get; set; }
    public string SellGenomes { get; set; }
  }

  public class RunData
  {
    public string BuyGenome { get; set; }
    public string SellGenome { get; set; }

    public decimal Drawdawn { get; set; } 
    public decimal Fitness { get; set; }
    public decimal OriginalFitness { get; set; }
    public decimal NumberOfTrades { get; set; }
    public decimal TotalValue { get; set; } 
  }
}
