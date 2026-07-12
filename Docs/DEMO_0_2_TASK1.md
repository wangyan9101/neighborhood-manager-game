# Demo 0.2 Task 1 - Configuration and Events

This batch updates the existing ScriptableObject content without changing UI, camera, reports, core architecture, or build settings.

## Balance Changes

- Initial budget: 2800
- Daily base income: 600
- High-satisfaction bonus: 200 at satisfaction 80+
- Low-satisfaction penalty: -200 below satisfaction 45
- Event spawn interval: 12-22 seconds
- Maximum active events: 4
- Worker duration multipliers: 1.0 matched, 1.6 mismatched

`LowSatisfactionPenalty` stores the positive penalty magnitude (`200`) because `ReportSystem` applies it as a negative budget change.

## Event Content

The five MVP events have updated costs, durations, timeouts, and resource effects. Four configuration assets are added: `ELEV_002`, `PARK_002`, `CHARGE_002`, and `GEN_002`, bringing the asset total to nine.

The active event pool is serialized on `GameRoot` in `Game_MVP`. This task intentionally does not modify the main scene. In Unity, add the four new assets from `Assets/_Project/Configs/Events/` to `GameRoot > Event Configs` before Play Mode validation.

## Applying Content

Run **Tools > Community Manager > Apply Demo 0.2 Config Content** to create/update the ScriptableObject assets through Unity Editor APIs. Existing `.meta` GUIDs are preserved; new assets receive GUIDs from Unity.
