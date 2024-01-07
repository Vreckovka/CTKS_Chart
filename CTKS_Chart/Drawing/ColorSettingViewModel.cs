using System;
using VCore.Standard;

namespace CTKS_Chart.ViewModels
{
  public class ColorSettingViewModel : ViewModel<ColorSetting>
  {
    private readonly ColorSetting model;

    public ColorSettingViewModel(ColorSetting model) : base(model)
    {
      this.model = model ?? throw new ArgumentNullException(nameof(model));
    }

    #region Brush

    public string Brush
    {
      get { return model.Brush; }
      set
      {
        if (value != model.Brush)
        {
          model.Brush = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}