# DevLog: Unity-Owned Custom Authentication Implementation

**Date:** 2026-08-08  
**ADR:** ADR-002

## Outcome

Implemented the local cutover from launch-code authentication to Unity-owned sign-in backed by a same-origin Cloudflare Pages Function and Firestore REST access. Build order is now Bootstrap, Authentication, Main Menu.

## What Changed

- Added zero-dependency Cloudflare login, logout, bootstrap, HMAC, OAuth, Firestore REST, secure-cookie, and rate-limit integration source.
- Added Unity authentication/bootstrap service interfaces with REST and `UNITY_EDITOR` mock implementations.
- Reworked Bootstrap to restore a browser cookie session and route unauthenticated players to Authentication.
- Consolidated Authentication onto UI Toolkit with opt-in Remember This Device and shared-device warning.
- Added Main Menu logout.
- Renamed the misspelled Authentication scene/UI assets while preserving their GUIDs.
- Detached but retained the old launch-code bridge for rollback until human WebGL E2E.

## Verification

- Unity scripts compiled with zero console errors.
- Bootstrap, Authentication, and Main Menu scene validation reported no missing scripts or broken references.
- Cloudflare JavaScript and JSON syntax checks passed.
- No automated tests or Play Mode E2E were run; human E2E is documented separately.

## Remaining Human Checkpoints

- Create Cloudflare encrypted secrets and login rate-limit binding.
- Assign least-privilege Firebase IAM and provision initial account/player documents.
- Choose production session expiry values under school/device policy.
- Deploy and measure Worker CPU/request usage.
- Complete Editor and hosted WebGL E2E before deleting the old launch-code files.
