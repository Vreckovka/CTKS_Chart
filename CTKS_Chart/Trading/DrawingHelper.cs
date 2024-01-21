using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace CTKS_Chart.Trading
{
  public static  class DrawingHelper
  {
    #region GetFormattedText

    public static FormattedText GetFormattedText(string text, Brush brush, int fontSize = 12)
    {
      return new FormattedText(text, CultureInfo.GetCultureInfo("en-us"),
        FlowDirection.LeftToRight,
        new Typeface(new FontFamily("Arial").ToString()),
        fontSize, brush);
    }

    #endregion

    #region GetBrushFromHex

    public static SolidColorBrush GetBrushFromHex(string hex)
    {
      return (SolidColorBrush)new BrushConverter().ConvertFrom(hex);
    }

    #endregion

    #region GetPositionThickness

    public static double GetPositionThickness(TimeFrame timeFrame)
    {
      switch (timeFrame)
      {
        case TimeFrame.M12:
          return 8;
        case TimeFrame.M6:
          return 6;
        case TimeFrame.M3:
          return 4;
        case TimeFrame.M1:
          return 2;
        case TimeFrame.W2:
          return 1;
        default:
          return 0.5;
      }
    }

    #endregion

  }
}