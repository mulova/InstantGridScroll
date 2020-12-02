using UnityEngine;
using static mulova.ugui.InstantScrollRect;

namespace mulova.ugui
{
    public abstract class InstantScrollItem : MonoBehaviour
    {
        abstract internal RectTransform bound { get; }
        private RectTransform _rect;
        public RectTransform rect
        {
            get
            {
                if (_rect == null)
                {
                    _rect = transform as RectTransform;
                }
                return _rect;
            }
        }
        internal Item item { get; set; }

        internal bool isValid => item != null;

        public Vector2 pos
        {
            set
            {
                rect.localPosition = value;
            }
            get
            {
                return rect.localPosition;
            }
        }


        public float y
        {
            set
            {
                var p = rect.localPosition;
                p.y = value;
                rect.localPosition = p;
            }
            get
            {
                return rect.localPosition.y;
            }
        }

        public float x
        {
            set
            {
                var p = rect.localPosition;
                p.x = value;
                rect.localPosition = p;
            }
            get
            {
                return rect.localPosition.x;
            }
        }

        internal void Clear()
        {
            item = null;
        }
    }
}
