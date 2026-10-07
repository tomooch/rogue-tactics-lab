using System;

namespace RogueTactics.Core
{
    // Integer coordinate arithmetic only. Traversal and gameplay legality remain undecided.
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }
        public GridPosition(int x, int y) { X = x; Y = y; }
        public GridPosition Offset(int x, int y) => new GridPosition(checked(X + x), checked(Y + y));
        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
    }
}
