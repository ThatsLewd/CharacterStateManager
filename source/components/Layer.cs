using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Layer
    {
      public static List<Layer> list = new List<Layer>();

      public string id { get { return name; } }
      public string name { get; private set; }

      public List<Animation> animations
      {
        get { return Animation.list.FindAll((animation) => animation.layer.id == this.id); }
      }

      public Layer(string name = null)
      {
        SetNameUnique(name ?? "layer");
        Layer.list.Add(this);
      }

      public Layer(Layer source) : this()
      {
        foreach (Animation animation in source.animations)
        {
          new Animation(this, animation);
        }
      }

      public void Delete()
      {
        Layer.list.Remove(this);
        foreach (Animation animation in animations)
        {
          animation.Delete();
        }
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
          foreach (Layer layer in Layer.list)
          {
            if (layer != this && layer.id == this.id)
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
