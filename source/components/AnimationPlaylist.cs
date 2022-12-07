using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
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

    public partial class AnimationPlaylist : IDisposable
    {
      public delegate void OnDeleteCallback(AnimationPlaylist playlist);
      public static event OnDeleteCallback OnDelete;

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

      public void Dispose()
      {
        AnimationPlaylist.OnDelete?.Invoke(this);
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

    public class PlaylistEntry : IWeightedItem
    {
      public AnimationPlaylist playlist { get; private set; }
      public Animation animation { get; private set; }

      public VaMUI.VaMStringChooser timingModeChooser { get; private set; }
      public VaMUI.VaMSlider weightSlider { get; private set; }
      public VaMUI.VaMSlider durationFixedSlider { get; private set; }
      public VaMUI.VaMSlider durationMinSlider { get; private set; }
      public VaMUI.VaMSlider durationMaxSlider { get; private set; }

      public float weight { get { return weightSlider.val; } }

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
