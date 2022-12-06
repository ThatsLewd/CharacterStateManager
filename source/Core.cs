using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public const string SaveFormatVersion = "v1";
    public static CharacterStateManager instance { get; private set; }

    public Atom person { get; private set; }
    public DAZCharacterSelector geometry { get; private set; }
    public List<FreeControllerV3> controllers { get; private set; }
    public GenerateDAZMorphsControlUI morphsControl { get { return geometry.morphsControlUI; }}

    public override void Init()
    {
      CharacterStateManager.instance = this;
      person = containingAtom;
      geometry = containingAtom?.GetStorableByID("geometry") as DAZCharacterSelector;
      if (person == null || geometry == null)
      {
        LogError("CharacterStateManager must be attached to a Person atom!");
        this.enabled = false;
        return;
      }
      controllers = new List<FreeControllerV3>();
      foreach (FreeControllerV3 controller in GetAllControllers())
      {
        if (controller == person.mainController)
        {
          continue;
        }
        if (controller.name.StartsWith("hair"))
        {
          continue;
        }
        controllers.Add(controller);
      }

      UIInit();
    }

    void OnDestroy()
    {
      UIDestroy();
    }

    void Update()
    {
      UIUpdate();
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
      JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
      this.needsStore = true;
      SuperController.LogMessage("SAVE");
      
      json["SaveFormatVersion"] = SaveFormatVersion;
      json["Layers"] = Layer.GetJSONTopLevel();
      json["Groups"] = Group.GetJSONTopLevel();
      json["Roles"] = Role.GetJSONTopLevel();
      json["Messages"] = Messages.GetJSONTopLevel();
      
      return json;
    }

    public override void LateRestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
    {
      base.LateRestoreFromJSON(json, restorePhysical, restoreAppearance, setMissingToDefault);
      if (json["id"]?.Value != this.storeId) return; // make sure this is our plugin
      if (json["SaveFormatVersion"]?.Value != SaveFormatVersion)
      {
        LogError("Your save data is from an incompatible version of CharacterStateManager. Load Aborted.");
        return;
      }
      SuperController.LogMessage("LOAD");

      Layer.RestoreFromJSONTopLevel(json["Layers"].AsObject);
      Group.RestoreFromJSONTopLevel(json["Groups"].AsObject);
      Role.RestoreFromJSONTopLevel(json["Roles"].AsObject);
      Messages.RestoreFromJSONTopLevel(json["Messages"].AsObject);

      RefreshUIAfterJSONLoad();
    }

    public static void LogMessage(string message)
    {
      SuperController.LogMessage($"[CharacterStateManager] {message}");
    }

    public static void LogError(string message)
    {
      SuperController.LogError($"[CharacterStateManager] {message}");
    }
  }
}
