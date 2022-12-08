using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    // All the state properties related to the group tab are in this file for clarity
    public partial class State : BaseComponent, IDisposable
    {
      public VaMUI.VaMStringChooser transitionModeChooser { get; private set; }
      public VaMUI.VaMSlider fixedDurationSlider { get; private set; }
      public VaMUI.VaMSlider minDurationSlider { get; private set; }
      public VaMUI.VaMSlider maxDurationSlider { get; private set; }

      public List<StateTransition> transitions { get; private set; } = new List<StateTransition>();

      public void AddTransition(State state)
      {
        if (transitions.Exists((t) => t.state == state)) return;
        transitions.Add(new StateTransition(state));
        transitions.Sort((a, b) => String.Compare(a.state.name, b.state.name));
      }

      private void HandleFixedDurationChange()
      {
        Helpers.SetSliderValues(minDurationSlider, fixedDurationSlider.val, fixedDurationSlider.min, fixedDurationSlider.max);
        Helpers.SetSliderValues(maxDurationSlider, fixedDurationSlider.val, fixedDurationSlider.min, fixedDurationSlider.max);
      }
    }

    public static class TransitionMode
    {
      public const string None = "None";
      public const string PlaylistCompleted = "Playlist Completed";
      public const string FixedDuration = "Fixed Duration";
      public const string RandomDuration = "Random Duration";

      public static readonly string[] list = new string[] { None, PlaylistCompleted, FixedDuration, RandomDuration };
    }

    public class StateTransition : IWeightedItem
    {
      public State state { get; private set; }
      public VaMUI.VaMSlider weightSlider { get; private set; }

      public float weight { get { return weightSlider.val; }}

      public StateTransition(State state)
      {
        this.state = state;
        weightSlider = VaMUI.CreateSlider("Weight", 0.5f, 0f, 1f);
      }

      public StateTransition Clone()
      {
        StateTransition newStateTransition = new StateTransition(state);
        newStateTransition.weightSlider.valNoCallback = weightSlider.val;
        newStateTransition.weightSlider.min = weightSlider.min;
        newStateTransition.weightSlider.max = weightSlider.max;
        return newStateTransition;
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["state"] = state.id;
        json["stateGroup"] = state.group.id;
        weightSlider.storable.StoreJSON(json);
        return json;
      }

      public static StateTransition FromJSON(JSONClass json)
      {
        string stateId = json["state"].Value;
        string groupId = json["stateGroup"].Value;
        Group group = Group.list.Find((g) => g.id == groupId);
        State state = group?.states.Find((s) => s.id == stateId);
        if (state == null)
        {
          LogError($"Could not find state: {stateId}");
          return null;
        }
        StateTransition transition = new StateTransition(state);
        transition.RestoreFromJSON(json);
        return transition;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        weightSlider.storable.RestoreFromJSON(json);
      }
    }
  }
}
