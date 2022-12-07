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
        time += Time.deltaTime;

        float totalAnimationTime = currentAnimation.GetTotalDuration();
        if (totalAnimationTime == 0f)
        {
          progress = 0f;
        }
        else
        {
          progress = Mathf.Clamp01(time / totalAnimationTime);
        }

        currentAnimation.onPlayingTrigger.Trigger(progress);

        if (keyframePlayer.targetKeyframe == null || keyframePlayer.keyframeCompleted)
        {
          Animation.Keyframe next = GetNextKeyframe();
          keyframePlayer.SetTargetKeyframe(next);
        }

        keyframePlayer.Update();
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
        time = 0f;
        progress = 0f;
      }

      Animation.Keyframe GetNextKeyframe()
      {
        if (currentAnimation.keyframes.Count == 0) return null;
        int nextIndex;
        if (keyframePlayer.targetKeyframe == null)
        {
          nextIndex = 0;
        }
        else
        {
          int currentIndex = currentAnimation.keyframes.FindIndex((k) => k == (BaseComponent)keyframePlayer.targetKeyframe);
          if (currentIndex >= 0)
          {
            nextIndex = (currentIndex + 1) % currentAnimation.keyframes.Count;
          }
          else
          {
            nextIndex = 0;
          }
        }
        return currentAnimation.keyframes[nextIndex];
      }
    }
  }
}
