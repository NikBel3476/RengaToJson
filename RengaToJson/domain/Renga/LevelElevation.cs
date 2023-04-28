using System;
using System.Collections.Generic;

namespace RengaToJson.domain.Renga;

public class LevelElevation
{
	public LevelElevation(Guid uuid, string name, double elevation, List<ModelWithCoordinates> modelsWithCoordinates)
	{
		Uuid = uuid;
		Name = name;
		Elevation = elevation;
		ModelsWithCoordinates = modelsWithCoordinates;
	}

	public Guid Uuid { get; set; }
	public string Name { get; set; }
	public double Elevation { get; set; }
	public List<ModelWithCoordinates> ModelsWithCoordinates { get; set; }
}