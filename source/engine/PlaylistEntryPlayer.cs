using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class PlaylistEntryPlayer : IDisposable
    {
      public PlaylistPlayer playlistPlayer { get; private set; } = null;
      public PlaylistEntry currentEntry { get; private set; } = null;
      public AnimationPlayer animationPlayer { get; private set; } = null;
      bool disposed = false;

      public float time { get; private set; } = 0f;
      public bool donePlaying { get { return GetIsDonePlaying(); } }

      float randomTargetTime = 0f;
      public float targetTime { get { return GetTargetTime(); } }

      public string timingMode { get { return currentEntry.timingModeChooser.val; } }

      public PlaylistEntryPlayer(PlaylistPlayer playlistPlayer)
      {
        this.playlistPlayer = playlistPlayer;
        animationPlayer = new AnimationPlayer(this);
      }

      public void Dispose()
      {
        disposed = true;
        UnregisterHandlers(currentEntry);
        animationPlayer.Dispose();
      }

      void RegisterHandlers(PlaylistEntry newEntry)
      {
        if (newEntry == null) return;
        newEntry.timingModeChooser.storable.setCallbackFunction += HandleSetTimingMode;
        newEntry.durationMinSlider.storable.setCallbackFunction += HandleSetRandomDuration;
        newEntry.durationMaxSlider.storable.setCallbackFunction += HandleSetRandomDuration;
      }

      void UnregisterHandlers(PlaylistEntry oldEntry)
      {
        if (oldEntry == null) return;
        oldEntry.timingModeChooser.storable.setCallbackFunction -= HandleSetTimingMode;
        oldEntry.durationMinSlider.storable.setCallbackFunction -= HandleSetRandomDuration;
        oldEntry.durationMaxSlider.storable.setCallbackFunction -= HandleSetRandomDuration;
      }

      void HandleSetTimingMode(string val)
      {
        if (val == TimingMode.RandomDuration)
        {
          NewRandomTargetTime();
        }
      }

      void HandleSetRandomDuration(float val)
      {
        NewRandomTargetTime();
      }

      bool GetIsDonePlaying()
      {
        if (currentEntry == null) return false;
        switch (timingMode)
        {
          case TimingMode.DurationFromAnimation:
            return animationPlayer.playOnceDone;
          case TimingMode.InfiniteDuration:
            return false;
          case TimingMode.FixedDuration:
            return time >= currentEntry.durationFixedSlider.val;
          case TimingMode.RandomDuration:
            return time >= randomTargetTime;
          default:
            return false;
        }
      }

      float GetTargetTime()
      {
        if (currentEntry == null) return 0f;
        switch (timingMode)
        {
          case TimingMode.FixedDuration:
            return currentEntry.durationFixedSlider.val;
          case TimingMode.RandomDuration:
            return randomTargetTime;
          default:
            return 0f;
        }
      }

      void NewRandomTargetTime()
      {
        if (currentEntry == null)
        {
          randomTargetTime = 0f;
          return;
        }
        randomTargetTime = UnityEngine.Random.Range(currentEntry.durationMinSlider.val, currentEntry.durationMaxSlider.val);
      }

      public void Update()
      {
        if (disposed) return;
        if (currentEntry == null) return;
        if (!animationPlayer.keyframePlayer.playingInBetweenKeyframe)
        {
          time += Time.deltaTime;
        }

        animationPlayer.Update();
      }

      void Reset()
      {
        time = 0f;
      }

      public void SetPlaylistEntry(PlaylistEntry newEntry)
      {
        UnregisterHandlers(currentEntry);
        RegisterHandlers(newEntry);
        currentEntry = newEntry;
        Reset();
        NewRandomTargetTime();
        animationPlayer.SetAnimation(newEntry?.animation);
      }
    }
  }
}
