using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public static class LoopType
    {
      public const string PlayOnce = "Play Once";
      public const string Loop = "Loop";
      public const string PingPong = "Ping Pong";

      public static readonly string[] list = new string[] { PlayOnce, Loop, PingPong };
    }

    public partial class Animation : BaseComponent, IDisposable, INamedItem
    {
      public delegate void OnDeleteCallback(Animation animation);
      public static event OnDeleteCallback OnDelete;

      public override string id { get; protected set; }
      public string name { get; set; }
      public Layer layer { get; private set; }
      public List<Keyframe> keyframes { get; private set; } = new List<Keyframe>();

      public VaMUI.VaMStringChooser loopTypeChooser;
      public VaMUI.VaMSlider playbackSpeedSlider;
      public VaMUI.VaMSlider positionNoiseSlider;
      public VaMUI.VaMSlider rotationNoiseSlider;
      public VaMUI.VaMSlider morphNoiseSlider;

      public VaMUI.VaMStringChooser defaultEasingChooser;
      public VaMUI.VaMSlider defaultDurationSlider;

      public VaMUI.EventTrigger onEnterTrigger = VaMUI.CreateEventTrigger("On Enter Animation");
      public VaMUI.ValueTrigger onPlayingTrigger = VaMUI.CreateValueTrigger("On Animation Playing");
      public VaMUI.EventTrigger onExitTrigger = VaMUI.CreateEventTrigger("On Exit Animation");

      public Animation(Layer layer, string name = null)
      {
        this.id = VaMUtils.GenerateRandomID(32);
        this.name = name ?? "animation";
        Helpers.EnsureUniqueName(layer.animations, this);
        this.layer = layer;
        this.loopTypeChooser = VaMUI.CreateStringChooser("Loop Type", LoopType.list.ToList(), LoopType.Loop);
        this.playbackSpeedSlider = VaMUI.CreateSlider("Playback Speed", 1f, 0f, 2f);
        this.positionNoiseSlider = VaMUI.CreateSlider("Position Noise", 0f, 0f, 1f);
        this.rotationNoiseSlider = VaMUI.CreateSlider("Rotation Noise", 0f, 0f, 1f);
        this.morphNoiseSlider = VaMUI.CreateSlider("Morph Noise", 0f, 0f, 1f);
        this.defaultEasingChooser = VaMUI.CreateStringChooser("Default Easing", Easing.list.ToList(), Easing.EasingType.Linear);
        this.defaultDurationSlider = VaMUI.CreateSlider("Default Duration", 0.5f, 0f, 1f);
        layer.animations.Add(this);
        Layer.OnDelete += HandleLayerDeleted;
      }

      public Animation Clone(Layer layer = null)
      {
        string name = layer == null ? Helpers.GetCopyName(this.name) : this.name;
        layer = layer ?? this.layer;
        Animation newAnimation = new Animation(layer, name);
        newAnimation.loopTypeChooser.valNoCallback = loopTypeChooser.val;
        Helpers.SetSliderValues(newAnimation.playbackSpeedSlider, playbackSpeedSlider.val, playbackSpeedSlider.min, playbackSpeedSlider.max);
        Helpers.SetSliderValues(newAnimation.positionNoiseSlider, positionNoiseSlider.val, positionNoiseSlider.min, positionNoiseSlider.max);
        Helpers.SetSliderValues(newAnimation.rotationNoiseSlider, rotationNoiseSlider.val, rotationNoiseSlider.min, rotationNoiseSlider.max);
        Helpers.SetSliderValues(newAnimation.morphNoiseSlider, morphNoiseSlider.val, morphNoiseSlider.min, morphNoiseSlider.max);
        newAnimation.defaultEasingChooser.valNoCallback = defaultEasingChooser.val;
        Helpers.SetSliderValues(newAnimation.defaultDurationSlider, defaultDurationSlider.val, defaultDurationSlider.min, defaultDurationSlider.max);
        newAnimation.onEnterTrigger = onEnterTrigger.Clone();
        newAnimation.onPlayingTrigger = onPlayingTrigger.Clone();
        newAnimation.onExitTrigger = onExitTrigger.Clone();
        foreach (Keyframe keyframe in keyframes)
        {
          keyframe.Clone(newAnimation);
        }
        return newAnimation;
      }

      private void HandleLayerDeleted(Layer layer)
      {
        if (layer == this.layer) Dispose();
      }

      public void Dispose()
      {
        layer.animations.Remove(this);
        Animation.OnDelete?.Invoke(this);
        Layer.OnDelete -= HandleLayerDeleted;
      }

      public void CopyActions()
      {
        ActionClipboard.onEnterTrigger = onEnterTrigger;
        ActionClipboard.onPlayingTrigger = onPlayingTrigger;
        ActionClipboard.onExitTrigger = onExitTrigger;
      }

      public void PasteActions()
      {
        if (ActionClipboard.onEnterTrigger != null)
          onEnterTrigger = ActionClipboard.onEnterTrigger.Clone(onEnterTrigger.name);
        if (ActionClipboard.onPlayingTrigger != null)
          onPlayingTrigger = ActionClipboard.onPlayingTrigger.Clone(onPlayingTrigger.name);
        if (ActionClipboard.onExitTrigger != null)
          onExitTrigger = ActionClipboard.onExitTrigger.Clone(onExitTrigger.name);

        CharacterStateManager.instance.RequestRedraw();
      }

      public float GetTotalDuration()
      {
        bool omitLast = loopTypeChooser.val != LoopType.Loop;
        float t = 0f;
        for (int i = 0; i < keyframes.Count; i++)
        {
          if (omitLast && i == keyframes.Count - 1) break;
          Keyframe keyframe = keyframes[i];
          t += keyframe.durationSlider.val;
        }
        return t;
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["id"] = id;
        json["name"] = name;
        json["keyframes"] = new JSONArray();
        foreach (Keyframe keyframe in keyframes)
        {
          json["keyframes"].AsArray.Add(keyframe.GetJSON());
        }
        loopTypeChooser.storable.StoreJSON(json);
        playbackSpeedSlider.storable.StoreJSON(json);
        positionNoiseSlider.storable.StoreJSON(json);
        rotationNoiseSlider.storable.StoreJSON(json);
        morphNoiseSlider.storable.StoreJSON(json);
        defaultEasingChooser.storable.StoreJSON(json);
        defaultDurationSlider.storable.StoreJSON(json);
        onEnterTrigger.StoreJSON(json);
        onPlayingTrigger.StoreJSON(json);
        onExitTrigger.StoreJSON(json);
        return json;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        id = json["id"].Value;
        name = json["name"].Value;
        Helpers.DisposeList(keyframes);
        foreach (JSONNode node in json["keyframes"].AsArray)
        {
          new Keyframe(this).RestoreFromJSON(node.AsObject);
        }
        loopTypeChooser.storable.RestoreFromJSON(json);
        playbackSpeedSlider.storable.RestoreFromJSON(json);
        positionNoiseSlider.storable.RestoreFromJSON(json);
        rotationNoiseSlider.storable.RestoreFromJSON(json);
        morphNoiseSlider.storable.RestoreFromJSON(json);
        defaultEasingChooser.storable.RestoreFromJSON(json);
        defaultDurationSlider.storable.RestoreFromJSON(json);
        onEnterTrigger.RestoreFromJSON(json);
        onPlayingTrigger.RestoreFromJSON(json);
        onExitTrigger.RestoreFromJSON(json);
      }
    }
  }
}
