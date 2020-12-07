using mulova.ugui;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(InstantGridScroll), true)]
[CanEditMultipleObjects]
public class InstantGridScrollEditor : ScrollRectEditor
{
    private SerializedProperty m_Prefab;
    private SerializedProperty m_Padding;
    private SerializedProperty m_Border;
    private SerializedProperty m_LineItemSize;
    private SerializedProperty m_Empty;
    private SerializedProperty m_ChangeItemName;
    private SerializedProperty m_AlighX;

    private InstantGridScroll grid;

    protected override void OnEnable()
    {
        base.OnEnable();
        grid = target as InstantGridScroll;
        m_Prefab = serializedObject.FindProperty("prefab");
        m_Padding = serializedObject.FindProperty("padding");
        m_Border = serializedObject.FindProperty("border");
        m_LineItemSize = serializedObject.FindProperty("lineItemSize");
        m_Empty = serializedObject.FindProperty("empty");
        m_ChangeItemName = serializedObject.FindProperty("changeItemName");
        m_AlighX = serializedObject.FindProperty("alignX");
    }

    public override void OnInspectorGUI()
    {
        if (!Application.isPlaying)
        {
            if (grid.content == null)
            {
                EditorGUILayout.HelpBox("content missing", MessageType.Error);
            } else if (grid.content.GetComponentInChildren<InstantGridItem>(true) == null)
            {
                EditorGUILayout.HelpBox(typeof(InstantGridItem).Name + " missing", MessageType.Error);
            } else if (m_LineItemSize.intValue <= 0)
            {
                EditorGUILayout.HelpBox("lineItemSize needs to be positive", MessageType.Error);
            }
        }
        using (var c = new EditorGUI.ChangeCheckScope())
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Prefab);
            EditorGUILayout.PropertyField(m_Padding);
            EditorGUILayout.PropertyField(m_Border);
            EditorGUILayout.PropertyField(m_LineItemSize);
            EditorGUILayout.PropertyField(m_Empty);
            EditorGUILayout.PropertyField(m_ChangeItemName);
            EditorGUILayout.PropertyField(m_AlighX);
            serializedObject.ApplyModifiedProperties();
            if (c.changed)
            {
                (target as InstantGridScroll).ResetAndArrange();
            }
        }
    }
}
