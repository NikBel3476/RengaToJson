using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RengaToJson.Domain.Json;

public class BuildingElement
{
	public BuildingElement(
		Guid uuid,
		Guid id,
		string name,
		double sizeZ,
		string sign,
		List<Coordinates> coordinates,
		List<Guid> outputs
	)
	{
		Uuid = uuid;
		Id = id;
		Name = name;
		SizeZ = sizeZ;
		Sign = sign;
		Coordinates = coordinates;
		Outputs = outputs;
	}

	// [JsonPropertyName("@")]
	[JsonProperty("@")]
	public Guid Uuid { get; set; }
	public Guid Id { get; set; }
	public string Name { get; set; }
	public double SizeZ { get; set; }
	public string Sign { get; set; }
	// [JsonPropertyName("XY")]
	[JsonProperty("XY")]
	public List<Coordinates> Coordinates { get; set; }
	[JsonProperty("Output")]
	public List<Guid> Outputs { get; set; }
}