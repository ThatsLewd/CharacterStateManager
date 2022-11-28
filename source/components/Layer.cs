using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Layer
    {
      public static List<Layer> list = new List<Layer>();

      public string id { get { return name; } }
      public string name { get; private set; }
      public List<TrackedControllerStorable> trackedControllers { get; private set; }
      public List<DAZMorph> trackedMorphs { get; private set; }


      public List<Animation> animations
      {
        get { return Animation.list.FindAll((animation) => animation.layer.id == this.id); }
      }

      public Layer(string name = null)
      {
        SetNameUnique(name ?? "layer");
        InitializeControllers();
        trackedMorphs = new List<DAZMorph>();
        Layer.list.Add(this);
      }

      public Layer Clone()
      {
        Layer newLayer = new Layer();
        foreach (TrackedControllerStorable source in trackedControllers)
        {
          TrackedControllerStorable target = newLayer.trackedControllers.Find((s) => s.controller.name == source.controller.name);
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
        foreach (Animation animation in animations)
        {
          animation.Delete();
        }
      }

      public void SetNameUnique(string name)
      {
        for (int i = 0; true; i++)
        {
          if (i == 0)
          {
            this.name = $"{name}";
          }
          else
          {
            this.name = $"{name} copy{i.ToString().PadLeft(3, '0')}";
          }

          bool matchFound = false;
          foreach (Layer layer in Layer.list)
          {
            if (layer != this && layer.id == this.id)
            {
              matchFound = true;
              break;
            }
          }

          if (!matchFound)
          {
            break;
          }
        }
      }

      private void InitializeControllers()
      {
        trackedControllers = new List<TrackedControllerStorable>();
        foreach (FreeControllerV3 controller in CharacterStateManager.instance.controllers)
        {
          trackedControllers.Add(new TrackedControllerStorable(controller));
        }
      }
    }

    public class TrackedControllerStorable
    {
      public FreeControllerV3 controller { get; private set; }
      public JSONStorableBool trackPositionStorable;
      public JSONStorableBool trackRotationStorable;

      public bool isTracked { get { return trackPositionStorable.val || trackRotationStorable.val; }} 

      public SetValueCallback setCallbackFunction;
      public delegate void SetValueCallback(FreeControllerV3 controller, bool positionVal, bool rotationVal);

      public TrackedControllerStorable(FreeControllerV3 controller)
      {
        this.controller = controller;
        trackPositionStorable = new JSONStorableBool($"Track {controller.name} Position", false);
        trackRotationStorable = new JSONStorableBool($"Track {controller.name} Rotation", false);
        trackPositionStorable.setCallbackFunction += OnValueChanged;
        trackRotationStorable.setCallbackFunction += OnValueChanged;
      }

      public void CopyFrom(TrackedControllerStorable source)
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
        setCallbackFunction?.Invoke(controller, trackPositionStorable.val, trackRotationStorable.val);
      }
    }
  }
}
