using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Actors
{
    /// <summary>
    /// World-space name label that floats above the player armature head and billboards
    /// toward the active gameplay camera. Updates when <see cref="PlayerActor.DisplayName"/>
    /// changes (local sheet load or replicated network identity).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerActor))]
    [AddComponentMenu("GameCore/Actors/Player Name Tag")]
    public class PlayerNameTag : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Actor supplying DisplayName. Auto-resolved from this GameObject.")]
        private PlayerActor _playerActor;

        [SerializeField]
        [Tooltip("Bone or transform the label floats above (typically the head bone).")]
        private Transform _anchor;

        [SerializeField]
        private Vector3 _anchorOffset = new Vector3(0f, 0.3f, 0f);

        [SerializeField]
        [Tooltip("TMP font used for the label.")]
        private TMP_FontAsset _fontAsset;

        [SerializeField]
        [Tooltip("Uniform scale applied to the world-space canvas.")]
        private float _canvasScale = 0.01f;

        [SerializeField]
        private float _fontSize = 14f;

        [SerializeField]
        private Vector2 _padding = new Vector2(6f, 3f);

        [SerializeField]
        private Color _textColor = Color.white;

        [SerializeField]
        private Color _backgroundColor = new Color(0f, 0f, 0f, 0.55f);

        [SerializeField]
        [Range(0f, 1f)]
        private float _outlineWidth = 0.2f;

        private Transform _labelRoot;
        private RectTransform _rootRect;
        private RectTransform _textRect;
        private TextMeshProUGUI _labelText;
        private string _lastDisplayedName;

        private void Awake()
        {
            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            if (_anchor == null)
            {
                var controller = GetComponent<PlayerController>();
                if (controller != null)
                    _anchor = controller.FirstPersonHeadBone;
            }

            if (_anchor == null)
                _anchor = transform;

            EnsureLabelHierarchy();
        }

        private void OnEnable()
        {
            ActorRegistry.ActorUpdated += HandleActorUpdated;
            RefreshLabel();
        }

        private void OnDisable()
        {
            ActorRegistry.ActorUpdated -= HandleActorUpdated;
        }

        private void LateUpdate()
        {
            if (_labelRoot == null || _anchor == null)
                return;

            _labelRoot.position = _anchor.position + _anchorOffset;

            Camera camera = ResolveCamera();
            if (camera == null)
                return;

            Vector3 forward = _labelRoot.position - camera.transform.position;
            if (forward.sqrMagnitude > 0.0001f)
                _labelRoot.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void HandleActorUpdated(IActor actor)
        {
            if (ReferenceEquals(actor, _playerActor))
                RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (_labelRoot == null || _labelText == null || _playerActor == null)
                return;

            if (!_playerActor.IsOwnershipResolved)
            {
                _labelRoot.gameObject.SetActive(false);
                return;
            }

            string name = _playerActor.DisplayName ?? string.Empty;
            bool hasName = !string.IsNullOrEmpty(name);
            _labelRoot.gameObject.SetActive(hasName);
            if (!hasName || name == _lastDisplayedName)
                return;

            _lastDisplayedName = name;
            _labelText.text = name;
            ResizeToFitText();
        }

        private void ResizeToFitText()
        {
            _labelText.ForceMeshUpdate();
            Vector2 textSize = _labelText.GetPreferredValues(_labelText.text);
            _textRect.sizeDelta = textSize;
            _rootRect.sizeDelta = textSize + _padding * 2f;
        }

        private void EnsureLabelHierarchy()
        {
            if (_labelRoot != null)
                return;

            var root = new GameObject("NameTag");
            root.transform.SetParent(transform, false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            _rootRect = root.GetComponent<RectTransform>();
            _rootRect.pivot = new Vector2(0.5f, 0.5f);
            _rootRect.localScale = Vector3.one * _canvasScale;

            var background = CreateUiObject<Image>("Background", root.transform);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.color = _backgroundColor;
            background.raycastTarget = false;

            var textObject = CreateUiObject<TextMeshProUGUI>("Label", root.transform);
            _textRect = textObject.GetComponent<RectTransform>();
            _textRect.anchorMin = new Vector2(0.5f, 0.5f);
            _textRect.anchorMax = new Vector2(0.5f, 0.5f);
            _textRect.pivot = new Vector2(0.5f, 0.5f);
            _textRect.anchoredPosition = Vector2.zero;

            _labelText = textObject;
            if (_fontAsset != null)
                _labelText.font = _fontAsset;

            _labelText.fontSize = _fontSize;
            _labelText.color = _textColor;
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.overflowMode = TextOverflowModes.Overflow;
            _labelText.enableWordWrapping = false;
            _labelText.raycastTarget = false;
            _labelText.outlineWidth = _outlineWidth;
            _labelText.outlineColor = new Color32(0, 0, 0, 200);

            _labelRoot = root.transform;
            _labelRoot.gameObject.SetActive(false);
        }

        private static T CreateUiObject<T>(string objectName, Transform parent) where T : Component
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.AddComponent<T>();
        }

        private static Camera ResolveCamera()
        {
            return Camera.main;
        }
    }
}
