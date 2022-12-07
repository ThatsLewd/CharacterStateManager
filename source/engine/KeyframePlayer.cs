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

      public IKeyframe currentKeyframe { get; private set; } = null;
      public IKeyframe targetKeyframe { get; private set; } = null;
      public float time { get; private set; } = 0f;
      public float progress { get; private set; } = 0f;

      public bool keyframeCompleted { get { return currentKeyframe != null && time > currentKeyframe.duration; }}

      public KeyframePlayer(AnimationPlayer animationPlayer)
      {
        this.animationPlayer = animationPlayer;
      }

      public void Dispose()
      {

      }

      public void Update()
      {
        if (targetKeyframe == null) return;
        if (currentKeyframe == null)
        {
          currentKeyframe = TemporaryKeyframe.CaptureTemporaryKeyframe(targetKeyframe.layer);
        }
        time += Time.deltaTime;
        
        if (currentKeyframe.duration == 0f)
        {
          progress = 0f; 
        }
        else
        {
          progress = Mathf.Clamp01(time / currentKeyframe.duration);
        }

        (currentKeyframe as Animation.Keyframe)?.onPlayingTrigger.Trigger(progress);

        UpdateControllers();
        UpdateMorphs();
      }

      void UpdateControllers()
      {
        if (currentKeyframe == null || targetKeyframe == null) return;
        Transform mainTransform = CharacterStateManager.instance.mainController.transform;

        foreach (TrackedController tc in targetKeyframe.layer.trackedControllers)
        {
          if (!tc.isTracked) continue;
          Transform controllerTransform = tc.controller.transform;
          string controllerName = tc.controller.name;

          CapturedController currentCapture = currentKeyframe.capturedControllers.Find((c) => c.name == controllerName);
          CapturedController targetCapture = targetKeyframe.capturedControllers.Find((c) => c.name == controllerName);

          if (tc.trackPositionToggle.val && currentCapture?.position != null && targetCapture?.position != null)
          {
            Vector3 currentPosition = mainTransform.TransformPoint(currentCapture.position.Value);
            Vector3 targetPosition = mainTransform.TransformPoint(targetCapture.position.Value);

            controllerTransform.position = Vector3.Lerp(currentPosition, targetPosition, progress);
          }
          if (tc.trackRotationToggle.val && currentCapture?.rotation != null && targetCapture?.rotation != null)
          {
            Quaternion currentRotation = mainTransform.rotation * currentCapture.rotation.Value;
            Quaternion targetRotation = mainTransform.rotation * targetCapture.rotation.Value;

            controllerTransform.rotation = Quaternion.Lerp(currentRotation, targetRotation, progress);
          }
        }
      }

      void UpdateMorphs()
      {
        if (currentKeyframe == null || targetKeyframe == null) return;

        foreach (TrackedMorph tm in targetKeyframe.layer.trackedMorphs)
        {
          CapturedMorph currentCapture = currentKeyframe.capturedMorphs.Find((m) => m.uid == tm.morph.uid);
          CapturedMorph targetCapture = targetKeyframe.capturedMorphs.Find((m) => m.uid == tm.morph.uid);

          if (currentCapture != null && targetCapture != null)
          {
            tm.morph.morphValue = Mathf.Lerp(currentCapture.value, targetCapture.value, progress);
          }
        }
      }

      public void SetTargetKeyframe(IKeyframe newKeyframe)
      {
        if (targetKeyframe != null)
        {
          (targetKeyframe as Animation.Keyframe)?.onExitTrigger.Trigger();
        }
        if (newKeyframe != null)
        {
          (newKeyframe as Animation.Keyframe)?.animation.onEnterTrigger.Trigger();
        }
        currentKeyframe = targetKeyframe;
        targetKeyframe = newKeyframe;
        time = 0f;
        progress = 0f;
      }
    }
  }
}
