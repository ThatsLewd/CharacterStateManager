using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Group : BaseComponent, IDisposable
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
        this.id = VaMUtils.GenerateRandomID();
        this.name = name ?? "group";
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

      public static JSONClass GetJSONTopLevel()
      {
        JSONClass json = new JSONClass();
        json["list"] = new JSONArray();
        foreach (Group group in Group.list)
        {
          json["list"].AsArray.Add(group.GetJSON());
        }
        return json;
      }

      public static void RestoreFromJSONTopLevel(JSONClass json)
      {
        Group.list.Clear();
        foreach (JSONNode node in json["list"].AsArray.Childs)
        {
          new Group().RestoreFromJSON(node.AsObject);
        }
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["id"] = id;
        json["name"] = name;
        json["states"] = new JSONArray();
        foreach (State state in states)
        {
          json["states"].AsArray.Add(state.GetJSON());
        }
        json["initialState"] = initialState?.id;
        playbackEnabledToggle.storable.StoreJSON(json);
        return json;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        id = json["id"].Value;
        name = json["name"].Value;
        states.Clear();
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
