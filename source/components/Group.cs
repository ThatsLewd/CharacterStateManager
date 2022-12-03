using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Group : BaseComponentWithId
    {
      public static List<Group> list = new List<Group>();

      public override string id { get; protected set; }
      public string name { get; set; }
      public List<State> states { get; private set; } = new List<State>();

      public Group(string name = null)
      {
        this.id = Utils.GenerateRandomID();
        this.name = name ?? "group";
        Group.list.Add(this);
      }

      public Group Clone()
      {
        Group newGroup = new Group(Helpers.GetCopyName(name));
        foreach (State state in states)
        {
          state.Clone(newGroup);
        }
        return newGroup;
      }

      public void Delete()
      {
        Group.list.Remove(this);
        foreach (State state in states.ToArray())
        {
          state.Delete();
        }
      }
    }
  }
}
