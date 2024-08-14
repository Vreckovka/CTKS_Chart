using System;

namespace CloudComputing.Domains
{

  public class RunData
  {
    public string BuyGenomes { get; set; }
    public string SellGenomes { get; set; }



    public decimal Average { get; set; }
    public decimal Drawdawn { get; set; } 
    public decimal Fitness { get; set; }
    public decimal OriginalFitness { get; set; }
    public decimal NumberOfTrades { get; set; }
    public decimal TotalValue { get; set; } 
  }
}
