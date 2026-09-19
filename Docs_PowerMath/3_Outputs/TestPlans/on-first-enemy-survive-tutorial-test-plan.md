# Test Plan: `OnFirstEnemySurvive`

Date: 2026-09-15

## Automated Coverage

- Predicate accepts a correct, positive-damage, action-consuming standard hit that leaves the same enemy alive.
- Predicate rejects zero damage, incorrect outcome, defeat, flee, Challenge Event, mismatched encounter, and missing action consumption.
- Sequence asset has four stable states, valid transitions, three localized keys, and a catalog reference.
- Combat Unity, runtime, Combat Unity EditMode tests, and Editor assemblies compile successfully.

## Editor Playtest

- [ ] Damage a standard enemy without defeating it; verify the sequence queues only after all result/action animations settle.
- [ ] Verify warning dialogue, explanation dialogue, queue-only focus, and final encouragement appear in order.
- [ ] Tap outside the action-queue focus and confirm no tutorial or gameplay command advances.
- [ ] Tap the queue focus and confirm cooldown, HP, Stage, Rank, and wallet remain unchanged.
- [ ] Trigger an enemy attack on the same hit and verify death/settlement resolves before tutorial presentation.
- [ ] Defeat an enemy, deal zero damage, or resolve a fleeing Event and confirm no eligibility.
- [ ] Make one hit qualify for Rank Change and Enemy Survive; verify both save without revision conflict and queue by stable ordering.
- [ ] Close during each state; verify safe restart and no duplicate completion/reward behavior.
- [ ] Verify English, Thai, Reduced Motion, and smallest supported mobile viewport.
