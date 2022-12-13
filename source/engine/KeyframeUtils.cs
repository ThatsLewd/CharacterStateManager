using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public interface IKeyframe
    {
      Layer layer { get; }
      float duration { get; }
      string easing { get; }
      List<CapturedController> capturedControllers { get; }
      List<CapturedMorph> capturedMorphs { get; }
    }

    public class TemporaryKeyframe : BaseComponent, IKeyframe
    {
      public override string id { get; protected set; }
      public Layer layer { get; private set; }
      public float duration { get; private set; }
      public string easing { get; private set; }
      public List<CapturedController> capturedControllers { get; private set; } = new List<CapturedController>();
      public List<CapturedMorph> capturedMorphs { get; private set; } = new List<CapturedMorph>();

      public static TemporaryKeyframe CaptureTemporaryKeyframe(Layer layer)
      {
        return new TemporaryKeyframe(layer, layer.defaultTransitionDurationSlider.val, layer.defaultTransitionEasingChooser.val);
      }

      private TemporaryKeyframe(Layer layer, float duration, string easing)
      {
        this.layer = layer;
        this.duration = duration;
        this.easing = easing;
        CaptureCurrentState();
      }

      private void CaptureCurrentState()
      {
        capturedControllers = CapturedController.CaptureCurrentState(layer.trackedControllers);
        capturedMorphs = CapturedMorph.CaptureCurrentState(layer.trackedMorphs);
      }
    }
    
    public class KeyframeNoiseHandler
    {
      public AnimationPlayer animationPlayer { get; private set; }

      public IKeyframe keyframe { get; private set; } = null;
      public Vector3 positionNoise { get; private set; } = Vector3.zero;
      public Quaternion rotationNoise { get; private set; } = Quaternion.identity;
      public float morphNoise { get; private set; } = 0f;

      public KeyframeNoiseHandler(AnimationPlayer animationPlayer)
      {
        this.animationPlayer = animationPlayer;
      }

      public void SetNewKeyframe(IKeyframe keyframe)
      {
        this.keyframe = keyframe;
        if (keyframe == null)
        {
          positionNoise = Vector3.zero;
          rotationNoise = Quaternion.identity;
          morphNoise = 0f;
        }
        else
        {
          positionNoise = NewPositionNoise();
          rotationNoise = NewRotationNoise();
          morphNoise = NewMorphNoise();
        }
      }

      public void Transfer(KeyframeNoiseHandler other)
      {
        keyframe = other.keyframe;
        positionNoise = other.positionNoise;
        rotationNoise = other.rotationNoise;
        morphNoise = other.morphNoise;
      }

      Vector3 NewPositionNoise()
      {
        float? noise = animationPlayer.currentAnimation?.positionNoiseSlider.val;
        if (noise == null || noise.Value == 0f) return Vector3.zero;
        Vector3 newPositionNoise;
        while (true)
        {
          newPositionNoise = new Vector3(
            UnityEngine.Random.Range(-noise.Value, noise.Value),
            UnityEngine.Random.Range(-noise.Value, noise.Value),
            UnityEngine.Random.Range(-noise.Value, noise.Value)
          );
          if (newPositionNoise.magnitude <= noise.Value)
          {
            return newPositionNoise;
          }
        }
      }

      Quaternion NewRotationNoise()
      {
        float? noise = animationPlayer.currentAnimation?.rotationNoiseSlider.val;
        if (noise == null || noise.Value == 0f) return Quaternion.identity;
        float axisAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        Vector3 axis = new Vector3(
          Mathf.Cos(axisAngle),
          Mathf.Sin(axisAngle),
          0f
        );
        return Quaternion.AngleAxis(
          UnityEngine.Random.Range(-noise.Value * 180f, noise.Value * 180f),
          axis
        );
      }

      float NewMorphNoise()
      {
        float? noise = animationPlayer.currentAnimation?.morphNoiseSlider.val;
        if (noise == null || noise.Value == 0f) return 0f;
        return UnityEngine.Random.Range(-noise.Value, noise.Value);
      }
    }
  }
}
