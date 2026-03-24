// Majority of bezier & spline code is open source and availiable from: 
// http://catlikecoding.com/unity/tutorials/curves-and-splines/

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; 
#endif

namespace BezierSplines
{
    public static class Bezier
    {
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            var oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p2;
        }

        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            var oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            var oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }
    }


#if UNITY_EDITOR
    [AddComponentMenu("")]
    public class BezierCurve : MonoBehaviour
    {
        public Vector3[] points;

        public void Reset()
        {
            points = new Vector3[] 
            {
                new Vector3(1f, 0f, 0f),
                new Vector3(2f, 0f, 0f),
                new Vector3(3f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            };
        }

        public Vector3 GetPoint(float t)
        {
            return transform.TransformPoint(Bezier.GetPoint(points[0], points[1], points[2], points[3], t));
        }

        public Vector3 GetVelocity(float t)
        {
            return transform.TransformPoint(Bezier.GetFirstDerivative(points[0], points[1], points[2], points[3], t)) - transform.position;
        }

        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }
    }

    [CustomEditor(typeof(BezierCurve))]
    public class BezierCurveEditorInspector : Editor
    {
        private BezierCurve curve;
        private Transform handleTransform;
        private Quaternion handleRotation;

        private const int LINE_STEPS = 10;

        private const float DIRECTION_SCALE = 0.5f;

        protected void OnSceneGUI()
        {
            curve = target as BezierCurve;
            if (curve != null) handleTransform = curve.transform;
            handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;

            var p0 = ShowPoint(0);
            var p1 = ShowPoint(1);
            var p2 = ShowPoint(2);
            var p3 = ShowPoint(3);

            Handles.color = Color.grey;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            ShowDirections();
            Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
        }

        private Vector3 ShowPoint(int index)
        {
            var point = handleTransform.TransformPoint(curve.points[index]);
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);
            if (!EditorGUI.EndChangeCheck()) return point;
            Undo.RecordObject(curve, "Move Point");
            EditorUtility.SetDirty(curve);
            curve.points[index] = handleTransform.InverseTransformPoint(point);
            return point;
        }

        private void ShowDirections()
        {
            Handles.color = Color.green;
            var point = curve.GetPoint(0f);
            Handles.DrawLine(point, point + curve.GetDirection(0f) * DIRECTION_SCALE);
            for (var i = 1; i <= LINE_STEPS; i++)
            {
                point = curve.GetPoint(i / (float)LINE_STEPS);
                Handles.DrawLine(point, point + curve.GetDirection(i / (float)LINE_STEPS) * DIRECTION_SCALE);
            }
        }
    }
#endif
}