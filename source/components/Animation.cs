using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
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

      public VaMUI.VaMStringChooser defaultEasingChooser;
      public VaMUI.VaMSlider defaultDurationSlider;

      public EventTrigger onEnterTrigger = VaMTrigger.Create<EventTrigger>("On Enter Animation");
      public ValueTrigger onPlayingTrigger = VaMTrigger.Create<ValueTrigger>("On Animation Playing");
      public EventTrigger onExitTrigger = VaMTrigger.Create<EventTrigger>("On Exit Animation");

      public List<Transition> fromTransitions
      {
        get { return Transition.list.FindAll((transition) => transition.from == this); }
      }

      public List<Transition> toTransitions
      {
        get { return Transition.list.FindAll((transition) => transition.to == this); }
      }

      public Animation(Layer layer, string name = null)
      {
        this.id = VaMUtils.GenerateRandomID();
        this.name = name ?? "animation";
        Helpers.EnsureUniqueName(layer.animations, this);
        this.layer = layer;
        this.loopTypeChooser = VaMUI.CreateStringChooser("Loop Type", LoopType.list.ToList(), LoopType.Loop);
        this.playbackSpeedSlider = VaMUI.CreateSlider("Playback Speed", 1f, 0f, 2f);
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
        newAnimation.defaultEasingChooser.valNoCallback = defaultEasingChooser.val;
        Helpers.SetSliderValues(newAnimation.defaultDurationSlider, defaultDurationSlider.val, defaultDurationSlider.min, defaultDurationSlider.max);
        newAnimation.onEnterTrigger = VaMTrigger.Clone(onEnterTrigger);
        newAnimation.onPlayingTrigger = VaMTrigger.Clone(onPlayingTrigger);
        newAnimation.onExitTrigger = VaMTrigger.Clone(onExitTrigger);
        foreach (Keyframe keyframe in keyframes)
        {
          keyframe.Clone(newAnimation);
        }
        foreach (Transition transition in fromTransitions)
        {
          transition.Clone(from: newAnimation);
        }
        foreach (Transition transition in toTransitions)
        {
          transition.Clone(to: newAnimation);
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
          onEnterTrigger = VaMTrigger.Clone(ActionClipboard.onEnterTrigger, onEnterTrigger.name);
        if (ActionClipboard.onPlayingTrigger != null)
          onPlayingTrigger = VaMTrigger.Clone(ActionClipboard.onPlayingTrigger, onPlayingTrigger.name);
        if (ActionClipboard.onExitTrigger != null)
          onExitTrigger = VaMTrigger.Clone(ActionClipboard.onExitTrigger, onExitTrigger.name);

        CharacterStateManager.instance.RequestRedraw();
      }

      public float GetTotalDuration(bool omitLast = false)
      {
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
        keyframes.Clear();
        foreach (JSONNode node in json["keyframes"].AsArray)
        {
          new Keyframe(this).RestoreFromJSON(node.AsObject);
        }
        loopTypeChooser.storable.RestoreFromJSON(json);
        playbackSpeedSlider.storable.RestoreFromJSON(json);
        defaultEasingChooser.storable.RestoreFromJSON(json);
        defaultDurationSlider.storable.RestoreFromJSON(json);
        onEnterTrigger.RestoreFromJSON(json);
        onPlayingTrigger.RestoreFromJSON(json);
        onExitTrigger.RestoreFromJSON(json);
      }
    }

    public static class LoopType
    {
      public const string PlayOnce = "Play Once";
      public const string Loop = "Loop";
      public const string PingPong = "Ping Pong";

      public static readonly string[] list = new string[] { PlayOnce, Loop, PingPong };
    }
  }
}
