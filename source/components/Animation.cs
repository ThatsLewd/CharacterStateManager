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
        this.layer = layer;
        this.loopTypeChooser = CreateLoopTypeChooser(LoopType.Loop);
        this.playbackSpeedSlider = CreatePlaybackSpeedSlider(1f, 0f, 2f);
        this.defaultEasingChooser = CreateEasingChooser(Easing.EasingType.Linear);
        this.defaultDurationSlider = CreateDurationSlider(1f, 0f, 10f);
        layer.animations.Add(this);
        Layer.OnDelete += HandleLayerDeleted;
      }

      public Animation Clone(Layer layer = null)
      {
        string name = layer == null ? Helpers.GetCopyName(this.name) : this.name;
        layer = layer ?? this.layer;
        Animation newAnimation = new Animation(layer, name);
        newAnimation.loopTypeChooser = CreateLoopTypeChooser(loopTypeChooser.val);
        newAnimation.playbackSpeedSlider = CreatePlaybackSpeedSlider(playbackSpeedSlider.val, playbackSpeedSlider.min, playbackSpeedSlider.max);
        newAnimation.defaultEasingChooser = CreateEasingChooser(defaultEasingChooser.val);
        newAnimation.defaultDurationSlider = CreateDurationSlider(defaultDurationSlider.val, defaultDurationSlider.min, defaultDurationSlider.max);
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

      private VaMUI.VaMStringChooser CreateLoopTypeChooser(string defaultValue)
      {
        return VaMUI.CreateStringChooser("Loop Type", LoopType.list.ToList(), defaultValue);
      }

      private VaMUI.VaMSlider CreatePlaybackSpeedSlider(float defaultValue, float minValue, float maxValue)
      {
        return VaMUI.CreateSlider("Playback Speed", defaultValue, minValue, maxValue);
      }

      private VaMUI.VaMStringChooser CreateEasingChooser(string defaultValue)
      {
        return VaMUI.CreateStringChooser("Default Easing", Easing.list.ToList(), defaultValue);
      }

      private VaMUI.VaMSlider CreateDurationSlider(float defaultValue, float minValue, float maxValue)
      {
        return VaMUI.CreateSlider("Default Duration", defaultValue, minValue, maxValue);
      }
    }
  }
}
