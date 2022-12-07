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
  }
}
