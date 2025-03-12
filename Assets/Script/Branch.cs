using UnityEngine;

public class Branch : MonoBehaviour
{
    public int branchIndex; // Số thứ tự của Branch trong danh sách
    public Vector3[] slotPositions; // Vị trí các slot trên branch

    private void OnMouseDown()
    {
        if (BirdManager.Instance != null)
        {
            BirdManager.Instance.OnBranchClicked(branchIndex);
        }
        else
        {
            Debug.LogError("❌ BirdManager.Instance bị null! Kiểm tra lại scene và chắc chắn rằng BirdManager đã được gán đúng.");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        if (slotPositions != null)
        {
            foreach (Vector3 slot in slotPositions)
            {
                Gizmos.DrawSphere(transform.position + slot, 0.1f);
            }
        }
    }
}
