using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class State : BaseComponentWithId
    {
      public override string id { get; protected set; }
      public string name { get; set; }
      public Group group { get; private set; }

      public State(Group group, string name = null)
      {
        this.id = VaMUtils.GenerateRandomID();
        this.group = group;
        this.name = name ?? "state";
        this.group.states.Add(this);
      }

      public State Clone(Group group = null)
      {
        string name = group == null ? Helpers.GetCopyName(this.name) : this.name;
        group = group ?? this.group;
        State newState = new State(group, name);
        return newState;
      }

      public void Delete()
      {
        group.states.Remove(this);
      }
    }
  }
}
