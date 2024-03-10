using System;
using CTKS_Chart.Trading;
using VCore.WPF.Prompts;

namespace CTKS_Chart.ViewModels
{
  public class AssetSettingViewModel : BasePromptViewModel
  {
    public AssetSettingViewModel(Asset asset)
    {
      Asset = asset ?? throw new ArgumentNullException(nameof(asset));
    }

    public override string Title { get; set; } = "Asset Settings";

    public Asset Asset { get; }
  }
}