# Demo 0.2 Task 2 - Core Rules and Settlement Data

This batch keeps the existing lightweight system flow and adds data for later UI work. It does not change UI scripts, prefabs, scenes, camera code, or build settings.

## Rules

- Daily income is calculated once from `GameBalanceConfig`: 800 at satisfaction 80+, 400 at 45 or below, otherwise 600.
- Budget failure requires budget at or below zero and at least one pending event while the game is actively playing.
- Dispatch duration is calculated by `DispatchSystem.CalculateHandleDuration`: 1.0 for a recommended worker and 1.6 otherwise.
- Dispatch cost remains a separate dispatch-time log and is not repeated in the completion resource delta.

## Result and Settlement Data

`ResourceDelta` records the actual applied change after resource limits. `ResourceDeltaFormatter` produces result text in budget, satisfaction, complaint, and facility-health order.

`DayReportModel` now exposes triggered reasons and up to three ordered suggestions. Only the income rules currently executed by the project are included as reasons; no new complaint or facility daily rules were introduced.

`GameResult` now includes the existing report grade. Completed and failed totals are sourced from saved day reports plus unreported current-day counts when a budget failure ends the game early.
