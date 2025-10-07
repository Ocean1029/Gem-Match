# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview
This is a Unity Match-3 gem puzzle game built with Unity 6000.2.2f1. The game features gem matching mechanics, bonus gems, various obstacles, level progression, and WebGL deployment to itch.io.

## Common Development Commands

### Unity Operations
```bash
# Open project in Unity (from project directory)
# Use Unity Hub or direct Unity editor launch

# Build for WebGL (automated via GitHub Actions)
# Builds are triggered on push to main branch

# Run tests (via Unity Test Runner or GitHub Actions)
# Tests run automatically on pull requests
```

### CI/CD Pipeline
```bash
# Build and deploy to itch.io
git push origin main  # Triggers full CI/CD pipeline

# Manual artifact download
# WebGL builds are available as GitHub Actions artifacts
```

## Game Architecture

### Core Systems Architecture

**GameManager (Singleton)**
- Central coordinator for all game systems
- Manages audio, VFX pooling, bonus items, coins/lives/stars
- Handles level transitions and scene management
- Provides singleton instance accessible throughout the game

**Board System**
- `Board.cs`: Main game board controller handling gem physics, matching, input
- `BoardCell.cs`: Individual cell state management (gems, obstacles, locks)
- Grid-based coordinate system using `Vector3Int` for cell positions
- Manages gem falling physics with diagonal movement support

**Match System**
- `Match.cs`: Represents a collection of matched gems with deletion timing
- Complex matching algorithm supporting lines (3+) and special bonus shapes
- Supports both player-initiated matches and bonus-triggered forced matches
- Handles match validation and bonus gem spawning

**Gem Hierarchy**
```
Gem (base class)
├── BonusGem (abstract)
│   ├── LineRocket (clears entire row/column)
│   ├── SmallBomb (3x3 area)
│   ├── LargeBomb (5x5 area)
│   └── ColorClean (removes all gems of same color)
├── Obstacle (destructible barriers)
└── Regular gems (by type/color)
```

### State Management

**Game States**
- Managed through `m_BoardWasInit`, `m_InputEnabled`, `m_FinalStretch` flags
- Complex state machine for gem movement (Still, Falling, Bouncing, Disappearing)
- Swap validation with rollback mechanism for invalid moves

**Level Management**
- `LevelData.cs`: Defines goals, move limits, visual settings per level
- Goal-based progression (collect specific gem types)
- Dynamic camera adjustment based on board bounds and screen ratio

### Input System
- Unity's new Input System for cross-platform compatibility
- Supports both swipe and double-tap interactions
- Touch/mouse position conversion to grid coordinates
- Debug mode for development (F12 key toggle)

### Visual Effects & Audio
- `VFXPoolSystem.cs`: Object pooling for performance optimization
- Separate audio channels for music and SFX with crossfading
- Visual feedback for matches, hints, and special effects
- Responsive camera system for different screen orientations

### Data Architecture
- ScriptableObject-based configuration (`GameSettings.cs`)
- Level data stored as prefabs with embedded `LevelData` components
- Settings separated into Visual, Sound, and Bonus categories
- JSON-based save system for audio preferences

## Key Development Patterns

**Singleton Pattern**
- `GameManager` and `Board` use singleton pattern for global access
- Proper cleanup and shutdown handling to prevent memory leaks

**Observer Pattern**
- Event-driven communication between systems (OnGoalChanged, OnMoveHappened, etc.)
- Callback system for cell-specific events (deletion, matching)

**State Machine Pattern**
- Gem states and board swap stages managed through enums
- Clear state transitions with validation

**Object Pooling**
- VFX instances pooled for performance
- Automatic expansion when pool is exhausted

## Development Notes

### Unity-Specific Considerations
- Uses Universal Render Pipeline (URP) for 2D rendering
- Tilemap system for level authoring
- Custom tiles for gem placement and obstacle setup
- Execution order attributes for initialization timing

### Editor Tools
- Custom inspectors for bonus gem configuration
- Debug tools for gem placement and testing (development builds only)
- Automated tile refresh for builds to ensure proper sprite display

### Performance Optimizations
- Object pooling for frequently created/destroyed objects
- Efficient gem movement sorting (bottom-left to top-right)
- Minimal garbage generation in update loops
- Conditional compilation for debug features

### Platform Targets
- Primary target: WebGL for browser deployment
- Mobile-friendly input system and responsive camera
- GitHub Actions integration for automated builds and itch.io deployment

## Testing & Debugging
- Unity Test Framework integration
- F12 debug menu in development builds for gem placement
- Visual debugging tools for match validation
- Automated build verification through CI pipeline