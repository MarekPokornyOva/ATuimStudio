namespace Avalonia
{
	public static class PointExtensions
	{
		extension (Point point)
		{
			public double Length
			{
				get
				{
					point.Deconstruct(out double x, out double y);
					return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
				}
			}
		}
	}
}
