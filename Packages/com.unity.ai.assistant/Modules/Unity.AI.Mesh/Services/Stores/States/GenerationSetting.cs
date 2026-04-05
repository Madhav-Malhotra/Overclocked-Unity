using System;
using Unity.AI.Toolkit.Asset;
using Unity.AI.Toolkit.Utility;

namespace Unity.AI.Mesh.Services.Stores.States
{
    [Serializable]
    record GenerationSetting
    {
        public SerializableDictionary<RefinementMode, ModelSelection> selectedModels = new();
        public string prompt = "";
        public string negativePrompt = "";
        public int variationCount = 1;
        public bool useCustomSeed;
        public int customSeed;
        public RefinementMode refinementMode;

        public PromptImageReference promptImageReference = new();

        public float historyDrawerHeight = 200;
        public float generationPaneWidth = 280;

        public MeshSettingsState meshSettings = new();
    }

    [Serializable]
    record PromptImageReference
    {
        public AssetReference asset = new();
    }

    enum RefinementMode : int
    {
        Generation = 0,
    }

    enum MeshPivotMode : int
    {
        Center = 0,
        BottomCenter = 1,
    }

    [Serializable]
    record ModelSelection
    {
        public string modelID = "";
    }
}