using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    JSONStorableString sendMessageAction;
    JSONStorableString broadcastMessageAction;

    void ActionsInit()
    {
      sendMessageAction = VaMUI.CreateStringAction("Send Message", "", HandleSendMessageAction);
      broadcastMessageAction = VaMUI.CreateStringAction("Broadcast Message", "", HandleBroadcastMessageAction);
    }

    public void CharacterStateManagerBroadcastMessage(string message)
    {
      foreach (MVRScript other in otherInstances)
      {
        other.SendMessage("CharacterStateManagerReceiveMessage", message);
      }
    }

    public void CharacterStateManagerReceiveMessage(string message)
    {
      foreach (MessageListener listener in Messages.listeners)
      {
        if (message == listener.text)
        {
          foreach (State target in listener.targets)
          {
            GroupPlayer.PlayState(target.group, target);
          }
        }
      }
    }

    void HandleSendMessageAction(string val)
    {
      sendMessageAction.valNoCallback = "";
      CharacterStateManagerReceiveMessage(MessageType.GetMessageTextCustom(Role.Self, val));
    }

    void HandleBroadcastMessageAction(string val)
    {
      broadcastMessageAction.valNoCallback = "";
      foreach (Role role in Role.list)
      {
        if (role.isSelf) continue;
        if (!role.useRoleToggle.val) continue;
        CharacterStateManagerBroadcastMessage(MessageType.GetMessageTextCustom(role.name, val));
      }
    }

    void BroadcastStateEnter(State state)
    {
      if (state == null) return;
      CharacterStateManagerReceiveMessage(MessageType.GetMessageTextEnterState(Role.Self, state.group.name, state.name));
      foreach (Role role in Role.list)
      {
        if (role.isSelf) continue;
        if (!role.useRoleToggle.val) continue;
        CharacterStateManagerBroadcastMessage(MessageType.GetMessageTextEnterState(role.name, state.group.name, state.name));
      }
    }

    void BroadcastStateExit(State state)
    {
      if (state == null) return;
      CharacterStateManagerReceiveMessage(MessageType.GetMessageTextExitState(Role.Self, state.group.name, state.name));
      foreach (Role role in Role.list)
      {
        if (role.isSelf) continue;
        if (!role.useRoleToggle.val) continue;
        CharacterStateManagerBroadcastMessage(MessageType.GetMessageTextExitState(role.name, state.group.name, state.name));
      }
    }
  }
}
