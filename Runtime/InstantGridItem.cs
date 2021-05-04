using UnityEngine;

namespace mulova.ugui
{
    public class InstantGridItem : GridItem
    {
        [SerializeField] private RectTransform bounds;
        internal override RectTransform bound => bounds;
    }
}
