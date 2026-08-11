# Direct Firestore prototype setup

Unity is configured for Firebase project `tools-games-f4684`, database `(default)`, collection `competition`, and question collection `question`. Cloudflare only needs to host the static WebGL build.

## Expected hierarchy

```text
competition                         collection
  level1                            document -> Grade 4
    {normalizedUsername}            dynamic map field
      userdata                      map
        username                    account username string
        password                    exactly six ASCII digits, stored as string
      gamedata                      map (direct fields)
  level2                            document -> Grade 5
  level3                            document -> Grade 6

question                            collection
  silver                            document -> Silver Rank questions
  gold                              document -> Gold Rank questions
  diamond                           document -> Diamond Rank questions

leaderboard-public                  sanitized projection collection
  level1                            document -> Grade 4 public entries
  level2                            document -> Grade 5 public entries
  level3                            document -> Grade 6 public entries
```

The username note above means the account username is a string; it does not need to be numeric. Passwords such as `001234` must remain Firestore strings so leading zeroes survive.

Unity reads only `level-1`, `level-2`, and `level-3`. It searches those documents in order, finds the top-level field whose name equals the normalized username, then verifies `userdata.username` and `userdata.password`. The level containing the student determines the grade; Unity does not trust a separate grade field.

## `gamedata.game1` fields

An empty `game1` map is valid and opens Main Menu with safe defaults: the username as display name, the level-derived grade, a default avatar, Stage 1, and empty wallet/loadout/inventory presentation.

These optional fields match the current Unity `PlayerSnapshot`:

- `revision`: integer
- `profile`: map containing `displayName`, `iconId`, `publicPlayerId`, and integer `displayNameChangedAtUnixSeconds`
- `progression`: map containing `currentStage`, lifetime `highestStage`, `activeRank`, `rankProgress`, `prestige`, `legacyAtkBonusBasisPoints`, `firstStage200Reached`, integer `firstStage200ReachedAtUnixSeconds`, and integer `totalDamage`
- `wallet`: map containing integer `silver`, `gold`, `diamond`, and `powerCoins`
- `inventory`: array of maps containing `itemId`, `owned`, and `upgradeLevel`
- `loadout`: map containing `petId`, `weaponId`, and `avatarId`
- `activeRun`: map containing `runId`, `currentStage`, biome/encounter identity, encounter kind and HP/cooldown state, Event question source/attempt ordinal, `committedAttemptId`, run-only `silverEarned`/`goldEarned`/`diamondEarned`, and `bonusMultiplierBasisPoints`
- `economy`: last accepted Weapon Ascend transaction ID, resulting level, and cost receipt
- `lastRunSettlement`: idempotency receipt containing the settled run ID, settlement type, Stage/rewards, and resulting Power Coin balance
- `analytics`: map containing resolved outcome totals, response score/efficiency sums, play time, last applied attempt ID, and per-Rank aggregates

`game2` and `temp` remain out of scope.

## Public leaderboard projection

Pre-create `leaderboard-public/level1`, `level2`, and `level3` with an immutable `_meta` map. Unity adds or replaces one map field keyed by the player's opaque `publicPlayerId`. Each player entry contains only `displayName`, avatar/pet/weapon IDs, current/highest stage, silver/gold/diamond balances, `weightedCurrencyScore`, `totalDamage`, and `entryRevision`. Never copy usernames, passwords, audit scores, or private analytics into this collection.

Unity reads exactly the signed-in student's level document when the leaderboard opens or the student presses Refresh. It does not poll. Ranking orders highest stage first, then `silver*5 + gold*7 + diamond*10`, with shared competition ranks for exact score ties. The sanitized projection also includes Weapon Ascend level for the leaderboard loadout display.

## Firestore rules

Review and manually publish `Firebase/firestore.rules` in **Firestore Database > Rules**. This repository update does not publish rules. The rules allow unauthenticated GET requests for the three private grade documents and three sanitized leaderboard documents. Private grade updates may change only one existing student map. Public leaderboard updates may add or replace one public-player map while preserving `_meta`. Lists, document creates, and deletes remain denied.

The update rule supports the ADR-006 missing-default repair and player persistence flow. It is still prototype-only authorization: because the client does not use Firebase Authentication and students are dynamic fields inside shared grade documents, Firestore rules cannot prove that an anonymous caller owns the one student map being changed.

This layout exposes every student map in a grade whenever that grade document is downloaded. The client-side password comparison is not secure authentication, and the API key does not provide authorization.

## Remember this device

When enabled, Unity stores the username and plaintext six-digit password in WebGL PlayerPrefs/browser IndexedDB. It survives browser restarts until sign-out or site-data removal. When disabled, credentials survive Unity scene changes only.
