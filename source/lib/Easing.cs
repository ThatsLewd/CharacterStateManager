using UnityEngine;

namespace ThatsLewd
{
  public static class Easing
  {
    public static class EasingType
    {
      public const string Hold = "Hold";
      public const string Linear = "Linear";
      public const string EaseInQuad = "EaseInQuad";
      public const string EaseOutQuad = "EaseOutQuad";
      public const string EaseInOutQuad = "EaseInOutQuad";
      public const string EaseInCubic = "EaseInCubic";
      public const string EaseOutCubic = "EaseOutCubic";
      public const string EaseInOutCubic = "EaseInOutCubic";
      public const string EaseInQuint = "EaseInQuint";
      public const string EaseOutQuint = "EaseOutQuint";
      public const string EaseInOutQuint = "EaseInOutQuint";
      public const string EaseInExp = "EaseInExp";
      public const string EaseOutExp = "EaseOutExp";
      public const string EaseInOutExp = "EaseInOutExp";
    }

    public static readonly string[] list = new string[]
    {
      EasingType.Hold,
      EasingType.Linear,
      EasingType.EaseInQuad,
      EasingType.EaseOutQuad,
      EasingType.EaseInOutQuad,
      EasingType.EaseInCubic,
      EasingType.EaseOutCubic,
      EasingType.EaseInOutCubic,
      EasingType.EaseInQuint,
      EasingType.EaseOutQuint,
      EasingType.EaseInOutQuint,
      EasingType.EaseInExp,
      EasingType.EaseOutExp,
      EasingType.EaseInOutExp,
    };

    public static float ApplyEasingFromSelection(float t, string easing)
    {
      switch (easing)
      {
        case EasingType.Hold:
          return Hold(t);
        case EasingType.Linear:
          return Linear(t);
        case EasingType.EaseInQuad:
          return EaseInQuad(t);
        case EasingType.EaseOutQuad:
          return EaseOutQuad(t);
        case EasingType.EaseInOutQuad:
          return EaseInOutQuad(t);
        case EasingType.EaseInCubic:
          return EaseInCubic(t);
        case EasingType.EaseOutCubic:
          return EaseOutCubic(t);
        case EasingType.EaseInOutCubic:
          return EaseInOutCubic(t);
        case EasingType.EaseInQuint:
          return EaseInQuint(t);
        case EasingType.EaseOutQuint:
          return EaseOutQuint(t);
        case EasingType.EaseInOutQuint:
          return EaseInOutQuint(t);
        case EasingType.EaseInExp:
          return EaseInExp(t);
        case EasingType.EaseOutExp:
          return EaseOutExp(t);
        case EasingType.EaseInOutExp:
          return EaseInOutExp(t);
        default:
          return Linear(t);
      }
    }

    public static float Hold(float t)
    {
      if (t >= 1f) return 1f;
      return 0f;
    }

    public static float Linear(float t)
    {
      return t;
    }

    public static float EaseInQuad(float t)
    {
      return t * t;
    }

    public static float EaseOutQuad(float t)
    {
      return 1f - (1f - t) * (1f - t);
    }

    public static float EaseInOutQuad(float t)
    {
      return t < 0.5f
        ? 2f * t * t
        : 1f - (-2f * t + 2f) * (-2f * t + 2f) / 2f;
    }

    public static float EaseInCubic(float t)
    {
      return t * t * t;
    }

    public static float EaseOutCubic(float t)
    {
      return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    public static float EaseInOutCubic(float t)
    {
      return t < 0.5f
        ? 4f * t * t * t
        : 1f - (-2f * t + 2f) * (-2f * t + 2f) * (-2f * t + 2f) / 2f;
    }

    public static float EaseInQuint(float t)
    {
      return t * t * t * t * t;
    }

    public static float EaseOutQuint(float t)
    {
      return 1f - (1f - t) * (1f - t) * (1f - t) * (1f - t) * (1f - t);
    }

    public static float EaseInOutQuint(float t)
    {
      return t < 0.5f
        ? 16f * t * t * t * t * t
        : 1f - (-2f * t + 2f) * (-2f * t + 2f) * (-2f * t + 2f) * (-2f * t + 2f) * (-2f * t + 2f) / 2f;
    }

    public static float EaseInExp(float t)
    {
      return t == 0f ? 0f : Mathf.Exp(7f * t - 7f);
    }

    public static float EaseOutExp(float t)
    {
      return t == 1f ? 1f : 1f - Mathf.Exp(-7f * t);
    }

    public static float EaseInOutExp(float t)
    {
      return t == 0f ? 0f
        : t == 1f ? 1f
        : t < 0.5f
          ? Mathf.Exp(14f * t - 7f) / 2f
          : 1f - Mathf.Exp(-14f * t + 7f) / 2f;
    }
  }
}
