using Newtonsoft.Json;

namespace RengaToJson.domain;

public class Point
{
	public Point(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	[JsonProperty("x")]
	public double X { get; set; }
	[JsonProperty("y")]
	public double Y { get; set; }
	[JsonProperty("z")]
	public double Z { get; set; }
}