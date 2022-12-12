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
    public const string GROUP_DIR = PLUGIN_DATA_DIR + @"\Groups";
    public const string STATE_DIR = PLUGIN_DATA_DIR + @"\States";
    public const string LAYER_DIR = PLUGIN_DATA_DIR + @"\Layers";
    public const string ANIMATION_DIR = PLUGIN_DATA_DIR + @"\Animations";
    public const string ROLE_DIR = PLUGIN_DATA_DIR + @"\Roles";

    public static class SerializableSection
    {
      public const string Instance = "Instance";
      public const string Group = "Group";
      public const string State = "State";
      public const string Layer = "Layer";
      public const string Animation = "Animation";
      public const string Roles = "Roles";

      public static readonly string[] list = new string[] { Instance, Group, State, Layer, Animation, Roles };
    }

    void InitDirectories()
    {
      FileManagerSecure.CreateDirectory(PLUGIN_DATA_DIR);
      FileManagerSecure.CreateDirectory(INSTANCE_DIR);
      FileManagerSecure.CreateDirectory(GROUP_DIR);
      FileManagerSecure.CreateDirectory(STATE_DIR);
      FileManagerSecure.CreateDirectory(LAYER_DIR);
      FileManagerSecure.CreateDirectory(ANIMATION_DIR);
      FileManagerSecure.CreateDirectory(ROLE_DIR);
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
