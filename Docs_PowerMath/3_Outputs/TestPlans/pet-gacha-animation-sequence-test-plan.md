# Pet Gacha Animation Sequence Test Plan

## Functional

1. Open Pet Gacha and verify Main Menu UI eases out, the screen fades through black, then topbar, copy, right-sliding featured pets, and footer enter with no overshoot.
2. Run 1x for each rarity and verify vortex → comet → burst timing, with a white R, purple SR, or yellow SSR comet cue.
3. Verify the individual reveal order is oversized silhouette settle, radial burst, white reveal flash, full pet, copy slide-up, then stars one by one.
4. Complete a 1x pull and verify the final result grid contains exactly one card.
5. Complete a 10x pull and verify transition token count, reveal count, progress count, and final grid count are all exactly ten.
6. Verify tapping/clicking the settled full-screen Reveal advances one item and `SKIP ALL` opens the unchanged final result grid.
7. Verify there is no visible or focusable `NEXT` button.
8. Advance between individual pets and verify the old name/rarity copy becomes fully transparent before the new text is assigned.
   Verify no old or new pet-info frame appears before the scheduled copy entrance.
   Verify the pet remains centered while copy is hidden and no full-color pet frame appears before the black silhouette.
9. Verify every rarity-star PNG renders white for R, SR, and SSR.

## Safety and Edge Cases

1. Spam Confirm; verify one transaction, one spend, and one presentation sequence.
2. Spam Skip/full-screen tap during locked timing; verify early taps are ignored and callbacks from prior reveals do not mutate the current reveal.
3. Mix new and duplicate pets; verify each badge and final card matches the saved receipt.
4. Put an SSR anywhere in a 10x receipt; verify the transition uses the SSR/yellow tier without exposing pet identity early.
5. Trigger recoverable transport failure; verify no animation begins until the saved receipt is recovered.
6. Try insufficient funds; verify no transition starts and no currency changes.

## Accessibility and Platforms

1. Enable reduced motion; verify the flow uses immediate state changes while retaining all text, counts, rarity, and results.
2. Verify controls remain usable with WebGL pointer input and mobile touch targets.
3. Check 16:9 and narrow/mobile layouts for clipped reveal copy, tokens, stars, and final cards.

## Automated Contract

- `CombatLobbyViewTests.MainMenuAsset_ContainsPetGachaInteractionContract` validates the transition, silhouette, reveal controls, final result grid, and enabled 10x entry contract.
- Unity runtime and EditMode test assemblies must compile without errors.
