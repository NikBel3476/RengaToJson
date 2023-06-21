using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Renga;
using Renga.GridTypes;
using RengaToJson.Domain.Exceptions;
using RengaToJson.Domain.Json;
using RengaToJson.domain.Renga;

namespace RengaToJson;

public class RengaToJsonPlugin : IPlugin
{
	private const string PluginName = "Export to JSON";
	private const string SaveFileDialogTitle = "Save json from renga file";
	private const string SaveFileDefaultPath = "";
	private const string SaveFileFilter = "Json files (*.json)|*.json";
	private Application app;
	private ActionEventSource events;

	public bool Initialize(string pluginFolder)
	{
		app = new Application();
		var ui = app.UI;
		var selection = app.Selection;
		var panelExtension = ui.CreateUIPanelExtension();
		var action = ui.CreateAction();
		action.ToolTip = PluginName;

		events = new ActionEventSource(action);
		events.Triggered += (s, e) =>
		{
			var filePath = ui.ShowSaveFileDialog(
				SaveFileDialogTitle,
				SaveFileDefaultPath,
				SaveFileFilter
			);

			if (filePath != "")
				try
				{
					var jsonString = TraverseModelObjects();
					File.WriteAllText(filePath, jsonString);
				}
				catch (NotFoundRelationshipException exception)
				{
					ui.ShowMessageBox(MessageIcon.MessageIcon_Info,
						PluginName,
						exception.Message
					);
					selection.SetSelectedObjects(new[] { exception.Model.Id });
				}
				catch (Exception exception)
				{
					ui.ShowMessageBox(MessageIcon.MessageIcon_Info,
						PluginName,
						exception.Message
					);
				}
		};
		panelExtension.AddToolButton(action);
		ui.AddExtensionToPrimaryPanel(panelExtension);

		return true;
	}

	public void Stop()
	{
		events.Dispose();
	}

	private string TraverseModelObjects()
	{
		var objects3D = app.Project.DataExporter.GetObjects3D();
		var modelObjectCollection = app.Project.Model.GetObjects();

		var modelsWithCoordinates = GetModelsWithCoordinates(objects3D, modelObjectCollection);

		// sort stairway points by Z coordinate
		modelsWithCoordinates.ForEach(modelWithCoordinates =>
		{
			if (modelWithCoordinates.Sign == "Staircase")
				modelWithCoordinates.Coordinates =
					modelWithCoordinates.Coordinates.OrderBy(coordinates => coordinates.Z).ToList();
		});

		// collect all building's levels
		var levelElevations = new List<LevelElevation>();
		for (var i = 0; i < modelObjectCollection.Count; i++)
		{
			var model = modelObjectCollection.GetByIndex(i);
			if (model.ObjectType == ObjectTypes.Level)
			{
				var level = (ILevel)model;
				levelElevations.Add(
					new LevelElevation(
						model.uniqueId,
						model.Name,
						Math.Round(level.Elevation),
						new List<ModelWithCoordinates>()
					)
				);
			}
		}

		var sortedLevelsByElevation = levelElevations.OrderBy(level => level.Elevation).ToList();
		// add highest level
		var highestLevel = new LevelElevation(
			Guid.NewGuid(),
			"highest level",
			double.PositiveInfinity,
			new List<ModelWithCoordinates>()
		);
		sortedLevelsByElevation.Add(highestLevel);

		// split models by levels
		for (var i = 0; i < sortedLevelsByElevation.Count - 1; i++)
		{
			var downsideLevel = sortedLevelsByElevation[i];
			var upsideLevel = sortedLevelsByElevation[i + 1];
			// TODO: create special case for stairways
			foreach (var model in modelsWithCoordinates)
				if (
					downsideLevel.Elevation <= model.Coordinates[0].Z &&
					model.Coordinates[0].Z < upsideLevel.Elevation
				)
				{
					if (model.Sign == "DoorWayInt")
						model.Coordinates = model.Coordinates.Select(coordinates =>
							{
								coordinates.Z = Convert.ToSingle(downsideLevel.Elevation);
								return coordinates;
							})
							.ToList();

					downsideLevel.ModelsWithCoordinates.Add(model);
				}
		}

		sortedLevelsByElevation.Remove(highestLevel);

		// add relationships between rooms and doors
		LevelElevation? previousLevel = null;
		foreach (var level in sortedLevelsByElevation)
		{
			foreach (var door in level.ModelsWithCoordinates)
				if (door.Sign == "DoorWayInt")
					foreach (var doorPoint in door.Coordinates)
					{
						foreach (var doorRelationModel in level.ModelsWithCoordinates)
							if (doorRelationModel.Sign == "Room")
								for (var i = 0; i < doorRelationModel.Coordinates.Count - 1; i++)
								{
									var lineSegment = new LineSegment(
										doorRelationModel.Coordinates[i],
										doorRelationModel.Coordinates[i + 1]
									);
									if (
										IsPointOnTheLineSegment(doorPoint, lineSegment) &&
										!door.Outputs.Contains(doorRelationModel.Uuid) &&
										!doorRelationModel.Outputs.Contains(door.Uuid)
									)
									{
										doorRelationModel.Outputs.Add(door.Uuid);
										door.Outputs.Add(doorRelationModel.Uuid);
									}
								}
							else if (doorRelationModel.Sign == "Staircase")
								for (var i = 0; i < doorRelationModel.Coordinates.Count - 1; i++)
								{
									var lineSegment = new LineSegment(
										doorRelationModel.Coordinates[i],
										doorRelationModel.Coordinates[i + 1]
									);
									if (
										NearlyEqual(doorPoint.Z, lineSegment.P1.Z, 0.1f) &&
										NearlyEqual(doorPoint.Z, lineSegment.P2.Z, 0.1f) &&
										IsPointOnTheLineSegment(doorPoint, lineSegment) &&
										!door.Outputs.Contains(doorRelationModel.Uuid) &&
										!doorRelationModel.Outputs.Contains(door.Uuid)
									)
									{
										doorRelationModel.Outputs.Add(door.Uuid);
										door.Outputs.Add(doorRelationModel.Uuid);
									}
								}

						// link stairs from previous level to doors
						previousLevel?.ModelsWithCoordinates.ForEach(modelWithCoordinates =>
						{
							if (modelWithCoordinates.Sign == "Staircase")
								for (var i = 0; i < modelWithCoordinates.Coordinates.Count - 1; i++)
								{
									var lineSegment = new LineSegment(
										modelWithCoordinates.Coordinates[i],
										modelWithCoordinates.Coordinates[i + 1]
									);
									if (
										NearlyEqual(doorPoint.Z, lineSegment.P1.Z, 0.1f) &&
										NearlyEqual(doorPoint.Z, lineSegment.P2.Z, 0.1f) &&
										IsPointOnTheLineSegment(doorPoint, lineSegment) &&
										!door.Outputs.Contains(modelWithCoordinates.Uuid) &&
										!modelWithCoordinates.Outputs.Contains(door.Uuid)
									)
									{
										modelWithCoordinates.Outputs.Add(door.Uuid);
										door.Outputs.Add(modelWithCoordinates.Uuid);
									}
								}
						});
					}

			previousLevel = level;
		}

		// add relationships between stairs and doors
		// foreach (var level in sortedLevelsByElevation)
		// foreach (var stair in level.ModelsWithCoordinates)
		// 	if (stair.Sign == "Staircase")
		// 	{
		// 		var maxByZPoint = stair.Coordinates.Aggregate((maxZCoordinate, coordinate) =>
		// 			maxZCoordinate.Z < coordinate.Z ? coordinate : maxZCoordinate);
		// 		// var minByZPoint = stair.Coordinates.Aggregate((minByZCoordinate, coordinate) =>
		// 		// 	minByZCoordinate.Z > coordinate.Z ? coordinate : minByZCoordinate);
		//
		// 		// var pointsWithMinZ = stair.Coordinates.Where(coordinate => coordinate.Z == minByZPoint.Z).ToList();
		// 		var pointsWithMaxZ = stair.Coordinates.Where(coordinate => coordinate.Z == maxByZPoint.Z).ToList();
		// 		pointsWithMaxZ.Add(pointsWithMaxZ[0]);
		//
		// 		for (var i = 0; i < pointsWithMaxZ.Count - 1; i++)
		// 		{
		// 			var lineSegment = new LineSegment(
		// 				pointsWithMaxZ[i],
		// 				pointsWithMaxZ[i + 1]
		// 			);
		// 			
		// 			
		// 		}
		// 	}

		// find output doors
		foreach (var level in sortedLevelsByElevation)
		foreach (var model in level.ModelsWithCoordinates)
			if (model.Sign == "DoorWayInt")
				if (model.Outputs.Count < 1 || model.Outputs.Count > 2)
					throw new NotFoundRelationshipException(
						$"\n{model.Name}\n{model.Uuid}\nhave {model.Outputs.Count} outputs",
						model
					);
				else if (model.Outputs.Count == 1) model.Sign = "DoorWayOut";

		// create models to export json
		var levels = new List<Level>();
		foreach (var levelElevation in sortedLevelsByElevation)
		{
			var buildingElements = new List<BuildingElement>();
			foreach (var model in levelElevation.ModelsWithCoordinates)
			{
				var points = new List<Point>();
				foreach (var modelCoordinates in model.Coordinates)
					points.Add(
						new Point(
							modelCoordinates.X / 1000,
							modelCoordinates.Y / 1000, // FIXME: resolve coordinate system issues
							modelCoordinates.Z / 1000
						)
					);

				var coordinates = new List<Coordinates>();
				coordinates.Add(new Coordinates(points));
				buildingElements.Add(new BuildingElement(
					model.Uuid,
					model.Uuid,
					model.Name,
					0, // TODO: get sizeZ from renga
					model.Sign,
					coordinates,
					model.Outputs
				));
			}

			levels.Add(
				new Level(
					levelElevation.Name,
					levelElevation.Elevation,
					buildingElements
				)
			);
		}

		var buildingInfo = app.Project.BuildingInfo;
		var addressInfo = buildingInfo.GetAddress();

		var address = new Address(addressInfo.Town, "", "");
		var devs = new List<int>();
		var building = new Building(buildingInfo.Name, address, levels, devs);

		return JsonConvert.SerializeObject(building, Formatting.Indented);
	}

	private List<ModelWithCoordinates> GetModelsWithCoordinates(
		IExportedObject3DCollection objects3D,
		IModelObjectCollection modelObjectCollection)
	{
		var modelsWithCoordinates = new List<ModelWithCoordinates>();
		for (var i = 0; i < objects3D.Count; i++)
		{
			var object3D = objects3D.Get(i);
			var modelObject = modelObjectCollection.GetById(object3D.ModelObjectId);
			var objectType = modelObject.ObjectType;

			for (var meshIndex = 0; meshIndex < object3D.MeshCount; meshIndex++)
			{
				var mesh = object3D.GetMesh(meshIndex);
				for (var gridIndex = 0; gridIndex < mesh.GridCount; gridIndex++)
				{
					var grid = mesh.GetGrid(gridIndex);
					if (objectType == ObjectTypes.Room)
					{
						var vertexes = new List<FloatPoint3D>();
						if (grid.GridType == (int)Room.Floor)
						{
							for (var vertexIndex = 0; vertexIndex < grid.VertexCount; vertexIndex++)
								vertexes.Add(grid.GetVertex(vertexIndex));

							vertexes.Add(vertexes.First());
							if (modelsWithCoordinates.Find(model => model.Uuid.Equals(modelObject.uniqueId)) == null)
								modelsWithCoordinates.Add(
									new ModelWithCoordinates(
										modelObject.Id,
										modelObject.uniqueId,
										modelObject.Name,
										vertexes,
										"Room"
									)
								);
						}
					}
					else if (objectType == ObjectTypes.Door)
					{
						if (grid.GridType == (int)Door.Reveal)
						{
							var vertexes = new List<FloatPoint3D>();
							for (var vertexIndex = 0; vertexIndex < grid.VertexCount; vertexIndex++)
								vertexes.Add(grid.GetVertex(vertexIndex));

							vertexes.Add(vertexes.First());
							if (
								vertexes.All(x => NearlyEqual(x.Z, vertexes.First().Z, 0.01f))
								&& modelsWithCoordinates.Find(model => model.Uuid.Equals(modelObject.uniqueId)) == null
							)
								modelsWithCoordinates.Add(
									new ModelWithCoordinates(
										modelObject.Id,
										modelObject.uniqueId,
										modelObject.Name,
										vertexes,
										"DoorWayInt"
									)
								);
						}
					}
					else if (objectType == ObjectTypes.Stair)
					{
						if (grid.GridType == (int)Stairway.Top)
						{
							var vertexes = new List<FloatPoint3D>();
							for (var vertexIndex = 0; vertexIndex < grid.VertexCount; vertexIndex++)
								vertexes.Add(grid.GetVertex(vertexIndex));

							var existingStairway =
								modelsWithCoordinates.Find(model => model.Uuid.Equals(modelObject.uniqueId));

							if (existingStairway != null)
								vertexes.ForEach(vertex =>
								{
									if (!existingStairway.Coordinates.Contains(vertex))
										existingStairway.Coordinates.Add(vertex);
								});
							// existingStairway.Coordinates.AddRange(vertexes);	
							else
								modelsWithCoordinates.Add(
									new ModelWithCoordinates(
										modelObject.Id,
										modelObject.uniqueId,
										modelObject.Name,
										vertexes,
										"Staircase"
									)
								);
						}
					}
				}
			}
		}

		return modelsWithCoordinates;
	}

	private bool IsPointOnTheLineSegment(FloatPoint3D point, LineSegment lineSegment)
	{
		var a = lineSegment.P1;
		var b = point;
		var c = lineSegment.P2;

		// bypass float point number precision
		var delta = 0.1;

		return NearlyEqual(Distance(a, b) + Distance(b, c), Distance(a, c), 0.01f) &&
		       Math.Min(a.X, c.X) - delta <= b.X && b.X <= Math.Max(a.X, c.X) + delta &&
		       Math.Min(a.Y, c.Y) - delta <= b.Y && b.Y <= Math.Max(a.Y, c.Y) + delta;

		// TODO: check that formula
		// return IsPointOnTheLine(point, lineSegment) &&
		//        Math.Min(a.X, c.X) - delta <= b.X && b.X <= Math.Max(a.X, c.X) + delta &&
		//        Math.Min(a.Y, c.Y) - delta <= b.Y && b.Y <= Math.Max(a.Y, c.Y) + delta;

		// var dxc = b.X - a.X;
		// var dyc = b.Y - a.Y;
		//
		// var dxl = c.X - a.X;
		// var dyl = c.Y - a.Y;
		//
		// var cross = dxc * dyl - dyc * dxl;
		//
		// if (!NearlyEqual(cross, 0f, 0.01f))
		// 	return false;
		//
		// if (Math.Abs(dxl) >= Math.Abs(dyl))
		// 	return dxl > 0 ? a.X <= b.X && b.X <= c.X : c.X <= b.X && b.X <= a.X;
		// return dyl > 0 ? a.Y <= b.Y && b.Y <= c.Y : c.Y <= b.Y && b.Y <= a.Y;
	}

	private bool IsPointOnTheLine(FloatPoint3D point, LineSegment lineSegment)
	{
		var a = lineSegment.P1;
		var b = point;
		var c = lineSegment.P2;
		var result = a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y);

		return NearlyEqual((a.X - c.X) * (a.Y - c.Y), (c.X - b.X) * (c.Y - b.Y), 0.1f);
	}

	private bool NearlyEqual(float a, float b, float epsilon)
	{
		var absA = Math.Abs(a);
		var absB = Math.Abs(b);
		var diff = Math.Abs(a - b);

		if (a == b) // shortcut, handles infinities
			return true;
		if (a == 0 || b == 0 || absA + absB < float.MinValue)
			// a or b is zero or both are extremely close to it
			// relative error is less meaningful here
			return diff < epsilon * float.MinValue;
		// use relative error
		return diff / (absA + absB) < epsilon;
	}

	// TODO: move to external class
	public static float Distance(FloatPoint3D p1, FloatPoint3D p2)
	{
		return Convert.ToSingle(Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
	}
}