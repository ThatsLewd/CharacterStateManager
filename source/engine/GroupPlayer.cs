using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class GroupPlayer : IDisposable
    {
      // static
      public static Dictionary<Group, GroupPlayer> list { get; private set; } = new Dictionary<Group, GroupPlayer>();

      public static void Init()
      {
        Group.OnDelete += HandleGroupDeleted;
      }

      static void HandleGroupDeleted(Group group)
      {
        if (list.ContainsKey(group))
        {
          list[group].Dispose();
          list.Remove(group);
        }
      }

      public static GroupPlayer GetOrCreate(Group group)
      {
        if (!list.ContainsKey(group))
        {
          list[group] = new GroupPlayer(group);
        }
        return list[group];
      }

      // instance
      public Group group { get; private set; }
      public StatePlayer statePlayer { get; private set; }

      public GroupPlayer(Group group)
      {
        this.group = group;
        this.statePlayer = new StatePlayer(this);
      }

      public void Dispose()
      {
        statePlayer.Dispose();
      }

      public void Update()
      {
        if (!group.playbackEnabledToggle.val) return;

        if (statePlayer.currentState == null && group.initialState != null)
        {
          statePlayer.SetState(group.initialState);
        }

        statePlayer.Update();
      }
    }
  }
}
