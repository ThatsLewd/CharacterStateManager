using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class State
    {
      public static List<State> list = new List<State>();

      public string id { get { return $"{group.name}::{name}"; } }
      public string name { get; private set; }
      public Group group { get; private set; }

      public State(Group group, string name = null)
      {
        this.group = group;
        SetNameUnique(name ?? "state");
        State.list.Add(this);
      }

      public State Clone(Group group = null)
      {
        if (group == null)
        {
          group = this.group;
        }
        State newState = new State(group);
        return newState;
      }

      public void Delete()
      {
        State.list.Remove(this);
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
          foreach (State state in State.list)
          {
            if (state != this && state.id == this.id)
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
