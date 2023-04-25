using System.Text;
using Renga;

namespace RengaToJson;

public class RengaToJsonPlugin : IPlugin
{
    private Application m_app;
    private ActionEventSource m_events;

    public bool Initialize(string pluginFolder)
    {
        m_app = new Application();
        var ui = m_app.UI;
        var panelExtension = ui.CreateUIPanelExtension();
        var action = ui.CreateAction();
        action.ToolTip = "Export to JSON";
        m_events = new ActionEventSource(action);
        m_events.Triggered += (s, e) =>
        {
            ui.ShowMessageBox(MessageIcon.MessageIcon_Info,
                "Model object list",
                TraverseModelObjects());
        };
        panelExtension.AddToolButton(action);
        ui.AddExtensionToPrimaryPanel(panelExtension);
        return true;
    }

    public void Stop()
    {
        m_events.Dispose();
    }

    private string TraverseModelObjects()
    {
        var modelCollection = m_app.Project.Model.GetObjects();

        var result = new StringBuilder("Renga levels, walls and columns:\n\n");

        var modelObjectCollection = m_app.Project.Model.GetObjects();

        var objCount = modelObjectCollection.Count;
        for (var i = 0; i < objCount; ++i)
        {
            var modelObject = modelObjectCollection.GetByIndex(i);
            var objectType = modelObject.ObjectType;

            if (objectType == ObjectTypes.Level)
            {
                var level = (ILevel)modelObject;

                result.AppendLine("Object type: Level");
                result.AppendLine("Level generated name: " + modelObject.Name);
                result.AppendLine("Level user defined name: " + level.LevelName);
                result.AppendLine($"Level elevation: {level.Elevation} mm.");
            }
            else if (objectType == ObjectTypes.Column)
            {
                var levelObject = (ILevelObject)modelObject;
                var objectWithMark = (IObjectWithMark)modelObject;
                var objectWithMaterial = (IObjectWithMaterial)modelObject;

                result.AppendLine("Object type: Column");
                result.AppendLine("Column name: " + modelObject.Name);
                result.AppendLine("Column parent level id: " + levelObject.LevelId);
                result.AppendLine("Column material id: " + objectWithMaterial.MaterialId);
                result.AppendLine("Column mark: " + objectWithMark.Mark);
                result.AppendLine($"Column offset: {levelObject.ElevationAboveLevel} mm.");
            }
            else if (objectType == ObjectTypes.Wall)
            {
                var levelObject = (ILevelObject)modelObject;
                var objectWithMark = (IObjectWithMark)modelObject;
                var objectWithLayeredMaterial = (IObjectWithLayeredMaterial)modelObject;

                result.AppendLine("Object type: Wall");
                result.AppendLine("Wall name: " + modelObject.Name);
                result.AppendLine("Wall parent level id: " + levelObject.LevelId);
                result.AppendLine("Wall material id: " + objectWithLayeredMaterial.LayeredMaterialId);
                result.AppendLine("Wall mark: " + objectWithMark.Mark);
                result.AppendLine($"Wall offset: {levelObject.ElevationAboveLevel} mm.");
            }
            else
            {
                continue;
            }

            result.AppendLine();
        }

        return result.ToString();
    }
}