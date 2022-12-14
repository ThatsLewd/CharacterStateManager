using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public class Layer : BaseComponent, IDisposable, INamedItem
    {
      public delegate void OnDeleteCallback(Layer layer);
      public static event OnDeleteCallback OnDelete;

      public static List<Layer> list = new List<Layer>();

      public override string id { get; protected set; }
      public string name { get; set; }
      public List<Animation> animations { get; private set; } = new List<Animation>();

      public VaMUI.VaMSlider defaultTransitionDurationSlider;
      public VaMUI.VaMStringChooser defaultTransitionEasingChooser;

      public List<TrackedController> trackedControllers { get; private set; } = new List<TrackedController>();
      public List<TrackedMorph> trackedMorphs { get; private set; } = new List<TrackedMorph>();

      public Layer(string name = null)
      {
        this.id = VaMUtils.GenerateRandomID(32);
        this.name = name ?? "layer";
        Helpers.EnsureUniqueName(Layer.list, this);
        defaultTransitionDurationSlider = VaMUI.CreateSlider("Transition Duration", 0.3f, 0f, 1f);
        defaultTransitionEasingChooser = VaMUI.CreateStringChooser("Easing", Easing.list.ToList(), Easing.EasingType.EaseInOutQuad);
        InitializeControllers();
        Layer.list.Add(this);
      }

      public Layer Clone()
      {
        Layer newLayer = new Layer(Helpers.GetCopyName(name));
        Helpers.SetSliderValues(newLayer.defaultTransitionDurationSlider, defaultTransitionDurationSlider.val, defaultTransitionDurationSlider.min, defaultTransitionDurationSlider.max);
        newLayer.defaultTransitionEasingChooser.valNoCallback = defaultTransitionEasingChooser.val;
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

      public void Dispose()
      {
        Layer.list.Remove(this);
        Layer.OnDelete?.Invoke(this);
      }

      public static Layer FindById(string id)
      {
        return Layer.list.Find((l) => l.id == id);
      }

      public void TrackMorph(DAZMorph morph)
      {
        if (trackedMorphs.Exists((tm) => tm.morph == morph)) return;
        trackedMorphs.Add(new TrackedMorph(morph));
        trackedMorphs.Sort((a, b) => String.Compare(a.standardName, b.standardName));
      }

      public void TrackMorphList(List<DAZMorph> morphList)
      {
        foreach (DAZMorph morph in morphList)
        {
          if (trackedMorphs.Exists((tm) => tm.morph == morph)) return;
          trackedMorphs.Add(new TrackedMorph(morph));
        }
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

      public static JSONClass GetJSONTopLevel(ReferenceCollector rc)
      {
        JSONClass json = new JSONClass();
        json["list"] = new JSONArray();
        foreach (Layer layer in Layer.list)
        {
          rc.layers[layer.id] = layer;
          json["list"].AsArray.Add(layer.GetJSON(rc));
        }
        return json;
      }

      public static void RestoreFromJSONTopLevel(JSONClass json)
      {
        Helpers.DisposeList(Layer.list);
        foreach (JSONNode node in json["list"].AsArray.Childs)
        {
          new Layer().RestoreFromJSON(node.AsObject);
        }
      }

      public JSONClass GetJSON(ReferenceCollector rc, bool storeAnimations = true)
      {
        JSONClass json = new JSONClass();
        json["id"] = id;
        json["name"] = name;
        defaultTransitionDurationSlider.StoreJSON(json);
        defaultTransitionEasingChooser.StoreJSON(json);
        if (storeAnimations)
        {
          json["animations"] = new JSONArray();
          foreach (Animation animation in animations)
          {
            rc.animations[animation.id] = animation;
            json["animations"].AsArray.Add(animation.GetJSON(rc));
          }
        }
        json["trackedControllers"] = new JSONClass();
        foreach (TrackedController tc in trackedControllers)
        {
          tc.StoreJSON(json["trackedControllers"].AsObject);
        }
        json["trackedMorphs"] = new JSONArray();
        foreach (TrackedMorph tm in trackedMorphs)
        {
          json["trackedMorphs"].AsArray.Add(tm.GetJSON());
        }
        return json;
      }

      public void RestoreFromJSON(JSONClass json, bool mergeAnimations = false)
      {
        id = json["id"].Value;
        name = json["name"].Value;
        defaultTransitionDurationSlider.RestoreFromJSON(json);
        defaultTransitionEasingChooser.RestoreFromJSON(json);
        if (!mergeAnimations)
        {
          Helpers.DisposeList(animations);
        }
        foreach (JSONNode node in json["animations"].AsArray)
        {
          new Animation(this).RestoreFromJSON(node.AsObject);
        }
        InitializeControllers();
        foreach (TrackedController tc in trackedControllers)
        {
          tc.RestoreFromJSON(json["trackedControllers"].AsObject);
        }
        trackedMorphs.Clear();
        foreach (JSONNode node in json["trackedMorphs"].AsArray)
        {
          TrackedMorph tm = TrackedMorph.FromJSON(node.AsObject);
          if (tm != null)
          {
            trackedMorphs.Add(tm);
          }
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
        this.trackPositionToggle = VaMUI.CreateToggle($"Track Position", false);
        this.trackRotationToggle = VaMUI.CreateToggle($"Track Rotation", false);
      }

      public void CopyFrom(TrackedController source)
      {
        if (controller.name != source.controller.name)
        {
          LogError("Tried to copy controller from wrong source!");
          return;
        }
        trackPositionToggle.valNoCallback = source.trackPositionToggle.val;
        trackRotationToggle.valNoCallback = source.trackRotationToggle.val;
      }

      public void StoreJSON(JSONClass json)
      {
        if (!isTracked) return;
        json[controller.name] = new JSONClass();
        trackPositionToggle.StoreJSON(json[controller.name].AsObject);
        trackRotationToggle.StoreJSON(json[controller.name].AsObject);
      }

      public void RestoreFromJSON(JSONClass json)
      {
        if (!json.HasKey(controller.name)) return;
        trackPositionToggle.RestoreFromJSON(json[controller.name].AsObject);
        trackRotationToggle.RestoreFromJSON(json[controller.name].AsObject);
      }
    }

    public class TrackedMorph
    {
      public DAZMorph morph { get; private set; }
      public string standardName { get; private set; }
      public VaMUI.VaMSlider slider { get; private set; }

      public TrackedMorph(DAZMorph morph)
      {
        this.morph = morph;
        this.standardName = Helpers.GetStandardMorphName(morph);
        this.slider = VaMUI.CreateSlider(standardName, morph.jsonFloat.defaultVal, -1f, 1f, callbackNoVal: HandleValueChange);
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

      private void HandleValueChange()
      {
        morph.morphValue = slider.val;
      }

      public JSONClass GetJSON()
      {
        JSONClass json = new JSONClass();
        json["morph"] = morph.uid;
        return json;
      }

      public static TrackedMorph FromJSON(JSONClass json)
      {
        string uid = json["morph"].Value;
        DAZMorph morph = CharacterStateManager.instance.morphsControl.GetMorphByUid(uid);
        if (morph == null)
        {
          LogError($"Failed to load tracked morph: {uid}");
          return null;
        }
        return new TrackedMorph(morph);
      }
    }
  }
}
