using DG.Tweening;
using UnityEngine;

/// <summary>
/// Centralises all board animation and timing parameters.
/// Assign one asset to <see cref="GamePlayBoard"/> via the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "BoardAnimationConfig", menuName = "Game/Board Animation Config")]
public class BoardAnimationConfig : ScriptableObject
{
    [Header("Drop Animation")]
    [Tooltip("Duration of the brick-drop tween in seconds.")]
    public float dropDuration     = 0.3f;

    [Tooltip("Per-brick delay before the drop tween starts. Stagger between bricks when set > 0.")]
    public float dropDelay        = 0.01f;

    [Tooltip("Easing curve applied to the drop tween.")]
    public Ease  dropEase         = Ease.InOutQuad;

    [Tooltip("How far above the top of the grid new bricks spawn before dropping in.")]
    public float dropHeightOffset = 2.3f;

    [Header("Destroy Animation")]
    [Tooltip("Total duration of the shrink-to-zero destruction animation in seconds.")]
    public float destroyDuration = 0.2f;

    [Header("Board")]
    [Tooltip("Extra padding added to each side of the board background sprite.")]
    public float backgroundPadding = 0.3f;

}
