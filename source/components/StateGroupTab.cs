using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public static class TransitionMode
    {
      public const string None = "None";
      public const string PlaylistCompleted = "Playlist Completed";
      public const string FixedDuration = "Fixed Duration";
      public const string RandomDuration = "Random Duration";

      public static readonly string[] list = new string[] { None, PlaylistCompleted, FixedDuration, RandomDuration };
    }

    // All the state properties related to the group tab are in this file for clarity
    // TODO send messages to other groups on state enter/exit
    public partial class State : BaseComponentWithId
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

    public class StateTransition
    {
      public State state { get; private set; }
      public VaMUI.VaMSlider weightSlider { get; private set; }

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
    }
  }
}
