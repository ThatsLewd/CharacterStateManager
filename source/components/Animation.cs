using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public partial class Animation : BaseComponentWithId
    {
      public override string id { get; protected set; }
      public string name { get; set; }
      public Layer layer { get; private set; }
      public List<Keyframe> keyframes { get; private set; } = new List<Keyframe>();

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
        this.id = Utils.GenerateRandomID();
        this.name = name ?? "animation";
        this.layer = layer;
        layer.animations.Add(this);
      }

      public Animation Clone(Layer layer = null)
      {
        string name = layer == null ? Helpers.GetCopyName(this.name) : this.name;
        layer = layer ?? this.layer;
        Animation newAnimation = new Animation(layer, name);
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

      public void Delete()
      {
        layer.animations.Remove(this);
        foreach (Keyframe keyframe in keyframes.ToArray())
        {
          keyframe.Delete();
        }
        foreach (Transition transition in fromTransitions.ToArray())
        {
          transition.Delete();
        }
        foreach (Transition transition in toTransitions.ToArray())
        {
          transition.Delete();
        }
      }
    }
  }
}
