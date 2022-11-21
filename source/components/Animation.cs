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
        SetNameUnique(name ?? "New Animation");
        Animation.list.Add(this);
      }

      public void Delete()
      {
        Animation.list.Remove(this);
      }

      public void SetNameUnique(string name)
      {
        for (int i = 1; true; i++)
        {
          if (i == 1)
          {
            this.name = $"{name}";
          }
          else
          {
            this.name = $"{name} {i.ToString().PadLeft(3, '0')}";
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
