using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public static CharacterStateManager instance { get; private set; }

    public Atom person { get; private set; }
    public DAZCharacterSelector geometry { get; private set; }
    public List<FreeControllerV3> controllers { get; private set; }

    public override void Init()
    {
      CharacterStateManager.instance = this;
      person = containingAtom;
      geometry = containingAtom?.GetStorableByID("geometry") as DAZCharacterSelector;
      if (person == null || geometry == null)
      {
        SuperController.LogError("CharacterStateManager must be attached to a Person atom!");
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

      return json;
    }

    public override void LateRestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
    {
      base.LateRestoreFromJSON(json, restorePhysical, restoreAppearance, setMissingToDefault);
    }
  }
}
