using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using VCore.WPF.Converters;

namespace CTKS_Chart.Converters
{
  public class LogScaleConverter : BaseConverter
  {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return Math.Log(double.Parse(value.ToString()));
    }

    public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return Math.Exp(double.Parse(value.ToString()));
    }
  }
}
