# DevLog — Synchronized Battle Impact Flow

Date: 2026-09-15

GDD references: `@tag:combat-attempt`, `@tag:feedback`, `@tag:player-experience`, `@tag:guardrails`, `@tag:playtest`.

## Outcome

Changed standard and recovered combat presentation so each attacker and target reaction play as one blocking paired action. Player attacks now trigger enemy TakeDamage, damage text, hit audio, and HP movement at the player impact marker. Enemy attacks now trigger player TakeDamage, the authoritative heart display, hurt feedback, local knockback, and a light combat-world impulse at the enemy impact marker.

This removes the old serial pauses between an attack finishing and its target reacting while preserving authoritative player-first resolution and the existing result-screen delay.

## Implementation

- Added one-shot, profile-driven impact callbacks to `ActorPresentationController` at normalized times 0.40 for Player Attack and 0.47 for Enemy Attack.
- Added paired-action orchestration to `CombatFeedbackPlayer`; input remains locked until both attacker and reaction coroutines settle.
- Added immediate heart rendering at enemy impact through `CombatLobbyView.SetPlayerHearts`.
- Added a lighter player-damage combat-root impulse: 8 reference pixels over 0.14 seconds with one directional oscillation. Existing player TakeDamage animation retains its local knockback.
- Kept critical player-hit impulse distinct at 14 reference pixels over 0.18 seconds with two oscillations.
- Reduced Motion keeps reaction state, HP/hearts, text, and audio synchronized while omitting combat-root translation.

## Verification

- Added EditMode coverage proving both attack types invoke their impact callback exactly once while still in `Attacking`, before returning to `Idle`.
- Added profile assertions for both impact markers and the light player-damage impulse values.
- Compiled `PowerMath.Gameplay.Combat.Unity` and `PowerMath.Gameplay.Combat.UnityEditModeTests` successfully with Unity 6000.5.3f1's generated response files.
- Unity Test Runner execution and final timing/motion feel remain the required open-Editor human checkpoint.

## Approved Impact Accent Extension

After project-owner `LGTM` on 2026-09-15, the paired flow received a presentation-only satisfaction pass:

- Added a local pose hold shared by attacker and reactor at contact; global time and authority remain untouched.
- Reshaped attacks into anticipation, accelerated travel, contact squash, and recovery.
- Replaced repeated damage wobble/flicker with a directional knockback, small settle overshoot, one white flash, and red recovery tint.
- Added a reusable six-ray uGUI starburst at the damaged actor's anchor with normal, critical, and player-damage hierarchy.
- Added a lost-heart punch and shortened normal/critical post-hit tails to 0.20/0.35 seconds.
- Reduced Motion removes positional reaction/impulse, limits the local hold to 25 ms, and retains static burst/fade, flash, values, and audio.
- Recompiled `PowerMath.Gameplay.Combat.Unity`, `Assembly-CSharp`, and `PowerMath.Gameplay.Combat.UnityEditModeTests` successfully with the impact extension and its focused tests.

## Clarity Tuning Revision

After playtest feedback that the combined motion felt too chaotic for a combat loop with one hit every 20–30 seconds:

- Removed squash/stretch from the Player during both attack and damage states while preserving anticipation, lunge, and knockback translation.
- Increased normal local hit stop from 45 ms to 60 ms so an infrequent normal hit has a readable contact frame without adding another effect.
- Increased enemy reaction travel from 18 px to 24 px for normal hits and from 26 px to 30 px for critical hits, preserving a restrained critical hierarchy.
- Initially kept normal hits free of combat-world impulse; the next approved revision superseded this choice.
- Recompiled `PowerMath.Gameplay.Combat.Unity`, `Assembly-CSharp`, and `PowerMath.Gameplay.Combat.UnityEditModeTests` successfully after the revision.

## Normal Impulse and Floating-Text Revision

- Added a restrained normal-hit combat-world impulse: 6 px over 120 ms with one oscillation. Critical remains 14 px over 180 ms with two oscillations.
- Made both Floating Damage Text and Floating Reward Text use bounded random spawn offsets.
- Replaced their local landing treatment with Pop -> 60 ms apex hold -> accelerating fall below the overlay bottom.
- Each floating value now independently chooses a clockwise or counter-clockwise rotational kick; the configured assets no longer override rotation to zero.
- Compile-validated `PowerMath.Gameplay.Combat.Unity`, `Assembly-CSharp`, and `PowerMath.Gameplay.Combat.UnityEditModeTests` after this revision.

## FDT Ballistic Trajectory Revision

- Corrected FDT `Drop Distance` so runtime uses the authored value instead of replacing it with the overlay-bottom position.
- Added authorable `Fall Horizontal Distance` and `Fall Gravity Power` controls.
- FDT now chooses a random left/right burst direction and follows a curved horizontal-and-vertical fall away from the enemy hit point.
- Preserved the project owner's live Unity asset timing edits (`Drop Seconds = 1.08`, `Pop Hold Seconds = 0.20`).

## FDT/FRT Kinematic Jump Revision

- Replaced the hand-shaped rise, apex cling, and eased fall with one continuous kinematic jump equation for both FDT and FRT.
- Motion profiles still author the result through apex height, total vertical drop, horizontal distance, and flight duration; runtime derives launch velocity and gravity.
- Pop scale, white flash, fade, and random rotation now layer over physical position instead of controlling it.
- Reduced both configured contact holds to 60 ms after feedback that the 200 ms pause felt sticky.

## FDT Readability Revision

- Matched FDT flight duration to FRT at 520 ms and added an authorable late-fade threshold.
- FDT stays opaque until 90% of its flight, then fades only during the final descent; no stationary hold was reintroduced.
