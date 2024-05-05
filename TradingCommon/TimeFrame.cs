using System.ComponentModel;

namespace CTKS_Chart.Trading
{
    public enum TimeFrame
  {
    [Description("Null")]
    Null = 8,
    [Description("12M")]
    M12 = 7,
    [Description("6M")]
    M6 = 6,
    [Description("3M")]
    M3 = 5,
    [Description("1M")]
    M1 = 4,
    [Description("2W")]
    W2 = 3,
    [Description("1W")]
    W1 = 2,
    [Description("1D")]
    D1 = 1,

    [Description("4H")]
    H4 = 240,
    [Description("12H")]
    H12 = 720,
  }
}
