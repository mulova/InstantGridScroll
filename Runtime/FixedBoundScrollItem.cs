using UnityEngine;

namespace mulova.ugui
{
    public class FixedBoundScrollItem : InstantScrollItem
    {
        [SerializeField] private RectTransform bounds;
        internal override RectTransform bound => bounds;
    }
}
