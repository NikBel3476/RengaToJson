using System.Collections.Generic;
using Newtonsoft.Json;

namespace RengaToJson.domain;

public class Building
{
	public Building(string name, Address address, List<Level> levels, List<int> devs)
	{
		BuildingName = name;
		Address = address;
		Levels = levels;
		Devs = devs;
	}

	public List<int> Devs { get; set; }
	// [JsonPropertyName("NameBuilding")]
	[JsonProperty("NameBuilding")]
	public string BuildingName { get; set; }
	public Address Address { get; set; }
	// [JsonPropertyName("Level")]
	[JsonProperty("Level")]
	private List<Level> Levels { get; set; }
}