using System.Runtime.InteropServices;

namespace Dbscan
{

  /// <summary>
  /// A point on the 2-d plane.
  /// </summary>
  /// <param name="X">The x-coordinate of the point.</param>
  /// <param name="Y">The y-coordinate of the point.</param>

  public struct Point
  {
    public Point(decimal x, decimal y)
    {
      X = x;
      Y = y;
    }

    public decimal X { get; }
    public decimal Y { get; }

    public override string ToString() => $"({X}, {Y})";
  }
}