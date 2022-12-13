using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public const string PLUGIN_NAME = "ThatsLewd.CharacterStateManager";
    public static CharacterStateManager instance { get; private set; }

    List<MVRScript> otherInstances = new List<MVRScript>();
    public Atom person { get; private set; }
    public DAZCharacterSelector geometry { get; private set; }
    public GenerateDAZMorphsControlUI morphsControl { get { return geometry.morphsControlUI; }}
    public List<FreeControllerV3> controllers { get; private set; }
    public FreeControllerV3 mainController { get { return person.mainController; }}
    public bool loadOnce { get; private set; } = false;

    float instancePollTimer = 69f; // nice

    readonly string[] orderedControllerNames = new string[]
    {
      "eyeTarget",
      "head",
      "neck",
      "rShoulder",
      "lShoulder",
      "rArm",
      "lArm",
      "rElbow",
      "lElbow",
      "rHand",
      "lHand",
      "chest",
      "rNipple",
      "lNipple",
      "abdomen2",
      "hip",
      "pelvis",
      "abdomen",
      "testes",
      "penisBase",
      "penisMid",
      "penisTip",
      "rThigh",
      "lThigh",
      "rKnee",
      "lKnee",
      "rFoot",
      "lFoot",
      "rToe",
      "lToe",
    };

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
      PopulateControllers();
      if (controllers.Count != 30)
      {
        LogError("CharacterStateManager failed to load -- missing expected controllers!");
        this.enabled = false;
        return;
      }

      InitDirectories();
      EngineInit();
      UIInit();
      ActionsInit();
    }

    void OnDestroy()
    {
      UIDestroy();
    }

    void Update()
    {
      instancePollTimer += Time.deltaTime;
      if (instancePollTimer >= 5f)
      {
        instancePollTimer = 0f;
        GetOtherInstancesInScene();
      }
      EngineUpdate();
      UIUpdate();
    }

    void PopulateControllers()
    {
      List<FreeControllerV3> allControllers = GetAllControllers().ToList();
      controllers = new List<FreeControllerV3>();
      foreach (string name in orderedControllerNames)
      {
        string fullName = $"{name}Control";
        FreeControllerV3 controller = allControllers.Find((c) => c.name == fullName);
        controllers.Add(controller);
      }
    }

    void GetOtherInstancesInScene()
    {
      otherInstances.Clear();
      List<Atom> allAtoms = SuperController.singleton.GetAtoms();
      foreach (Atom atom in allAtoms)
      {
        bool isPerson = (atom.GetStorableByID("geometry") as DAZCharacterSelector) != null;
        if (!isPerson) continue;
        MVRPluginManager pluginManager = atom.GetStorableByID("PluginManager") as MVRPluginManager;
        if (pluginManager == null) continue;
        foreach (Transform transform in pluginManager.gameObject.transform.Find("Plugins"))
        {
          MVRScript plugin = transform.gameObject.GetComponent<MVRScript>();
          if (!plugin.storeId.EndsWith(PLUGIN_NAME)) continue;
          if (atom == containingAtom && plugin.storeId == storeId) continue;
          otherInstances.Add(plugin);
        }
      }
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
      JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
      this.needsStore = true;

      InstanceStoreJSON(json);
      
      return json;
    }

    public override void LateRestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
    {
      base.LateRestoreFromJSON(json, restorePhysical, restoreAppearance, setMissingToDefault);
      if (loadOnce) return; // LateRestoreFromJSON gets called twice for some reason? And it's fucking my shit with a race condition or something
      if (json["id"]?.Value != this.storeId) return; // make sure this data is our plugin
      InstanceRestoreFromJSON(json);
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

// TODO:
// capture controller states
// character locomotion
// layer offset mode vs absolute mode
// animation noise
// merge
