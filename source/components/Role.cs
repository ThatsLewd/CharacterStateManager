using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Role
    {
      public const string Self = "__self";

      // static stuff
      public static List<Role> list { get; private set; } = new List<Role>();
      public static VaMUI.VaMTextInput addRoleInput;

      public static void Init()
      {
        addRoleInput = VaMUI.CreateTextInput("New Role");
      }

      public static void AddRole(string name)
      {
        if (name == "" || name == Self || Role.list.Exists((r) => r.name == name)) return;
        Role.list.Add(new Role(name));
        Role.list.Sort((a, b) =>
        {
          if (a.isSelf) return -1;
          if (b.isSelf) return 1;
          return String.Compare(a.name, b.name);
        });
      }

      public static void RemoveRole(Role role)
      {
        if (role.isSelf) return;
        Role.list.Remove(role);
      }

      // instance stuff
      public string name { get; private set; }
      public bool isSelf { get { return name == Self; }}

      public VaMUI.VaMToggle useRoleToggle;

      public Role(string name)
      {
        this.name = name;
        InitUI();
      }

      private void InitUI()
      {
        useRoleToggle = VaMUI.CreateToggle("Broadcast as role", false);
        if (isSelf) useRoleToggle.valNoCallback = true;
      }

      public static JSONClass GetJSONTopLevel(ReferenceCollector rc)
      {
        JSONClass json = new JSONClass();
        json["list"] = new JSONArray();
        foreach (Role role in Role.list)
        {
          json["list"].AsArray.Add(role.GetJSON(rc));
        }
        return json;
      }

      public static void RestoreFromJSONTopLevel(JSONClass json)
      {
        Role.list.Clear();
        foreach (JSONNode node in json["list"].AsArray.Childs)
        {
          Role.list.Add(Role.FromJSON(node.AsObject));
        }
      }

      public JSONClass GetJSON(ReferenceCollector rc)
      {
        JSONClass json = new JSONClass();
        json["name"] = name;
        useRoleToggle.StoreJSON(json);
        return json;
      }

      public static Role FromJSON(JSONClass json)
      {
        string name = json["name"].Value;
        Role role = new Role(name);
        role.RestoreFromJSON(json);
        return role;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        useRoleToggle.RestoreFromJSON(json);
      }
    }
  }
}
