using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using MVR.FileManagementSecure;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public const string JSON_FORMAT_VERSION = "v1";
    public const string FILE_EXTENSION = "csm";
    public const string PLUGIN_DATA_DIR = @"Saves\PluginData\CharacterStateManager";
    public const string INSTANCE_DIR = PLUGIN_DATA_DIR + @"\Instances";

    void InitDirectories()
    {
      FileManagerSecure.CreateDirectory(PLUGIN_DATA_DIR);
      FileManagerSecure.CreateDirectory(INSTANCE_DIR);
    }

    void SaveJSONWithExtension(JSONClass json, string path)
    {
      if (path.Length == 0) return;
      if (!path.EndsWith($".{FILE_EXTENSION}"))
      {
        path = $"{path}.{FILE_EXTENSION}";
      }
      SaveJSON(json, path);
    }

    void HeaderStoreJSON(JSONClass json)
    {
      json["_format"] = JSON_FORMAT_VERSION;
    }

    bool VerifyFormat(JSONClass json)
    {
      if (json["_format"]?.Value != JSON_FORMAT_VERSION)
      {
        LogError("Your save data is from an incompatible version of CharacterStateManager. Load Aborted.");
        return false;
      }
      return true;
    }

    void InstanceStoreJSON(JSONClass json)
    {
      SuperController.LogMessage("SAVE");
      HeaderStoreJSON(json);
      json["Layers"] = Layer.GetJSONTopLevel();
      json["Groups"] = Group.GetJSONTopLevel();
      json["Roles"] = Role.GetJSONTopLevel();
      json["Messages"] = Messages.GetJSONTopLevel();
      SuperController.LogMessage("SAVE DONE");
    }

    void InstanceRestoreFromJSON(JSONClass json)
    {
      if (!VerifyFormat(json)) return;
      SuperController.LogMessage("LOAD");
      Layer.RestoreFromJSONTopLevel(json["Layers"].AsObject);
      Group.RestoreFromJSONTopLevel(json["Groups"].AsObject);
      Role.RestoreFromJSONTopLevel(json["Roles"].AsObject);
      Messages.RestoreFromJSONTopLevel(json["Messages"].AsObject);

      SuperController.LogMessage("LOAD DONE");
      RefreshUIAfterJSONLoad();
      SuperController.LogMessage("LOAD REFRESH");
    }
  }
}
