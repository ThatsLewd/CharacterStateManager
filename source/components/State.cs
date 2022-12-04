using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class State : BaseComponentWithId
    {
      public delegate void OnDeleteCallback(State state);
      public static event OnDeleteCallback OnDelete;

      public override string id { get; protected set; }
      public string name { get; set; }
      public Group group { get; private set; }

      public List<AnimationPlaylist> playlists { get; private set; } = new List<AnimationPlaylist>();

      public State(Group group, string name = null)
      {
        this.id = VaMUtils.GenerateRandomID();
        this.group = group;
        this.name = name ?? "state";
        this.group.states.Add(this);
        Group.OnDelete += HandleGroupDeleted;
        Layer.OnDelete += HandleLayerDeleted;
        Animation.OnDelete += HandleAnimationDeleted;
      }

      public State Clone(Group group = null)
      {
        string name = group == null ? Helpers.GetCopyName(this.name) : this.name;
        group = group ?? this.group;
        State newState = new State(group, name);
        foreach (AnimationPlaylist playlist in playlists)
        {
          newState.playlists.Add(playlist.Clone());
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

      public JSONStorableStringChooser modeStorable { get; private set; }

      public JSONStorableStringChooser defaultTimingModeStorable { get; private set; }
      public JSONStorableFloat defaultWeightStorable { get; private set; }
      public JSONStorableFloat defaultDurationFixedStorable { get; private set; }
      public JSONStorableFloat defaultDurationMinStorable { get; private set; }
      public JSONStorableFloat defaultDurationMaxStorable { get; private set; }

      public AnimationPlaylist(Layer layer)
      {
        this.layer = layer;
        modeStorable = new JSONStorableStringChooser("Play Mode", PlaylistMode.list.ToList(), PlaylistMode.Sequential, "Play Mode");
        defaultTimingModeStorable = new JSONStorableStringChooser("Timing Mode", TimingMode.list.ToList(), TimingMode.DurationFromAnimation, "Timing Mode");
        defaultWeightStorable = new JSONStorableFloat("Default Weight", 0.5f, 0f, 1f, true, true);
        defaultDurationFixedStorable = new JSONStorableFloat("Default Duration", 10f, 0f, 30f, true, true);
        defaultDurationFixedStorable.setCallbackFunction = HandleDurationFixedChange;
        defaultDurationMinStorable = new JSONStorableFloat("Default Duration Min", 10f, 0f, 30f, true, true);
        defaultDurationMaxStorable = new JSONStorableFloat("Default Duration Max", 10f, 0f, 30f, true, true);
      }

      public AnimationPlaylist Clone()
      {
        AnimationPlaylist newPlaylist = new AnimationPlaylist(layer);
        newPlaylist.modeStorable.valNoCallback = modeStorable.val;
        newPlaylist.defaultTimingModeStorable.valNoCallback = defaultTimingModeStorable.val;

        newPlaylist.defaultWeightStorable.valNoCallback = defaultWeightStorable.val;
        newPlaylist.defaultWeightStorable.min = defaultWeightStorable.min;
        newPlaylist.defaultWeightStorable.max = defaultWeightStorable.max;

        newPlaylist.defaultDurationFixedStorable.valNoCallback = defaultDurationFixedStorable.val;
        newPlaylist.defaultDurationFixedStorable.min = defaultDurationFixedStorable.min;
        newPlaylist.defaultDurationFixedStorable.max = defaultDurationFixedStorable.max;

        newPlaylist.defaultDurationMinStorable.valNoCallback = defaultDurationMinStorable.val;
        newPlaylist.defaultDurationMinStorable.min = defaultDurationMinStorable.min;
        newPlaylist.defaultDurationMinStorable.max = defaultDurationMinStorable.max;

        newPlaylist.defaultDurationMaxStorable.valNoCallback = defaultDurationMaxStorable.val;
        newPlaylist.defaultDurationMaxStorable.min = defaultDurationMaxStorable.min;
        newPlaylist.defaultDurationMaxStorable.max = defaultDurationMaxStorable.max;

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
        defaultDurationMinStorable.valNoCallback = defaultDurationFixedStorable.val;
        defaultDurationMinStorable.min = defaultDurationFixedStorable.min;
        defaultDurationMinStorable.max = defaultDurationFixedStorable.max;

        defaultDurationMaxStorable.valNoCallback = defaultDurationFixedStorable.val;
        defaultDurationMaxStorable.min = defaultDurationFixedStorable.min;
        defaultDurationMaxStorable.max = defaultDurationFixedStorable.max;
      }
    }

    public class PlaylistEntry
    {
      public AnimationPlaylist playlist { get; private set; }
      public Animation animation { get; private set; }

      public JSONStorableStringChooser timingModeStorable { get; private set; }
      public JSONStorableFloat weightStorable { get; private set; }
      public JSONStorableFloat durationFixedStorable { get; private set; }
      public JSONStorableFloat durationMinStorable { get; private set; }
      public JSONStorableFloat durationMaxStorable { get; private set; }

      public PlaylistEntry(AnimationPlaylist playlist, Animation animation)
      {
        this.playlist = playlist;
        this.animation = animation;
        timingModeStorable = new JSONStorableStringChooser("Timing Mode", TimingMode.list.ToList(), playlist.defaultTimingModeStorable.val, "Timing Mode");
        weightStorable = new JSONStorableFloat("Weight", playlist.defaultWeightStorable.val, playlist.defaultWeightStorable.min, playlist.defaultWeightStorable.max, true, true);
        durationFixedStorable = new JSONStorableFloat("Duration", playlist.defaultDurationFixedStorable.val, playlist.defaultDurationFixedStorable.min, playlist.defaultDurationFixedStorable.max, true, true);
        durationFixedStorable.setCallbackFunction = HandleDurationFixedChange;
        durationMinStorable = new JSONStorableFloat("Duration Min", playlist.defaultDurationMinStorable.val, playlist.defaultDurationMinStorable.min, playlist.defaultDurationMinStorable.max, true, true);
        durationMaxStorable = new JSONStorableFloat("Duration Max", playlist.defaultDurationMaxStorable.val, playlist.defaultDurationMaxStorable.min, playlist.defaultDurationMaxStorable.max, true, true);
      }

      public PlaylistEntry Clone(AnimationPlaylist playlist = null)
      {
        playlist = playlist ?? this.playlist;
        PlaylistEntry newEntry = new PlaylistEntry(playlist, animation);
        newEntry.timingModeStorable.valNoCallback = timingModeStorable.val;

        newEntry.weightStorable.valNoCallback = weightStorable.val;
        newEntry.weightStorable.min = weightStorable.min;
        newEntry.weightStorable.max = weightStorable.max;

        newEntry.durationFixedStorable.valNoCallback = durationFixedStorable.val;
        newEntry.durationFixedStorable.min = durationFixedStorable.min;
        newEntry.durationFixedStorable.max = durationFixedStorable.max;

        newEntry.durationMinStorable.valNoCallback = durationMinStorable.val;
        newEntry.durationMinStorable.min = durationMinStorable.min;
        newEntry.durationMinStorable.max = durationMinStorable.max;

        newEntry.durationMaxStorable.valNoCallback = durationMaxStorable.val;
        newEntry.durationMaxStorable.min = durationMaxStorable.min;
        newEntry.durationMaxStorable.max = durationMaxStorable.max;

        return newEntry;
      }

      public void SetFromDefaults()
      {
        timingModeStorable.val = playlist.defaultTimingModeStorable.val;

        weightStorable.val = playlist.defaultWeightStorable.val;
        weightStorable.min = playlist.defaultWeightStorable.min;
        weightStorable.max = playlist.defaultWeightStorable.max;

        durationFixedStorable.val = playlist.defaultDurationFixedStorable.val;
        durationFixedStorable.min = playlist.defaultDurationFixedStorable.min;
        durationFixedStorable.max = playlist.defaultDurationFixedStorable.max;

        durationMinStorable.val = playlist.defaultDurationMinStorable.val;
        durationMinStorable.min = playlist.defaultDurationMinStorable.min;
        durationMinStorable.max = playlist.defaultDurationMinStorable.max;

        durationMaxStorable.val = playlist.defaultDurationMaxStorable.val;
        durationMaxStorable.min = playlist.defaultDurationMaxStorable.min;
        durationMaxStorable.max = playlist.defaultDurationMaxStorable.max;
      }

      private void HandleDurationFixedChange(float val)
      {
        durationMinStorable.valNoCallback = durationFixedStorable.val;
        durationMinStorable.min = durationFixedStorable.min;
        durationMinStorable.max = durationFixedStorable.max;

        durationMaxStorable.valNoCallback = durationFixedStorable.val;
        durationMaxStorable.min = durationFixedStorable.min;
        durationMaxStorable.max = durationFixedStorable.max;
      }
    }
  }
}
