using System;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public abstract class BaseComponent : IEquatable<BaseComponent>
    {
      public abstract string id { get; protected set; }

      public bool Equals(BaseComponent other)
      {
        return other.id == this.id;
      }

      public override bool Equals(object other)
      {
        if (other is BaseComponent)
        {
          return this.Equals((BaseComponent)other);
        }
        return false;
      }

      public override int GetHashCode()
      {
        return id.GetHashCode();
      }

      public static bool operator ==(BaseComponent a, BaseComponent b)
      {
        if (ReferenceEquals(a, b)) return true;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
        return a.Equals(b);
      }

      public static bool operator !=(BaseComponent a, BaseComponent b)
      {
        return !(a == b);
      }
    }
  }
}
