using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class KeyframePlayer : IDisposable
    {
      public AnimationPlayer animationPlayer { get; private set; } = null;
      bool disposed = false;

      public IKeyframe currentKeyframe { get; private set; } = null;
      public IKeyframe targetKeyframe { get; private set; } = null;

      public float time { get; private set; } = 0f;
      public float progress { get; private set; } = 0f;

      public bool playingInBetweenKeyframe { get { return keyframeIsTemporary || keyframeIsNewAnimation; }}
      bool keyframeIsTemporary = false;
      bool keyframeIsNewAnimation = false;
      public bool keyframeCompleted { get { return GetKeyframeCompleted(); } }

      public KeyframePlayer(AnimationPlayer animationPlayer)
      {
        this.animationPlayer = animationPlayer;
      }

      public void Dispose()
      {
        disposed = true;
      }

      public void Update()
      {
        if (disposed) return;
        keyframeIsTemporary = false;
        if (targetKeyframe == null) return;
        if (currentKeyframe == null)
        {
          currentKeyframe = TemporaryKeyframe.CaptureTemporaryKeyframe(targetKeyframe.layer);
          Reset();
        }
        if (currentKeyframe is TemporaryKeyframe)
        {
          keyframeIsTemporary = true;
        }
        
        time += Time.deltaTime * animationPlayer.playbackSpeed;
        progress = currentKeyframe.duration == 0f ? 0f : Mathf.Clamp01(time / currentKeyframe.duration);
        if (animationPlayer.reverse) progress = 1f - Mathf.Clamp01(time / targetKeyframe.duration);

        if (animationPlayer.reverse)
        {
          (targetKeyframe as Animation.Keyframe)?.onPlayingTrigger.Trigger(progress);
        }
        else
        {
          (currentKeyframe as Animation.Keyframe)?.onPlayingTrigger.Trigger(progress);
        }

        UpdateControllers();
        UpdateMorphs();
      }

      bool GetKeyframeCompleted()
      {
        if (currentKeyframe == null) return false;
        return animationPlayer.reverse ? time >= targetKeyframe.duration : time >= currentKeyframe.duration;
      }

      void Reset()
      {
        time = 0f;
        progress = 0f;
      }

      void UpdateControllers()
      {
        if (currentKeyframe == null || targetKeyframe == null) return;
        Transform mainTransform = CharacterStateManager.instance.mainController.transform;
        float t;
        if (animationPlayer.reverse)
        {
          t = 1f - Easing.ApplyEasingFromSelection(progress, targetKeyframe.easing);
        }
        else
        {
          t = Easing.ApplyEasingFromSelection(progress, currentKeyframe.easing);
        }

        foreach (TrackedController tc in targetKeyframe.layer.trackedControllers)
        {
          if (!tc.isTracked) continue;
          Transform controllerTransform = tc.controller.transform;
          string controllerName = tc.controller.name;

          CapturedController currentCapture = currentKeyframe.capturedControllers.Find((c) => c.name == controllerName);
          CapturedController targetCapture = targetKeyframe.capturedControllers.Find((c) => c.name == controllerName);

          if (tc.trackPositionToggle.val && currentCapture?.position != null && targetCapture?.position != null)
          {
            Vector3 currentPosition = mainTransform.TransformPoint(currentCapture.position.Value.value);
            Vector3 targetPosition = mainTransform.TransformPoint(targetCapture.position.Value.value);
            controllerTransform.position = Vector3.Lerp(currentPosition, targetPosition, t);

            if (animationPlayer.reverse)
            {
              tc.controller.currentPositionState = (FreeControllerV3.PositionState)currentCapture.position.Value.state;
            }
            else
            {
              tc.controller.currentPositionState = (FreeControllerV3.PositionState)targetCapture.position.Value.state;
            }
          }
          if (tc.trackRotationToggle.val && currentCapture?.rotation != null && targetCapture?.rotation != null)
          {
            Quaternion currentRotation = mainTransform.rotation * currentCapture.rotation.Value.value;
            Quaternion targetRotation = mainTransform.rotation * targetCapture.rotation.Value.value;
            controllerTransform.rotation = Quaternion.Lerp(currentRotation, targetRotation, t);

            if (animationPlayer.reverse)
            {
              tc.controller.currentRotationState = (FreeControllerV3.RotationState)currentCapture.rotation.Value.state;
            }
            else
            {
              tc.controller.currentRotationState = (FreeControllerV3.RotationState)targetCapture.rotation.Value.state;
            }
          }
        }
      }

      void UpdateMorphs()
      {
        if (currentKeyframe == null || targetKeyframe == null) return;
        float t = Easing.ApplyEasingFromSelection(progress, currentKeyframe.easing);

        foreach (TrackedMorph tm in targetKeyframe.layer.trackedMorphs)
        {
          CapturedMorph currentCapture = currentKeyframe.capturedMorphs.Find((m) => m.uid == tm.morph.uid);
          CapturedMorph targetCapture = targetKeyframe.capturedMorphs.Find((m) => m.uid == tm.morph.uid);

          if (currentCapture != null && targetCapture != null)
          {
            tm.morph.morphValue = Mathf.Lerp(currentCapture.value, targetCapture.value, t);
          }
        }
      }

      public void SetTargetKeyframe(IKeyframe newKeyframe, bool isNewAnimation = false)
      {
        if (targetKeyframe != null)
        {
          if (animationPlayer.reverse)
          {
            (targetKeyframe as Animation.Keyframe)?.onEnterTrigger.Trigger();
          }
          else
          {
            (targetKeyframe as Animation.Keyframe)?.onExitTrigger.Trigger();
          }
        }
        if (newKeyframe != null)
        {
          if (animationPlayer.reverse)
          {
            (newKeyframe as Animation.Keyframe)?.onExitTrigger.Trigger();
          }
          else
          {
            (newKeyframe as Animation.Keyframe)?.onEnterTrigger.Trigger();
          }
        }
        currentKeyframe = targetKeyframe;
        targetKeyframe = newKeyframe;
        keyframeIsNewAnimation = isNewAnimation;
        if (isNewAnimation)
        {
          currentKeyframe = null;
        }
        if (!animationPlayer.playOnceDone)
        {
          Reset();
        }
      }
    }
  }
}
