using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Group
    {
      public static List<Group> list = new List<Group>();

      public string id { get { return name; } }
      public string name { get; private set; }

      public List<State> states
      {
        get { return State.list.FindAll((state) => state.group.id == this.id); }
      }

      public Group(string name = null)
      {
        SetNameUnique(name ?? "New Group");
        Group.list.Add(this);
      }

      public void Delete()
      {
        Group.list.Remove(this);
        foreach (State state in states)
        {
          state.Delete();
        }
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
          foreach (Group group in Group.list)
          {
            if (group != this && group.id == this.id)
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
