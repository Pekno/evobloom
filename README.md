# EvoBloom - An AI Life Simulation

EvoBloom is a 2D simulation game developed in C# with the Godot Engine. It features autonomous creatures called "Bloomies" that inhabit a procedurally generated world. Players can observe Bloomies as they explore, satisfy their physiological needs (hunger, thirst), interact with the environment, and socialize with other Bloomies. The game aims to simulate emergent behaviors arising from a set of defined rules and interactions.

## Table of Contents

1.  [Game Overview](#game-overview)
    *   [The Bloomies](#the-bloomies)
    *   [Genetic Traits (DNA)](#genetic-traits-dna)
    *   [Aging & Life Stages](#aging--life-stages)
    *   [Needs & Feelings](#needs--feelings)
    *   [Perception & Memory](#perception--memory)
    *   [Interaction](#interaction)
    *   [Procedural World](#procedural-world)
    *   [Player's Role](#players-role)
2.  [Core Architecture](#core-architecture)
    *   [The Bloomy Entity (`Bloomy.cs`)](#the-bloomy-entity-bloomycs)
    *   [Component-Based Design (`IBloomyComponent`)](#component-based-design-ibloomycomponent)
    *   [Key Bloomy Components (Bodyparts)](#key-bloomy-components-bodyparts)
        *   [Genetics, Physiology & Growth](#genetics-physiology--growth)
        *   [Sensation & Cognition](#sensation--cognition)
        *   [Action & Behavior](#action--behavior)
        *   [Social Interaction](#social-interaction)
        *   [Visual Feedback & Animation](#visual-feedback--animation)
    *   [Behaviour States (`IBehaviourState`, `IConditionalBehaviourState`)](#behaviour-states-ibehaviourstate-iconditionalbehaviourstate)
    *   [Environment & Interactables (`CanBeSeenNode2D`)](#environment--interactables-canbeseennode2d)
    *   [World Generation & Spawning](#world-generation--spawning)
    *   [Player Interaction System](#player-interaction-system)
3.  [How It All Works Together (A Day in the Life of a Bloomy)](#how-it-all-works-together-a-day-in-the-life-of-a-bloomy)
4.  [Key Data Structures & Enums](#key-data-structures--enums)
5.  [Project Structure (Overview)](#project-structure-overview)
6.  [Development Setup](#development-setup)
7.  [Potential Future Development](#potential-future-development)

---

## 1. Game Overview

### The Bloomies
Bloomies are the central actors in EvoBloom. Each Bloomy is an individual entity with a unique procedurally generated name (e.g., "Flufflet," "Sparko"). They possess a set of genetic traits (DNA) that influence their base attributes. They navigate the world, driven by their internal state (needs, feelings) and perceptions, and progress through various life stages as they age, with their capabilities evolving accordingly.

### Genetic Traits (DNA)
Each Bloomy is born with a unique set of genetic traits, managed by a `DNAComponent`. These traits are defined as multipliers (e.g., 1.0 is average) and are set at birth (currently randomized, planned for inheritance via reproduction). They influence fundamental aspects like:
*   **Metabolism:** How quickly hunger and thirst increase.
*   **Cognition:** The base rate at which memories decay.
*   **Physicality:** Base movement speed and sensory range.
*   **Development:** The rate of maturation through life stages.
These genetic predispositions interact with environmental factors and life stage modifiers to shape a Bloomy's overall characteristics and behavior.

### Aging & Life Stages
Bloomies are born as Babies and age over time (at a rate influenced by their DNA), progressing through several life stages: Baby, Youngling, Adolescent, Adult, and Old. Each stage applies modifiers to the Bloomy's DNA-influenced base attributes:
*   **Memory Decay:** Younger Bloomies might have better memory retention (lower decay factor), while older Bloomies might forget things more quickly (higher decay factor).
*   **Movement Speed:** Speed can vary with age, with young and old Bloomies potentially being slower than adults.
*   **Visual Size:** Bloomies visually grow from Baby to Adult size.
*   **Reproductive Capability:** Only Bloomies in certain life stages (e.g., Adult) are capable of reproduction.

### Needs & Feelings
Bloomies are driven by fundamental physiological needs, primarily:
*   **Hunger:** This need gradually increases over time, at a rate influenced by their genetic `HungerRateMultiplier`. Bloomies satisfy hunger by consuming fruit.
*   **Thirst:** Similar to hunger, thirst also rises over time, influenced by their genetic `ThirstRateMultiplier`, and is quenched by drinking from water sources.

These needs, along with other factors like environmental perceptions, contribute to a Bloomy's current "Feeling." Their brain evaluates various feelings (e.g., Hunger, Thirst, Boredom, Fear), assigning a weight or intensity to each. The strongest feeling typically becomes dominant and heavily influences their subsequent actions and decisions.

### Perception & Memory
Bloomies perceive their surroundings and build a model of the world through a dynamic memory system:
*   **Sight:** Using a sensory area, whose base range is influenced by their genetic `SensoryRangeMultiplier`, Bloomies "see" objects and entities. All current sightings are relayed to their brain.
*   **Memory System:**
    *   **Short-Term Memory:** Holds unprocessed, recently detected observations.
    *   **Long-Term Memory:** Objects deemed significant are processed and committed to long-term memory.
        *   **Memory Strength & Decay:** Each long-term memory has an `InitialStrength`. This strength decays over time. The base decay rate is influenced by the Bloomy's genetic `MemoryDecayRateMultiplier` and further modified by their current life stage (age). Memories are automatically pruned when their strength reaches zero.
        *   **Reinforcement:** Re-sighting an object reinforces its memory, resetting its decay timer and boosting its strength.
        *   **Forgetting Specifics (Positional Accuracy):** As a memory's strength decays, its positional accuracy degrades. The brain may apply a random offset to target positions from weaker memories.
    *   Bloomies leverage their memory—factoring in desirability, current strength (influenced by DNA and age), and accuracy—to make decisions.

### Interaction
Bloomies can interact with:
*   **The Environment:**
    *   **Fruit Trees:** Can be shaken to make fruit fall, which can then be eaten.
    *   **Water Sources:** Bloomies can drink from water sources.
*   **Other Bloomies:**
    *   **Talking:** Bloomies can engage in "conversations."
    *   **Memory Exchange:** During conversations, Bloomies can share long-term memories.

### Procedural World
The game world is procedurally generated with tile-based maps, water bodies, and resource locations.

### Player's Role
The player is primarily an observer but can:
*   **Observe:** Watch Bloomies with debug views for stats, thoughts, memory visualizations (including accuracy), age/life stage, and (eventually) DNA traits.
*   **Direct Interaction:** Drag Bloomies or lock the camera to them.

---

## 2. Core Architecture

EvoBloom employs a robust component-based architecture for Bloomy entities.

### The Bloomy Entity (`Bloomy.cs`)
The root `Node2D` for each Bloomy, acting as a container and update manager for its components. Provides `GetBodyPart<T>()` for inter-component communication.

### Component-Based Design (`IBloomyComponent`)
An interface defining the contract for all functional parts of a Bloomy. `BloomyComponent.cs` is an abstract base class providing common utilities.

### Key Bloomy Components (Bodyparts)

#### Genetics, Physiology & Growth
*   **`DNAComponent.cs`:** (New) Stores the immutable genetic traits of a Bloomy, defined as a dictionary of `DNATraitType` keys and float multiplier values. These traits (e.g., `HungerRateMultiplier`, `SpeedMultiplier`, `MemoryDecayRateMultiplier`, `SensoryRangeMultiplier`, `MaturationRateMultiplier`) are set at birth (currently randomized) and influence the baseline for various other component functionalities.
*   **`BioComponent.cs`:** Manages physiological needs (hunger, thirst). The rates at which these needs increase are now derived by multiplying a base rate with the genetic `HungerRateMultiplier` and `ThirstRateMultiplier` from the `DNAComponent`.
*   **`GrowingComponent.cs`:** Manages aging and life stages (Baby, Youngling, etc.).
    *   The rate at which `CurrentAge` increases is influenced by the `MaturationRateMultiplier` from `DNAComponent`.
    *   `StageModifiers` (now including `VisualScale`) for each life stage define factors that modulate DNA-influenced attributes (e.g., memory decay, speed) and determine capabilities like reproduction.

#### Sensation & Cognition
*   **`BrainComponent.cs`:** The "mind" of the Bloomy.
    *   **Long-Term Memory System:**
        *   The base rate of memory decay (`BaseMemoryDecayPerSecond`) is modulated by both the genetic `MemoryDecayRateMultiplier` (from `DNAComponent`) and an age-based factor (from `GrowingComponent`).
    *   `GetBestTarget()`: Selects targets, applying positional fuzzing based on memory accuracy (which is affected by strength, in turn influenced by DNA/age decay rates).
    *   Other functionalities (short-term memory, processing visions, feelings, memory import) remain largely the same but operate within the context of these genetically and age-influenced parameters.
*   **`SightComponent.cs`:** Handles visual perception.
    *   The effective radius of its sensory `Area2D` (if using a `CircleShape2D`) is now determined by multiplying a base radius (defined in the scene) with the `SensoryRangeMultiplier` trait from the `DNAComponent`.
    *   It reports all detected objects to the BrainComponent for processing.
*   **`ThoughtComponent.cs`:** Generates textual thoughts. It can now also connect to `GrowingComponent.LifeStageChanged` to announce life stage transitions.

#### Action & Behavior
*   **`BehaviourComponent.cs`:** Orchestrates states. Uses `DesiredTargetPosition` obtained from the brain, which already incorporates memory fuzziness.
*   **`MuscleComponent.cs`:** Responsible for movement.
    *   Its `SpeciesBaseMaxSpeed` is modulated by the genetic `SpeedMultiplier` (from `DNAComponent`) and an age-based speed factor (from `GrowingComponent`) to determine the Bloomy's `CurrentMaxSpeed`.
*   **`NavigationComponent.cs`:** Manages exploration. Its `_Draw` method for visualizing chunks is now isolated using `top_level = true` to prevent scaling issues from parent Bloomy visuals.

#### Social Interaction
*   **`SocialComponent.cs`:** Manages interactions. Will use `GrowingComponent.CanReproduce()` (which reads from `StageModifiers` influenced by DNA-timed aging) for future reproduction logic.

#### Visual Feedback & Animation
*   **`AnimationComponent.cs`:** Drives animations and visual scale.
    *   Listens to `GrowingComponent.LifeStageChanged`.
    *   Tweens the Bloomy's visual scale based on `VisualScale` defined in `StageModifiers` (which are accessed via `GrowingComponent`), creating a visual growth effect.
*   **`FloatingThoughtComponent.cs`, `StatsBarComponent.cs`, `TalkBubbleComponent.cs`:** Function as previously described, providing visual feedback for thoughts, needs, and conversations.

*(Other sections like Behaviour States, Environment, World Gen, Player Interaction remain largely the same in principle, but the Bloomies operating within them are now more diverse due to DNA and aging.)*

---

## 3. How It All Works Together (A Day in the Life of a Bloomy)

This illustrates a typical sequence of events, now considering DNA and age:

1.  **Birth & Genetics:** A Bloomy is instantiated. Its `DNAComponent` initializes its genetic traits (e.g., faster metabolism, better base memory retention).
2.  **Aging & Development:** The `GrowingComponent`, influenced by DNA's `MaturationRateMultiplier`, advances the Bloomy's `CurrentAge`. As it crosses age thresholds, its `LifeStage` changes. This triggers updates in:
    *   `AnimationComponent`: Visual scale changes.
    *   `BrainComponent`: The effective memory decay rate is adjusted.
    *   `MuscleComponent`: Effective speed is adjusted.
3.  **Needs Arise:** `BioComponent` updates `HungerLevel`, the rate influenced by its DNA.
4.  **Feeling Dominates:** `BrainComponent` evaluates feelings. The relevance of long-term memories is affected by their current strength, which is a product of initial strength, base decay, DNA decay multiplier, and age-based decay factor.
5.  **Behaviour Reacts:** `BehaviourComponent` gets a `DesiredTargetPosition` from `BrainComponent`. This position might be fuzzed.
6.  **Movement:** `MoveState` uses `MuscleComponent`, whose speed is now a product of its species base speed, DNA speed multiplier, and age-based speed factor.
7.  **Perception & Arrival:** `SightComponent` (with its DNA-influenced range) reports sightings. `BrainComponent` reinforces memories.
8.  **Action or Re-evaluation:** The Bloomy acts or re-evaluates, its decisions colored by its genetically and age-influenced cognitive and physical abilities.

This cycle of needs, feelings, perception, decision-making, and action repeats, with individual Bloomies exhibiting varied behaviors and capabilities due to their unique DNA and current life stage.

---

## 4. Key Data Structures & Enums

*   **Models:**
    *   `VisionResult.cs`: Represents detected objects.
    *   `MemoryVisionResult.cs`: For long-term memory, with `InitialStrength`, `LastSeenTimestamp`, and methods for `GetCurrentStrength` and `GetCurrentPositionAccuracy`.
    *   `ProcessedVisionResult.cs`: Holds `Weight` and `CalculatedInitialMemoryStrength` of a vision.
    *   `Thought.cs`: For textual thoughts.
    *   `StageModifiers.cs`: Defines properties for life stages (memory/speed factors, reproduction capability, visual scale).
*   **Enums:**
    *   `DNATraitType.cs`: (New) Defines genetic traits (e.g., `HungerRateMultiplier`, `SensoryRangeMultiplier`).
    *   `LifeStage.cs`: Defines life stages.
    *   `FeelingType.cs`, `VisionType.cs`, `FruitType.cs`, `ConversationStatus.cs`: As previously described.
*   **Utilities:** `VisionTypeProperties.cs`, `TargetScoreEvaluators.cs`, `BloomyNameGenerator.cs`, `RNGExtensions.cs`.

---

## 5. Project Structure (Overview)

The project is organized into several folders within `scripts\`:

*   **`AI`**: `BrainComponent`, `BehaviourComponent`, `Feelings`, `States`.
*   **`Components`**: Animation, Bio, `DNAComponent` (New), Growing, Muscle, Navigation, Sight, Social, Thought, and visual feedback components.
*   **`Environment`**: `Generation`, `Interactables`.
*   **`Gameplay`**: High-level controllers.
*   **`Shared`**:
    *   **`DataModels`**: VisionResult, MemoryVisionResult, ProcessedVisionResult, Thought, StageModifiers.
    *   **`Enums`**: All enums, including `DNATraitType` (New) and `LifeStage`.
    *   **`Interfaces`**: All interfaces.
    *   **`Utils`**: Utility classes.
*   **Root Scripts**: `Bloomy.cs`.

*(Adjusted to include new DNA-related files.)*

---

## 6. Development Setup

### Prerequisites
- **Godot Engine 4.4.1+** with C# support
- **.NET 8.0** SDK
- **Paid Asset Packs** (see Asset Setup below)

### Asset Setup ⚠️
**Important**: This project uses paid asset packs that are **not included** in the repository.

👉 **See [assets/PAID-ASSETS-REQUIRED.md](assets/PAID-ASSETS-REQUIRED.md) for purchase links and drag-and-drop installation instructions**

### Quick Start
1. Clone this repository
2. Purchase and install required asset packs (see [assets/PAID-ASSETS-REQUIRED.md](assets/PAID-ASSETS-REQUIRED.md))
3. Open project in Godot Engine 4.4.1+
4. Build: `dotnet build`
5. Run the project

### VS Code Setup
For VS Code development, see **[vscode-setup.md](vscode-setup.md)** for configuration templates.

---

## 7. Potential Future Development

The current architecture provides a solid foundation for many exciting features:

*   **Reproduction & Genetics:** Fully implement mating behaviors. Offspring inherit a mix of `DNATraitType` values from parents, possibly with mutations, making the `DNAComponent` central to evolution.
*   **Memory Linking/Association:** Connect related memories.
*   **Evolutionary Dynamics:** Observe how different genetic traits spread or die out in the population based on their survival and reproductive success.
*   **More Complex Social Interactions:** Alliances, rivalries, family units, care for young (influenced by life stage).
*   **Predators & Threats:** Introduce elements that actively hunt Bloomies.
*   **Death & Ecosystem Impact:** Implement consequences for reaching max age (potentially a DNA trait for longevity) or succumbing to unmet needs/threats.
*   **Expanded DNA Traits:** Add more traits like disease resistance, specific resource preferences, learning speed, etc.