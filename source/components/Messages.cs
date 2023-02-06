using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public static class Messages
    {
      public static List<MessageListener> listeners { get; private set; } = new List<MessageListener>();
      public static VaMUI.VaMStringChooser roleChooser;
      public static VaMUI.VaMStringChooser messageTypeChooser;
      public static VaMUI.VaMTextInput customMessageInput;
      public static VaMUI.VaMTextInput groupInput;
      public static VaMUI.VaMTextInput stateInput;
      public static VaMUI.VaMStringChooser groupChooser;
      public static VaMUI.VaMStringChooser stateChooser;

      public static void Init()
      {
        roleChooser = VaMUI.CreateStringChooser("Role", defaultValue: Role.Self, callbackNoVal: instance.RequestRedraw);
        messageTypeChooser = VaMUI.CreateStringChooser("Message Type", MessageType.list.ToList(), MessageType.Custom, callbackNoVal: instance.RequestRedraw);
        customMessageInput = VaMUI.CreateTextInput("Custom Message");
        groupInput = VaMUI.CreateTextInput("Group Name");
        stateInput = VaMUI.CreateTextInput("State Name");
        groupChooser = VaMUI.CreateStringChooserKeyVal("Group", callbackNoVal: instance.RequestRedraw);
        stateChooser = VaMUI.CreateStringChooserKeyVal("State", callbackNoVal: instance.RequestRedraw);

        State.OnDelete += HandleStateDeleted;
      }

      public static string AddListener()
      {
        if (roleChooser.val == "") return null;

        string listenerText = "";
        if (messageTypeChooser.val == MessageType.Custom)
        {
          if (customMessageInput.val == "") return null;
          listenerText = MessageType.GetMessageTextCustom(roleChooser.val, customMessageInput.val);
        }
        else
        {
          string groupName = roleChooser.val == Role.Self ? VaMUtils.GetStringChooserDisplayFromVal(groupChooser.storable, groupChooser.val) : groupInput.val;
          string stateName = roleChooser.val == Role.Self ? VaMUtils.GetStringChooserDisplayFromVal(stateChooser.storable, stateChooser.val) : stateInput.val;
          if (groupName == "" || stateName == "") return null;
          if (messageTypeChooser.val == MessageType.EnterState)
          {
            listenerText = MessageType.GetMessageTextEnterState(roleChooser.val, groupName, stateName);
          }
          else if (messageTypeChooser.val == MessageType.ExitState)
          {
            listenerText = MessageType.GetMessageTextEnterState(roleChooser.val, groupName, stateName);
          }
        }

        if (listenerText == "") return null;
        if (listeners.Exists((l) => l.text == listenerText)) return null;
        listeners.Add(new MessageListener(listenerText));
        return listenerText;
      }

      public static void RemoveListener(MessageListener listener)
      {
        listeners.Remove(listener);
      }

      private static void HandleStateDeleted(State state)
      {
        foreach (MessageListener listener in listeners)
        {
          listener.targets.RemoveAll((t) => t == state);
        }
      }

      public static void SetRoleChooserChoices()
      {
        List<string> choices = new List<string>();
        choices.Add(Role.Self);
        foreach (Role role in Role.list)
        {
          choices.Add(role.name);
        }
        roleChooser.choices = choices;
      }

      public static void SetGroupStateChooserChoices()
      {
        List<KeyValuePair<string, string>> groupChoices = new List<KeyValuePair<string, string>>();
        List<KeyValuePair<string, string>> stateChoices = new List<KeyValuePair<string, string>>();

        foreach (Group group in Group.list)
        {
          groupChoices.Add(new KeyValuePair<string, string>(group.id, group.name));
        }
        groupChoices.Sort((a, b) => String.Compare(a.Value, b.Value));

        Group currentGroup = Group.list.Find((g) => g.id == groupChooser.val);
        if (currentGroup != null)
        {
          foreach (State state in currentGroup.states)
          {
            stateChoices.Add(new KeyValuePair<string, string>(state.id, state.name));
          }
        }
        stateChoices.Sort((a, b) => String.Compare(a.Value, b.Value));

        VaMUtils.SetStringChooserChoices(groupChooser.storable, groupChoices);
        VaMUtils.SetStringChooserChoices(stateChooser.storable, stateChoices);
      }

      public static JSONClass GetJSONTopLevel(ReferenceCollector rc)
      {
        JSONClass json = new JSONClass();
        json["listeners"] = new JSONArray();
        foreach (MessageListener listener in Messages.listeners)
        {
          json["listeners"].AsArray.Add(listener.GetJSON(rc));
        }
        return json;
      }

      public static void RestoreFromJSONTopLevel(JSONClass json)
      {
        Messages.listeners.Clear();
        foreach (JSONNode node in json["listeners"].AsArray.Childs)
        {
          MessageListener listener = MessageListener.FromJSON(node.AsObject);
          if (listener != null && !listeners.Contains(listener))
          {
            listeners.Add(listener);
          }
        }
      }
    }

    public static class MessageType
    {
      public const string Custom = "Custom";
      public const string EnterState = "Enter State";
      public const string ExitState = "Exit State";

      public static string GetMessageTextCustom(string role, string customName) { return $"{role}::{customName}"; }
      public static string GetMessageTextEnterState(string role, string group, string state) { return $"{role}::enter::{group}::{state}"; }
      public static string GetMessageTextExitState(string role, string group, string state) { return $"{role}::exit::{group}::{state}"; }

      public static readonly string[] list = new string[] { Custom, EnterState, ExitState };
    }

    public class MessageListener
    {
      public string text { get; private set; }
      public List<State> targets { get; private set; } = new List<State>();

      public MessageListener (string text)
      {
        this.text = text;
      }

      public void AddTarget(State target)
      {
        if (targets.Exists((t) => t == target)) return;
        targets.Add(target);
      }

      public void RemoveTarget(State target)
      {
        targets.Remove(target);
      }

      public JSONClass GetJSON(ReferenceCollector rc)
      {
        JSONClass json = new JSONClass();
        json["text"] = text;
        json["targets"] = new JSONArray();
        foreach (State state in targets)
        {
          JSONClass stateJSON = new JSONClass();
          rc.states[state.id] = state;
          stateJSON["state"] = state.id;
          rc.groups[state.group.id] = state.group;
          stateJSON["stateGroup"] = state.group.id;
          json["targets"].AsArray.Add(stateJSON);
        }
        return json;
      }

      public static MessageListener FromJSON(JSONClass json)
      {
        string text = json["text"];
        MessageListener listener = Messages.listeners.Find((l) => l.text == text);
        if (listener == null)
        {
          listener = new MessageListener(text);
        }
        foreach (JSONNode node in json["targets"].AsArray)
        {
          string stateId = node["state"].Value;
          string groupId = node["stateGroup"].Value;
          Group group = Group.list.Find((g) => g.id == groupId);
          State state = group?.states.Find((s) => s.id == stateId);
          if (state == null)
          {
            LogError($"Could not find state: {stateId}");
            continue;
          }
          listener.targets.Add(state);
        }

        // legacy
        if (json.HasKey("state") && json.HasKey("stateGroup"))
        {
          string stateId = json["state"].Value;
          string groupId = json["stateGroup"].Value;
          Group group = Group.list.Find((g) => g.id == groupId);
          State state = group?.states.Find((s) => s.id == stateId);
          if (state == null)
          {
            LogError($"Could not find state: {stateId}");
          }
          else
          {
            listener.targets.Add(state);
          }
        }
        return listener;
      }
    }
  }
}
