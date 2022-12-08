using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class PlaylistPlayer : IDisposable
    {
      public StatePlayer statePlayer { get; private set; }
      public AnimationPlaylist playlist { get; private set; }
      public AnimationPlayer animationPlayer { get; private set; }

      public bool playlistCompleted { get; set; } = false;

      public PlaylistPlayer(StatePlayer statePlayer, AnimationPlaylist playlist)
      {
        this.statePlayer = statePlayer;
        this.playlist = playlist;
        this.animationPlayer = new AnimationPlayer(this);
        RegisterHandlers();
      }

      public void Dispose()
      {
        UnregisterHandlers();
        animationPlayer.Dispose();
      }

      void RegisterHandlers()
      {
        playlist.playModeChooser.storable.setCallbackFunction += HandleSetPlayMode;
      }

      void UnregisterHandlers()
      {
        playlist.playModeChooser.storable.setCallbackFunction += HandleSetPlayMode;
      }

      void HandleSetPlayMode(string val)
      {
        playlistCompleted = false;
      }

      public void Update()
      {
        if (animationPlayer.currentEntry == null || animationPlayer.donePlaying)
        {
          GetNextAnimation();
        }
        animationPlayer.Update();
      }

      void GetNextAnimation()
      {
        PlaylistEntry next = null;
        switch (playlist.playModeChooser.val)
        {
          case PlaylistMode.Sequential:
            next = GetNextAnimationSequential();
            break;
          case PlaylistMode.Random:
            next = GetNextAnimationRandom();
            break;
        }
        animationPlayer.SetAnimation(next);
      }

      PlaylistEntry GetNextAnimationSequential()
      {
        if (playlist.entries.Count == 0) return null;
        int nextIndex;
        if (animationPlayer.currentEntry == null)
        {
          nextIndex = 0;
        }
        else
        {
          int currentIndex = playlist.entries.FindIndex((e) => e == animationPlayer.currentEntry);
          if (currentIndex >= 0)
          {
            nextIndex = currentIndex + 1;
            if (nextIndex >= playlist.entries.Count)
            {
              playlistCompleted = true;
              nextIndex = 0;
            }
          }
          else
          {
            nextIndex = 0;
          }
        }
        return playlist.entries[nextIndex];
      }

      PlaylistEntry GetNextAnimationRandom()
      {
        return Helpers.ChooseWeightedItem(playlist.entries);
      }
    }
  }
}
