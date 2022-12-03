using System;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public abstract class BaseComponentWithId : IEquatable<BaseComponentWithId>
    {
      public abstract string id { get; protected set; }

      public bool Equals(BaseComponentWithId other)
      {
        return other.id == this.id;
      }

      public override bool Equals(object other)
      {
        if (other is BaseComponentWithId)
        {
          return this.Equals((BaseComponentWithId)other);
        }
        return false;
      }

      public override int GetHashCode()
      {
        return id.GetHashCode();
      }

      public static bool operator ==(BaseComponentWithId a, BaseComponentWithId b)
      {
        if (ReferenceEquals(a, b)) return true;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
        return a.Equals(b);
      }

      public static bool operator !=(BaseComponentWithId a, BaseComponentWithId b)
      {
        return !(a == b);
      }
    }
  }
}
