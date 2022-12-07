using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Transition : BaseComponent, IDisposable
    {
      public delegate void OnDeleteCallback(Transition transition);
      public static event OnDeleteCallback OnDelete;

      public static List<Transition> list = new List<Transition>();

      public override string id { get; protected set; }
      public Animation from { get; private set; }
      public Animation to { get; private set; }

      public Transition(Animation from, Animation to)
      {
        this.id = VaMUtils.GenerateRandomID();
        this.from = from;
        this.to = to;
        Transition.list.Add(this);
        Animation.OnDelete += OnAnimationDeleted;
      }

      public Transition Clone(Animation from = null, Animation to = null)
      {
        from = from ?? this.from;
        to = to ?? this.to;
        Transition newTransition = new Transition(from, to);
        return newTransition;
      }

      private void OnAnimationDeleted(Animation animation)
      {
        if (from == animation || to == animation) Dispose();
      }

      public void Dispose()
      {
        Transition.list.Remove(this);
        Transition.OnDelete?.Invoke(this);
        Animation.OnDelete -= OnAnimationDeleted;
      }
    }
  }
}
