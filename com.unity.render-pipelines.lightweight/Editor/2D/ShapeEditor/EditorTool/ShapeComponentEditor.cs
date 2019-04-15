using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.LWRP.Path2D;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal abstract class ShapeComponentEditor<T> : Editor where T : ScriptableShapeEditor
    {
        private static class Contents
        {
            public static readonly GUIContent snappingLabel = new GUIContent("Snapping", "Snap points using the snap settings");
        }

        private Editor m_CachedEditor = null;

        protected void DoEditButton<U>(GUIContent icon, string label) where U : ShapeEditorTool<T>
        {
            const float kButtonWidth = 33;
            const float kButtonHeight = 23;
            const float k_SpaceBetweenLabelAndButton = 5;
            var buttonStyle = new GUIStyle("EditModeSingleButton");

            var rect = EditorGUILayout.GetControlRect(true, kButtonHeight, buttonStyle);
            var buttonRect = new Rect(rect.xMin + EditorGUIUtility.labelWidth, rect.yMin, kButtonWidth, kButtonHeight);

            var labelContent = new GUIContent(label);
            var labelSize = GUI.skin.label.CalcSize(labelContent);

            var labelRect = new Rect(
                buttonRect.xMax + k_SpaceBetweenLabelAndButton,
                rect.yMin + (rect.height - labelSize.y) * .5f,
                labelSize.x,
                rect.height);

            using (new EditorGUI.DisabledGroupScope(!EditorToolManager.IsAvailable<U>()))
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var isActive = GUI.Toggle(buttonRect, EditorToolManager.IsActiveTool<U>(), icon, buttonStyle);
                    
                    GUI.Label(labelRect, label);

                    if (check.changed)
                    {
                        if (isActive)
                            EditorTools.EditorTools.SetActiveTool<U>();
                        else
                            EditorTools.EditorTools.RestorePreviousTool();
                    }
                }
            }
        }

        protected void DoShapeEditorInspector<U>() where U : ShapeEditorTool<T>
        {
            if (EditorToolManager.IsActiveTool<U>() && EditorToolManager.IsAvailable<U>())
            {
                var shapeEditors = EditorToolManager.GetEditorTool<U>().shapeEditors;

                CreateCachedEditor(shapeEditors, null, ref m_CachedEditor);

                if (m_CachedEditor == null) //Needed to avoid a nullref on exiting playmode
                    return;

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_CachedEditor.OnInspectorGUI();

                    if (check.changed)
                        EditorToolManager.GetEditorTool<U>().SetShapes();
                }
            }
        }

        protected void DoSnappingInspector<U>() where U : ShapeEditorTool<T>
        {
            if (EditorToolManager.IsActiveTool<U>() && EditorToolManager.IsAvailable<U>())
            {
                var tool = EditorToolManager.GetEditorTool<U>();
                tool.enableSnapping = EditorGUILayout.Toggle(Contents.snappingLabel, tool.enableSnapping);
            }
        }

        protected void DoOpenEndedInspector<U>(SerializedProperty isOpenEndedProperty) where U : ShapeEditorTool<T>
        {
            serializedObject.Update();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(isOpenEndedProperty);

                if (check.changed)
                {
                    if (EditorToolManager.IsActiveTool<U>() && EditorToolManager.IsAvailable<U>())
                    {
                        var shapeEditors = EditorToolManager.GetEditorTool<U>().shapeEditors;
                        
                        foreach (var shapeEditor in shapeEditors)
                        {
                            shapeEditor.undoObject.RegisterUndo("Set Open Ended");
                            shapeEditor.isOpenEnded = isOpenEndedProperty.boolValue;
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
