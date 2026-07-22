// UIPanelMaxSize.cs
using UnityEngine;

namespace AIAirHockey
{
    // Attach to the ROOT RectTransform of any popup/panel (DifficultyPanel,
    // SettingsPanel, PausePopup, ResultPopup, the MainMenu button group...).
    //
    // This is a SAFETY NET, not the primary fix. The panels are expanding
    // on tablet because of the Canvas Scaler setup, not because of this
    // script's absence -- see the checklist below. Fix that first; this
    // component then guarantees no panel can ever exceed a sane size even
    // if a Canvas Scaler setting drifts later or a panel's anchors are
    // accidentally left as "stretch".
    //
    // === Primary fix (do this in the Unity Editor, once per Canvas) ===
    //   Canvas Scaler component, on EVERY Canvas in EVERY scene
    //   (MainMenu, Gameplay, Bootstrap if it has UI):
    //     UI Scale Mode      = Scale With Screen Size
    //     Reference Resolution = 1080 x 1920
    //     Screen Match Mode  = Match Width Or Height
    //     Match              = 0.5 (start here, tune by eye)
    //   And on each popup panel's RectTransform: use a FIXED anchor
    //   (e.g. center, 0.5/0.5) with an explicit width/height, NOT a
    //   stretched anchor (0,0)-(1,1) with small offsets. A stretched
    //   anchor is what makes a panel resize itself to a % of the
    //   screen/canvas instead of a fixed design size -- that's almost
    //   certainly why it looked fine on phone and ballooned on tablet.
    //
    // === What this script adds on top ===
    // Reads the panel's ACTUAL rendered size every time the screen/canvas
    // changes. If it's within the cap, this script does nothing and never
    // touches your anchors. If it ever exceeds the cap (e.g. a stretched
    // anchor on a wide/tall tablet), it forcibly re-anchors the panel to a
    // fixed center anchor at the clamped size, so it can't grow past the
    // cap again.
    //
    // _maxWidth/_maxHeight are in the SAME units as your Canvas Scaler's
    // reference resolution (e.g. with 1080x1920, a comfortable popup width
    // is often ~650-750).
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class UIPanelMaxSize : MonoBehaviour
    {
        [SerializeField] private float _maxWidth = 700f;
        [SerializeField] private float _maxHeight = 1100f;
        [Tooltip("Scale both axes together so the panel keeps its designed proportions instead of squashing.")]
        [SerializeField] private bool _preserveAspect = true;

        private RectTransform _rt;

        private void Awake()  { _rt = GetComponent<RectTransform>(); }
        private void OnEnable()  => Apply();
        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();

            // .rect.size is the ACTUAL computed size after anchors/parent
            // layout are resolved -- this works whether the panel uses a
            // fixed anchor + sizeDelta, or a stretched anchor.
            Vector2 size = _rt.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;

            float scale = 1f;
            if (size.x > _maxWidth)  scale = Mathf.Min(scale, _maxWidth  / size.x);
            if (size.y > _maxHeight) scale = Mathf.Min(scale, _maxHeight / size.y);

            if (scale >= 0.999f) return; // already within the cap -- leave anchors untouched

            Vector2 clamped = _preserveAspect
                ? size * scale
                : new Vector2(Mathf.Min(size.x, _maxWidth), Mathf.Min(size.y, _maxHeight));

            // Lock to a fixed, centered size so it can't balloon again.
            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.sizeDelta = clamped;
        }
    }
}
