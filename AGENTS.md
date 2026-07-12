# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 6 (`6000.3.19f1`) project using the Universal Render Pipeline and the Input System. Runtime assets belong under `Assets/`; the current playable scene is `Assets/Scenes/SampleScene.unity`, rendering profiles are in `Assets/Settings/`, and input mappings live in `Assets/InputSystem_Actions.inputactions`. Put game scripts in feature-oriented folders under `Assets/Scripts/` and editor-only code in an `Editor/` subfolder. Keep tests in `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`. Package dependencies are declared in `Packages/manifest.json`; project-wide Unity settings are stored in `ProjectSettings/`.

Do not commit generated directories such as `Library/`, `Temp/`, `Logs/`, or local `UserSettings/`. Preserve Unity-generated `.meta` files when moving or adding assets.

## Build, Test, and Development Commands

Open the project through Unity Hub with Editor `6000.3.19f1`, or from PowerShell:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -projectPath .
```

Use **File > Build Profiles** for local player builds; no scripted build pipeline exists yet. Run automated tests headlessly with:

```powershell
& $Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -quit
& $Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -quit
```

Set `$Unity` to the editor executable above. Also open `SampleScene` and enter Play Mode before submitting gameplay changes.

## Windows Development Rules

- Do not run long-lived development servers such as `pnpm dev`.
- The user starts the development server manually.
- You may run `pnpm lint`, `pnpm typecheck`, `pnpm test`, and one-off builds.
- Do not modify ACLs, ownership, or Windows security permissions.
- Do not run commands as administrator.
- Do not use `takeown` or `icacls` unless explicitly requested.

## Coding Style & Naming Conventions

Use C# with four-space indentation and braces on separate lines. Name types, methods, properties, scenes, prefabs, and ScriptableObject assets in `PascalCase`; use `camelCase` for locals and private fields. Prefer `[SerializeField] private` fields over public mutable state. Keep each primary type in a same-named `.cs` file and separate runtime logic from `UnityEditor` dependencies. No formatter or linter is configured, so match nearby code and remove unused imports.

### Runtime UI Event Wiring

Editor setup methods such as `Configure()` must only assign serialized references. Do not rely on `UnityEvent.AddListener` calls made while an Editor tool is generating a scene: runtime listeners are not persisted in scene assets. Register uGUI callbacks in `Awake` or `OnEnable`, remove the same callback in `OnDestroy` or `OnDisable`, and use `RemoveListener` rather than clearing unrelated listeners with `RemoveAllListeners`.

## Testing Guidelines

The Unity Test Framework `1.6.0` is installed. Core Edit Mode tests live in `Assets/_Project/Tests/EditMode/` under an `.asmdef`; name new fixtures `FeatureNameTests.cs`. Prefer Edit Mode tests for deterministic domain logic and Play Mode tests for scene, lifecycle, input, or physics behavior. New features and bug fixes should include focused regression coverage where practical.

## Commit & Pull Request Guidelines

The repository uses `main` as its default branch and has no commit history yet. Until a project convention is established, use short imperative subjects, optionally scoped, such as `feat(input): add camera pan controls` or `fix: prevent duplicate residents`. Pull requests should explain player-visible behavior, list validation performed, link relevant issues, and include screenshots or short clips for visual changes. Call out modified scenes, prefabs, settings, or package dependencies to reduce merge risk.
