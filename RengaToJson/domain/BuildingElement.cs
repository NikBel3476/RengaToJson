using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RengaToJson.domain;

public class BuildingElement
{
	public BuildingElement(
		Guid uuid,
		Guid id,
		string name,
		double sizeZ,
		string sign,
		List<Coordinates> coordinates
	)
	{
		Uuid = uuid;
		Id = id;
		Name = name;
		SizeZ = sizeZ;
		Sign = sign;
		Coordinates = coordinates;
	}

	// [JsonPropertyName("@")]
	[JsonProperty("@")]
	private Guid Uuid { get; set; }

	private Guid Id { get; set; }
	private string Name { get; set; }
	private double SizeZ { get; set; }
	private string Sign { get; set; }

	// [JsonPropertyName("XY")]
	[JsonProperty("XY")]
	private List<Coordinates> Coordinates { get; set; }
}