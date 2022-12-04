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
    
    public partial class Animation : BaseComponentWithId
    {
      public delegate void OnDeleteCallback(Animation animation);
      public static event OnDeleteCallback OnDelete;

      public override string id { get; protected set; }
      public string name { get; set; }
      public Layer layer { get; private set; }
      public List<Keyframe> keyframes { get; private set; } = new List<Keyframe>();

      public JSONStorableStringChooser loopTypeStorable;
      public JSONStorableFloat playbackSpeedStorable;

      public JSONStorableStringChooser defaultEasingStorable;
      public JSONStorableFloat defaultDurationStorable;

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
        this.layer = layer;
        this.loopTypeStorable = CreateLoopTypeStorable(LoopType.Loop);
        this.playbackSpeedStorable = CreatePlaybackSpeedStorable(1f, 0f, 2f);
        this.defaultEasingStorable = CreateEasingStorable(Easing.EasingType.Linear);
        this.defaultDurationStorable = CreateDurationStorable(1f, 0f, 10f);
        layer.animations.Add(this);
        Layer.OnDelete += HandleLayerDeleted;
      }

      public Animation Clone(Layer layer = null)
      {
        string name = layer == null ? Helpers.GetCopyName(this.name) : this.name;
        layer = layer ?? this.layer;
        Animation newAnimation = new Animation(layer, name);
        newAnimation.loopTypeStorable = CreateLoopTypeStorable(loopTypeStorable.val);
        newAnimation.playbackSpeedStorable = CreatePlaybackSpeedStorable(playbackSpeedStorable.val, playbackSpeedStorable.min, playbackSpeedStorable.max);
        newAnimation.defaultEasingStorable = CreateEasingStorable(defaultEasingStorable.val);
        newAnimation.defaultDurationStorable = CreateDurationStorable(defaultDurationStorable.val, defaultDurationStorable.min, defaultDurationStorable.max);
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
        if (layer == this.layer) Delete();
      }

      public void Delete()
      {
        layer.animations.Remove(this);
        Animation.OnDelete?.Invoke(this);
        Layer.OnDelete -= HandleLayerDeleted;
      }

      private JSONStorableStringChooser CreateLoopTypeStorable(string defaultValue)
      {
        return new JSONStorableStringChooser("Loop Type", LoopType.list.ToList(), defaultValue, "Loop Type");
      }

      private JSONStorableFloat CreatePlaybackSpeedStorable(float defaultValue, float minValue, float maxValue)
      {
        return new JSONStorableFloat("Playback Speed", defaultValue, minValue, maxValue, true, true);
      }

      private JSONStorableStringChooser CreateEasingStorable(string defaultValue)
      {
        return new JSONStorableStringChooser("Default Easing", Easing.list.ToList(), defaultValue, "Default Easing");
      }

      private JSONStorableFloat CreateDurationStorable(float defaultValue, float minValue, float maxValue)
      {
        return new JSONStorableFloat("Default Duration", defaultValue, minValue, maxValue, true, true);
      }
    }
  }
}
