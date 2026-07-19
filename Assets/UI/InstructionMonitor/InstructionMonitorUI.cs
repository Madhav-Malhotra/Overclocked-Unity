using UnityEngine;
using UnityEngine.UIElements;

public class InstructionMonitorUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private InstructionBrickHoldingSystem holdingSystem;
    [SerializeField] private CPUController cpuController;

    private Label pcValue;
    private Label instValue;
    private Label rdValue;
    private Label addrValue;

    private const string Blank = "--";

    void Awake()
    {
        var root = uiDocument.rootVisualElement;
        pcValue = root.Q<Label>("pc-value");
        instValue = root.Q<Label>("inst-value");
        rdValue = root.Q<Label>("rd-value");
        addrValue = root.Q<Label>("addr-value");
    }

    void Update()
    {
        InstructionBrick brick = holdingSystem != null ? holdingSystem.GetHeldBrick() : null;

        if (brick == null)
        {
            Clear();
            return;
        }

        Refresh(brick);
    }

    private void Clear()
    {
        SetText(pcValue, Blank);
        SetText(instValue, Blank);
        SetText(rdValue, Blank);
        SetText(addrValue, Blank);
    }

    private void Refresh(InstructionBrick brick)
    {
        SetText(pcValue, $"0x{brick.InstructionPc:X8}");

        if (brick.HasBeenDecoded)
        {
            SetText(instValue, brick.InstructionLabel);
        }
        else
        {
            SetText(instValue, Blank);
        }

        if (brick.HasDestRegAddr)
        {
            uint regValue = cpuController != null ? cpuController.GetStateB().regs[brick.DestRegAddr] : 0;
            SetText(rdValue, $"x{brick.DestRegAddr} = {regValue}");
        }
        else
        {
            SetText(rdValue, Blank);
        }

        if (brick.HasCapturedMemAddr)
        {
            SetText(addrValue, $"0x{brick.CapturedMemAddr:X8}");
        }
        else
        {
            SetText(addrValue, Blank);
        }
    }

    private static void SetText(Label label, string text)
    {
        if (label != null)
        {
            label.text = text;
        }
    }
}
