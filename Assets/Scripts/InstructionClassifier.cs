public static class InstructionClassifier
{
    private static readonly string[] MemoryMnemonics = { "lw", "lb", "lh", "lbu", "lhu", "sw", "sb", "sh" };

    public static bool IsMemoryOp(string label)
    {
        if (string.IsNullOrEmpty(label)) return false;

        string trimmed = label.TrimStart().ToLowerInvariant();
        foreach (string mnemonic in MemoryMnemonics)
        {
            if (trimmed.StartsWith(mnemonic + " ") || trimmed == mnemonic)
                return true;
        }

        return false;
    }
}
