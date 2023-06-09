﻿using System;
using System.Collections.Generic;
using Renga;

namespace RengaToJson.domain.Renga;

public class ModelWithCoordinates
{
	public ModelWithCoordinates(
		Guid uuid,
		string name,
		List<FloatPoint3D> coordinates,
		string sign,
		List<Guid> outputs
	)
	{
		Uuid = uuid;
		Name = name;
		Coordinates = coordinates;
		Sign = sign;
		Outputs = outputs;
	}

	public ModelWithCoordinates(
		int id,
		Guid uuid,
		string name,
		List<FloatPoint3D> coordinates,
		string sign
	)
	{
		Id = id;
		Uuid = uuid;
		Name = name;
		Coordinates = coordinates;
		Sign = sign;
		Outputs = new List<Guid>();
	}

	public int Id { get; set; }
	public Guid Uuid { get; set; }
	public string Name { get; set; }
	public List<FloatPoint3D> Coordinates { get; set; }
	public string Sign { get; set; }
	public List<Guid> Outputs { get; set; }
}