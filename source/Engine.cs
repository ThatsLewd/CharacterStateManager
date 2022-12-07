using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    void EngineInit()
    {
      GroupPlayer.Init();
    }

    void EngineUpdate()
    {
      if (!playbackEnabledToggle.val) return;

      foreach (Group group in Group.list)
      {
        GroupPlayer player = GroupPlayer.GetOrCreate(group);
        player.Update();
      }
    }
  }
}
