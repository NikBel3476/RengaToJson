using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Renga;
using Renga.GridTypes;
using RengaToJson.domain;

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
						"Model object list",
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

		var modelObjectCount = modelObjectCollection.Count;

		result.AppendLine($"objects3DCount: {objects3D.Count}");

		for (var i = 0; i < objects3D.Count; i++)
		{
			var object3D = objects3D.Get(i);
			var modelObject = modelObjectCollection.GetById(object3D.ModelObjectId);
			var objectType = modelObject.ObjectType;

			if (objectType == ObjectTypes.Room)
			{
				var meshes = new List<IMesh>();
				var grids = new List<IGrid>();
				var vertexes = new List<FloatPoint3D>();
				for (var meshIndex = 0; meshIndex < object3D.MeshCount; meshIndex++)
				{
					var mesh = object3D.GetMesh(meshIndex);
					meshes.Add(mesh);
					for (var gridIndex = 0; gridIndex < mesh.GridCount; gridIndex++)
					{
						var grid = mesh.GetGrid(gridIndex);
						if (grid.GridType == (int)Room.Floor)
						{
							grids.Add(grid);
							for (var vertexIndex = 0; vertexIndex < grid.VertexCount; vertexIndex++)
							{
								var vertex = grid.GetVertex(vertexIndex);
								vertexes.Add(vertex);
								result.AppendLine($"Coordinates: X: {vertex.X} Y: {vertex.Y} Z: {vertex.Z}");
							}
						}
					}
				}
			}
		}

		var buildingInfo = app.Project.BuildingInfo;
		var addressInfo = buildingInfo.GetAddress();

		var address = new Address(addressInfo.Town, "", "");
		var devs = new List<int>();
		var building = new Building(buildingInfo.Name, address, devs);

		return JsonConvert.SerializeObject(building, Formatting.Indented);
	}
}