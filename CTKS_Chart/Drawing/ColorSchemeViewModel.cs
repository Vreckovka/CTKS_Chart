using System.Collections.Generic;

namespace CTKS_Chart.ViewModels
{
  public class ColorSchemeViewModel
  {
    public Dictionary<ColorPurpose, ColorSettingViewModel> ColorSettings { get; set; } = new Dictionary<ColorPurpose, ColorSettingViewModel>();
  }
}