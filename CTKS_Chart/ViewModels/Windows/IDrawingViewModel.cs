namespace CTKS_Chart.ViewModels
{
  public interface IDrawingViewModel
  {
    public decimal MaxValue { get; set; }
    public decimal MinValue { get; set; }

    public double CanvasHeight { get; set; }
    public double CanvasWidth { get; set; }
  }
}