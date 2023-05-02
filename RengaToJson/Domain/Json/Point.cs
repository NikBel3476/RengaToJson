using Newtonsoft.Json;
using Renga;

namespace RengaToJson.Domain.Json;

public class Point
{
	public Point(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Point(FloatPoint3D point3D)
	{
		X = point3D.X;
		Y = point3D.Y;
		Z = point3D.Z;
	}

	[JsonProperty("x")]
	public double X { get; set; }
	[JsonProperty("y")]
	public double Y { get; set; }
	[JsonProperty("z")]
	public double Z { get; set; }
}