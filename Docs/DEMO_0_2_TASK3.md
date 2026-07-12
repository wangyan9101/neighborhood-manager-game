# Demo 0.2 Task 3 - UI Readability

This batch improves the existing UGUI and TextMeshPro presentation without changing gameplay values, event rules, camera behavior, or build settings.

## Presentation

- The top bar retains named resources and adds text warnings at dangerous values.
- Event items show urgency, category, countdown severity, state, recommended worker, cost, and success/failure summaries.
- Worker items show idle or working state, the current event, remaining work time, selected-event match text, and the authoritative expected duration queried from `GameSession`.
- Result logs receive visible dispatch, completion, or failure prefixes and retain the existing maximum of 20 messages.
- Daily settlement displays resource changes, triggered reasons, and ordered suggestions.
- Final settlement displays the final grade, totals, and final resources from `GameResult`.

## Layout

The UI remains embedded in `Game_MVP`; no second UI architecture or prefab set was introduced. Only event/worker item heights and modal dimensions were adjusted in the scene. The matching setup defaults in `CommunityManagerMvpSetup` were updated for future scene generation.
