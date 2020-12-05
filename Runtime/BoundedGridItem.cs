using UnityEngine;

namespace mulova.ugui
{
    public class BoundedGridItem : InstantGridItem
    {
        [SerializeField] private RectTransform bounds;
        internal override RectTransform bound => bounds;
    }
}
