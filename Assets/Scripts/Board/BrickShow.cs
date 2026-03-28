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

    public void Hide() => transform.localScale = Vector3.zero;
    public void Show() => transform.localScale = Vector3.one;

    // ── Input ─────────────────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(brick);

    // ── Animation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Snaps to <paramref name="originY"/> then tweens down to <paramref name="targetY"/> (local space).
    /// Returns <c>this</c> for optional chaining.
    /// </summary>
    public BrickShow TweenMove(float originY, float targetY)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, originY);
        transform.DOLocalMoveY(targetY, 0.3f).SetEase(Ease.InOutQuad).SetDelay(0.01f);
        return this;
    }
}
