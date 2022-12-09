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
      public Dictionary<AnimationPlaylist, PlaylistPlayer> playlistPlayers { get; private set; } = new Dictionary<AnimationPlaylist, PlaylistPlayer>();
      bool disposed = false;

      public State currentState { get; private set; } = null;
      public float time { get; private set; } = 0f;
      public bool donePlaying { get { return GetIsDonePlaying(); } }

      float randomTargetTime = 0f;
      public float targetTime { get { return GetTargetTime(); } }

      public StatePlayer(GroupPlayer groupPlayer)
      {
        this.groupPlayer = groupPlayer;
        AnimationPlaylist.OnDelete += HandlePlaylistDeleted;
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
        AnimationPlaylist.OnDelete -= HandlePlaylistDeleted;
        foreach (KeyValuePair<AnimationPlaylist, PlaylistPlayer> entry in playlistPlayers)
        {
          entry.Value.Dispose();
        }
        playlistPlayers.Clear();
      }

      void HandlePlaylistDeleted(AnimationPlaylist playlist)
      {
        if (playlistPlayers.ContainsKey(playlist))
        {
          playlistPlayers[playlist].Dispose();
          playlistPlayers.Remove(playlist);
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
          RefreshPlaylists();
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
            return playlistPlayers.ToList().Exists((entry) => entry.Value.playlistCompleted);
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

      void RefreshPlaylists()
      {
        foreach (var entry in playlistPlayers)
        {
          entry.Value.playlistCompleted = false;
        }
      }

      public void Update()
      {
        if (disposed) return;
        if (currentState == null) return;
        time += Time.deltaTime;

        foreach (AnimationPlaylist playlist in currentState.playlists)
        {
          PlaylistPlayer player = GetOrCreatePlaylistPlayer(playlist);
          player.Update();
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
        currentState = newState;
        playlistPlayers.Clear();
        NewRandomTargetTime();
        time = 0f;
      }

      PlaylistPlayer GetOrCreatePlaylistPlayer(AnimationPlaylist playlist)
      {
        if (!playlistPlayers.ContainsKey(playlist))
        {
          playlistPlayers[playlist] = new PlaylistPlayer(this, playlist);
        }
        return playlistPlayers[playlist];
      }
    }
  }
}
