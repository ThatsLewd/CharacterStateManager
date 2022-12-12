using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class StatePlayer : IDisposable
    {
      public GroupPlayer groupPlayer { get; private set; }
      public PlaylistPlayer playlistPlayer { get; private set; } = null;
      bool disposed = false;

      public State currentState { get; private set; } = null;
      public float time { get; private set; } = 0f;
      public bool donePlaying { get { return GetIsDonePlaying(); } }

      float randomTargetTime = 0f;
      public float targetTime { get { return GetTargetTime(); } }

      public StatePlayer(GroupPlayer groupPlayer)
      {
        this.groupPlayer = groupPlayer;
      }

      void RegisterHandlers(State newState)
      {
        if (newState == null) return;
        newState.transitionModeChooser.storable.setCallbackFunction += HandleSetTransitionMode;
        newState.minDurationSlider.storable.setCallbackFunction += HandleSetRandomDuration;
        newState.maxDurationSlider.storable.setCallbackFunction += HandleSetRandomDuration;
      }

      void UnregisterHandlers(State oldState)
      {
        if (oldState == null) return;
        oldState.transitionModeChooser.storable.setCallbackFunction -= HandleSetTransitionMode;
        oldState.minDurationSlider.storable.setCallbackFunction -= HandleSetRandomDuration;
        oldState.maxDurationSlider.storable.setCallbackFunction -= HandleSetRandomDuration;
      }

      public void Dispose()
      {
        disposed = true;
        UnregisterHandlers(currentState);
        if (playlistPlayer != null)
        {
          playlistPlayer.Dispose();
        }
      }

      void HandleSetTransitionMode(string val)
      {
        if (val == TransitionMode.RandomDuration)
        {
          NewRandomTargetTime();
        }
        if (val == TransitionMode.PlaylistCompleted)
        {
          RefreshPlaylist();
        }
      }

      void HandleSetRandomDuration(float val)
      {
        NewRandomTargetTime();
      }

      bool GetIsDonePlaying()
      {
        if (currentState == null) return false;
        switch (currentState.transitionModeChooser.val)
        {
          case TransitionMode.PlaylistCompleted:
            return playlistPlayer?.playlistCompleted ?? false;
          case TransitionMode.FixedDuration:
            return time >= currentState.fixedDurationSlider.val;
          case TransitionMode.RandomDuration:
            return time >= randomTargetTime;
          default:
            return false;
        }
      }

      float GetTargetTime()
      {
        if (currentState == null) return 0f;
        switch (currentState.transitionModeChooser.val)
        {
          case TransitionMode.FixedDuration:
            return currentState.fixedDurationSlider.val;
          case TransitionMode.RandomDuration:
            return randomTargetTime;
          default:
            return 0f;
        }
      }

      void NewRandomTargetTime()
      {
        if (currentState == null) randomTargetTime = 0f;
        randomTargetTime = UnityEngine.Random.Range(currentState.minDurationSlider.val, currentState.maxDurationSlider.val);
      }

      void RefreshPlaylist()
      {
        if (playlistPlayer != null)
        {
          playlistPlayer.playlistCompleted = false;
        }
      }

      public void Update()
      {
        if (disposed) return;
        if (currentState == null) return;
        time += Time.deltaTime;

        if (playlistPlayer != null)
        {
          playlistPlayer.Update();
        }
      }

      public void SetState(State newState)
      {
        if (currentState != null)
        {
          currentState.onExitTrigger.Trigger();
          CharacterStateManager.instance.BroadcastStateExit(currentState);
        }
        if (newState != null)
        {
          newState.onEnterTrigger.Trigger();
          CharacterStateManager.instance.BroadcastStateEnter(newState);
        }
        UnregisterHandlers(currentState);
        RegisterHandlers(newState);
        if (playlistPlayer != null)
        {
          playlistPlayer.Dispose();
        }
        playlistPlayer = null;
        if (newState != null)
        {
          playlistPlayer = new PlaylistPlayer(this, newState.playlist);
        }
        currentState = newState;
        NewRandomTargetTime();
        time = 0f;
      }
    }
  }
}
