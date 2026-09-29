# Encounter Luck and Critical Roll Audit

## Bugs Found

The Event schedule always added one guaranteed Challenge Event before applying Encounter Luck, then interpreted every full 100% of Luck as another guaranteed Event. A 210% value could therefore schedule three guaranteed Events plus a bonus roll, before authored fixed Events were counted. The intended rule is a block quota: 210% means two guaranteed Events and a 10% chance for a third, capped at five.

The primary player critical path composes base, Weapon Ascend, and pet Critical Rate, clamps the result to 0–100%, then performs one `NextUnit() < CriticalRate` roll per successful player hit. A crit/no-crit/crit sequence has about an 8% chance at a 35% rate. The deployed WebGL runtime settings used fixed RNG seed `1337`, so identical action sequences repeat across runs. Pet follow-up criticals are a separate passive-gated roll.

## Changes

- Apply the 500% total Encounter Luck cap and schedule `floor(luck / 100%)` guarantees plus one remainder roll per 20-Stage block.
- Count fixed Challenge Events toward the block quota and retain the five-Event ceiling.
- Refresh restored schedules from the current rule while retaining Event Stages already reached.
- Update the GDD and ADR-019 with the owner-confirmed quota rule. Update existing schedule assertions to the new counts.
- Critical calculation remains unchanged; the audited primary-hit probability matches the displayed composed rate.
- Removed the fixed `1337` seed from Combat Runtime Settings; each runtime initialization seeds its random stream with a fresh GUID-derived value.
- Removed the Editor sample-student authentication, bootstrap, reset, lifecycle, social-profile, and combat-selection path. Editor and player builds now select Direct Firestore services.

## Verification

- Reviewed the scheduler inputs, saved schedule restoration path, player stat projection, and primary/pet critical roll paths.
- Automated tests were not run.
