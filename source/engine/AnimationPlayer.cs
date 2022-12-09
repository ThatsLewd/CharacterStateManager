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
      public PlaylistEntryPlayer playlistEntryPlayer { get; private set; } = null;
      public Animation currentAnimation { get; private set; } = null;
      bool disposed = false;

      public float time { get; private set; } = 0f;
      public float progress { get; private set; } = 0f;

      public bool playOnceDone { get; private set; } = false;
      public bool reverse { get; private set; } = false;      
      public float playbackSpeed { get { return currentAnimation?.playbackSpeedSlider.val ?? 1f; }}
      public string loopType { get { return currentAnimation.loopTypeChooser.val; }}

      public KeyframePlayer keyframePlayer { get; private set; }

      public AnimationPlayer(PlaylistEntryPlayer playlistEntryPlayer)
      {
        this.playlistEntryPlayer = playlistEntryPlayer;
        keyframePlayer = new KeyframePlayer(this);
      }

      public void Dispose()
      {
        disposed = true;
        UnregisterHandlers(currentAnimation);
        keyframePlayer.Dispose();
      }

      void RegisterHandlers(Animation newAnimation)
      {
        if (newAnimation == null) return;
        newAnimation.loopTypeChooser.storable.setCallbackFunction += HandleSetLoopType;
      }

      void UnregisterHandlers(Animation oldAnimation)
      {
        if (oldAnimation == null) return;
        oldAnimation.loopTypeChooser.storable.setCallbackFunction -= HandleSetLoopType;
      }

      void HandleSetLoopType(string val)
      {
        ResetPlayMode();
      }

      public void Update()
      {
        if (disposed) return;
        if (currentAnimation == null) return;
        if (!keyframePlayer.playingInBetweenKeyframe)
        {
          if (playOnceDone) return;
          time += Time.deltaTime * playbackSpeed;
        }

        float totalAnimationTime = currentAnimation.GetTotalDuration(loopType != LoopType.Loop);
        if (totalAnimationTime == 0f) progress = 0f;
        else if (reverse) progress = 1f - Mathf.Clamp01((time - totalAnimationTime) / totalAnimationTime);
        else progress = Mathf.Clamp01(time / totalAnimationTime);

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
        time = 0f;
        progress = 0f;
      }

      void ResetPlayMode()
      {
        playOnceDone = false;
        reverse = false;
      }

      public void SetAnimation(Animation newAnimation)
      {
        if (newAnimation == currentAnimation) return;
        if (currentAnimation != null)
        {
          currentAnimation.onExitTrigger.Trigger();
        }
        if (newAnimation != null)
        {
          newAnimation.onEnterTrigger.Trigger();
        }
        UnregisterHandlers(currentAnimation);
        RegisterHandlers(newAnimation);
        currentAnimation = newAnimation;
        Reset();
        ResetPlayMode();
        Animation.Keyframe firstKeyframe = newAnimation.keyframes.Count > 0 ? newAnimation.keyframes[0] : null;
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
