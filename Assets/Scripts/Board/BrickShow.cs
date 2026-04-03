using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// View component for a single board cell.
/// Handles sprite display, click forwarding, and drop animations.
/// </summary>
public class BrickShow : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Brick         brick;
    private Action<Brick> onClick;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void SetData(Brick brick)
    {
        this.brick              = brick;
        transform.localPosition = brick.Position;
    }

    public void SetSprite(Sprite sprite)         => spriteRenderer.sprite = sprite;
    public void SetOnClickAction(Action<Brick> a) => onClick = a;

    // ── Visibility ────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a short-lived visual ghost at this brick's position and plays the
    /// pop-then-shrink animation on it, then destroys the ghost.
    /// The original BrickShow is immediately set to scale zero so the gravity
    /// system can recycle it for the next incoming brick without interrupting
    /// the destruction animation.
    /// </summary>
    public void Hide(BoardAnimationConfig config)
    {
        // Create a lightweight ghost that carries the visual while this BrickShow is recycled.
        var ghost = new GameObject("BrickGhost");
        ghost.transform.SetParent(transform.parent);
        ghost.transform.localPosition = transform.localPosition;
        ghost.transform.localScale    = Vector3.one;

        var ghostRenderer             = ghost.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite          = spriteRenderer.sprite;
        ghostRenderer.sortingLayerID  = spriteRenderer.sortingLayerID;
        ghostRenderer.sortingOrder    = spriteRenderer.sortingOrder + 1; // render above all live bricks

        // InBack easing gives a punchy snap-to-zero feel without expanding
        // outside the brick's cell boundaries (important in a dense grid).
        ghost.transform.DOScale(0f, config.destroyDuration)
             .SetEase(Ease.InBack)
             .OnComplete(() => Destroy(ghost));

        // Immediately hide this BrickShow — gravity will reuse it for the incoming brick.
        transform.localScale = Vector3.zero;
    }

    /// <summary>Restores the brick to full size when it is recycled for an incoming brick.</summary>
    public void Show() => transform.localScale = Vector3.one;

    // ── Input ─────────────────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(brick);

    // ── Animation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Snaps to <paramref name="originY"/> then tweens down to <paramref name="targetY"/> (local space).
    /// Timing and easing are taken from <paramref name="config"/>.
    /// <paramref name="onComplete"/> is invoked when the tween finishes (optional).
    /// Returns <c>this</c> for optional chaining.
    /// </summary>
    public BrickShow TweenMove(float originY, float targetY, BoardAnimationConfig config, Action onComplete = null)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, originY);
        transform.DOLocalMoveY(targetY, config.dropDuration)
                 .SetEase(config.dropEase)
                 .SetDelay(config.dropDelay)
                 .OnComplete(() => onComplete?.Invoke());
        return this;
    }
}
