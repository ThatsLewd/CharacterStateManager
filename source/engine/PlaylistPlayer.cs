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

      public PlaylistPlayer(StatePlayer statePlayer, AnimationPlaylist playlist)
      {
        this.statePlayer = statePlayer;
        this.playlist = playlist;
        this.animationPlayer = new AnimationPlayer(this);
      }

      public void Dispose()
      {
        animationPlayer.Dispose();
      }

      public void Update()
      {
        if (animationPlayer.currentEntry == null)
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
            nextIndex = (currentIndex + 1) % playlist.entries.Count;
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
