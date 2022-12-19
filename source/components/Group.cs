using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Group : BaseComponent, IDisposable, INamedItem
    {
      public delegate void OnDeleteCallback(Group group);
      public static event OnDeleteCallback OnDelete;

      public static List<Group> list = new List<Group>();

      public override string id { get; protected set; }
      public string name { get; set; }
      public List<State> states { get; private set; } = new List<State>();

      public State initialState = null;
      public VaMUI.VaMToggle playbackEnabledToggle;

      public Group(string name = null)
      {
        this.id = VaMUtils.GenerateRandomID(32);
        this.name = name ?? "group";
        Helpers.EnsureUniqueName(Group.list, this);
        playbackEnabledToggle = VaMUI.CreateToggle("Playback Enabled", true);
        Group.list.Add(this);
      }

      public Group Clone()
      {
        Group newGroup = new Group(Helpers.GetCopyName(name));
        newGroup.playbackEnabledToggle.valNoCallback = playbackEnabledToggle.val;
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

      public void Dispose()
      {
        Group.list.Remove(this);
        Group.OnDelete?.Invoke(this);
      }

      public static Group FindById(string id)
      {
        return Group.list.Find((g) => g.id == id);
      }

      public static JSONClass GetJSONTopLevel(ReferenceCollector rc)
      {
        JSONClass json = new JSONClass();
        json["list"] = new JSONArray();
        foreach (Group group in Group.list)
        {
          rc.groups[group.id] = group;
          json["list"].AsArray.Add(group.GetJSON(rc));
        }
        return json;
      }

      public static void RestoreFromJSONTopLevel(JSONClass json)
      {
        Helpers.DisposeList(Group.list);
        foreach (JSONNode node in json["list"].AsArray.Childs)
        {
          new Group().RestoreFromJSON(node.AsObject);
        }
      }

      public JSONClass GetJSON(ReferenceCollector rc)
      {
        JSONClass json = new JSONClass();
        json["id"] = id;
        json["name"] = name;
        json["states"] = new JSONArray();
        foreach (State state in states)
        {
          rc.states[state.id] = state;
          json["states"].AsArray.Add(state.GetJSON(rc));
        }
        json["initialState"] = initialState?.id;
        playbackEnabledToggle.storable.StoreJSON(json);
        return json;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        id = json["id"].Value;
        name = json["name"].Value;
        Helpers.DisposeList(states);
        foreach (JSONNode node in json["states"].AsArray.Childs)
        {
          new State(this).RestoreFromJSON(node.AsObject);
        }
        foreach (JSONNode node in json["states"].AsArray.Childs)
        {
          State state = states.Find((s) => s.id == node["id"].Value);
          state.LateRestoreFromJSON(node.AsObject);
        }
        string initialStateId = json["initialState"]?.Value;
        initialState = states.Find((s) => s.id == initialStateId);
        playbackEnabledToggle.storable.RestoreFromJSON(json);
      }
    }
  }
}
