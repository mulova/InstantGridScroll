using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace mulova.ugui
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class InvisibleWidget : Graphic
    {
        protected override void UpdateGeometry() { }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(InvisibleWidget))]
    public class ClickableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI(){}
    }
#endif
}

