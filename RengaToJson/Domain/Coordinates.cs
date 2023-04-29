using System.Collections.Generic;
using Newtonsoft.Json;

namespace RengaToJson.domain;

public class Coordinates
{
	public Coordinates(List<Point> points)
	{
		Points = points;
	}

	// [JsonPropertyName("points")]
	[JsonProperty("points")]
	public List<Point> Points { get; set; }
}