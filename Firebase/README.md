# Direct Firestore prototype setup

Unity is configured for Firebase project `tools-games-f4684`, database `(default)`,
the three player collections `level-1`, `level-2`, and `level-3`, and the shared
question collection `question`. Cloudflare only hosts the static WebGL build.

## Current hierarchy

```text
level-1                            collection -> Grade 4
  {normalizedUsername}            one document per player
    userdata                      map
      username                    account username string
      password                    exactly six ASCII digits, stored as string
      admin                       optional owner-managed boolean
    gamedata                      map (canonical player save)

level-2                            collection -> Grade 5
  {normalizedUsername}            same document shape

level-3                            collection -> Grade 6
  {normalizedUsername}            same document shape

question                           collection
  silver                           Rank questions
  gold
  diamond
  challenge-silver                 Challenge questions
  challenge-gold
  challenge-diamond

leaderboard-public                 sanitized projection collection
  level1                           Grade 4 entries
  level2                           Grade 5 entries
  level3                           Grade 6 entries
```

The username is a string and does not need to be numeric. Passwords such as
`001234` must remain Firestore strings so leading zeroes survive. At login, Unity
tries the configured level collections in order and reads only the document whose
ID is the normalized username. The collection containing the player determines
the Grade 4/5/6 band.

## Canonical `gamedata`

`gamedata` is stored directly beside `userdata`; there is no `game1` wrapper in
the current layout. Missing optional fields are repaired additively by a masked
PATCH. Existing values and unknown future feature maps are not replaced.

The current save contains these major sections:

- `schemaVersion` and `revision`
- `profile`, `preferences`, `onboarding`, `tutorial`, and `tutorialMap`
- `progression`, `wallet`, `inventory`, and `loadout`
- `activeRun`, including Event schedule state, Challenge reservation/cursors,
  combat state, reward receipts, passive counters, and pending presentation
- `economy`, including Weapon Ascend, Pet Gacha/equip receipts, pity state, and
  the last multi-pull results
- `academic`, `analytics`, `lastRunSettlement`, and optional `adminTuning`

Schema migrations are applied in memory and persisted through the same optimistic
concurrency PATCH as the schema-version and revision update. This prevents a
corrected value from being lost while the stored version advances.

## Compatibility and rollback

The checked-in rules retain the older `competition/{levelId}` document path as
rollback compatibility only. The `competition-2` collection has been migrated
and deleted; it is intentionally absent from the current rules. The current
client does not read or write either legacy path.

## Public leaderboard projection

Pre-create `leaderboard-public/level1`, `level2`, and `level3` with an immutable
`_meta` map. Unity adds or replaces one map field keyed by the player's opaque
`publicPlayerId`. Each player entry contains only display name, avatar/pet/weapon
IDs, Weapon Ascend level, current/highest stage, silver/gold/diamond balances,
weighted currency score, total damage, and entry revision. Never copy usernames,
passwords, audit scores, or private analytics into this collection.

Unity reads the signed-in player's public cohort document only when the
leaderboard opens or the player presses Refresh. Ranking orders highest stage
first, then `silver*5 + gold*7 + diamond*10`, with shared competition ranks for
exact score ties.

## Firestore rules

Review and manually publish `Firebase/firestore.rules` in **Firestore Database >
Rules**. Repository changes do not publish rules.

The current per-player rule permits unauthenticated GET for an exact player
document and permits an update only when `gamedata` is the sole changed top-level
field. List, create, and delete remain denied. The question rule permits GET for
the three Rank documents and three Challenge documents. Legacy rules remain for
rollback compatibility.

This is prototype-only authorization. The client does not use Firebase
Authentication, so Firestore cannot prove that an anonymous caller owns a player
document. The API key identifies the Firebase project; it is not an authorization
secret. Client-side password comparison is not secure authentication.

## Remember this device

When enabled, Unity stores the username and plaintext six-digit password in
WebGL PlayerPrefs/browser IndexedDB. It survives browser restarts until sign-out
or site-data removal. When disabled, credentials survive Unity scene changes only.
