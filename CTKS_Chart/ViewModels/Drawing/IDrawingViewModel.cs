using CTKS_Chart.Trading;

namespace CTKS_Chart.ViewModels
{
  public interface IDrawingViewModel
  {
    public decimal MaxValue { get; set; }
    public decimal MinValue { get; set; }

    public long MaxUnix { get; set; }
    public long MinUnix { get; set; }

    public double CanvasHeight { get; set; }
    public double CanvasWidth { get; set; }

    public void RenderOverlay(decimal? athPrice = null, Candle actual = null);
    public void Raise(string name);

    public bool LockChart { get; set; }
    public bool EnableAutoLock { get; set; }

    public void SetMaxValue(decimal newValue);
    public void SetMinValue(decimal newValue);
    public void SetMaxUnix(long newValue);
    public void SetMinUnix(long newValue);
    public void SetLock(bool newValue);
  }
}