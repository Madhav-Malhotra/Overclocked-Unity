using System.Collections;
using System.Globalization;
using UnityEngine;

public class StartPlatform : Table
{
    [SerializeField] private GameObject instructionBrickPrefab;
    [SerializeField] private float spawnDelay = 1f;

    private int _spawnIndex;
    private const uint ImemBaseAddr = 0x01000000u;

    private uint _prevPc = ImemBaseAddr;

    public uint NextSpawnPc => ImemBaseAddr + (uint)(_spawnIndex * 4);

    public void SpawnNextInstruction()
    {
        if (HasBrick) return;

        if (LevelManager.Instance == null) return;

        InstructionData next = LevelManager.Instance.GetNextInstruction();
        if (next == null)
        {
            _prevPc = ImemBaseAddr + (uint)((_spawnIndex) * 4); // update the last PC to the last instruction, since _spawnIndex won't be incremented
            return;
        }

        if (instructionBrickPrefab == null)
        {
            Debug.LogError("StartPlatform: instructionBrickPrefab not assigned");
            return;
        }

        Transform slot = GetBrickSlot();
        Transform spawnParent = slot != null ? slot : transform;
        GameObject brickObj = Instantiate(instructionBrickPrefab, spawnParent.position, Quaternion.identity, spawnParent);
        InstructionBrick brick = brickObj.GetComponent<InstructionBrick>();

        if (brick == null)
        {
            Debug.LogError("StartPlatform: instructionBrickPrefab is missing InstructionBrick component");
            Destroy(brickObj);
            return;
        }

        brick.SetInstructionPc(ImemBaseAddr + (uint)(_spawnIndex * 4));
        _prevPc = ImemBaseAddr + (uint)((_spawnIndex - 1) * 4);

        if (!string.IsNullOrEmpty(next.hex))
        {
            string cleanHex = next.hex.StartsWith("0x") ? next.hex.Substring(2) : next.hex;
            if (uint.TryParse(cleanHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hexValue))
            {
                brick.SetInstructionHex(hexValue);
            }
            else
            {
                Debug.LogError($"StartPlatform: could not parse hex '{next.hex:X}' for instruction '{next.label}'");
            }
        }

        brick.SetInstructionLabel(next.label);

        _spawnIndex++;
        PlaceBrick(brick);
    }

    public override void PlaceBrick(InstructionBrick brick)
    {
        if (!base.HasBrick) // the platform doesn't already have a brick, this must be called from SpawnNewInstruction
        {
            base.PlaceBrick(brick);
            return;
        }
        else {
            // otherwise, a previous brick is being returned to the StartPlatform
            // since spawnNewInstruction retrieves level data from the LevelManager queue, we need to push the instruction back onto the queue
            if (LevelManager.Instance == null) return;
            InstructionData toReturn = new InstructionData{
                label = base.CurrentBrick.InstructionLabel,
                hex = $"0x{base.CurrentBrick.InstructionHex.ToString("X")}"
            };
            LevelManager.Instance.RequeueInstruction(toReturn);
            base.DeleteBrick(); // remove the current brick and replace with the held brick
            _spawnIndex--;
            base.PlaceBrick(brick);
        }
    }

    public override bool CanInteract()
    {
        // check the base conditions first, but also allow a player to return their current brick to StartPlatform if the next brick has already spawned
        return base.CanInteract() || (
            HoldingSystem.IsHoldingBrick() && _prevPc == HoldingSystem.GetHeldBrick().InstructionPc
        );
    }

    public override void OnInteract()
    {
        // we override the OnInteract because we want to check if the player is trying to place a brick back on the platform first
        if (HoldingSystem == null) return;

        if (HoldingSystem.IsHoldingBrick())
        {
            if (!TryPlaceHeldBrick())
            {
                Debug.LogWarning("Table: Failed to place brick");
            }
            return;
        }
        if (HasBrick)
        {
            InstructionBrick brickToPickup = RemoveBrick();
            HoldingSystem.PickUpBrick(brickToPickup);
            return;
        }
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
