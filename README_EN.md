# ZeroFramework (ZF)

## 🎯 Framework Introduction

ZeroFramework (ZF for short) is a highly modular Unity game framework, with its core advantage being **detachable modules**, allowing developers to flexibly select and combine modules according to project requirements, achieving true on-demand usage.

## ✨ Core Features

- **Highly Modular**: All functions are implemented as independent modules, supporting on-demand loading and unloading
- **Three-level Module System**: Level 1 core modules, Level 2 basic modules, Level 3 extension modules, with clear hierarchy
- **Unified Module Management**: Module unified acquisition and management through `ModuleSystem`
- **Low Coupling Design**: Modules communicate through interfaces, reducing dependencies between modules
- **Easy to Extend**: Provides a clear module extension mechanism, supporting custom module development
- **High Performance**: Optimized memory management and resource loading mechanism

## 📁 Directory Structure

```
ZF/
├── Editor/              # Editor-related code
├── Runtime/             # Runtime code
│   ├── Module/          # Module implementation
│   │   ├── Primary/     # Level 1 modules (core functionality)
│   │   ├── Secondary/   # Level 2 modules (basic functionality)
│   │   └── Tertiary/    # Level 3 modules (extension functionality)
└── Setup/               # Framework settings (private use, users don't need to care)
```

## 🚀 Quick Start

### 1. Install Framework

Copy the `ZF` directory to the `Assets` directory of your Unity project, open Unity editor and wait for automatic compilation.

### 2. Initialize Framework

Initialize the framework in the game entry script (refer to `GameEntry.cs`):

```csharp
using UnityEngine;
using ZF;

public class GameEntry : MonoBehaviour
{
    private void Awake()
    {
        // Get or create module root node
        var root = ModuleRoot.Instance;
        if (root == null)
        {
            root = new GameObject("ModuleRoot").AddComponent<ModuleRoot>();
        }
        
        // Load modules on demand (select modules according to project requirements)
        
        // Level 1 modules (core functionality)
        ModuleSystem.GetModule<IArchitectureModule>();
        ModuleSystem.GetModule<IEventModule>();
        ModuleSystem.GetModule<IFsmModule>();
        ModuleSystem.GetModule<IObjectPoolModule>();
        ModuleSystem.GetModule<ITimerModule>();
        ModuleSystem.GetModule<IUpdateDriver>();
        ModuleSystem.GetModule<IWABModule>();
        
        // Level 2 modules (basic functionality)
        ModuleSystem.GetModule<IDebuggerModule>();
        ModuleSystem.GetModule<IProcedureModule>();
        ModuleSystem.GetModule<IResourceModule>();
        ModuleSystem.GetModule<ISingletonModule>();
        
        // Level 3 modules (extension functionality, some need initialization)
        ModuleSystem.GetModule<IAudioModule>().Initialize(Settings.AudioSetting);
        ModuleSystem.GetModule<IConfigModule>();
        ModuleSystem.GetModule<ILocalizationModule>().Language = root.EditorLanguage;
        ModuleSystem.GetModule<ISceneModule>();
        ModuleSystem.GetModule<IUIModule>();
        
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        // Start game procedure
        Settings.ProcedureSetting.StartProcedure().Forget();
    }
}
```

## 📚 Module Usage Examples

### 1. Get Module Instance

```csharp
// Get module instance through interface
var eventModule = ModuleSystem.GetModule<IEventModule>();
var timerModule = ModuleSystem.GetModule<ITimerModule>();
var uiModule = ModuleSystem.GetModule<IUIModule>();
```

### 2. Event System

```csharp
// Subscribe to event
eventModule.Subscribe(EventId.OnGameStart, OnGameStart);

// Publish event
eventModule.Publish(EventId.OnGameStart, gameData);

// Unsubscribe from event
eventModule.Unsubscribe(EventId.OnGameStart, OnGameStart);

private void OnGameStart(object data)
{
    // Handle game start event
}
```

### 3. Timer

```csharp
// Set delay execution
timerModule.SetTimeout(2f, () =>
{
    Debug.Log("Execute after 2 seconds");
});

// Set loop execution
timerModule.SetInterval(1f, () =>
{
    Debug.Log("Execute every second");
});
```

### 4. UI System

```csharp
// Open window
uiModule.OpenWindow<MainWindow>();

// Close window
uiModule.CloseWindow<MainWindow>();
```

## 📋 Module List

### Level 1 Modules (Core Functionality)
- `IArchitectureModule`: Architecture management
- `IEventModule`: Event system
- `IFsmModule`: State machine
- `IObjectPoolModule`: Object pool
- `ITimerModule`: Timer
- `IUpdateDriver`: Update driver
- `IWABModule`: World-Actor-Behavior system

### Level 2 Modules (Basic Functionality)
- `IDebuggerModule`: Debugger
- `IProcedureModule`: Procedure system
- `IResourceModule`: Resource management
- `ISingletonModule`: Singleton management

### Level 3 Modules (Extension Functionality)
- `IAudioModule`: Audio management
- `IConfigModule`: Configuration management
- `ILocalizationModule`: Localization
- `ISceneModule`: Scene management
- `IUIModule`: UI system

## 💡 Best Practices

1. **Load Modules On Demand**: Only load modules that are actually needed for the project to avoid unnecessary performance overhead
2. **Use Interface Communication**: Modules communicate through interfaces, do not directly depend on specific implementations
3. **Reasonable Module Division**: When developing custom functions, refer to the framework's module division principles to maintain high cohesion and low coupling
4. **Prioritize Existing Modules**: The modules provided by the framework have been optimized, prioritize using existing modules rather than reinventing the wheel

## 📄 License

MIT License - see [LICENSE.md](LICENSE.md) for details

## Version History

See [CHANGELOG.md](CHANGELOG.md) for details.

---

**ZeroFramework** - Making game development more flexible and efficient!