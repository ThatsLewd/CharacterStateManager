using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Transition : BaseComponentWithId
    {
      public static List<Transition> list = new List<Transition>();

      public override string id { get; protected set; }
      public Animation from { get; private set; }
      public Animation to { get; private set; }

      public Transition(Animation from, Animation to)
      {
        this.id = Utils.GenerateRandomID();
        this.from = from;
        this.to = to;
        Transition.list.Add(this);
      }

      public Transition Clone(Animation from = null, Animation to = null)
      {
        from = from ?? this.from;
        to = to ?? this.to;
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
