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
      public float time { get; private set; } = 0f;
      public float progress { get; private set; } = 0f;
      public bool donePlaying { get; private set; } = false;
      public bool reverse { get; private set; } = false;
      public float playbackSpeed { get { return currentAnimation?.playbackSpeedSlider.val ?? 1f; }}

      public KeyframePlayer keyframePlayer { get; private set; }

      public AnimationPlayer(PlaylistPlayer playlistPlayer)
      {
        this.playlistPlayer = playlistPlayer;
        keyframePlayer = new KeyframePlayer(this);
      }

      public void Dispose()
      {
        keyframePlayer.Dispose();
      }

      public void Update()
      {
        if (currentEntry == null) return;
        if (donePlaying)
        {
          if (currentAnimation.loopTypeChooser.val == LoopType.PlayOnce) return;
          donePlaying = false;
        }
        if (!keyframePlayer.playingTemporaryKeyframe)
        {
          time += Time.deltaTime * playbackSpeed;
        }

        float totalAnimationTime = currentAnimation.GetTotalDuration(currentAnimation.loopTypeChooser.val == LoopType.PingPong);
        if (totalAnimationTime == 0f) progress = 0f;
        else if (reverse) progress = 1f - Mathf.Clamp01((time - totalAnimationTime) / totalAnimationTime);
        else progress = Mathf.Clamp01(time / totalAnimationTime);

        currentAnimation.onPlayingTrigger.Trigger(progress);

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
        time = 0f;
        progress = 0f;
        donePlaying = false;
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
        currentEntry = newEntry;
        Reset();
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

        switch (currentAnimation.loopTypeChooser.val)
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
          donePlaying = true;
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
