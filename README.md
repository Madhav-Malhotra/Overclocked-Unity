# Overclocked - Unity Development Setup

Welcome to the development repository for **Overclocked**. This guide will help you set up your local environment, configure Git correctly for Unity, and get the project running without missing assets or metadata.

---

## 🛠 Prerequisites

Before cloning, ensure you have the following installed:

1.  **Unity Hub:** [Download here](https://unity.com/download)
2.  **Unity Editor Version:** `6000.3.6f1`
    * *Note: Install this specific version via Unity Hub to avoid version mismatch errors.*
3.  **Git:** [Download here](https://git-scm.com/downloads)
4.  **Git LFS (Large File Storage):** Required for pulling textures, models, and audio.
    * *Windows:* Included with Git for Windows.
    * *Mac/Linux:* Run `brew install git-lfs` or `sudo apt-get install git-lfs`.

---

## 🚀 Installation & First-Time Setup

### 1. Initialize Git LFS
Before cloning, ensure LFS is initialized globally on your machine:

```bash
git lfs install
```

---


### 2. Pull LFS Assets

Explicitly pull the large binary files (textures, sounds, models) to ensure you don’t just get pointer files:
```
git lfs pull
```
---

## ⚙️ Critical Configuration: Smart Merge

Do **not** skip this step.

Unity scenes (`.unity`) and prefabs (`.prefab`) are YAML files that often cause merge conflicts. We use Unity’s built-in YAML Merge (Smart Merge) tool to resolve these automatically.

Run the following commands **inside the project folder**.

---

### For Windows Users

```
git config merge.unityyamlmerge.name "Unity SmartMerge (YAML)"
git config merge.unityyamlmerge.driver "'C:\Program Files\Unity\Hub\Editor6000.3.6f1\Editor\Data\Tools\UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

---

### For macOS Users


```
git config merge.unityyamlmerge.name "Unity SmartMerge (YAML)"
git config merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/6000.3.6f1/Unity.app/Contents/Tools/UnityYAMLMerge' merge -p %O %B %A %A"
```

---

## 🎮 Opening the Project

1. Open Unity Hub
2. Click **Add** (or **Open**) and select the folder you just cloned
3. Click the project name to launch it

Note: The first launch will take some time as Unity builds the `/Library` folder and imports assets. This is normal.

---

## ⚠️ Troubleshooting & Best Practices

### Pink Materials / Missing Models

This usually means Git LFS failed to pull the actual files.

Fix:
`git lfs pull`

Then re-open the project in Unity.

---

### "Meta file missing" Warnings

Never delete, move, or rename an asset outside of the Unity Editor.

Always rename or move files inside Unity’s **Project** window so the `.meta` file moves with it.

---

### Merge Conflicts in Scenes

If the Smart Merge tool does not auto-resolve a conflict, it is safer to coordinate with the team member working on that scene rather than forcing a manual merge in a text editor.

---

## 🤝 Contribution Workflow

git pull origin main
git checkout -b feature/my-new-feature
git commit -m "Added jump mechanic"
git push origin feature/my-new-feature


---

## 🙏 Credits & Third-Party Assets

The UI Toolkit design system used throughout the project's screens (`Assets/DesignSystem/`) is vendored from [sinanata/unity-ui-toolkit-design-system](https://github.com/sinanata/unity-ui-toolkit-design-system).