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

      public State currentState { get; private set; } = null;
      public float time { get; private set; } = 0f;

      public StatePlayer(GroupPlayer groupPlayer)
      {
        this.groupPlayer = groupPlayer;
        AnimationPlaylist.OnDelete += HandlePlaylistDeleted;
      }

      void HandlePlaylistDeleted(AnimationPlaylist playlist)
      {
        if (playlistPlayers.ContainsKey(playlist))
        {
          playlistPlayers[playlist].Dispose();
          playlistPlayers.Remove(playlist);
        }
      }

      public void Dispose()
      {
        AnimationPlaylist.OnDelete -= HandlePlaylistDeleted;
      }

      public void Update()
      {
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
        }
        if (newState != null)
        {
          newState.onEnterTrigger.Trigger();
        }
        currentState = newState;
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
