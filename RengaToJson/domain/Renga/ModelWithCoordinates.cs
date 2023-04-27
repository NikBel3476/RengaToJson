using System;
using System.Collections.Generic;
using Renga;

namespace RengaToJson.domain.Renga;

public class ModelWithCoordinates
{
	public ModelWithCoordinates(Guid uuid, string name, List<FloatPoint3D> coordinates)
	{
		Uuid = uuid;
		Name = name;
		Coordinates = coordinates;
	}

	public Guid Uuid { get; set; }
	public string Name { get; set; }
	public List<FloatPoint3D> Coordinates { get; set; }
}