---
slug: shared-settings-panel-admin-tools
status: partially-superseded-by-adr-018
source: manual
gdd_tags:
  - player-experience
  - combat-stats
  - answer-scoring
  - economy
  - server-authority
  - guardrails
owner: architect-agent
human_checkpoint: passed
next_agent: implementation-agent
blocked_by: []
---

# ADR-017: Shared Settings and Scoped Admin Test Authority

| Field | Value |
| --- | --- |
| Status | **Partially superseded by ADR-018** |
| Date | 2026-09-10 |
| Author | architect-agent |
| GDD Section | `@tag:player-experience`, `@tag:combat-stats`, `@tag:answer-scoring`, `@tag:economy`, `@tag:server-authority`, `@tag:guardrails` |
| Extends | ADR-003, ADR-008, ADR-014, ADR-015, ADR-016 |

Accepted by the product owner on 2026-09-10 (`lgtm`).

The session-only combat overrides, synthetic Quick Test runtime, and hard-coded
username allowlist were superseded by ADR-018 after deployed mobile testing on
2026-09-11. The shared Settings UI, revision-checked commands, and reset boundary
remain current.

## Context

Authentication and Main Menu currently expose different Settings behavior: the Authentication gear has no controller, while Main Menu opens an unrestricted reset-only AdminPanel. The product owner approved one shared child-friendly Settings panel with General and Sound for everyone and an Admin tab for exact test accounts `test1` through `test10`. Admin tools include combat overrides, Rank/wallet/HP commands, isolated answer-zero testing, and a full save reset that preserves credentials.

These controls cross scene UI, device preferences, combat rules, academic progression, economy, and destructive persistence. Putting them in one panel controller would create a UI-owned authority path and conflict with accepted architecture.

## Decision

- Reuse one Settings UXML/USS template in both scenes while keeping panel instances, lifecycle, focus, and composition scene-scoped under ADR-014.
- Store only non-critical Music/SFX/fullscreen intent locally. Apply actual fullscreen through a platform service and audio through existing controller APIs.
- Author the admin username allowlist and mutation limits as fail-closed definitions. Resolve access from stable authenticated identity, never display name, and recheck it at every command boundary.
- Treat ATK/CR/CD and Invincibility as session-only rule overrides captured immutably when an attempt commits.
- Implement answer-zero video skipping as an ADR-015-style isolated practice runtime with no real progression, economy, analytics, settlement, or leaderboard writes.
- Implement Rank, wallet, Restore HP, and Reset as explicit current-account, operation-ID, revision/update-time-checked commands outside the UI layer.
- Replace the entire canonical `gamedata` map while preserving `userdata`; keep idempotency/leaderboard-repair state in a separate `adminops` sibling map and do not report full success while cleanup is incomplete.
- Keep the accepted direct-Firestore prototype limitation explicit. These seams can move behind the trusted command service proposed by ADR-016 without replacing UI or domain policy.

## Alternatives Considered

| Alternative | Advantage | Rejected because |
| --- | --- | --- |
| Duplicate Settings panels in each scene | Fast markup changes | Guarantees style/content drift and violates the product request |
| Persistent global UI manager | One access point | Conflicts with ADR-014 scene ownership and causes focus/lifetime coupling |
| Add all controls to `AdminPanelController` | Minimal class count | UI would own audio, combat, authorization, Firestore, and reset policy |
| Hide Admin only in UXML/C# | Simple | Visibility is not command authorization and is trivially bypassed |
| Persist combat cheats in player save | Survives reload | Pollutes gameplay state and creates schema/leaderboard ambiguity |
| Let answer-zero attempts use live academic persistence | Exercises full pipeline | Contaminates educational audit, Rank Currency, analytics, and public results |
| Direct field patches per button | Easy implementation | Duplicates schema paths and lacks revision/idempotency/recovery guarantees |
| Delete the entire student map | Truly empty account | Deletes credentials and conflicts with preserving login identity |

## Consequences

### Positive

- Children see a small, consistent Settings experience before and after login.
- Audio preferences survive scene changes without becoming critical account state.
- Admin test behavior is visible, scoped to current test identity, and isolated from normal students.
- Quick testing cannot alter educational placement or public results.
- Persisted admin mutations use the same concurrency and recovery shape as existing economy/progression commands.
- The old reset-only AdminPanel can be retired without moving authority into a view.

### Negative / Trade-offs

- The prototype's client-side allowlist cannot provide production-grade security.
- Two scene composition adapters are required even though the visual template is shared.
- Safe combat overrides require a new rule-source seam and immutable per-attempt capture.
- Reset plus leaderboard cleanup is a recoverable multi-step operation rather than a single blind patch.
- A trusted backend remains necessary before these commands are suitable for a hardened public deployment.

## Migration

1. Add shared panel/presenters and local preferences while retaining old AdminPanel.
2. Add admin access policy and test-only runtime overrides.
3. Add isolated Quick Test mode.
4. Add revision-checked persisted commands and reset cleanup receipts.
5. Verify both scene instances and all denial/recovery paths.
6. Remove the old AdminPanel assets/controller after no references remain.

## Related

- `Docs_PowerMath/3_Outputs/Specs/shared-settings-panel-admin-tools-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/shared-settings-panel-admin-tools-arch-plan.md`
- ADR-008: Atomic Run Settlement and Weapon Ascension.
- ADR-014: Scene-Scoped UI Composition and Shared Motion.
- ADR-015: Isolated Question Fallback Practice Session.
- ADR-016: Player lifecycle and live service boundaries.
