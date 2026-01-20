# ZeroFramework

A modular, lightweight, and extensible Unity framework for game development.

## Features

- **Modular Architecture**: Core functionality is organized into independent modules for easy maintenance and expansion
- **Runtime Framework**: Core systems for logging, memory management, event handling, and more
- **Editor Extensions**: Tools to enhance Unity Editor workflow and productivity
- **Third-Party Integration**: Built-in support for popular libraries like DOTween
- **UI System**: Comprehensive UI framework with window management, layers, and more
- **Resource Management**: Advanced resource loading and management system
- **Localization**: Multi-language support with editor tools
- **Audio System**: Flexible audio management
- **Scene Management**: Simplified scene loading and management
- **Procedure System**: State machine for game flow control
- **Debugging Tools**: Built-in debugger with various information panels

## Architecture

### Core Modules

- **Architecture**: Framework core and module management
- **Event**: Event-driven communication system
- **FSM**: Finite State Machine implementation
- **ObjectPool**: Efficient object pooling system
- **Timer**: High-performance timer system
- **UpdateDriver**: Custom update loop system
- **WAB**: World-Actor-Behavior architecture

### Secondary Modules

- **Debugger**: Runtime debugging tools
- **Procedure**: Game flow management
- **Resource**: Resource loading and management
- **Singleton**: Singleton pattern implementation

### Tertiary Modules

- **Audio**: Audio management
- **Config**: Configuration system
- **GameObjectPool**: Unity GameObject pooling
- **Localization**: Multi-language support
- **Scene**: Scene management
- **UI**: User interface system

## Getting Started

### Installation

1. Clone or download this repository
2. Copy the `ZF` folder to your Unity project's `Assets` directory
3. Unity will automatically import the package

### Basic Usage

```csharp
// Initialize the framework
Architecture.Instance.Initialize();

// Register modules
Architecture.Instance.RegisterModule<EventModule>();
Architecture.Instance.RegisterModule<TimerModule>();

// Use modules
Architecture.Instance.GetModule<IEventModule>().Subscribe<int>(EventId.TestEvent, OnTestEvent);
```

## Directory Structure

```
ZF/
├── Editor/            # Editor extensions and tools
├── Runtime/           # Runtime framework code
├── Setup/             # Framework setup and configuration
└── ThirdParty/        # Third-party libraries
```

## Editor Tools

- **Atlas Generator**: Create and manage sprite atlases
- **Toolbar Extender**: Extend Unity's main toolbar with custom buttons
- **UI Builder**: Generate UI scripts from templates
- **Localization Editor**: Manage multi-language support
- **Builder**: Build pipeline tools

## Dependencies

- Unity 2021.3 or later
- DOTween (included in ThirdParty folder)

## License

MIT License - see [LICENSE.md](LICENSE.md) for details

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## Version History

See [CHANGELOG.md](CHANGELOG.md) for details.
