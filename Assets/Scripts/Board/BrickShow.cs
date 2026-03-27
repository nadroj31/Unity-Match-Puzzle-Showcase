using DG.Tweening;
using System;
using UnityEngine;

/// <summary>
/// View / presentation layer for a single Brick.
/// Handles sprite display, click forwarding, and drop animations.
/// External code uses Hide() / Show() — never touches transform.localScale directly.
/// </summary>
public class BrickShow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Brick         brick;
    private Action<Brick> onClick;

    // -------------------------------------------------------------------------
    // Setup
    // -------------------------------------------------------------------------

    public void SetData(Brick brick)
    {
        this.brick              = brick;
        transform.localPosition = brick.Position;
    }

    public void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }

    public void SetOnClickAction(Action<Brick> action)
    {
        onClick = action;
    }

    // -------------------------------------------------------------------------
    // Visibility — callers use semantic methods, not raw transform access
    // -------------------------------------------------------------------------

    public void Hide() => transform.localScale = Vector3.zero;
    public void Show() => transform.localScale = Vector3.one;

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    private void OnMouseUp()
    {
        onClick?.Invoke(brick);
    }

    // -------------------------------------------------------------------------
    // Animation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Teleports the view to <paramref name="originY"/> then tweens it to
    /// <paramref name="targetY"/>. Both values are in local space.
    /// Returns self for optional chaining.
    /// </summary>
    public BrickShow TweenMove(float originY, float targetY)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, originY);
        // DOLocalMoveY keeps the tween in local space, matching SetData / SetPosition
        transform.DOLocalMoveY(targetY, 0.3f).SetEase(Ease.InOutQuad).SetDelay(0.01f);
        return this;
    }
}
