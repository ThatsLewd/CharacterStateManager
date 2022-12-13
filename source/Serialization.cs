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

    void HeaderStoreJSON(JSONClass json, string type)
    {
      json["_format"] = JSON_FORMAT_VERSION;
      json["_type"] = type;
    }

    bool VerifyHeader(JSONClass json, string type)
    {
      if (json["_format"]?.Value != JSON_FORMAT_VERSION)
      {
        LogError("Your save data is from an incompatible version of CharacterStateManager. Load aborted.");
        return false;
      }
      if (json["_type"]?.Value != type)
      {
        LogError($"This file is not a CharacterStateManager {type} file. Load aborted.");
        return false;
      }
      return true;
    }

    // Instance
    void InstanceStoreJSON(JSONClass json)
    {
      HeaderStoreJSON(json, SerializableSection.Instance);
      json["Layers"] = Layer.GetJSONTopLevel();
      json["Groups"] = Group.GetJSONTopLevel();
      json["Roles"] = Role.GetJSONTopLevel();
      json["Messages"] = Messages.GetJSONTopLevel();
    }

    void InstanceRestoreFromJSON(JSONClass json)
    {
      if (!VerifyHeader(json, SerializableSection.Instance)) return;
      Layer.RestoreFromJSONTopLevel(json["Layers"].AsObject);
      Group.RestoreFromJSONTopLevel(json["Groups"].AsObject);
      Role.RestoreFromJSONTopLevel(json["Roles"].AsObject);
      Messages.RestoreFromJSONTopLevel(json["Messages"].AsObject);
      RefreshUIAfterJSONLoad();
    }

    // Group
    void GroupStoreJSON(JSONClass json, Group group)
    {
      HeaderStoreJSON(json, SerializableSection.Group);
      json["Group"] = group.GetJSON();
    }

    void GroupRestoreFromJSON(JSONClass json)
    {
      if (!VerifyHeader(json, SerializableSection.Group)) return;
      Group group = new Group();
      group.RestoreFromJSON(json["Group"].AsObject);
      RefreshUIAfterJSONLoad();
    }

    // State
    void StateStoreJSON(JSONClass json, State state)
    {
      HeaderStoreJSON(json, SerializableSection.State);
      json["State"] = state.GetJSON();
    }

    void StateRestoreFromJSON(JSONClass json, Group group)
    {
      if (!VerifyHeader(json, SerializableSection.State)) return;
      State state = new State(group);
      state.RestoreFromJSON(json["State"].AsObject);
      RefreshUIAfterJSONLoad();
    }

    // Layer
    void LayerStoreJSON(JSONClass json, Layer layer)
    {
      HeaderStoreJSON(json, SerializableSection.Layer);
      json["Layer"] = layer.GetJSON();
    }

    void LayerRestoreFromJSON(JSONClass json)
    {
      if (!VerifyHeader(json, SerializableSection.Layer)) return;
      Layer layer = new Layer();
      layer.RestoreFromJSON(json["Layer"].AsObject);
      RefreshUIAfterJSONLoad();
    }

    // Animation
    void AnimationStoreJSON(JSONClass json, Animation animation)
    {
      HeaderStoreJSON(json, SerializableSection.Animation);
      json["Animation"] = animation.GetJSON();
    }

    void AnimationRestoreFromJSON(JSONClass json, Layer layer)
    {
      if (!VerifyHeader(json, SerializableSection.Animation)) return;
      Animation animation = new Animation(layer);
      animation.RestoreFromJSON(json["Animation"].AsObject);
      RefreshUIAfterJSONLoad();
    }

    // Roles
    void RolesStoreJSON(JSONClass json)
    {
      HeaderStoreJSON(json, SerializableSection.Roles);
      json["Roles"] = Role.GetJSONTopLevel();
    }

    void RolesRestoreFromJSON(JSONClass json)
    {
      if (!VerifyHeader(json, SerializableSection.Roles)) return;
      Role.RestoreFromJSONTopLevel(json["Roles"].AsObject);
      RefreshUIAfterJSONLoad();
    }
  }
}
