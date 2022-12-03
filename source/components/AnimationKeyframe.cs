using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public partial class Animation : BaseComponentWithId
    {
      public class Keyframe : BaseComponentWithId
      {
        public override string id { get; protected set; }
        public Animation animation { get; private set; }

        public JSONStorableColor colorStorable;
        public JSONStorableStringChooser easingStorable;
        public JSONStorableFloat durationStorable;

        public EventTrigger onEnterTrigger = VaMTrigger.Create<EventTrigger>("On Enter Keyframe");
        public FloatTrigger onPlayingTrigger = VaMTrigger.Create<FloatTrigger>("On Keyframe Playing");
        public EventTrigger onExitTrigger = VaMTrigger.Create<EventTrigger>("On Exit Keyframe");

        public List<CapturedController> capturedControllers = new List<CapturedController>();
        public List<CapturedMorph> capturedMorphs = new List<CapturedMorph>();

        public Keyframe(Animation animation, int index = -1)
        {
          this.id = VaMUtils.GenerateRandomID();
          this.animation = animation;
          this.colorStorable = CreateColorStorable();
          this.easingStorable = CreateEasingStorable(Easing.EasingType.Linear);
          this.durationStorable = CreateDurationStorable(5f, 0f, 10f);
          if (index == -1)
          {
            index = this.animation.keyframes.Count;
          }
          this.animation.keyframes.Insert(index, this);
        }

        public Keyframe Clone(Animation animation = null, int index = -1)
        {
          animation = animation ?? this.animation;
          Keyframe newKeyframe = new Keyframe(animation, index);
          newKeyframe.colorStorable = CreateColorStorable(colorStorable.val);
          newKeyframe.easingStorable = CreateEasingStorable(easingStorable.val);
          newKeyframe.durationStorable = CreateDurationStorable(durationStorable.val, durationStorable.min, durationStorable.max);
          newKeyframe.onEnterTrigger = VaMTrigger.Clone(onEnterTrigger);
          newKeyframe.onPlayingTrigger = VaMTrigger.Clone(onPlayingTrigger);
          newKeyframe.onExitTrigger = VaMTrigger.Clone(onExitTrigger);
          foreach (CapturedController capture in capturedControllers)
          {
            newKeyframe.capturedControllers.Add(capture.Clone());
          }
          foreach (CapturedMorph capture in capturedMorphs)
          {
            newKeyframe.capturedMorphs.Add(capture.Clone());
          }
          return newKeyframe;
        }

        public void Delete()
        {
          this.animation.keyframes.Remove(this);
        }

        public void CaptureLayerState()
        {
          Transform mainTransform = CharacterStateManager.instance.person.mainController.transform;

          capturedControllers.Clear();
          foreach (TrackedController tc in animation.layer.trackedControllers)
          {
            if (!tc.isTracked) continue;
            Transform controllerTransform = tc.controller.transform;
            CapturedController capture = new CapturedController();
            capture.name = tc.controller.name;
            if (tc.trackPositionStorable.val)
            {
              capture.position = Helpers.GetPositionDifference(controllerTransform, mainTransform);
            }
            if (tc.trackRotationStorable.val)
            {
              capture.rotation = Helpers.GetRotationDifference(controllerTransform, mainTransform);
            }
            capturedControllers.Add(capture);
          }

          capturedMorphs.Clear();
          foreach (DAZMorph morph in animation.layer.trackedMorphs)
          {
            CapturedMorph capture = new CapturedMorph();
            capture.uid = morph.uid;
            capture.value = morph.appliedValue;
            capturedMorphs.Add(capture);
          }
        }

        public CapturedController GetCapturedController(string name)
        {
          return capturedControllers.Find((c) => c.name == name);
        }

        public CapturedMorph GetCapturedMorph(string uid)
        {
          return capturedMorphs.Find((m) => m.uid == uid);
        }

        private HSVColor GetRandomColor()
        {
          float hue = UnityEngine.Random.value;
          HSVColor color = new HSVColor()
          {
            H = hue,
            S = 0.5f,
            V = 1.0f
          };
          return color;
        }

        private JSONStorableColor CreateColorStorable(HSVColor? defaultValue = null)
        {
          if (defaultValue == null)
          {
            defaultValue = GetRandomColor();
          }
          return new JSONStorableColor("Keyframe Color", defaultValue.Value);
        }

        private JSONStorableStringChooser CreateEasingStorable(string defaultValue)
        {
          return new JSONStorableStringChooser("Select Easing", Easing.list.ToList(), defaultValue, "Select Easing");
        }

        private JSONStorableFloat CreateDurationStorable(float defaultValue, float minValue, float maxValue)
        {
          return new JSONStorableFloat("Duration", defaultValue, minValue, maxValue, true, true);
        }
      }
    }

    public class CapturedController
    {
      public string name = "";
      public Vector3? position = null;
      public Quaternion? rotation = null;

      public CapturedController Clone()
      {
        return new CapturedController()
        {
          name = name,
          position = position,
          rotation = rotation,
        };
      }
    }

    public class CapturedMorph
    {
      public string uid = "";
      public float value = 0f;

      public CapturedMorph Clone()
      {
        return new CapturedMorph()
        {
          uid = uid,
          value = value,
        };
      }
    }
  }
}
