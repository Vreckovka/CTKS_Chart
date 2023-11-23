using System.Windows.Controls;

namespace CTKS_Chart
{
  public class Layout
  {
    public string Title { get; set; }
    public Canvas Canvas { get; set; }
    public Ctks Ctks { get; set; }
    public double MaxValue { get; set; }
    public double MinValue { get; set; }
  }
}