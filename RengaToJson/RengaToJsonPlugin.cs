using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Renga;
using Renga.GridTypes;
using RengaToJson.domain;
using RengaToJson.domain.Renga;

namespace RengaToJson;

public class RengaToJsonPlugin : IPlugin
{
	private const string PluginName = "Export to JSON";
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
				"Save json from renga file",
				"",
				"Json files (*.json)|*.json");

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

			// var textInMessageBox = filePath != "" ? TraverseModelObjects() : "No selected file";
			// ui.ShowMessageBox(MessageIcon.MessageIcon_Info,
			//     "Model object list",
			//     filePath
			// );
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
		var modelCollection = app.Project.Model.GetObjects();
		var objects3D = app.Project.DataExporter.GetObjects3D();

		var result = new StringBuilder("Renga levels, walls and columns:\n\n");

		var modelObjectCollection = app.Project.Model.GetObjects();

		result.AppendLine($"objects3DCount: {objects3D.Count}");

		var modelsWithCoordinates = GetModelsWithCoordinates(objects3D, modelObjectCollection);
		foreach (var model in modelsWithCoordinates)
		{
			result.AppendLine($"{model.Name} {model.Uuid}");
			foreach (var coordinates in model.Coordinates)
				result.AppendLine($"X: {coordinates.X} Y: {coordinates.Y} Z: {coordinates.Z}");
		}


		// for (var i = 0; i < objects3D.Count; i++)
		// {
		// 	var object3D = objects3D.Get(i);
		// 	var modelObject = modelObjectCollection.GetById(object3D.ModelObjectId);
		// 	var objectType = modelObject.ObjectType;
		//
		// 	var meshes = new List<IMesh>();
		// 	var grids = new List<IGrid>();
		// 	var vertexes = new List<FloatPoint3D>();
		// 	for (var meshIndex = 0; meshIndex < object3D.MeshCount; meshIndex++)
		// 	{
		// 		var mesh = object3D.GetMesh(meshIndex);
		// 		meshes.Add(mesh);
		// 		for (var gridIndex = 0; gridIndex < mesh.GridCount; gridIndex++)
		// 		{
		// 			var grid = mesh.GetGrid(gridIndex);
		// 			if (objectType == ObjectTypes.Room)
		// 			{
		// 				if (grid.GridType == (int)Room.Floor)
		// 				{
		// 					grids.Add(grid);
		// 					for (var vertexIndex = 0; vertexIndex < grid.VertexCount; vertexIndex++)
		// 					{
		// 						var vertex = grid.GetVertex(vertexIndex);
		// 						vertexes.Add(vertex);
		// 						result.AppendLine($"Name: ${modelObject.Name}");
		// 						result.AppendLine($"Coordinates: X: {vertex.X} Y: {vertex.Y} Z: {vertex.Z}");
		// 					}
		// 				}
		// 			}
		// 			else if (objectType == ObjectTypes.Door)
		// 			{
		// 				if (grid.GridType == (int)Door.Reveal)
		// 					for (var vertexIndex = 0; vertexIndex < grid.VertexCount; vertexIndex++)
		// 					{
		// 						var vertex = grid.GetVertex(vertexIndex);
		// 						result.AppendLine($"Name: ${modelObject.Name}");
		// 						result.AppendLine($"Coordinates: X: {vertex.X} Y: {vertex.Y} Z: {vertex.Z}");
		// 					}
		// 			}
		// 		}
		// 	}

		/*else if (objectType == ObjectTypes.Door)
		{
			var properties = modelObject.GetProperties();
			if (properties != null)
			{
				var ids = properties.GetIds();
				for (var propertyIndex = 0; propertyIndex < ids.Count; propertyIndex++)
				{
					var property = properties.Get(ids.Get(propertyIndex));
					result.AppendLine($"{property.Name} {}")
				}
			}
		}*/
		// }

		var buildingInfo = app.Project.BuildingInfo;
		var addressInfo = buildingInfo.GetAddress();

		var address = new Address(addressInfo.Town, "", "");
		var devs = new List<int>();
		var building = new Building(buildingInfo.Name, address, devs);

		return result.ToString();
		// return JsonConvert.SerializeObject(building, Formatting.Indented);
	}

	// private string ModelProperties()
	// {
	// }

	// private object GetPropertyValue(IProperty property)
	// {
	// 	switch (property.Type)
	// 	{
	// 		case PropertyType
	// 	}
	// }

	private List<ModelWithCoordinates> GetModelsWithCoordinates(IExportedObject3DCollection objects3D,
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

							modelsWithCoordinates.Add(
								new ModelWithCoordinates(modelObject.uniqueId, modelObject.Name, vertexes)
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

							if (vertexes.All(x => x.Z == vertexes.First().Z))
								modelsWithCoordinates.Add(
									new ModelWithCoordinates(modelObject.uniqueId, modelObject.Name, vertexes)
								);
						}
					}
				}
			}
		}

		return modelsWithCoordinates;
	}
}