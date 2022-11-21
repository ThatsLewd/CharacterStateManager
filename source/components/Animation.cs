using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Animation
    {
      public static List<Animation> list = new List<Animation>();

      public string id { get { return $"{layer.name}::{name}"; } }
      public string name { get; private set; }
      public Layer layer { get; private set; }

      public List<Transition> fromTransitions
      {
        get { return Transition.list.FindAll((transition) => transition.from.id == this.id); }
      }

      public List<Transition> toTransitions
      {
        get { return Transition.list.FindAll((transition) => transition.to.id == this.id); }
      }

      public Animation(Layer layer, string name = null)
      {
        this.layer = layer;
        SetNameUnique(name ?? "animation");
        Animation.list.Add(this);
      }

      public Animation(Layer layer, Animation source) : this(layer)
      {
        foreach (Transition transition in source.fromTransitions)
        {
          new Transition(this, transition.to);
        }
        foreach (Transition transition in source.toTransitions)
        {
          new Transition(transition.from, this);
        }
      }

      public Animation(Animation source) : this(source.layer, source) { }

      public void Delete()
      {
        Animation.list.Remove(this);
      }

      public void SetNameUnique(string name)
      {
        for (int i = 0; true; i++)
        {
          if (i == 0)
          {
            this.name = $"{name}";
          }
          else
          {
            this.name = $"{name} copy{i.ToString().PadLeft(3, '0')}";
          }

          bool matchFound = false;
          foreach (Animation animation in Animation.list)
          {
            if (animation != this && animation.id == this.id)
            {
              matchFound = true;
              break;
            }
          }

          if (!matchFound)
          {
            break;
          }
        }
      }
    }
  }
}
