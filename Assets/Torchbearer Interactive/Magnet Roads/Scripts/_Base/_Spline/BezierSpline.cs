// Majority of bezier & spline code is open source and availiable from: 
// http://catlikecoding.com/unity/tutorials/curves-and-splines/

// This source file has been modified from the original source
// Re-implementing this code as intended in the original source material
// may not work as expected

// Additions include: 
// + Methods to extrapolate specifically offset vectors from the spline
// + Expansions to AddCurve method for more functionality

using UnityEngine;
using MagnetRoads;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BezierSplines
{
    public enum BezierControlPointMode
    {
        Free,
        Aligned,
        Mirrored
    }

    [AddComponentMenu("")]
    public class BezierSpline : MonoBehaviour
    {
        [SerializeField]
        private BezierControlPointMode[] modes;
        [SerializeField]
        private Vector3[] points;

        public int CurveCount
        {
            get
            {
                return (points.Length - 1) / 3;
            }
        }

        public int ControlPointCount
        {
            get
            {
                return points.Length;
            }
        }

        public Vector3 GetControlPoint(int index)
        {
            return points[index];
        }

        public void Awake()
        {
            if (points == null) Reset();
        }

        public void SetControlPoint(int index, Vector3 point)
        {
            if(index >= points.Length)
            {
                Debug.Log("Index out of range : Setcontrolepoint : BezierSpline line 77");
                return;
            }
            if (index % 3 == 0)
            {
                // if we're adjusting the position of a centre point, then we should also adjust the position of the
                // neighbouring points too
                var centrePointPositionOffset = point - points[index];
                if (index > 0)
                {
                    points[index - 1] += centrePointPositionOffset;
                }
                if (index + 1 < points.Length)
                {
                    points[index + 1] += centrePointPositionOffset;
                }
            }
            points[index] = point;
            EnforceMode(index);
        }

        public void Reset()
        {
            points = new[] 
            {
                new Vector3(-1.5f, 0f, 0f),
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(1.5f, 0f, 0f)
            };
            modes = new[] 
            {
                BezierControlPointMode.Aligned,
                BezierControlPointMode.Aligned
            };
        }

        // This method has been expanded to take into account consistent distances between points
        // at different road scales and to allow you to extend the spline from both ends
        public void AddCurve(int stepsPerCurve, bool atPositive, float distanceBetweenPoints = 1f)
        {
            Vector3 point, direction;
            if (!atPositive)
            {
                point = points[points.Length - 1];
                direction = transform.InverseTransformDirection(GetDirection(points.Length - 1));

                Array.Resize(ref points, points.Length + 3);
                point += direction * distanceBetweenPoints;
                points[points.Length - 3] = point;
                point += direction * distanceBetweenPoints;
                points[points.Length - 2] = point;
                point += direction * distanceBetweenPoints;
                points[points.Length - 1] = point;
            }
            else
            {
                point = points[0];
                direction = transform.InverseTransformDirection(GetDirection(0));

                Array.Resize(ref points, points.Length + 3);
                for (var i = points.Length - 1; i > 2; i--)
                {
                    points[i] = points[i - 3];
                }
                point -= direction * distanceBetweenPoints;
                points[2] = point;
                point -= direction * distanceBetweenPoints;
                points[1] = point;
                point -= direction * distanceBetweenPoints;
                points[0] = point;
            }

            Array.Resize(ref modes, modes.Length + 1);
            modes[modes.Length - 1] = modes[modes.Length - 2];
            EnforceMode(points.Length - 4);
        }

        public void RemoveCurve(bool atPositive)
        {
            if (points.Length <= 4) return;
            if (!atPositive) Array.Resize(ref points, points.Length - 3);
            else
            {
                var newPoints = new Vector3[points.Length - 3];
                for (var i = 3; i < points.Length; i++)
                {
                    newPoints[i - 3] = points[i];
                }
                points = newPoints;
            }
        }

        public Vector3 GetPoint(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], t));
        }

        public Vector3 GetVelocity(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
        }

        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

        public Vector3 GetRotation(float t, Vector3? offset = null)
        {
            var rotation = offset != null ? Quaternion.Euler(offset.Value.x, offset.Value.y, offset.Value.z) : Quaternion.Euler(0,0,0);
            return rotation * transform.InverseTransformDirection(GetDirection(t));
        }

        private void EnforceMode(int index)
        {
            var modeIndex = (index + 1) / 3;
            var mode = modes[modeIndex];
            if (mode == BezierControlPointMode.Free || modeIndex == 0 || modeIndex == modes.Length - 1)
            {
                return;
            }
            var middleIndex = modeIndex * 3;
            int fixedIndex, enforcedIndex;
            if (index <= middleIndex)
            {
                fixedIndex = middleIndex - 1;
                enforcedIndex = middleIndex + 1;
            }
            else
            {
                fixedIndex = middleIndex + 1;
                enforcedIndex = middleIndex - 1;
            }
            var middle = points[middleIndex];
            var enforcedTangent = middle - points[fixedIndex];
            if (mode == BezierControlPointMode.Aligned && enforcedIndex <= points.Length - 1)
            {
                enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
            }
            if (enforcedIndex <= points.Length - 1) points[enforcedIndex] = middle + enforcedTangent;
        }

        public BezierControlPointMode GetControlPointMode(int index) { return modes[(index + 1) / 3]; }

        public void SetControlPointMode(int index, BezierControlPointMode mode)
        {
            modes[(index + 1) / 3] = mode;
            EnforceMode(index);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(BezierSpline))]
    public class BezierSplineEditorInspector : Editor
    {
        private BezierSpline spline;
        private Transform handleTransform;
        private Quaternion handleRotation;

        private const int STEPS_PER_CURVE = 10;
        private const float DIRECTION_SCALE = 0.5f;
        private const float HANDLE_SIZE = 0.08f;
        private const float PICK_SIZE = 0.1f;

        private int selectedIndex = -1;

        private static readonly Color[] sModeColors = {
            Color.white,
            Color.yellow,
            Color.cyan
        };

        public override void OnInspectorGUI()
        {
            spline = target as BezierSpline;
            if (spline == null) return;
            var thisMagnetRoad = spline.gameObject.GetComponent<MagnetRoad>();

            GUILayout.Label("Curve Point Editor:", EditorStyles.boldLabel);
            if (selectedIndex >= 0 && selectedIndex < spline.ControlPointCount)
            {
                DrawSelectedPointInspector();
                thisMagnetRoad.SnapFirstAndLastPoints(selectedIndex);
            }
            else
            {
                GUILayout.Label("      NO CURVE POINT SELECTED", EditorStyles.miniBoldLabel);
            }
        }

        private void DrawSelectedPointInspector()
        {
            EditorGUI.BeginChangeCheck();
            var point = EditorGUILayout.Vector3Field("Position", spline.GetControlPoint(selectedIndex));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "Move Point");
                EditorUtility.SetDirty(spline);
                spline.SetControlPoint(selectedIndex, point);
            }
            EditorGUI.BeginChangeCheck();
            var mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Alignment Mode", spline.GetControlPointMode(selectedIndex));
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(spline, "Change Point Alignment Mode");
            spline.SetControlPointMode(selectedIndex, mode);
            EditorUtility.SetDirty(spline);
        }

        protected void OnSceneGUI() 
        {
            spline = target as BezierSpline;
            if (spline != null)
            {
                handleTransform = spline.transform;
                handleRotation = Tools.pivotRotation == PivotRotation.Local
                    ? handleTransform.rotation
                    : Quaternion.identity;

                var p0 = ShowPoint(0);
                for (var i = 1; i < spline.ControlPointCount; i += 3)
                {
                    var p1 = ShowPoint(i,Color.blue);
                    var p2 = ShowPoint(i + 1, Color.red);
                    var p3 = ShowPoint(i + 2);

                    Handles.color = Color.yellow;
                    Handles.DrawLine(p0, p1);
                    Handles.DrawLine(p2, p3);

                    Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
                    p0 = p3;
                }
            }
            ShowDirections();
        }

        private Vector3 ShowPoint(int index, Color? setColour = null)
        {
            var point = handleTransform.TransformPoint(spline.GetControlPoint(index));
            var size = HandleUtility.GetHandleSize(point);
            Handles.color = setColour == null ? sModeColors[(int)spline.GetControlPointMode(index)] : setColour.Value;
#if UNITY_2017_1_OR_NEWER
            if (Handles.Button(point, handleRotation, size * HANDLE_SIZE, size * PICK_SIZE, Handles.DotHandleCap))
            {
                selectedIndex = index;
                Repaint();
            }
#else
            if (Handles.Button(point, handleRotation, size * HANDLE_SIZE, size * PICK_SIZE, Handles.DotCap))
            {
                selectedIndex = index;
                Repaint();
            }
#endif

            if (selectedIndex != index) return point;

            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);

            if (!EditorGUI.EndChangeCheck()) return point;

            Undo.RecordObject(spline, "Move Point");
            EditorUtility.SetDirty(spline);
            spline.SetControlPoint(index, handleTransform.InverseTransformPoint(point));

            return point;
        }

        private void ShowDirections()
        {
            Handles.color = Color.green;
            var point = spline.GetPoint(0f);
            Handles.DrawLine(point, point + spline.GetDirection(0f) * DIRECTION_SCALE);
            var steps = STEPS_PER_CURVE * spline.CurveCount;
            for (var i = 1; i <= steps; i++)
            {
                point = spline.GetPoint(i / (float)steps);
                Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * DIRECTION_SCALE);
            }
        }
    }
#endif
        }