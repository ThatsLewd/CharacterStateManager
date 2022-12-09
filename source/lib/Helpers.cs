using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

    public static T ChooseWeightedItem<T>(IEnumerable<T> items) where T : IWeightedItem
    {
      float totalWeight = 0f;
      foreach (T item in items)
      {
        totalWeight += item.weight;
      }
      float r = UnityEngine.Random.Range(0f, totalWeight);
      float sum = 0f;
      T currentItem = default(T);
      foreach (T item in items)
      {
        currentItem = item;
        sum += item.weight;
        if (sum >= r)
        {
          break;
        }
      }
      return currentItem;
    }

    static Regex nameWithNumberRegex = new Regex(@"^(.*?)(\d*)$");
    public static void EnsureUniqueName<T>(IEnumerable<T> list, T item) where T : INamedItem
    {
      Match match = nameWithNumberRegex.Match(item.name);
      string baseName;
      if (match.Success)
      {
        baseName = match.Groups[1].Value;
      }
      else
      {
        baseName = item.name;
      }
      int i = 1;
      while (true)
      {
        bool matchFound = false;
        if (i > 1)
        {
          item.name = $"{baseName}{i}";
        }
        foreach (T otherItem in list)
        {
          if (ReferenceEquals(otherItem, item)) continue;
          if (otherItem.name == item.name)
          {
            matchFound = true;
            break;
          }
        }
        if (!matchFound)
        {
          break;
        }
        i++;
      }
    }
  }

  public interface IWeightedItem
  {
    float weight { get; }
  }

  public interface INamedItem
  {
    string name { get; set; }
  }
}
