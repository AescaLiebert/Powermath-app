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
```

The username note above means the account username is a string; it does not need to be numeric. Passwords such as `001234` must remain Firestore strings so leading zeroes survive.

Unity reads only `level-1`, `level-2`, and `level-3`. It searches those documents in order, finds the top-level field whose name equals the normalized username, then verifies `userdata.username` and `userdata.password`. The level containing the student determines the grade; Unity does not trust a separate grade field.

## `gamedata.game1` fields

An empty `game1` map is valid and opens Main Menu with safe defaults: the username as display name, the level-derived grade, a default avatar, Stage 1, and empty wallet/loadout/inventory presentation.

These optional fields match the current Unity `PlayerSnapshot`:

- `revision`: integer
- `profile`: map containing `displayName` and `iconId` strings
- `progression`: map containing `currentStage`, `highestStage`, `activeRank`, `rankProgress`, `prestige`, and `firstStage200Reached`
- `wallet`: map containing integer `silver`, `gold`, `diamond`, and `powerCoins`
- `inventory`: array of maps containing `itemId`, `owned`, and `upgradeLevel`
- `loadout`: map containing `petId`, `weaponId`, and `avatarId`
- `activeRun`: map containing `runId`, `currentStage`, and `committedAttemptId`

`game2`, `temp`, and all leaderboard behavior are intentionally out of scope.

## Firestore rules

Review and manually publish `Firebase/firestore.rules` in **Firestore Database > Rules**. The rules allow unauthenticated GET requests for the three grade documents and allow an update only when it changes exactly one existing top-level student map without adding or removing student keys. Lists, creates, and deletes remain denied.

The update rule supports the ADR-006 missing-default repair and player persistence flow. It is still prototype-only authorization: because the client does not use Firebase Authentication and students are dynamic fields inside shared grade documents, Firestore rules cannot prove that an anonymous caller owns the one student map being changed.

This layout exposes every student map in a grade whenever that grade document is downloaded. The client-side password comparison is not secure authentication, and the API key does not provide authorization.

## Remember this device

When enabled, Unity stores the username and plaintext six-digit password in WebGL PlayerPrefs/browser IndexedDB. It survives browser restarts until sign-out or site-data removal. When disabled, credentials survive Unity scene changes only.
