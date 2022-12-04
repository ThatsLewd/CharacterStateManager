using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Layer : BaseComponentWithId
    {
      public delegate void OnDeleteCallback(Layer layer);
      public static event OnDeleteCallback OnDelete;

      public static List<Layer> list = new List<Layer>();

      public override string id { get; protected set; }
      public string name { get; set; }
      public List<Animation> animations { get; private set; } = new List<Animation>();

      public List<TrackedController> trackedControllers { get; private set; } = new List<TrackedController>();
      public List<TrackedMorph> trackedMorphs { get; private set; } = new List<TrackedMorph>();

      public Layer(string name = null)
      {
        this.id = VaMUtils.GenerateRandomID();
        this.name = name ?? "layer";
        InitializeControllers();
        Layer.list.Add(this);
      }

      public Layer Clone()
      {
        Layer newLayer = new Layer(Helpers.GetCopyName(name));
        foreach (TrackedController source in trackedControllers)
        {
          TrackedController target = newLayer.trackedControllers.Find((tc) => tc.controller.name == source.controller.name);
          target.CopyFrom(source);
        }
        foreach (TrackedMorph tm in trackedMorphs)
        {
          newLayer.trackedMorphs.Add(tm.Clone());
        }
        foreach (Animation animation in animations)
        {
          animation.Clone(newLayer);
        }
        return newLayer;
      }

      public void Delete()
      {
        Layer.list.Remove(this);
        Layer.OnDelete?.Invoke(this);
      }

      public void TrackMorph(DAZMorph morph)
      {
        if (trackedMorphs.Exists((tm) => tm.morph == morph)) return;
        trackedMorphs.Add(new TrackedMorph(morph));
        trackedMorphs.Sort((a, b) => String.Compare(a.standardName, b.standardName));
      }

      private void InitializeControllers()
      {
        trackedControllers.Clear();
        foreach (FreeControllerV3 controller in CharacterStateManager.instance.controllers)
        {
          trackedControllers.Add(new TrackedController(controller));
        }
      }
    }

    public class TrackedController
    {
      public FreeControllerV3 controller { get; private set; }
      public VaMUI.VaMToggle trackPositionToggle { get; private set; }
      public VaMUI.VaMToggle trackRotationToggle { get; private set; }

      public bool isTracked { get { return trackPositionToggle.val || trackRotationToggle.val; }}

      public TrackedController(FreeControllerV3 controller)
      {
        this.controller = controller;
        this.trackPositionToggle = VaMUI.CreateToggle($"Track {controller.name} Position", false);
        this.trackRotationToggle = VaMUI.CreateToggle($"Track {controller.name} Rotation", false);
      }

      public void CopyFrom(TrackedController source)
      {
        if (controller.name != source.controller.name)
        {
          SuperController.LogError("Tried to copy controller from wrong source!");
          return;
        }
        trackPositionToggle.valNoCallback = source.trackPositionToggle.val;
        trackRotationToggle.valNoCallback = source.trackRotationToggle.val;
      }
    }

    public class TrackedMorph
    {
      public string standardName { get; private set; }
      public DAZMorph morph { get; private set; }
      public VaMUI.VaMSlider slider { get; private set; }

      public float defaultValue { get; private set; } = 0f;
      public float defaultMin { get; private set; } = -1f;
      public float defaultMax { get; private set; } = 1f;

      public TrackedMorph(DAZMorph morph)
      {
        this.morph = morph;
        this.standardName = Helpers.GetStandardMorphName(morph);
        this.defaultValue = morph.jsonFloat.defaultVal;
        this.slider = VaMUI.CreateSlider(standardName, defaultValue, defaultMin, defaultMax, callback: HandleValueChange);
        UpdateSliderToMorph();
      }

      public TrackedMorph Clone()
      {
        return new TrackedMorph(morph);
      }

      public void UpdateSliderToMorph()
      {
        float value = morph.morphValue;
        float currMin = slider.min;
        float currMax = slider.max;
        float range = Mathf.Max(new float[] { 1f, Mathf.Abs(value), Mathf.Abs(currMin), Mathf.Abs(currMin) });

        slider.min = -range;
        slider.max = range;
        slider.valNoCallback = value;
      }

      private void HandleValueChange(float val)
      {
        morph.SetValue(val);
      }
    }
  }
}
