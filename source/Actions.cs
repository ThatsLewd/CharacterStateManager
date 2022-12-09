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

    public new void BroadcastMessage(string message)
    {

    }

    public void ReceiveMessage(string message)
    {
      foreach (MessageListener listener in Messages.listeners)
      {
        if (message == listener.text)
        {
          GroupPlayer.PlayState(listener.target.group, listener.target);
        }
      }
    }

    void HandleSendMessageAction(string val)
    {
      sendMessageAction.valNoCallback = "";
      ReceiveMessage(MessageType.GetMessageTextCustom(Role.Self, val));
    }

    void HandleBroadcastMessageAction(string val)
    {
      broadcastMessageAction.valNoCallback = "";
      foreach (Role role in Role.list)
      {
        if (role.isSelf) continue;
        if (!role.useRoleToggle.val) continue;
        BroadcastMessage(MessageType.GetMessageTextCustom(role.name, val));
      }
    }

    void BroadcastStateEnter(State state)
    {
      if (state == null) return;
      ReceiveMessage(MessageType.GetMessageTextEnterState(Role.Self, state.group.name, state.name));
      foreach (Role role in Role.list)
      {
        if (role.isSelf) continue;
        if (!role.useRoleToggle.val) continue;
        BroadcastMessage(MessageType.GetMessageTextEnterState(role.name, state.group.name, state.name));
      }
    }

    void BroadcastStateExit(State state)
    {
      if (state == null) return;
      ReceiveMessage(MessageType.GetMessageTextExitState(Role.Self, state.group.name, state.name));
      foreach (Role role in Role.list)
      {
        if (role.isSelf) continue;
        if (!role.useRoleToggle.val) continue;
        BroadcastMessage(MessageType.GetMessageTextExitState(role.name, state.group.name, state.name));
      }
    }
  }
}
