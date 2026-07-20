public static class InstructionClassifier
{
    private static readonly string[] MemoryMnemonics = { "lw", "lb", "lh", "lbu", "lhu", "sw", "sb", "sh" };
    private static readonly string[] StoreMnemonics = { "sw", "sb", "sh" };

    public static bool IsMemoryOp(string label)
    {
        return StartsWithMnemonic(label, MemoryMnemonics);
    }

    public static bool IsStoreOp(string label)
    {
        return StartsWithMnemonic(label, StoreMnemonics);
    }

    private static bool StartsWithMnemonic(string label, string[] mnemonics)
    {
        if (string.IsNullOrEmpty(label)) return false;

        string trimmed = label.TrimStart().ToLowerInvariant();
        foreach (string mnemonic in mnemonics)
        {
            if (trimmed.StartsWith(mnemonic + " ") || trimmed == mnemonic)
                return true;
        }

        return false;
    }
}
