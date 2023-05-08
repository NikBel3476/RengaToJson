using System;
using RengaToJson.domain.Renga;

namespace RengaToJson.Domain.Exceptions;

public class NotFoundRelationshipException : Exception
{
	public NotFoundRelationshipException(string message, ModelWithCoordinates model) : base(message)
	{
		Model = model;
	}

	public ModelWithCoordinates Model { get; set; }
}