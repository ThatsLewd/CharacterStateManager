using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Transition
    {
      public static List<Transition> list = new List<Transition>();

      public string id { get { return $"{from.id}->{to.id}"; } }
      public Animation from { get; private set; }
      public Animation to { get; private set; }

      public Transition(Animation from, Animation to)
      {
        this.from = from;
        this.to = to;
        Transition.list.Add(this);
      }

      public Transition Clone(Animation from = null, Animation to = null)
      {
        if (from == null)
        {
          from = this.from;
        }
        if (to == null)
        {
          to = this.to;
        }
        Transition newTransition = new Transition(from, to);
        return newTransition;
      }

      public void Delete()
      {
        Transition.list.Remove(this);
      }
    }
  }
}
