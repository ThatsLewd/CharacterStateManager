using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleJSON;
using VaMLib;

namespace ThatsLewd
{
  public static class Helpers
  {
    public static string GetCopyName(string originalName)
    {
      return $"{originalName} - copy";
    }

    public static string GetStandardMorphName(DAZMorph morph)
    {
      return $"{morph.resolvedDisplayName} {morph.version}";
    }

    public static void SetSliderValues(VaMUI.VaMSlider slider, float val, float min, float max, bool noCallback = true)
    {
      if (noCallback)
      {
        slider.valNoCallback = val;
      }
      else
      {
        slider.val = val;
      }
      slider.min = min;
      slider.max = max;
    }
  }
}
