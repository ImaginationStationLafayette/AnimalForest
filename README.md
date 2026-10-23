# AnimalForest

AnimalForest is a Unity-based educational exploration game for children ages 5–10.

Players explore a stylized forest environment, approach animals, and interact with them through simple touch-based controls. The long-term goal is to let children have safe, age-appropriate conversations with animal characters while learning about their species, diet, habitat, behavior, and other facts.

## Current Features

The current prototype includes:

- Stylized 3D forest environment
- Multiple animated animal exhibits
- Touch / click-to-move navigation using Unity NavMesh
- First-person exploration camera
- Intro sequence that begins in third person and transitions into first person
- Animal proximity detection
- Animal interaction prompts
- Animal collision and navigation obstacles
- Basic microphone interaction UI
- Per-animal educational metadata such as:
  - Name
  - Species
  - Diet
  - Habitat
  - Fun fact
- Mouse support for development and testing on desktop

## Planned Interaction Flow

The intended animal interaction experience is:

1. Explore the forest.
2. Approach an animal.
3. A small interaction prompt appears.
4. Tap the animal to begin interacting.
5. The animal greets the child.
6. Suggested questions are displayed, such as:
   - What do you eat?
   - Where do you live?
   - Tell me something interesting!
7. The child can eventually ask their own question using speech or text.
8. An information button provides a trusted educational information card.
9. Closing the interaction returns the player to exploration.

## AI Conversation Goals

Animal conversations are planned to use an AI backend with strict safety and educational constraints.

The system should:

- Produce short, child-friendly responses
- Stay focused on the animal, nature, and appropriate educational topics
- Use curated animal facts as the primary knowledge source
- Avoid inappropriate, unsafe, or unrelated content
- Never request personal information from children
- Redirect unsuitable or unrelated questions back to the animal interaction
- Apply input and output safety checks
- Keep verified information cards separate from AI-generated conversation

The AI conversation backend is not yet part of the current prototype.

## Controls

### Touchscreen

- Tap the ground to move
- Drag the screen to look around
- Tap an animal when close enough to interact

### Desktop Development Controls

- Mouse click to move
- Mouse drag to look around

Desktop controls are primarily intended for development and testing.

## Project Structure

Important Unity project folders:

```text
AnimalForest/
├── Assets/
├── Packages/
├── ProjectSettings/
├── .gitignore
├── .gitattributes
└── README.md