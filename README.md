# Bullet Heaven - Unity Developer Case Study

## 📌 Overview
This project is a 3D "Bullet Heaven / Survivor" style game developed as a Unity Developer Case Study. The player controls a soldier using a virtual joystick, surviving against progressively challenging waves of enemies for 3 minutes. The game tracks the number of enemies defeated and features a persistent save system.

## 🎯 Features

* **State Machine Pattern:** The game core is driven by a robust State Machine. Game states such as `PlayingState`, `GameOverState`, and `LevelTransitionState` are completely decoupled, ensuring scalable and clean flow control.
* **Level System:** Features 3 distinct levels with progressive difficulty via a custom ScriptableObject-based configuration (`LevelConfigSO`).
* **NavMesh / AI Coding:** Enemies utilize Unity's `NavMeshAgent`.
* **Object Pooling:** Implemented a highly optimized `PoolManager` for zero-allocation enemy spawning and recycling during massive wave stages.
* **Data Persistence:** Defeated enemy counts and unlocked level progression are persistently saved using a custom `ISaveService` interface (JSON).
* **Tweening & UI Polish:** Integrated **DOTween** for smooth UI transitions and dynamic damage feedback. 
* **Optimization:**
    * **Baked Animation Meshes (Animation Snapshotting):** Replacing heavy `Animator` and `SkinnedMeshRenderer` components by baking animation frames into static `Mesh` snapshots (inspired by LlamAcademy's approach). This eliminates the massive CPU skinning overhead and allows rendering massive crowds.

## 🛠️ Third-Party Assets & Libraries
* **DOTween:** Used for UI animations and in-game visual feedback.
* **TextMeshPro**
* **Joystick Pack:** Used for mobile input integration.

## 🚀 How to Play
1.  **Movement:** Use the on-screen virtual joystick to navigate the arena.
2.  **Combat:** The hero automatically attacks incoming enemies.
3.  **Objective:** Survive the 3-minute countdown while defeating as many enemies as possible.
4.  **Progression:** Upon survival, a "Game Won" screen displays your total kills, and you can proceed to the next unlocked tier.
