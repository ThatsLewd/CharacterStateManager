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

      public static void PlayState(Group group, State state)
      {
        if (list.ContainsKey(group))
        {
          GroupPlayer player = list[group];
          player.PlayState(state);
        }
      }

      // instance
      public Group group { get; private set; }
      public StatePlayer statePlayer { get; private set; }

      bool initialized = false;

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

        if (!initialized && statePlayer.currentState == null && group.initialState != null)
        {
          statePlayer.SetState(group.initialState);
          initialized = true;
        }

        if (statePlayer.donePlaying)
        {
          State next = GetNextState();
          statePlayer.SetState(next);
        }

        statePlayer.Update();
      }

      State GetNextState()
      {
        if (statePlayer.currentState.transitions.Count == 0) return null;
        return Helpers.ChooseWeightedItem(statePlayer.currentState.transitions).state;
      }

      public void PlayState(State state)
      {
        statePlayer.SetState(state);
      }
    }
  }
}
