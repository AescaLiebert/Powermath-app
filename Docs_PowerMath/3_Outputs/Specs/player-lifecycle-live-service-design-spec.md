---
slug: player-lifecycle-live-service
status: approved
source: manual
gdd_tags: [server-authority, player-experience, leaderboard-profile]
owner: Codex
human_checkpoint: required
next_agent: implementation
blocked_by: []
---

# Player lifecycle design proposal

## Player goal

Enter in a readable language, choose a recognizable protagonist once, and trust that refreshes and patches preserve progress. The references establish selection overview, mirrored character detail, and name confirmation; they do not supply final UI assets or story content.

## Proposed experience

- Language switch is available before login and in settings. Default Thai; English remains selectable. Keep HYWenHai as requested and verify Thai combining marks and wrapping in actual UI.
- A genuinely new account starts opening content, then character overview, Ricko or Stellar detail, then display-name confirmation. Back navigation before confirmation changes the draft only.
- Opening supports an explicit play action where video playback needs it, subtitles, skip, and localized text fallback if video cannot load. No invented opening script.
- Confirmation shows the chosen character and name. Disable duplicate submission, show saving status, and enter the hub only after persistence succeeds.
- Character choice is permanent for this initial implementation proposal; no gameplay stat differences are introduced. Display-name editing later retains the GDD seven-day rename rule; initial setup is proposed exempt.
- Existing saves bypass the opening. If character identity cannot be mapped reliably, offer a one-time choice while preserving all progress and their existing name.
- Tutorial progress is separate from onboarding. Provide a versioned checkpoint mechanism, but the lesson steps and any rewards require authored design. A patch never resets all tutorials.
- Announcements are optional reading after critical recovery and onboarding. Read acknowledgements never grant rewards; mailbox claim is a separate operation.

## State transitions

| State | Entry / exit | Refresh or interruption |
|---|---|---|
| Loading | Restore language, establish session, validate compatibility, fetch save | Retry or login; no default save on network/permission/parse failure |
| Opening | Confirmed new-player record; completed/skip checkpoint leads to selection | Replay current unacknowledged segment; completed segments stay completed |
| Character selection | Opening acknowledged, or legacy missing choice | Persist accepted selection checkpoint; back can revise until final confirmation |
| Name entry | Valid selected character | Resume accepted draft; unsent keystrokes need not persist |
| Confirming | Valid name and character; submit stable operation ID | Query/retry that operation; no second initialization |
| Tutorial / hub | Server-confirmed preparation completion | Restore tutorial checkpoint or committed gameplay/presentation receipt |

Onboarding does not spend currency or grant an unrequested reward. Logout clears account-scoped runtime state and draft caches. Account switching must never apply one player's language write or choice to another player.

## Experience evaluation

| Component | Design application |
|---|---|
| Clarity | Show selected character/name, permanence note, save status, localized failure reason |
| Motivation | The chosen character appears consistently across the game |
| Response | Immediate selection highlight; back before confirm; retries preserve choice |
| Satisfaction | Confirmation visual transition and a configured UI sound after acknowledged save |
| Fit | Use the reference compositions and existing UI styling, without fabricated lore |

No numeric animation tuning is prescribed; evaluate selection readability and response before adding spectacle.

## Validation scenarios

A new Thai-reading player can change language, skip/replay opening, choose either character, enter Thai text, and identify the same character in hub/combat/profile. Test mouse and touch at supported aspect ratios. Spam confirm and refresh every step; expect one committed profile. Switch accounts during a pending request; expect no cross-account updates. Load an existing save with a pending combat receipt; onboarding must not discard it. Full tutorial instruction quality remains pending authored steps.


Approval: user accepted design and architecture with “LGTM” on 2026-09-08. Placeholder character/opening assets explicitly authorized. Backend hosting/enrollment and live deployment remain separate integration inputs.
