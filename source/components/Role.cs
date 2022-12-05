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
      public static List<Role> list { get; private set; } = new List<Role>() { new Role(Self) };
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
        useRoleToggle = VaMUI.CreateToggle("Broadcast as role", false);
        if (isSelf) useRoleToggle.valNoCallback = true;
      }
    }
  }
}
