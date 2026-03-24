// Majority of bezier & spline code is open source and availiable from: 
// http://catlikecoding.com/unity/tutorials/curves-and-splines/

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BezierSplines
{
    [AddComponentMenu("")]
    public class Line : MonoBehaviour
    {
        public Vector3 p0, p1;
    }

#if UNITY_EDITOR 
    [CustomEditor(typeof(Line))]
    public class LineEdiorInspector : Editor
    {
        protected void OnSceneGUI()
        {
            var line = target as Line;
            var handleTransform = line.transform;
            var handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;
            var p0 = handleTransform.TransformPoint(line.p0);
            var p1 = handleTransform.TransformPoint(line.p1);

            Handles.color = Color.white;
            Handles.DrawLine(line.p0, line.p1);
            Handles.DoPositionHandle(p0, handleRotation);
            Handles.DoPositionHandle(p1, handleRotation);

            EditorGUI.BeginChangeCheck();
            p0 = Handles.DoPositionHandle(p0, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(line, "Move Point");
                EditorUtility.SetDirty(line);
                line.p0 = handleTransform.InverseTransformPoint(p0);
            }
            EditorGUI.BeginChangeCheck();
            p1 = Handles.DoPositionHandle(p1, handleRotation);
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(line, "Move Point");
            EditorUtility.SetDirty(line);
            line.p1 = handleTransform.InverseTransformPoint(p1);
        }
    }
#endif
}