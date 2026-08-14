# Outside Tester Recruitment Plan (Phase 2b)

Draft only. Not active. `PROJECT_SPEC.md` §9 is explicit: *"No outside testers, public demos, or marketing while the graybox still feels unfinished"* and Phase 2b stays deferred *"until Blake approves the graybox for sharing."* This document exists so recruitment can start the same day that approval happens, instead of being designed from scratch under time pressure. Nothing here authorizes contacting anyone yet; contacting outside parties is an ask-before action per `PROJECT_SPEC.md` §11 regardless of this plan.

## Target tester profile

Straight from `PROJECT_SPEC.md` §3: mobile gamers who clicked or installed a horde-shooter advertisement, expected the advertised lane-battle mechanic, and felt deceived when the downloaded game was something else. Recruitment should target people who match that frustration, not just "anyone who plays mobile games." A generic mobile gamer won't validate the thing this project actually needs validated, which is whether the honest version of that ad-promised loop holds up.

## Success gate (already defined, reproduced here for reference)

- At least 7 of 10 testers finish Level 1.
- At least 5 of 10 replay without being asked to.
- Full production stays blocked until both thresholds clear.

## Recruitment channels, ranked by fit

1. **Reddit communities that specifically discuss ad-bait mobile games.** r/incremental_games, r/AndroidGaming, r/mobilegaming, and threads about "hyper-casual ad games" tend to already contain the exact frustrated audience this game is aimed at. A short, honest post ("made the game the ad promised, want honest feedback") fits that culture better than a generic playtest call.
2. **r/playmygame and r/IndieDev.** Built specifically for early playtest recruitment; audience expects graybox/prototype-quality builds and knows to give structured feedback rather than App Store review energy.
3. **Indie dev Discord servers with playtesting channels** (e.g. general indie-dev servers, Unity-specific servers). Higher signal than Reddit for engaged, repeat feedback, but smaller reach; good for the first 2 to 3 testers before a wider call.
4. **Personal network, one hop out.** People Blake knows who play mobile games but have no stake in the project's success, explicitly not close friends/family who'll be positively biased. Two or three of the ten testers from this channel keeps the sample from being 100% strangers.
5. **Google Play Closed Testing track / TestFlight, once package/bundle identity is finalized.** Cleanest distribution mechanism once it's usable (real install flow, real device diversity), but blocked on the still-open publisher identity and package ID decisions in `PROJECT_SPEC.md` §10, so it's a later-wave channel, not a first-wave one.

Recommend running channels 1 to 4 for the first wave (5 to 10 minutes each was already an approved-vs-real-device gap for this build; sideloaded APK plus a short form is the fastest path to the first 10 responses), and adding channel 5 only if a second validation wave is needed.

## Outreach message template

> Made a mobile game that's an honest version of one of those "ad shows one game, download is a different game" ads. Looking for 10 people to play the first level (60 to 90 seconds) and tell me honestly if you'd play a second one. Android sideload APK, takes under 5 minutes, no account needed, nothing collected but what you tell me in a short form after. [link when ready]

Keep it short and specifically call out "honest feedback wanted" since the target testers are people burned by dishonest marketing; overselling the game to them undermines the exact thing being tested.

## Consent and privacy

- No account, no PII collection. The results sheet below is anonymous by design (no name/email field).
- State plainly in the outreach message that the build is a prototype/graybox, not a finished game, so feedback is calibrated correctly (visual polish isn't the thing being tested yet).
- If a tester volunteers contact info for follow-up questions, store it separately from the results sheet, not merged into it.

## Anonymous results sheet (fields)

One row per tester. Suggested as a simple spreadsheet or form, not a new code deliverable.

| Field | Type | Notes |
|---|---|---|
| Tester ID | Auto-increment (T01 to T10) | No name, no email |
| Device model | Free text | e.g. "Pixel 8," "Galaxy S23" |
| Android OS version | Free text | |
| Completed Level 1? | Yes / No | Primary success metric |
| Replayed without being asked? | Yes / No | Primary success metric |
| Total time played (minutes) | Number | Self-reported or observed |
| Furthest level reached | Number (1 to 10) | |
| "Did this feel like the ad you expected?" | 1 to 5 scale | Directly tests the core positioning from `PROJECT_SPEC.md` §2 |
| "Would you play a second run right now?" | Yes / No | Proxy for the voluntary-replay metric if not directly observed |
| Biggest frustration (free text) | Free text | |
| Anything that broke / crashed | Free text | Bug triage input, separate from the qualitative feedback above |
| One thing to change first | Free text | Forces a single prioritized suggestion instead of a scattershot list |

## After the first wave

Tally the two binary metrics (Level 1 completion, voluntary replay) against the 7/10 and 5/10 thresholds before reading anything else. If either threshold fails, treat the free-text answers as the diagnosis, not as a separate to-do list to work through in parallel; the qualitative feedback exists to explain the miss, not to compete with it for priority.
