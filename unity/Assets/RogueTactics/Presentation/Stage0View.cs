using RogueTactics.Core;
using UnityEngine;

namespace RogueTactics.Presentation
{
    // Static scaffold only: no input, turn resolution, targeting, or intent prediction.
    public sealed class Stage0View : MonoBehaviour
    {
        public GridPosition ReferenceOrigin { get; private set; }
        private void Awake() => ReferenceOrigin = new GridPosition(0, 0);
    }
}
