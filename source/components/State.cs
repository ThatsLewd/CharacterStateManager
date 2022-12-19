using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public partial class State : BaseComponent, IDisposable, INamedItem
    {
      public delegate void OnDeleteCallback(State state);
      public static event OnDeleteCallback OnDelete;

      public override string id { get; protected set; }
      public string name { get; set; }
      public Group group { get; private set; }

      public VaMUI.EventTrigger onEnterTrigger = VaMUI.CreateEventTrigger("On Enter State");
      public VaMUI.EventTrigger onExitTrigger = VaMUI.CreateEventTrigger("On Exit State");

      public AnimationPlaylist playlist { get; private set; } = new AnimationPlaylist();

      public State(Group group, string name = null)
      {
        this.id = VaMUtils.GenerateRandomID(32);
        this.group = group;
        this.name = name ?? "state";
        Helpers.EnsureUniqueName(group.states, this);
        this.group.states.Add(this);
        this.transitionModeChooser = VaMUI.CreateStringChooser("Transition Condition", TransitionMode.list.ToList(), TransitionMode.None, callbackNoVal: instance.RequestRedraw);
        this.fixedDurationSlider = VaMUI.CreateSlider("Duration", 10f, 0f, 30f, callbackNoVal: HandleFixedDurationChange);
        this.minDurationSlider = VaMUI.CreateSlider("Min Duration", 10f, 0f, 30f);
        this.maxDurationSlider = VaMUI.CreateSlider("Max Duration", 30f, 0f, 30f);
        Group.OnDelete += HandleGroupDeleted;
      }

      public State Clone(Group group = null)
      {
        string name = group == null ? Helpers.GetCopyName(this.name) : this.name;
        group = group ?? this.group;
        State newState = new State(group, name);
        newState.onEnterTrigger = onEnterTrigger.Clone();
        newState.onExitTrigger = onExitTrigger.Clone();
        newState.playlist = playlist.Clone();
        newState.transitionModeChooser.valNoCallback = transitionModeChooser.val;
        Helpers.SetSliderValues(newState.fixedDurationSlider, fixedDurationSlider.val, fixedDurationSlider.min, fixedDurationSlider.max);
        Helpers.SetSliderValues(newState.minDurationSlider, minDurationSlider.val, minDurationSlider.min, minDurationSlider.max);
        Helpers.SetSliderValues(newState.maxDurationSlider, maxDurationSlider.val, maxDurationSlider.min, maxDurationSlider.max);
        foreach (StateTransition transition in transitions)
        {
          newState.transitions.Add(transition.Clone());
        }
        return newState;
      }

      private void HandleGroupDeleted(Group group)
      {
        if (group == this.group) Dispose();
      }

      public void Dispose()
      {
        group.states.Remove(this);
        State.OnDelete?.Invoke(this);
        Group.OnDelete -= HandleGroupDeleted;
      }

      public static State FindById(string id)
      {
        foreach (Group group in Group.list)
        {
          State state = group.states.Find((s) => s.id == id);
          if (state != null)
          {
            return state;
          }
        }
        return null;
      }

      public void CopyActions()
      {
        ActionClipboard.onEnterTrigger = onEnterTrigger;
        ActionClipboard.onPlayingTrigger = null;
        ActionClipboard.onExitTrigger = onExitTrigger;
      }

      public void PasteActions()
      {
        if (ActionClipboard.onEnterTrigger != null)
          onEnterTrigger = ActionClipboard.onEnterTrigger.Clone(onEnterTrigger.name);
        if (ActionClipboard.onExitTrigger != null)
          onExitTrigger = ActionClipboard.onExitTrigger.Clone(onExitTrigger.name);

        CharacterStateManager.instance.RequestRedraw();
      }

      public JSONClass GetJSON(ReferenceCollector rc, bool saveTransitions = true)
      {
        JSONClass json = new JSONClass();
        json["id"] = id;
        json["groupId"] = group.id;
        json["name"] = name;
        onEnterTrigger.StoreJSON(json);
        onExitTrigger.StoreJSON(json);
        json["playlist"] = playlist.GetJSON(rc);
        transitionModeChooser.storable.StoreJSON(json);
        fixedDurationSlider.storable.StoreJSON(json);
        minDurationSlider.storable.StoreJSON(json);
        maxDurationSlider.storable.StoreJSON(json);
        if (saveTransitions)
        {
          json["transitions"] = new JSONArray();
          foreach (StateTransition transition in transitions)
          {
            json["transitions"].AsArray.Add(transition.GetJSON(rc));
          }
        }
        return json;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        id = json["id"].Value;
        name = json["name"].Value;
        onEnterTrigger.RestoreFromJSON(json);
        onExitTrigger.RestoreFromJSON(json);
        playlist.RestoreFromJSON(json["playlist"].AsObject);
        transitionModeChooser.storable.RestoreFromJSON(json);
        fixedDurationSlider.storable.RestoreFromJSON(json);
        minDurationSlider.storable.RestoreFromJSON(json);
        maxDurationSlider.storable.RestoreFromJSON(json);
      }

      public void LateRestoreFromJSON(JSONClass json)
      {
        transitions.Clear();
        foreach (JSONNode node in json["transitions"].AsArray.Childs)
        {
          StateTransition transition = StateTransition.FromJSON(node.AsObject);
          if (transition != null)
          {
            transitions.Add(transition);
          }
        }
      }
    }
  }
}
