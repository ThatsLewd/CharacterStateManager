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
    public GenerateDAZMorphsControlUI morphsControl { get { return geometry.morphsControlUI; }}
    public List<FreeControllerV3> controllers { get; private set; }
    public FreeControllerV3 mainController { get { return person.mainController; }}
    public bool loadOnce { get; private set; } = false;

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

      EngineInit();
      UIInit();
    }

    void OnDestroy()
    {
      UIDestroy();
    }

    void Update()
    {
      EngineUpdate();
      UIUpdate();
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
      JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
      this.needsStore = true;
      
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
      if (loadOnce) return; // LateRestoreFromJSON gets called twice for some reason? And it's fucking my shit with a race condition or something
      if (json["id"]?.Value != this.storeId) return; // make sure this data is our plugin
      if (json["SaveFormatVersion"]?.Value != SaveFormatVersion)
      {
        LogError("Your save data is from an incompatible version of CharacterStateManager. Load Aborted.");
        return;
      }

      Layer.RestoreFromJSONTopLevel(json["Layers"].AsObject);
      Group.RestoreFromJSONTopLevel(json["Groups"].AsObject);
      Role.RestoreFromJSONTopLevel(json["Roles"].AsObject);
      Messages.RestoreFromJSONTopLevel(json["Messages"].AsObject);

      RefreshUIAfterJSONLoad();
      loadOnce = true;
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
