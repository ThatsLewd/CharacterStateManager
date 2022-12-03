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

    public static Vector3 GetPositionDifference(Transform from, Transform to)
    {
      return to.position - from.position;
    }

    public static Quaternion GetRotationDifference(Transform from, Transform to)
    {
      return Quaternion.Inverse(to.rotation) * from.rotation;
    }
  }
}
