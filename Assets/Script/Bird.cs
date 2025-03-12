using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public class Bird : MonoBehaviour
{
    public string birdType; // Loại chim
    public int branchIndex; // Branch hiện tại của bird
    public int slotIndex;   // Slot trong branch
    public Transform branchTransform;
    public SpriteRenderer spriteRenderer;
    public Vector3 originalScale; // ✅ Lưu scale gốc của Bird
    private List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();


    private void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>().ToList();

        if (spriteRenderers.Count == 0)
        {
            Debug.LogError($"❌ Bird {name} không có SpriteRenderer! Kiểm tra lại Prefab.");
        }

        // ✅ Đảm bảo originalScale không phải (0,0,0)
        originalScale = transform.localScale;
        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
            transform.localScale = originalScale;
        }
    }
    public void FlipBird(bool isFacingLeft)
    {
        foreach (var sr in spriteRenderers)
        {
            sr.flipX = isFacingLeft;
        }
    }


    private void OnMouseDown()
    {
        if (BirdManager.Instance != null)
        {
            BirdManager.Instance.SelectBird(this);
        }
        else
        {
            Debug.LogError("BirdManager.Instance bị null! Kiểm tra xem BirdManager có tồn tại không.");
        }
    }

    public void SelectAnimation()
    {
        transform.DOKill();
        transform.DOScale(originalScale * 1.3f, 0.3f).SetEase(Ease.OutBack);
        transform.DOShakeRotation(0.4f, 5, 10);
    }

    public void ResetSize()
    {
        transform.DOKill();
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.InOutSine);
    }

    public void MoveTo(Vector3 targetPosition, float duration, System.Action onComplete = null)
    {
        transform.DOKill();

        bool isCurrentLeftBranch = branchTransform.position.x < 0;
        bool isMovingToLefttBranch = targetPosition.x < 0;

        if (isCurrentLeftBranch != isMovingToLefttBranch)
        {
            spriteRenderer.flipX = isMovingToLefttBranch;
        }

        transform.DOScale(originalScale, 0.2f);
        transform.DOMove(targetPosition, duration)
                 .SetEase(Ease.InOutSine)
                 .OnComplete(() =>
                 {
                     onComplete?.Invoke();
                 });
    }
}
