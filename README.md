# ShaderLabBuildRepo

一个用于构建 ShaderLab 渲染脚本的自动化 CI 仓库。
所有操作均可在 GitHub 网页端完成，无需本地安装 Unity 完整组件。

An automated CI repository for building ShaderLab rendering scripts.
All operations are performed on the GitHub web interface — no local Unity installation required.

---

## Repository layout

```
.github/workflows/
  activation.yml       # Single-use: generate a Unity manual-activation (.alf) file
  build.yml            # CI: build & export all ShaderLab fragments as a Unity package

Assets/
  Editor/
    ShaderBuildScript.cs  # Unity editor script — exports shaders to build/ShaderLib.unitypackage
  Shaders/
    ExampleUnlit.shader             # Example ShaderLab fragment (unlit pass)
    CGInclude/
      ExampleInclude.cginc          # Shared HLSL/CG utility helpers

Packages/
  manifest.json        # Minimal Unity package dependencies

ProjectSettings/
  ProjectVersion.txt   # Unity 2022.3 LTS
  ProjectSettings.asset
```

---

## First-time setup — Unity license activation

The build workflow requires a valid Unity license stored as repository secrets.
Follow these steps **once** to activate the license:

### Step 1 — Generate the activation request file

1. Go to **Actions** → **"Acquire activation file"** → **"Run workflow"**.
2. Wait for the workflow to finish.
3. Download the **Manual-Activation-File** artifact (a `.alf` file).

### Step 2 — Activate the license on Unity's website

1. Open <https://license.unity3d.com/manual>.
2. Sign in with your Unity account and upload the `.alf` file.
3. Download the resulting `.ulf` license file.

### Step 3 — Store secrets in GitHub

Go to **Settings → Secrets and variables → Actions** and add:

| Secret name       | Value                                    |
|-------------------|------------------------------------------|
| `UNITY_LICENSE`   | Full contents of the `.ulf` file         |
| `UNITY_EMAIL`     | Email address of your Unity account      |
| `UNITY_PASSWORD`  | Password of your Unity account           |

---

## Building the shaders

After the license secrets are configured:

- **Automatic build**: every push to `main` triggers the **"Build ShaderLab"** workflow.
- **Manual build**: go to **Actions → "Build ShaderLab" → "Run workflow"**.

The workflow exports all `.shader` files and HLSL/CG includes from `Assets/Shaders/`
into a distributable **`ShaderLib.unitypackage`** file, which is uploaded as the
**ShaderLib-Package** artifact.

---

## Adding new shaders

1. Create a `.shader` file (or `.cginc` / `.hlsl` include) anywhere under `Assets/Shaders/`.
2. Unity requires a `.meta` file alongside each asset for proper tracking.
   Generate one by copying an existing `.meta` file and replacing the `guid` value
   with a fresh [UUID v4](https://www.uuidgenerator.net/) (remove the hyphens).
3. Commit and push — the CI workflow will pick up the new shader automatically.

---

## Unity version

This project targets **Unity 2022.3 LTS** (`2022.3.20f1`).
To change the version, update `ProjectSettings/ProjectVersion.txt`.

---

## License

MIT
