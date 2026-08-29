# Task Card: Rank Notification Game Juice and Sequence Refactor

- **Slug**: `rank-notification-juice-sequence`
- **Type**: Feature / Polish & Juice Refactor
- **Status**: In Progress

## Objectives
- Replace prototype pop-up message box for Academic Rank promotion and demotion with an impactful, winning-screen style Rank Notification Overlay.
- Implement game juice sequence:
  1. Rank Notification Image / Banner pops up with scale bounce and audio fanfare.
  2. Transition text fades in showing strictly `[Current Rank Icon] ➔ [Destined Rank Icon]`.
  3. Holds and waits for player tap anywhere.
  4. On tap, instantly clears text and smoothly slides the rank banner away.

## Handoff Checklist
- [x] Design Spec
- [x] UI Documents & USS Styles
- [x] AcademicProgressionView refactor
- [x] RankTransitionFeedbackPlayer coroutine refactor
- [x] AcademicAudioPlayer fanfare enhancement
- [x] EditMode and PlayMode tests verified
