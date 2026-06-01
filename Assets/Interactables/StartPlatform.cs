using System.Collections;
using UnityEngine;

public class StartPlatform : Table
{
    [SerializeField] private GameObject instructionBrickPrefab;
    [SerializeField] private float spawnDelay = 1f;

    public void SpawnNextInstruction()
    {
        if (HasBrick) return;

        if (LevelManager.Instance == null) return;

        InstructionData next = LevelManager.Instance.GetNextInstruction();
        if (next == null) return;

        if (instructionBrickPrefab == null)
        {
            Debug.LogError("StartPlatform: instructionBrickPrefab not assigned");
            return;
        }

        Transform slot = GetBrickSlot();
        Vector3 spawnPos = slot != null ? slot.position : transform.position;
        GameObject brickObj = Instantiate(instructionBrickPrefab, spawnPos, Quaternion.identity, transform);
        InstructionBrick brick = brickObj.GetComponent<InstructionBrick>();

        if (brick == null)
        {
            Debug.LogError("StartPlatform: instructionBrickPrefab is missing InstructionBrick component");
            Destroy(brickObj);
            return;
        }

        PlaceBrick(brick);
    }

    protected override void OnBrickRemoved()
    {
        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnNextInstruction();
    }
}
