using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public partial class Animation : BaseComponent
    {
      public class Keyframe : BaseComponent, IKeyframe, IDisposable
      {
        public delegate void OnDeleteCallback(Keyframe keyframe);
        public static event OnDeleteCallback OnDelete;

        public override string id { get; protected set; }
        public Animation animation { get; private set; }
        public Layer layer { get { return animation.layer; }}

        public VaMUI.VaMTextInput labelInput;
        public VaMUI.VaMColorPicker colorPicker;
        public VaMUI.VaMStringChooser easingChooser;
        public VaMUI.VaMSlider durationSlider;

        public EventTrigger onEnterTrigger = VaMTrigger.Create<EventTrigger>("On Enter Keyframe");
        public ValueTrigger onPlayingTrigger = VaMTrigger.Create<ValueTrigger>("On Keyframe Playing");
        public EventTrigger onExitTrigger = VaMTrigger.Create<EventTrigger>("On Exit Keyframe");

        public List<CapturedController> capturedControllers { get; private set; } = new List<CapturedController>();
        public List<CapturedMorph> capturedMorphs { get; private set; } = new List<CapturedMorph>();

        public float duration { get { return durationSlider.val; }}
        public string easing { get { return easingChooser.val; }}

        public Keyframe(Animation animation, int index = -1)
        {
          this.id = VaMUtils.GenerateRandomID(32);
          this.animation = animation;
          this.labelInput = VaMUI.CreateTextInput("Label", "", callbackNoVal: instance.RequestRedraw);
          this.colorPicker = VaMUI.CreateColorPicker("Keyframe Color", GetRandomColor());
          this.easingChooser = VaMUI.CreateStringChooser("Select Easing", Easing.list.ToList(), Easing.EasingType.Linear);
          this.easingChooser.valNoCallback = animation.defaultEasingChooser.val;
          this.durationSlider = VaMUI.CreateSlider("Duration", 0.5f, 0f, 1f);
          Helpers.SetSliderValues(durationSlider, animation.defaultDurationSlider.val, animation.defaultDurationSlider.min, animation.defaultDurationSlider.max);
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
          newKeyframe.labelInput.valNoCallback = labelInput.val;
          newKeyframe.colorPicker.valNoCallback = colorPicker.val;
          newKeyframe.easingChooser.valNoCallback = easingChooser.val;
          Helpers.SetSliderValues(newKeyframe.durationSlider, durationSlider.val, durationSlider.min, durationSlider.max);
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

        public void Dispose()
        {
          animation.keyframes.Remove(this);
          Keyframe.OnDelete?.Invoke(this);
        }

        public void CopyActions()
        {
          ActionClipboard.onEnterTrigger = onEnterTrigger;
          ActionClipboard.onPlayingTrigger = onPlayingTrigger;
          ActionClipboard.onExitTrigger = onExitTrigger;
        }

        public void PasteActions()
        {
          if (ActionClipboard.onEnterTrigger != null)
            onEnterTrigger = VaMTrigger.Clone(ActionClipboard.onEnterTrigger, onEnterTrigger.name);
          if (ActionClipboard.onPlayingTrigger != null)
            onPlayingTrigger = VaMTrigger.Clone(ActionClipboard.onPlayingTrigger, onPlayingTrigger.name);
          if (ActionClipboard.onExitTrigger != null)
            onExitTrigger = VaMTrigger.Clone(ActionClipboard.onExitTrigger, onExitTrigger.name);

          CharacterStateManager.instance.RequestRedraw();
        }

        public void CaptureLayerState()
        {
          capturedControllers = CapturedController.CaptureCurrentState(layer.trackedControllers);
          capturedMorphs = CapturedMorph.CaptureCurrentState(layer.trackedMorphs);
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

        public JSONClass GetJSON()
        {
          JSONClass json = new JSONClass();
          json["id"] = id;
          labelInput.storable.StoreJSON(json);
          colorPicker.storable.StoreJSON(json);
          easingChooser.storable.StoreJSON(json);
          durationSlider.storable.StoreJSON(json);
          onEnterTrigger.StoreJSON(json);
          onPlayingTrigger.StoreJSON(json);
          onExitTrigger.StoreJSON(json);
          json["capturedControllers"] = new JSONArray();
          foreach (CapturedController capture in capturedControllers)
          {
            json["capturedControllers"].AsArray.Add(capture.GetJSON());
          }
          json["capturedMorphs"] = new JSONArray();
          foreach (CapturedMorph capture in capturedMorphs)
          {
            json["capturedMorphs"].AsArray.Add(capture.GetJSON());
          }
          return json;
        }

        public void RestoreFromJSON(JSONClass json)
        {
          id = json["id"].Value;
          labelInput.storable.RestoreFromJSON(json);
          colorPicker.storable.RestoreFromJSON(json);
          easingChooser.storable.RestoreFromJSON(json);
          durationSlider.storable.RestoreFromJSON(json);
          onEnterTrigger.RestoreFromJSON(json);
          onPlayingTrigger.RestoreFromJSON(json);
          onExitTrigger.RestoreFromJSON(json);
          capturedControllers.Clear();
          foreach (JSONNode node in json["capturedControllers"].AsArray)
          {
            CapturedController capture = new CapturedController();
            capture.RestoreFromJSON(node.AsObject);
            capturedControllers.Add(capture);
          }
          capturedMorphs.Clear();
          foreach (JSONNode node in json["capturedMorphs"].AsArray)
          {
            CapturedMorph capture = new CapturedMorph();
            capture.RestoreFromJSON(node.AsObject);
            capturedMorphs.Add(capture);
          }
        }
      }
    }

    public class CapturedController
    {
      public string name = "";
      public Vector3? position = null;
      public Quaternion? rotation = null;

      public static List<CapturedController> CaptureCurrentState(List<TrackedController> trackedControllers)
      {
        List<CapturedController> captures = new List<CapturedController>();
        Transform mainTransform = CharacterStateManager.instance.mainController.transform;
        foreach (TrackedController tc in trackedControllers)
        {
          if (!tc.isTracked) continue;
          Transform controllerTransform = tc.controller.transform;
          CapturedController capture = new CapturedController();
          capture.name = tc.controller.name;
          if (tc.trackPositionToggle.val)
          {
            capture.position = mainTransform.InverseTransformPoint(controllerTransform.position);
          }
          if (tc.trackRotationToggle.val)
          {
            capture.rotation = Quaternion.Inverse(mainTransform.rotation) * controllerTransform.rotation;
          }
          captures.Add(capture);
        }
        return captures;
      }

      public CapturedController Clone()
      {
        return new CapturedController()
        {
          name = name,
          position = position,
          rotation = rotation,
        };
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["name"] = name;
        if (position != null)
        {
          json["position"] = new JSONClass();
          json["position"]["x"].AsFloat = position.Value.x;
          json["position"]["y"].AsFloat = position.Value.y;
          json["position"]["z"].AsFloat = position.Value.z;
        }
        if (rotation != null)
        {
          json["rotation"] = new JSONClass();
          json["rotation"]["x"].AsFloat = rotation.Value.x;
          json["rotation"]["y"].AsFloat = rotation.Value.y;
          json["rotation"]["z"].AsFloat = rotation.Value.z;
          json["rotation"]["w"].AsFloat = rotation.Value.w;
        }
        return json;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        name = json["name"].Value;
        if (json.HasKey("position"))
        {
          position = new Vector3(
            json["position"]["x"].AsFloat,
            json["position"]["y"].AsFloat,
            json["position"]["z"].AsFloat
          );
        }
        if (json.HasKey("rotation"))
        {
          rotation = new Quaternion(
            json["rotation"]["x"].AsFloat,
            json["rotation"]["y"].AsFloat,
            json["rotation"]["z"].AsFloat,
            json["rotation"]["w"].AsFloat
          );
        }
      }
    }

    public class CapturedMorph
    {
      public string uid = "";
      public float value = 0f;

      public static List<CapturedMorph> CaptureCurrentState(List<TrackedMorph> trackedMorphs)
      {
        List<CapturedMorph> captures = new List<CapturedMorph>();
        foreach (TrackedMorph tm in trackedMorphs)
        {
          CapturedMorph capture = new CapturedMorph();
          capture.uid = tm.morph.uid;
          capture.value = tm.morph.morphValue;
          captures.Add(capture);
        }
        return captures;
      }

      public CapturedMorph Clone()
      {
        return new CapturedMorph()
        {
          uid = uid,
          value = value,
        };
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["uid"] = uid;
        json["value"].AsFloat = value;
        return json;
      }

      public void RestoreFromJSON(JSONClass json)
      {
        uid = json["uid"].Value;
        value = json["value"].AsFloat;
      }
    }
  }
}
