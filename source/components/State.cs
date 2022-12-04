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
        this.transitionModeChooser = VaMUI.CreateStringChooser("Transition Condition", TransitionMode.list.ToList(), TransitionMode.None);
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
        playModeChooser = VaMUI.CreateStringChooser("Play Mode", PlaylistMode.list.ToList(), PlaylistMode.Sequential);
        defaultTimingModeChooser = VaMUI.CreateStringChooser("Timing Mode", TimingMode.list.ToList(), TimingMode.DurationFromAnimation);
        defaultWeightSlider = VaMUI.CreateSlider("Default Weight", 0.5f, 0f, 1f);
        defaultDurationFixedSlider = VaMUI.CreateSlider("Default Duration", 10f, 0f, 30f, callback: HandleDurationFixedChange);
        defaultDurationMinSlider = VaMUI.CreateSlider("Default Duration Min", 10f, 0f, 30f);
        defaultDurationMaxSlider = VaMUI.CreateSlider("Default Duration Max", 10f, 0f, 30f);
      }

      public AnimationPlaylist Clone()
      {
        AnimationPlaylist newPlaylist = new AnimationPlaylist(layer);
        newPlaylist.playModeChooser.valNoCallback = playModeChooser.val;
        newPlaylist.defaultTimingModeChooser.valNoCallback = defaultTimingModeChooser.val;

        newPlaylist.defaultWeightSlider.valNoCallback = defaultWeightSlider.val;
        newPlaylist.defaultWeightSlider.min = defaultWeightSlider.min;
        newPlaylist.defaultWeightSlider.max = defaultWeightSlider.max;

        newPlaylist.defaultDurationFixedSlider.valNoCallback = defaultDurationFixedSlider.val;
        newPlaylist.defaultDurationFixedSlider.min = defaultDurationFixedSlider.min;
        newPlaylist.defaultDurationFixedSlider.max = defaultDurationFixedSlider.max;

        newPlaylist.defaultDurationMinSlider.valNoCallback = defaultDurationMinSlider.val;
        newPlaylist.defaultDurationMinSlider.min = defaultDurationMinSlider.min;
        newPlaylist.defaultDurationMinSlider.max = defaultDurationMinSlider.max;

        newPlaylist.defaultDurationMaxSlider.valNoCallback = defaultDurationMaxSlider.val;
        newPlaylist.defaultDurationMaxSlider.min = defaultDurationMaxSlider.min;
        newPlaylist.defaultDurationMaxSlider.max = defaultDurationMaxSlider.max;

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

      private void HandleDurationFixedChange(float val)
      {
        defaultDurationMinSlider.valNoCallback = defaultDurationFixedSlider.val;
        defaultDurationMinSlider.min = defaultDurationFixedSlider.min;
        defaultDurationMinSlider.max = defaultDurationFixedSlider.max;

        defaultDurationMaxSlider.valNoCallback = defaultDurationFixedSlider.val;
        defaultDurationMaxSlider.min = defaultDurationFixedSlider.min;
        defaultDurationMaxSlider.max = defaultDurationFixedSlider.max;
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
        timingModeChooser = VaMUI.CreateStringChooser("Timing Mode", TimingMode.list.ToList(), playlist.defaultTimingModeChooser.val);
        weightSlider = VaMUI.CreateSlider("Weight", playlist.defaultWeightSlider.val, playlist.defaultWeightSlider.min, playlist.defaultWeightSlider.max);
        durationFixedSlider = VaMUI.CreateSlider("Duration", playlist.defaultDurationFixedSlider.val, playlist.defaultDurationFixedSlider.min, playlist.defaultDurationFixedSlider.max, callback: HandleDurationFixedChange);
        durationMinSlider = VaMUI.CreateSlider("Duration Min", playlist.defaultDurationMinSlider.val, playlist.defaultDurationMinSlider.min, playlist.defaultDurationMinSlider.max);
        durationMaxSlider = VaMUI.CreateSlider("Duration Max", playlist.defaultDurationMaxSlider.val, playlist.defaultDurationMaxSlider.min, playlist.defaultDurationMaxSlider.max);
      }

      public PlaylistEntry Clone(AnimationPlaylist playlist = null)
      {
        playlist = playlist ?? this.playlist;
        PlaylistEntry newEntry = new PlaylistEntry(playlist, animation);
        newEntry.timingModeChooser.valNoCallback = timingModeChooser.val;

        newEntry.weightSlider.valNoCallback = weightSlider.val;
        newEntry.weightSlider.min = weightSlider.min;
        newEntry.weightSlider.max = weightSlider.max;

        newEntry.durationFixedSlider.valNoCallback = durationFixedSlider.val;
        newEntry.durationFixedSlider.min = durationFixedSlider.min;
        newEntry.durationFixedSlider.max = durationFixedSlider.max;

        newEntry.durationMinSlider.valNoCallback = durationMinSlider.val;
        newEntry.durationMinSlider.min = durationMinSlider.min;
        newEntry.durationMinSlider.max = durationMinSlider.max;

        newEntry.durationMaxSlider.valNoCallback = durationMaxSlider.val;
        newEntry.durationMaxSlider.min = durationMaxSlider.min;
        newEntry.durationMaxSlider.max = durationMaxSlider.max;

        return newEntry;
      }

      public void SetFromDefaults()
      {
        timingModeChooser.val = playlist.defaultTimingModeChooser.val;

        weightSlider.val = playlist.defaultWeightSlider.val;
        weightSlider.min = playlist.defaultWeightSlider.min;
        weightSlider.max = playlist.defaultWeightSlider.max;

        durationFixedSlider.val = playlist.defaultDurationFixedSlider.val;
        durationFixedSlider.min = playlist.defaultDurationFixedSlider.min;
        durationFixedSlider.max = playlist.defaultDurationFixedSlider.max;

        durationMinSlider.val = playlist.defaultDurationMinSlider.val;
        durationMinSlider.min = playlist.defaultDurationMinSlider.min;
        durationMinSlider.max = playlist.defaultDurationMinSlider.max;

        durationMaxSlider.val = playlist.defaultDurationMaxSlider.val;
        durationMaxSlider.min = playlist.defaultDurationMaxSlider.min;
        durationMaxSlider.max = playlist.defaultDurationMaxSlider.max;
      }

      private void HandleDurationFixedChange(float val)
      {
        durationMinSlider.valNoCallback = durationFixedSlider.val;
        durationMinSlider.min = durationFixedSlider.min;
        durationMinSlider.max = durationFixedSlider.max;

        durationMaxSlider.valNoCallback = durationFixedSlider.val;
        durationMaxSlider.min = durationFixedSlider.min;
        durationMaxSlider.max = durationFixedSlider.max;
      }
    }
  }
}
