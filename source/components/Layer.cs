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
      public static List<Layer> list = new List<Layer>();

      public override string id { get; protected set; }
      public string name { get; set; }
      public List<Animation> animations { get; private set; } = new List<Animation>();

      public List<TrackedController> trackedControllers { get; private set; } = new List<TrackedController>();
      public List<DAZMorph> trackedMorphs { get; private set; } = new List<DAZMorph>();

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
        foreach (DAZMorph morph in trackedMorphs)
        {
          newLayer.trackedMorphs.Add(morph);
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
        foreach (Animation animation in animations.ToArray())
        {
          animation.Delete();
        }
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
      public JSONStorableBool trackPositionStorable;
      public JSONStorableBool trackRotationStorable;

      public bool isTracked { get { return trackPositionStorable.val || trackRotationStorable.val; }} 

      public SetValueCallback setCallbackFunction;
      public delegate void SetValueCallback(TrackedController trackedController);

      public TrackedController(FreeControllerV3 controller)
      {
        this.controller = controller;
        this.trackPositionStorable = new JSONStorableBool($"Track {controller.name} Position", false);
        this.trackRotationStorable = new JSONStorableBool($"Track {controller.name} Rotation", false);
        this.trackPositionStorable.setCallbackFunction += OnValueChanged;
        this.trackRotationStorable.setCallbackFunction += OnValueChanged;
      }

      public void CopyFrom(TrackedController source)
      {
        if (controller.name != source.controller.name)
        {
          SuperController.LogError("Tried to copy controller from wrong source!");
          return;
        }
        trackPositionStorable.valNoCallback = source.trackPositionStorable.val;
        trackRotationStorable.valNoCallback = source.trackRotationStorable.val;
        setCallbackFunction = source.setCallbackFunction;
      }

      private void OnValueChanged(bool val)
      {
        setCallbackFunction?.Invoke(this);
      }
    }
  }
}
