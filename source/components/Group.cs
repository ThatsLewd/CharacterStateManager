using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Group : BaseComponentWithId
    {
      public delegate void OnDeleteCallback(Group group);
      public static event OnDeleteCallback OnDelete;

      public static List<Group> list = new List<Group>();

      public override string id { get; protected set; }
      public string name { get; set; }
      public List<State> states { get; private set; } = new List<State>();

      public State initialState { get; set; } = null;

      public Group(string name = null)
      {
        this.id = VaMUtils.GenerateRandomID();
        this.name = name ?? "group";
        Group.list.Add(this);
      }

      public Group Clone()
      {
        Group newGroup = new Group(Helpers.GetCopyName(name));
        foreach (State state in states)
        {
          State newState = state.Clone(newGroup);
          if (state == initialState)
          {
            newGroup.initialState = newState;
          }
        }
        return newGroup;
      }

      public void Delete()
      {
        Group.list.Remove(this);
        Group.OnDelete?.Invoke(this);
      }
    }
  }
}
