using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Renga;
using Renga.GridTypes;
using RengaToJson.Domain.Json;
using RengaToJson.domain.Renga;

namespace RengaToJson;

public class RengaToJsonPlugin : IPlugin
{
	private const string PluginName = "Export to JSON";
	private const string saveFileDialogTitle = "Save json from renga file";
	private const string saveFileDefaultPath = "";
	private const string saveFileFilter = "Json files (*.json)|*.json";
	private Application app;
	private ActionEventSource events;

	public bool Initialize(string pluginFolder)
	{
		app = new Application();
		var ui = app.UI;
		var panelExtension = ui.CreateUIPanelExtension();
		var action = ui.CreateAction();
		action.ToolTip = PluginName;

		events = new ActionEventSource(action);
		events.Triggered += (s, e) =>
		{
			var filePath = ui.ShowSaveFileDialog(
				saveFileDialogTitle,
				saveFileDefaultPath,
				saveFileFilter
			);

			if (filePath != "")
				try
				{
					var jsonString = TraverseModelObjects();
					File.WriteAllText(filePath, jsonString);
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

		var levelElevations = new List<LevelElevation>();
		for (var i = 0; i < modelObjectCollection.Count; i++)
		{
			var model = modelObjectCollection.GetByIndex(i);
			if (model.ObjectType == ObjectTypes.Level)
			{
				var level = (ILevel)model;
				levelElevations.Add(new LevelElevation(model.uniqueId, model.Name, Math.Round(level.Elevation),
					new List<ModelWithCoordinates>()));
			}
		}

		var sortedLevelElevations = levelElevations.OrderBy(level => level.Elevation).ToList();
		// add highest level
		var highestLevel =
			new LevelElevation(Guid.NewGuid(), "highest level", double.PositiveInfinity,
				new List<ModelWithCoordinates>());
		sortedLevelElevations.Add(highestLevel);

		for (var i = 0; i < sortedLevelElevations.Count - 1; i++)
		{
			var downsideLevel = sortedLevelElevations[i];
			var upsideLevel = sortedLevelElevations[i + 1];
			foreach (var model in modelsWithCoordinates)
				if (downsideLevel.Elevation <= model.Coordinates[0].Z &&
				    model.Coordinates[0].Z < upsideLevel.Elevation)
					downsideLevel.ModelsWithCoordinates.Add(model);
		}

		sortedLevelElevations.Remove(highestLevel);

		// add relationships
		foreach (var level in sortedLevelElevations)
		foreach (var doorRelationModel in level.ModelsWithCoordinates)
			if (doorRelationModel.Sign == "Room" || doorRelationModel.Sign == "Staircase")
				for (var i = 0; i < doorRelationModel.Coordinates.Count - 1; i++)
				{
					var lineSegment = new LineSegment(
						doorRelationModel.Coordinates[i],
						doorRelationModel.Coordinates[i + 1]
					);
					foreach (var door in level.ModelsWithCoordinates)
						if (door.Sign == "DoorWayInt")
							foreach (var doorPoint in door.Coordinates)
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

		// find output doors
		foreach (var level in sortedLevelElevations)
		foreach (var model in level.ModelsWithCoordinates)
			if (model.Sign == "DoorWayInt" && model.Outputs.Count == 1)
				model.Sign = "DoorWayOut";

		// create models to export json
		var levels = new List<Level>();
		foreach (var levelElevation in sortedLevelElevations)
		{
			var buildingElements = new List<BuildingElement>();
			foreach (var model in levelElevation.ModelsWithCoordinates)
			{
				var points = new List<Point>();
				foreach (var modelCoordinates in model.Coordinates)
					points.Add(
						new Point(
							modelCoordinates.X / 1000,
							-modelCoordinates.Y / 1000, // FIXME: resolve coordinate system issues
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
							modelsWithCoordinates.Add(
								new ModelWithCoordinates(
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
							if (vertexes.All(x => x.Z == vertexes.First().Z))
								modelsWithCoordinates.Add(
									new ModelWithCoordinates(
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
								existingStairway.Coordinates.AddRange(vertexes);
							else
								modelsWithCoordinates.Add(
									new ModelWithCoordinates(
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
		return IsPointOnTheLine(point, lineSegment) &&
		       Math.Min(a.X, c.X) <= b.X && b.X <= Math.Max(a.X, c.X) &&
		       Math.Min(a.Y, c.Y) <= b.Y && b.Y <= Math.Max(a.Y, c.Y);
	}

	private bool IsPointOnTheLine(FloatPoint3D point, LineSegment lineSegment)
	{
		var a = lineSegment.P1;
		var b = point;
		var c = lineSegment.P2;
		return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y) == 0;
	}
}