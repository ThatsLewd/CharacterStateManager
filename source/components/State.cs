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
        SetNameUnique(name ?? "New State");
        State.list.Add(this);
      }

      public void Delete()
      {
        State.list.Remove(this);
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
