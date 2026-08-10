# Direct Firestore authentication E2E test plan

Human-run only. No automated or Play Mode test is part of this refactor.

## Editor sample mode

1. Keep `Use Editor Sample Student` enabled on `GameApiSettings`.
2. Start from `BootstrapScene`; confirm it routes to `AuthenticationScene` when the editor mock has no session.
3. Sign in with `sample-student` / `123456`.
4. Confirm Bootstrap loads and Main Menu shows the sample profile, progression, wallet, inventory, and loadout.
5. Sign out and confirm Authentication opens again.

## Direct Firestore mode in Editor

1. Enter the Firebase project ID and Web API key in `GameApiSettings`.
2. Disable `Use Editor Sample Student`.
3. Confirm wrong username, non-six-digit password, wrong six-digit password, missing `game1`, and denied rules all show recoverable messages.
4. Confirm accounts placed in `level-1`, `level-2`, and `level-3` load as Grade 4, Grade 5, and Grade 6 respectively.
5. Confirm empty `game1` maps load safe Main Menu defaults and populated maps load the matching profile, progression, wallet, inventory, and loadout.
6. Confirm `temp` and `game2` are ignored.

## WebGL on Cloudflare hosting

1. With Remember unchecked, sign in and navigate scenes; refresh and confirm login is required again.
2. With Remember checked, sign in, refresh, close/reopen the browser, and confirm Bootstrap restores the account.
3. Sign out, refresh, and confirm saved credentials were removed.
4. Clear browser site data and confirm the remembered login is removed.
5. Confirm Firestore writes are denied.

## Player-experience scenarios

- **New player:** Confirm the student can sign in without selecting or knowing their level document.
- **Stress:** Submit repeatedly, refresh during Bootstrap, and retry after a network failure; confirm there are no duplicate scene loads.
- **Skill:** Not applicable; authentication intentionally has no mastery component.
- **Abuse:** Try usernames from another grade and verify only the matching username/password pair succeeds. Record that document contents remain publicly inspectable as an accepted prototype risk.
- **Readability:** Confirm a student can distinguish invalid login, connection failure, and loading states without developer help.
