using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public partial class State : BaseComponentWithId
    {
      public delegate void OnDeleteCallback(State state);
      public static event OnDeleteCallback OnDelete;

      public override string id { get; protected set; }
      public string name { get; set; }
      public Group group { get; private set; }

      public EventTrigger onEnterTrigger = VaMTrigger.Create<EventTrigger>("On Enter State");
      public EventTrigger onExitTrigger = VaMTrigger.Create<EventTrigger>("On Exit State");

      public List<AnimationPlaylist> playlists { get; private set; } = new List<AnimationPlaylist>(); 

      public State(Group group, string name = null)
      {
        this.id = VaMUtils.GenerateRandomID();
        this.group = group;
        this.name = name ?? "state";
        this.group.states.Add(this);
        this.transitionModeChooser = VaMUI.CreateStringChooser("Transition Condition", TransitionMode.list.ToList(), TransitionMode.None, callbackNoVal: instance.RequestRedraw);
        this.fixedDurationSlider = VaMUI.CreateSlider("Duration", 10f, 0f, 30f, callbackNoVal: HandleFixedDurationChange);
        this.minDurationSlider = VaMUI.CreateSlider("Min Duration", 10f, 0f, 30f);
        this.maxDurationSlider = VaMUI.CreateSlider("Max Duration", 30f, 0f, 30f);
        Group.OnDelete += HandleGroupDeleted;
        Layer.OnDelete += HandleLayerDeleted;
        Animation.OnDelete += HandleAnimationDeleted;
      }

      public State Clone(Group group = null)
      {
        string name = group == null ? Helpers.GetCopyName(this.name) : this.name;
        group = group ?? this.group;
        State newState = new State(group, name);
        newState.onEnterTrigger = VaMTrigger.Clone(onEnterTrigger);
        newState.onExitTrigger = VaMTrigger.Clone(onExitTrigger);
        foreach (AnimationPlaylist playlist in playlists)
        {
          newState.playlists.Add(playlist.Clone());
        }
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
        if (group == this.group) Delete();
      }

      private void HandleLayerDeleted(Layer layer)
      {
        playlists.RemoveAll((p) => p.layer == layer);
      }

      private void HandleAnimationDeleted(Animation animation)
      {
        foreach (AnimationPlaylist playlist in playlists)
        {
          playlist.entries.RemoveAll((e) => e.animation == animation);
        }
      }

      public void Delete()
      {
        group.states.Remove(this);
        State.OnDelete?.Invoke(this);
        Group.OnDelete -= HandleGroupDeleted;
        Layer.OnDelete -= HandleLayerDeleted;
        Animation.OnDelete -= HandleAnimationDeleted;
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
          onEnterTrigger = VaMTrigger.Clone(ActionClipboard.onEnterTrigger, onEnterTrigger.name);
        if (ActionClipboard.onExitTrigger != null)
          onExitTrigger = VaMTrigger.Clone(ActionClipboard.onExitTrigger, onExitTrigger.name);

        CharacterStateManager.instance.RequestRedraw();
      }

      public AnimationPlaylist GetPlaylist(Layer layer)
      {
        return playlists.Find((p) => p.layer == layer);
      }

      public bool PlaylistExists(Layer layer)
      {
        return playlists.Exists((p) => p.layer == layer);
      }

      public void CreatePlaylist(Layer layer)
      {
        if (PlaylistExists(layer)) return;
        playlists.Add(new AnimationPlaylist(layer));
        playlists.Sort((a, b) => String.Compare(a.layer.name, b.layer.name));
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["id"] = id;
        json["name"] = name;
        onEnterTrigger.StoreJSON(json);
        onExitTrigger.StoreJSON(json);
        json["playlists"] = new JSONArray();
        foreach (AnimationPlaylist playlist in playlists)
        {
          json["playlists"].AsArray.Add(playlist.GetJSON());
        }
        transitionModeChooser.storable.StoreJSON(json);
        fixedDurationSlider.storable.StoreJSON(json);
        minDurationSlider.storable.StoreJSON(json);
        maxDurationSlider.storable.StoreJSON(json);
        json["transitions"] = new JSONArray();
        foreach (StateTransition transition in transitions)
        {
          json["transitions"].AsArray.Add(transition.GetJSON());
        }
        return json;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        id = json["id"].Value;
        name = json["name"].Value;
        onEnterTrigger.RestoreFromJSON(json);
        onExitTrigger.RestoreFromJSON(json);
        transitionModeChooser.storable.RestoreFromJSON(json);
        fixedDurationSlider.storable.RestoreFromJSON(json);
        minDurationSlider.storable.RestoreFromJSON(json);
        maxDurationSlider.storable.RestoreFromJSON(json);
      }

      public void LateRestoreFromJSON(JSONClass json)
      {
        playlists.Clear();
        foreach (JSONNode node in json["playlists"].AsArray.Childs)
        {
          AnimationPlaylist playlist = AnimationPlaylist.FromJSON(node.AsObject);
          if (playlist != null)
          {
            playlists.Add(playlist);
          }
        }
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

    public static class PlaylistMode
    {
      public const string Sequential = "Sequential";
      public const string Random = "Random";

      public static readonly string[] list = new string[] { Sequential, Random };
    }

    public static class TimingMode
    {
      public const string DurationFromAnimation = "Duration From Animation";
      public const string FixedDuration = "Fixed Duration";
      public const string RandomDuration = "Random Duration";

      public static readonly string[] list = new string[] { DurationFromAnimation, FixedDuration, RandomDuration };
    }

    public class AnimationPlaylist
    {
      public Layer layer { get; private set; }
      public List<PlaylistEntry> entries { get; private set; } = new List<PlaylistEntry>();

      public VaMUI.VaMStringChooser playModeChooser { get; private set; }

      public VaMUI.VaMStringChooser defaultTimingModeChooser { get; private set; }
      public VaMUI.VaMSlider defaultWeightSlider { get; private set; }
      public VaMUI.VaMSlider defaultDurationFixedSlider { get; private set; }
      public VaMUI.VaMSlider defaultDurationMinSlider { get; private set; }
      public VaMUI.VaMSlider defaultDurationMaxSlider { get; private set; }

      public AnimationPlaylist(Layer layer)
      {
        this.layer = layer;
        playModeChooser = VaMUI.CreateStringChooser("Play Mode", PlaylistMode.list.ToList(), PlaylistMode.Sequential, callbackNoVal: instance.RequestRedraw);
        defaultTimingModeChooser = VaMUI.CreateStringChooser("Timing Mode", TimingMode.list.ToList(), TimingMode.DurationFromAnimation, callbackNoVal: instance.RequestRedraw);
        defaultWeightSlider = VaMUI.CreateSlider("Default Weight", 0.5f, 0f, 1f);
        defaultDurationFixedSlider = VaMUI.CreateSlider("Default Duration", 10f, 0f, 30f, callbackNoVal: HandleDurationFixedChange);
        defaultDurationMinSlider = VaMUI.CreateSlider("Default Duration Min", 10f, 0f, 30f);
        defaultDurationMaxSlider = VaMUI.CreateSlider("Default Duration Max", 10f, 0f, 30f);
      }

      public AnimationPlaylist Clone()
      {
        AnimationPlaylist newPlaylist = new AnimationPlaylist(layer);
        newPlaylist.playModeChooser.valNoCallback = playModeChooser.val;
        newPlaylist.defaultTimingModeChooser.valNoCallback = defaultTimingModeChooser.val;
        Helpers.SetSliderValues(newPlaylist.defaultWeightSlider, defaultWeightSlider.val, defaultWeightSlider.min, defaultWeightSlider.max);
        Helpers.SetSliderValues(newPlaylist.defaultDurationFixedSlider, defaultDurationFixedSlider.val, defaultDurationFixedSlider.min, defaultDurationFixedSlider.max);
        Helpers.SetSliderValues(newPlaylist.defaultDurationMinSlider, defaultDurationMinSlider.val, defaultDurationMinSlider.min, defaultDurationMinSlider.max);
        Helpers.SetSliderValues(newPlaylist.defaultDurationMaxSlider, defaultDurationMaxSlider.val, defaultDurationMaxSlider.min, defaultDurationMaxSlider.max);

        foreach (PlaylistEntry entry in entries)
        {
          newPlaylist.entries.Add(entry.Clone(newPlaylist));
        }

        return newPlaylist;
      }

      public void AddEntry(Animation animation)
      {
        entries.Add(new PlaylistEntry(this, animation));
      }

      private void HandleDurationFixedChange()
      {
        Helpers.SetSliderValues(defaultDurationMinSlider, defaultDurationFixedSlider.val, defaultDurationFixedSlider.min, defaultDurationFixedSlider.max);
        Helpers.SetSliderValues(defaultDurationMaxSlider, defaultDurationFixedSlider.val, defaultDurationFixedSlider.min, defaultDurationFixedSlider.max);
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["layer"] = layer.id;
        json["entries"] = new JSONArray();
        foreach (PlaylistEntry entry in entries)
        {
          json["entries"].AsArray.Add(entry.GetJSON());
        }
        playModeChooser.storable.StoreJSON(json);
        defaultTimingModeChooser.storable.StoreJSON(json);
        defaultWeightSlider.storable.StoreJSON(json);
        defaultDurationFixedSlider.storable.StoreJSON(json);
        defaultDurationMinSlider.storable.StoreJSON(json);
        defaultDurationMaxSlider.storable.StoreJSON(json);
        return json;
      }

      public static AnimationPlaylist FromJSON(JSONClass json)
      {
        string layerId = json["layer"].Value;
        Layer layer = Layer.list.Find((l) => l.id == layerId);
        if (layer == null)
        {
          LogError($"Could not find layer: {layerId}");
          return null;
        }
        AnimationPlaylist playlist = new AnimationPlaylist(layer);
        playlist.RestoreFromJSON(json);
        return playlist;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        entries.Clear();
        foreach (JSONNode node in json["entries"].AsArray.Childs)
        {
          PlaylistEntry entry = PlaylistEntry.FromJSON(this, node.AsObject);
          if (entry != null)
          {
            entries.Add(entry);
          }
        }
        playModeChooser.storable.RestoreFromJSON(json);
        defaultTimingModeChooser.storable.RestoreFromJSON(json);
        defaultWeightSlider.storable.RestoreFromJSON(json);
        defaultDurationFixedSlider.storable.RestoreFromJSON(json);
        defaultDurationMinSlider.storable.RestoreFromJSON(json);
        defaultDurationMaxSlider.storable.RestoreFromJSON(json);
      }
    }

    public class PlaylistEntry
    {
      public AnimationPlaylist playlist { get; private set; }
      public Animation animation { get; private set; }

      public VaMUI.VaMStringChooser timingModeChooser { get; private set; }
      public VaMUI.VaMSlider weightSlider { get; private set; }
      public VaMUI.VaMSlider durationFixedSlider { get; private set; }
      public VaMUI.VaMSlider durationMinSlider { get; private set; }
      public VaMUI.VaMSlider durationMaxSlider { get; private set; }

      public PlaylistEntry(AnimationPlaylist playlist, Animation animation)
      {
        this.playlist = playlist;
        this.animation = animation;
        timingModeChooser = VaMUI.CreateStringChooser("Timing Mode", TimingMode.list.ToList(), TimingMode.DurationFromAnimation, callbackNoVal: instance.RequestRedraw);
        timingModeChooser.valNoCallback = playlist.defaultTimingModeChooser.val;
        weightSlider = VaMUI.CreateSlider("Weight", 0.5f, 0f, 1f);
        Helpers.SetSliderValues(weightSlider, playlist.defaultWeightSlider.val, playlist.defaultWeightSlider.min, playlist.defaultWeightSlider.max);
        durationFixedSlider = VaMUI.CreateSlider("Duration", 10f, 0f, 30f, callbackNoVal: HandleDurationFixedChange);
        Helpers.SetSliderValues(durationFixedSlider, playlist.defaultDurationFixedSlider.val, playlist.defaultDurationFixedSlider.min, playlist.defaultDurationFixedSlider.max);
        durationMinSlider = VaMUI.CreateSlider("Duration Min", 10f, 0f, 30f);
        Helpers.SetSliderValues(durationMinSlider, playlist.defaultDurationMinSlider.val, playlist.defaultDurationMinSlider.min, playlist.defaultDurationMinSlider.max);
        durationMaxSlider = VaMUI.CreateSlider("Duration Max", 10f, 0f, 30f);
        Helpers.SetSliderValues(durationMaxSlider, playlist.defaultDurationMaxSlider.val, playlist.defaultDurationMaxSlider.min, playlist.defaultDurationMaxSlider.max);
      }

      public PlaylistEntry Clone(AnimationPlaylist playlist = null)
      {
        playlist = playlist ?? this.playlist;
        PlaylistEntry newEntry = new PlaylistEntry(playlist, animation);
        newEntry.timingModeChooser.valNoCallback = timingModeChooser.val;
        Helpers.SetSliderValues(newEntry.weightSlider, weightSlider.val, weightSlider.min, weightSlider.max);
        Helpers.SetSliderValues(newEntry.durationFixedSlider, durationFixedSlider.val, durationFixedSlider.min, durationFixedSlider.max);
        Helpers.SetSliderValues(newEntry.durationMinSlider, durationMinSlider.val, durationMinSlider.min, durationMinSlider.max);
        Helpers.SetSliderValues(newEntry.durationMaxSlider, durationMaxSlider.val, durationMaxSlider.min, durationMaxSlider.max);
        return newEntry;
      }

      public void SetFromDefaults()
      {
        timingModeChooser.val = playlist.defaultTimingModeChooser.val;
        Helpers.SetSliderValues(weightSlider, playlist.defaultWeightSlider.val, playlist.defaultWeightSlider.min, playlist.defaultWeightSlider.max, noCallback: false);
        Helpers.SetSliderValues(durationFixedSlider, playlist.defaultDurationFixedSlider.val, playlist.defaultDurationFixedSlider.min, playlist.defaultDurationFixedSlider.max, noCallback: false);
        Helpers.SetSliderValues(durationMinSlider, playlist.defaultDurationMinSlider.val, playlist.defaultDurationMinSlider.min, playlist.defaultDurationMinSlider.max, noCallback: false);
        Helpers.SetSliderValues(durationMaxSlider, playlist.defaultDurationMaxSlider.val, playlist.defaultDurationMaxSlider.min, playlist.defaultDurationMaxSlider.max, noCallback: false);
      }

      private void HandleDurationFixedChange()
      {
        Helpers.SetSliderValues(durationMinSlider, durationFixedSlider.val, durationFixedSlider.min, durationFixedSlider.max);
        Helpers.SetSliderValues(durationMaxSlider, durationFixedSlider.val, durationFixedSlider.min, durationFixedSlider.max);
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["animation"] = animation.id;
        json["animationLayer"] = animation.layer.id;
        timingModeChooser.storable.StoreJSON(json);
        weightSlider.storable.StoreJSON(json);
        durationFixedSlider.storable.StoreJSON(json);
        durationMinSlider.storable.StoreJSON(json);
        durationMaxSlider.storable.StoreJSON(json);
        return json;
      }

      public static PlaylistEntry FromJSON(AnimationPlaylist playlist, JSONClass json)
      {
        string animationId = json["animation"].Value;
        string layerId = json["animationLayer"].Value;
        Layer layer = Layer.list.Find((l) => l.id == layerId);
        Animation animation = layer?.animations.Find((a) => a.id == animationId);
        if (animation == null)
        {
          LogError($"Could not find animation: {animationId}");
          return null;
        }
        PlaylistEntry entry = new PlaylistEntry(playlist, animation);
        entry.RestoreFromJSON(json);
        return entry;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        timingModeChooser.storable.RestoreFromJSON(json);
        weightSlider.storable.RestoreFromJSON(json);
        durationFixedSlider.storable.RestoreFromJSON(json);
        durationMinSlider.storable.RestoreFromJSON(json);
        durationMaxSlider.storable.RestoreFromJSON(json);
      }
    }
  }
}
