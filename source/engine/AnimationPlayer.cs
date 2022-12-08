using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class AnimationPlayer : IDisposable
    {
      public PlaylistPlayer playlistPlayer { get; private set; } = null;

      public PlaylistEntry currentEntry { get; private set; } = null;
      public Animation currentAnimation { get { return currentEntry?.animation; }}

      public float entryTime { get; private set; } = 0f;
      public float animationTime { get; private set; } = 0f;
      public float progress { get; private set; } = 0f;

      public bool donePlaying { get { return GetIsDonePlaying(); }}
      public bool reverse { get; private set; } = false;      
      public float playbackSpeed { get { return currentAnimation?.playbackSpeedSlider.val ?? 1f; }}

      public string loopType { get { return currentAnimation.loopTypeChooser.val; }}
      public string timingMode { get { return currentEntry.timingModeChooser.val; }}
      
      public bool playOnceDone { get; private set; } = false;
      float randomTargetTime = 0f;
      public float targetTime { get { return GetTargetTime(); }}

      public KeyframePlayer keyframePlayer { get; private set; }

      public AnimationPlayer(PlaylistPlayer playlistPlayer)
      {
        this.playlistPlayer = playlistPlayer;
        keyframePlayer = new KeyframePlayer(this);
      }

      public void Dispose()
      {
        UnregisterHandlers(currentEntry);
        keyframePlayer.Dispose();
      }

      void RegisterHandlers(PlaylistEntry newEntry)
      {
        if (newEntry == null) return;
        newEntry.animation.loopTypeChooser.storable.setCallbackFunction += HandleSetLoopType;
        newEntry.timingModeChooser.storable.setCallbackFunction += HandleSetTimingMode;
        newEntry.durationMinSlider.storable.setCallbackFunction += HandleSetRandomDuration;
        newEntry.durationMaxSlider.storable.setCallbackFunction += HandleSetRandomDuration;
      }

      void UnregisterHandlers(PlaylistEntry oldEntry)
      {
        if (oldEntry == null) return;
        oldEntry.animation.loopTypeChooser.storable.setCallbackFunction -= HandleSetLoopType;
        oldEntry.timingModeChooser.storable.setCallbackFunction -= HandleSetTimingMode;
        oldEntry.durationMinSlider.storable.setCallbackFunction -= HandleSetRandomDuration;
        oldEntry.durationMaxSlider.storable.setCallbackFunction -= HandleSetRandomDuration;
      }

      void HandleSetLoopType(string val)
      {
        ResetPlayMode();
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
            return playOnceDone;
          case TimingMode.FixedDuration:
            return entryTime >= currentEntry.durationFixedSlider.val;
          case TimingMode.RandomDuration:
            return entryTime >= randomTargetTime;
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
        if (currentEntry == null) randomTargetTime = 0f;
        randomTargetTime = UnityEngine.Random.Range(currentEntry.durationMinSlider.val, currentEntry.durationMaxSlider.val);
      }

      public void Update()
      {
        if (currentEntry == null) return;
        if (!keyframePlayer.playingInBetweenKeyframe)
        {
          entryTime += Time.deltaTime;
          if (playOnceDone) return;
          animationTime += Time.deltaTime * playbackSpeed;
        }

        float totalAnimationTime = currentAnimation.GetTotalDuration(loopType != LoopType.Loop);
        if (totalAnimationTime == 0f) progress = 0f;
        else if (reverse) progress = 1f - Mathf.Clamp01((animationTime - totalAnimationTime) / totalAnimationTime);
        else progress = Mathf.Clamp01(animationTime / totalAnimationTime);

        if (!keyframePlayer.playingInBetweenKeyframe)
        {
          currentAnimation.onPlayingTrigger.Trigger(progress);
        }

        if (keyframePlayer.targetKeyframe == null || keyframePlayer.keyframeCompleted)
        {
          Animation.Keyframe next = GetNextKeyframe();
          keyframePlayer.SetTargetKeyframe(next);
          if (GetKeyframeIndex(keyframePlayer.currentKeyframe) == 0)
          {
            Reset();
          }
        }

        keyframePlayer.Update();
      }

      void Reset()
      {
        animationTime = 0f;
        progress = 0f;
      }

      void ResetPlayMode()
      {
        playOnceDone = false;
        reverse = false;
      }

      public void SetAnimation(PlaylistEntry newEntry)
      {
        if (currentEntry != null)
        {
          currentAnimation.onExitTrigger.Trigger();
        }
        if (newEntry != null)
        {
          newEntry.animation.onEnterTrigger.Trigger();
        }
        UnregisterHandlers(currentEntry);
        RegisterHandlers(newEntry);
        currentEntry = newEntry;
        entryTime = 0f;
        Reset();
        ResetPlayMode();
        NewRandomTargetTime();
        Animation.Keyframe firstKeyframe = newEntry.animation.keyframes.Count > 0 ? newEntry.animation.keyframes[0] : null;
        keyframePlayer.SetTargetKeyframe(firstKeyframe, true);
      }

      public int GetKeyframeIndex(IKeyframe keyframe)
      {
        if (currentAnimation.keyframes.Count == 0) return -1;
        Animation.Keyframe animationKeyframe = keyframe as Animation.Keyframe;
        int index;
        if (animationKeyframe == null)
        {
          index = -1;
        }
        else
        {
          index = currentAnimation.keyframes.FindIndex((k) => k == animationKeyframe);
        }
        return index;
      }

      Animation.Keyframe GetNextKeyframe()
      {
        int currentIndex = GetKeyframeIndex(keyframePlayer.targetKeyframe);
        if (currentIndex == -1)
        {
          return currentAnimation.keyframes.Count > 0 ? currentAnimation.keyframes[0] : null;
        }

        switch (loopType)
        {
          case LoopType.PlayOnce:
            return GetNextKeyframePlayOnce(currentIndex);
          case LoopType.Loop:
            return GetNextKeyframeLoop(currentIndex);
          case LoopType.PingPong:
            return GetNextKeyframePingPong(currentIndex);
          default:
            return null;
        }
      }

      Animation.Keyframe GetNextKeyframePlayOnce(int currentIndex)
      {
        int nextIndex = currentIndex + 1;
        if (nextIndex >= currentAnimation.keyframes.Count)
        {
          playOnceDone = true;
          return currentAnimation.keyframes[currentAnimation.keyframes.Count - 1];
        }
        return currentAnimation.keyframes[nextIndex];
      }

      Animation.Keyframe GetNextKeyframeLoop(int currentIndex)
      {
        int nextIndex = (currentIndex + 1) % currentAnimation.keyframes.Count;
        return currentAnimation.keyframes[nextIndex];
      }

      Animation.Keyframe GetNextKeyframePingPong(int currentIndex)
      {
        int nextIndex = reverse ? currentIndex - 1 : currentIndex + 1;
        if (reverse && nextIndex < 0)
        {
          reverse = false;
          return currentAnimation.keyframes[1];
        }
        if (!reverse && nextIndex >= currentAnimation.keyframes.Count)
        {
          reverse = true;
          return currentAnimation.keyframes[currentAnimation.keyframes.Count - 2];
        }
        return currentAnimation.keyframes[nextIndex];
      }
    }
  }
}
