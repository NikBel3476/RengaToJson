using System.Collections.Generic;
using Newtonsoft.Json;

namespace RengaToJson.domain;

public class Level
{
	// [JsonPropertyName("BuildElement")]
	[JsonProperty("BuildElement")]
	private List<BuildingElement> BuildingElements;

	// [JsonPropertyName("NameLevel")]
	[JsonProperty("NameLevel")]
	private string Name;

	private double ZLevel;

	public Level(string name, double zLevel, List<BuildingElement> buildingElements)
	{
		Name = name;
		ZLevel = zLevel;
		BuildingElements = buildingElements;
	}
}