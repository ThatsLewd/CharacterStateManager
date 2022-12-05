using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public static class Messages
    {
      public static List<string> roles { get; private set; } = new List<string>();
      
      public static VaMUI.VaMTextInput addRoleInput;
      public static VaMUI.VaMLabelWithToggle selfRoleToggle;
      public static VaMUI.VaMLabelWithToggle globalRoleToggle;

      public static void Init()
      {
        addRoleInput = VaMUI.CreateTextInput("Role", callbackNoVal: CharacterStateManager.instance.RequestRedraw);
        selfRoleToggle = VaMUI.CreateLabelWithToggle("self", true);
        globalRoleToggle = VaMUI.CreateLabelWithToggle("global", true);
      }
    }
  }
}
