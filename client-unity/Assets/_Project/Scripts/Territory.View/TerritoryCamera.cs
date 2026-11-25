using UnityEngine;

namespace Ecosystem.Territory.View
{
    /// <summary>
    /// Encuadra el territorio con la cámara ortográfica: recalcula solo cuando cambian el tamaño
    /// de la pantalla, el área segura o la disposición (research.md R-6; FR-018). La inclinación es
    /// la de la propia cámara (sin giro); este componente solo cambia su posición y su tamaño.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class TerritoryCamera : MonoBehaviour
    {
        [SerializeField] private TerritoryView view;
        [SerializeField, Range(0f, 0.2f)] private float marginFraction = 0.06f;
        [SerializeField, Min(1f)] private float distance = 20f;

        private Camera _camera;
        private int _lastWidth;
        private int _lastHeight;
        private Rect _lastSafeArea;
        private TerritoryLayout _lastLayout;

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            _lastLayout = null;
            _lastWidth = _lastHeight = 0;
        }

        private void LateUpdate()
        {
            if (view == null || view.Layout == null) return;

            var width = _camera.pixelWidth;
            var height = _camera.pixelHeight;
            // Fuera de Play, Screen describe la vista que se dibuja (también la de escena): el área
            // segura solo se usa en Play y cuando corresponde a la pantalla de la cámara.
            var safeArea = Application.isPlaying && Screen.width == width && Screen.height == height
                ? Screen.safeArea
                : new Rect(0, 0, width, height);

            if (width == _lastWidth && height == _lastHeight && safeArea == _lastSafeArea && ReferenceEquals(view.Layout, _lastLayout)) return;
            Apply(width, height, safeArea);
        }

        /// <summary>Aplica el encuadre para una pantalla dada (también lo usan las pruebas).</summary>
        internal void Apply(int width, int height, Rect safeArea)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (view == null || view.Layout == null || width <= 0 || height <= 0) return;

            var origin = view.transform.position;
            var local = view.Layout.Bounds(HexMetrics.Default).Corners();
            var corners = new Point3[local.Length];
            for (var i = 0; i < local.Length; i++)
            {
                corners[i] = new Point3(local[i].X + origin.x, local[i].Y + origin.y, local[i].Z + origin.z);
            }

            var tilt = transform.eulerAngles.x;
            var safe = new ScreenRect(safeArea.x, safeArea.y, safeArea.width, safeArea.height);
            var framing = TerritoryFraming.Compute(corners, tilt, width, height, safe, marginFraction);
            var (centerU, centerV) = TerritoryFraming.ProjectedCenter(corners, tilt);

            var rotation = Quaternion.Euler(tilt, 0f, 0f);
            var right = rotation * Vector3.right;
            var up = rotation * Vector3.up;
            var forward = rotation * Vector3.forward;
            var u = centerU + framing.CameraOffsetU;
            var v = centerV + framing.CameraOffsetV;

            _camera.orthographic = true;
            _camera.orthographicSize = framing.OrthographicSize;
            transform.position = right * u + up * v - forward * distance;

            _lastWidth = width;
            _lastHeight = height;
            _lastSafeArea = safeArea;
            _lastLayout = view.Layout;
        }
    }
}
