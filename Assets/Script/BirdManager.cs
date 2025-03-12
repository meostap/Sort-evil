using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using TMPro;

public class BirdManager : MonoBehaviour
{
    public static BirdManager Instance { get; private set; }

    [Header("Cài đặt Bird & Branch")]
    public List<GameObject> birdPrefabs;
    public List<Branch> branchObjects;
    public List<Transform> branchTransforms = new List<Transform>();

    [Header("Cài đặt Spawn")]
    public int birdsPerBranch = 3;

    private List<List<Bird>> branchBirds = new List<List<Bird>>();
    private int emptyBranchIndex;
    public SpriteRenderer spriteRenderer;
    private List<Bird> selectedBirds = new List<Bird>();
    private Bird selectedBird = null;
    private IEnumerator WaitAndCheckRemove(int branchIndex)
    {
        yield return new WaitForSeconds(0.5f); // ✅ Đợi Bird di chuyển xong rồi mới kiểm tra
        CheckAndRemoveBirds(branchIndex);
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeBranches();
        SpawnBirds();
    }

    private void InitializeBranches()
    {
        for (int i = 0; i < branchObjects.Count; i++)
        {
            branchObjects[i].branchIndex = i;
            branchTransforms.Add(branchObjects[i].transform);
            branchBirds.Add(new List<Bird>());
        }
    }

    private void SpawnBirds()
    {
        // Kiểm tra danh sách birdPrefabs và branchObjects
        if (birdPrefabs == null || birdPrefabs.Count == 0)
        {
            Debug.LogError("❌ Không có Bird Prefabs! Kiểm tra lại danh sách birdPrefabs trong Inspector.");
            return;
        }

        if (branchObjects == null || branchObjects.Count == 0)
        {
            Debug.LogError("❌ Không có Branch Objects! Kiểm tra lại danh sách branchObjects trong Inspector.");
            return;
        }

        // Số loại chim hiện có
        int birdTypeCount = birdPrefabs.Count;

        // Kiểm tra số branch có đủ không
        if (branchObjects.Count < birdTypeCount)
        {
            Debug.LogError($"❌ Không đủ branch để spawn! Cần ít nhất {birdTypeCount} branch nhưng chỉ có {branchObjects.Count}.");
            return;
        }

        // Tạo danh sách tổng hợp: 5 con cho mỗi loại chim
        List<GameObject> birdsToSpawn = new List<GameObject>();
        foreach (var birdType in birdPrefabs)
        {
            for (int i = 0; i < 5; i++)
            {
                birdsToSpawn.Add(birdType);
            }
        }

        // Xáo trộn danh sách chim
        birdsToSpawn = birdsToSpawn.OrderBy(x => Random.value).ToList();

        // Chọn ngẫu nhiên các branch để spawn
        List<int> selectedBranches = new List<int>();
        while (selectedBranches.Count < birdTypeCount)
        {
            int randomBranch = Random.Range(0, branchObjects.Count);
            if (!selectedBranches.Contains(randomBranch))
            {
                selectedBranches.Add(randomBranch);
            }
        }

        Debug.Log($"🌿 Các branch được chọn để spawn: {string.Join(", ", selectedBranches)}");

        // Spawn chim trên các branch đã chọn
        int birdIndex = 0;
        foreach (int branchIndex in selectedBranches)
        {
            Vector3[] slots = SortSlots(branchObjects[branchIndex], branchObjects[branchIndex].slotPositions);

            for (int slotIndex = 0; slotIndex < 5; slotIndex++)
            {
                GameObject birdType = birdsToSpawn[birdIndex];
                birdIndex++;

                Vector3 spawnPos = branchTransforms[branchIndex].position + slots[slotIndex];
                GameObject birdObj = Instantiate(birdType, spawnPos, Quaternion.identity);

                Bird bird = birdObj.GetComponent<Bird>();
                if (bird != null)
                {
                    bird.branchIndex = branchIndex;
                    bird.slotIndex = slotIndex;
                    bird.branchTransform = branchTransforms[branchIndex];
                }

                //SpriteRenderer birdSprite = birdObj.GetComponent<SpriteRenderer>() ?? birdObj.GetComponentInChildren<SpriteRenderer>();
                SpriteRenderer birdSprite = birdObj.GetComponent<SpriteRenderer>() ?? birdObj.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    bool isLeftBranch = branchTransforms[branchIndex].position.x < 0;
                    spriteRenderer.flipX = isLeftBranch;
                }
                else
                {
                    Debug.Log($"❌ {name} không có spriteRenderer! Kiểm tra lại Prefab.");
                }
                

                branchBirds[branchIndex].Add(bird);
                //Debug.Log($"✅ Spawn {birdType.name} trên branch {branchIndex} tại slot {slotIndex}");
            }
        }
    }


    public void OnBranchClicked(int branchIndex)
    {
        if (selectedBirds == null || selectedBirds.Count == 0)
        {
            Debug.Log("Chưa chọn chim. Hãy click vào chim trước.");
            return;
        }

        // Kiểm tra nhánh đích có đủ chỗ cho cả nhóm không
        if (branchBirds[branchIndex].Count + selectedBirds.Count > birdsPerBranch)
        {
            Debug.Log("Nhánh này không đủ chỗ cho cả nhóm chim!");
            return;
        }

        // Lưu thông tin nhánh cũ từ chim đầu tiên trong nhóm
        int oldBranchIndex = selectedBirds[0].branchIndex;
        Transform oldBranchTransform = selectedBirds[0].branchTransform;

        // Kiểm tra xem nhóm có bị chặn không (chim cuối cùng trong nhóm)
        int maxSlotIndex = selectedBirds.Max(b => b.slotIndex);
        if (branchBirds[oldBranchIndex].Any(b => b.slotIndex == maxSlotIndex + 1))
        {
            Debug.Log("Không thể di chuyển! Có chim chặn phía trước nhóm.");
            return;
        }

        // Kiểm tra điều kiện: tất cả chim trong selectedBirds phải cùng loài với chim đứng đầu nhánh đích (slotIndex lớn nhất)
        string targetSpecies = null;
        if (branchBirds[branchIndex].Count > 0)
        {
            // Lấy loài của chim có slotIndex lớn nhất
            targetSpecies = branchBirds[branchIndex].OrderByDescending(b => b.slotIndex).First().birdType;
        }

        // Kiểm tra từng chim trong selectedBirds
        bool allMatchSpecies = branchBirds[branchIndex].Count == 0; // Nếu nhánh trống, cho phép di chuyển
        if (!allMatchSpecies)
        {
            allMatchSpecies = selectedBirds.All(b => b.birdType == targetSpecies);
        }

        if (!allMatchSpecies)
        {
            Debug.Log($"Không thể di chuyển! Các chim trong nhóm không cùng loài với chim đứng đầu nhánh {branchIndex}.");
            // Thêm animation lắc nhẹ cho nhánh đích
            branchObjects[branchIndex].transform.DOShakePosition(0.2f, 0.1f, 10, 50f, false, true)
                .OnComplete(() => branchObjects[branchIndex].transform.localPosition = Vector3.zero);
            return;
        }

        // Lấy danh sách slot trên nhánh đích
        Vector3[] slots = SortSlots(branchObjects[branchIndex], branchObjects[branchIndex].slotPositions);

        // Xác định slotIndex bắt đầu cho nhóm chim
        int startSlotIndex = branchBirds[branchIndex].Count;

        // Di chuyển từng con chim trong nhóm
        for (int i = 0; i < selectedBirds.Count; i++)
        {
            Bird bird = selectedBirds[i];
            int targetSlotIndex = startSlotIndex + i;

            if (targetSlotIndex >= slots.Length)
            {
                Debug.Log("Không còn slot trống hợp lệ!");
                return;
            }

            Vector3 targetPos = branchTransforms[branchIndex].position + slots[targetSlotIndex];

            // Xóa chim khỏi nhánh cũ
            branchBirds[oldBranchIndex].Remove(bird);

            // Cập nhật thông tin và di chuyển chim
            bird.transform.DOKill();
            bird.branchTransform = branchTransforms[branchIndex];

            bird.MoveTo(targetPos, 0.5f, () =>
            {

                bird.spriteRenderer.flipX = bird.branchTransform.position.x < 0;
                // Lật sprite nếu di chuyển giữa nhánh trái/phải
                bird.slotIndex = targetSlotIndex;
                bird.branchIndex = branchIndex;
                

                // Đặt lại kích thước chim
                bird.transform.DOScale(bird.originalScale, 0.2f);
            });
        }

        // Thêm tất cả chim vào nhánh đích sau khi di chuyển
        branchBirds[branchIndex].AddRange(selectedBirds);

        // Cập nhật slotIndex cho các chim còn lại trên nhánh cũ
        for (int i = 0; i < branchBirds[oldBranchIndex].Count; i++)
        {
            branchBirds[oldBranchIndex][i].slotIndex = i;
        }

        // Xóa danh sách chọn và kiểm tra xóa chim
        selectedBirds.Clear();
        StartCoroutine(WaitAndCheckRemove(branchIndex));
    }

    public void SelectBird(Bird bird)
    {
        if (selectedBirds.Count > 0)
        {
            foreach (var b in selectedBirds)  
            {
                b.ResetSize();
            }
            selectedBirds.Clear();
        }

        // Lấy thông tin của chim được chọn
        int currentSlotIndex = bird.slotIndex;
        string species = bird.birdType;
        int branchIndex = bird.branchIndex;

        // Duyệt ngược về phía sau để tìm chim cùng loài đứng liền kề
        for (int i = currentSlotIndex; i >= 0; i--)
        {
            // Tìm chim tại slotIndex hiện tại
            Bird candidate = branchBirds[branchIndex].Find(b => b.slotIndex == i);
            if (candidate != null && candidate.birdType == species)
            {
                selectedBirds.Add(candidate);
            }
            else
            {
                break; // Dừng khi gặp chim khác loài hoặc không có chim
            }
        }
        foreach (var selected in selectedBirds)
        {
            if (selected.spriteRenderer == null)
            {
                Debug.Log($"❌ Bird {selected.name} không có SpriteRenderer! Kiểm tra lại Prefab.");
                continue;
            }
            selected.SelectAnimation();
        }
        if (selectedBird == bird)
        {
            Debug.Log("Đã chọn bird này rồi.");
            return;
        }

        if (selectedBird != null)
        {
            selectedBird.ResetSize();
        }

        selectedBird = bird;

        

        selectedBird.SelectAnimation();

        Debug.Log($"Chọn bird: {bird.name}, Slot Index: {bird.slotIndex}");
    }

    private Vector3[] SortSlots(Branch branch, Vector3[] slots)
    {
        if (branch.name.Trim().ToLower().Contains("right"))
        {
            return slots.OrderByDescending(s => s.x).ToArray();
        }
        else
        {
            return slots.OrderBy(s => s.x).ToArray();
        }
    }
    private void CheckAndRemoveBirds(int branchIndex)
    {
        Dictionary<string, List<Bird>> birdGroups = new Dictionary<string, List<Bird>>();

        // ✅ Gom nhóm bird theo loại
        foreach (Bird bird in branchBirds[branchIndex])
        {
            if (!birdGroups.ContainsKey(bird.birdType))
            {
                birdGroups[bird.birdType] = new List<Bird>();
            }
            birdGroups[bird.birdType].Add(bird);
        }

        List<Bird> birdsToRemove = new List<Bird>();

        // ✅ Chỉ xóa khi đủ 5 con cùng loại
        foreach (var pair in birdGroups)
        {
            if (pair.Value.Count == 5) // 🔥 Chỉ xóa khi chính xác 5 con!
            {
                Debug.Log($"Branch {branchIndex}: Xóa {pair.Value.Count} chim loại {pair.Key}");

                birdsToRemove.AddRange(pair.Value);
            }
        }

        if (birdsToRemove.Count > 0)
        {
            foreach (Bird bird in birdsToRemove)
            {
                branchBirds[branchIndex].Remove(bird); // ✅ Xóa khỏi danh sách trước
            }

            for (int i = 0; i < branchBirds[branchIndex].Count; i++)
            {
                branchBirds[branchIndex][i].slotIndex = i;
            }

            // ✅ Animation biến mất
            foreach (Bird bird in birdsToRemove)
            {
                bird.transform.DOScale(Vector3.zero, 0.3f).OnComplete(() =>
                {
                    Destroy(bird.gameObject); // ✅ Chỉ xóa khi đủ 5 con!
                });
            }
        }
    }


    private void MoveBirdToBranch(Bird bird, int branchIndex, int slotOffset)
    {
        int targetSlotIndex = branchBirds[branchIndex].Count + slotOffset;
        Vector3[] slots = SortSlots(branchObjects[branchIndex], branchObjects[branchIndex].slotPositions);

        if (targetSlotIndex >= slots.Length)
        {
            Debug.Log($"Không đủ chỗ trên branch {branchIndex} cho {bird.name}");
            return;
        }

        Vector3 targetPos = branchTransforms[branchIndex].position + slots[targetSlotIndex];

        bird.transform.DOKill();
        bird.transform.SetParent(branchTransforms[branchIndex]);

        bird.MoveTo(targetPos, 0.5f, () =>
        {
            bird.slotIndex = targetSlotIndex;
            bird.branchIndex = branchIndex;
            branchBirds[branchIndex].Add(bird);
            Debug.Log($"Di chuyển {bird.name} đến branch {branchIndex}, slot {targetSlotIndex}");
        });
    }


}