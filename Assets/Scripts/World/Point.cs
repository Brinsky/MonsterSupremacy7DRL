using UnityEngine;
using System.Collections;

public class Point
{
    public int x;
    public int y;

    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Point(Point other)
    {
        this.x = other.x;
        this.y = other.y;
    }

    public float Distance(Point other)
    {
        return Vector2.Distance(this, other);
    }

    public override bool Equals(System.Object obj)
    {
        if ((object)obj == null)
        {
            return false;
        }

        return obj is Point && this == (Point)obj;
    }

    public static bool operator ==(Point a, Point b)
    {
        // If both are null, or both are same instance, return true.
        if (System.Object.ReferenceEquals(a, b))
        {
            return true;
        }

        // If one is null, but not both, return false.
        if (((object)a == null) || ((object)b == null))
        {
            return false;
        }

        // Return true if the fields match:
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Point a, Point b)
    {
        return !(a == b);
    }

    public override string ToString()
    {
        return "(" + x + "," + y + ")";
    }
    
    public static implicit operator Vector2(Point p)
    {
        return new Vector2(p.x, p.y);
    }

}

