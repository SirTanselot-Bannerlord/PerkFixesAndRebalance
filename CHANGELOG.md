# Changelog

## 1.0.0

Initial Nexus release. See `NEXUS.md`.

## 1.0.1

- Clarify more Personal / Party Leader role labels (label-only, under **Clarify perk descriptions**): Warlord (battle influence), Famous Commander (battle renown + recruitment XP), Besieged (siege influence), Granary Accountant / Tradeyard Foreman (party trade sell penalties). See `docs/RoleLabelAudit-1.0.1.md`.
- Clarify Roguery 125 governor text (same option): White Lies is +1 relation with a random notable; Scarface is +1 relation with a random gang leader. Same daily roll, 2% vs 5%.
- Fix a crash when a battle loads for a party not in an army whose leader has Commodore, with **Army commander perks when alone** on. The perk read the flagship of parties with no ships (`PartyBase.FlagShip` throws). Also stop a lone party's flagship crew from getting the Lion figurehead morale twice.
- Fix a daily-tick crash when a ship is bought in a port whose Merchant Prince governor is not inside a town. The sale still happens; only that perk's gold is skipped.
- Fix Doctor's Oath surgery Medicine XP under RBM. RBM's `GetSurvivalChance` prefix drops vanilla's `OnSurgeryApplied`, and the fight hooks after that never saw the killer (siege removal has no affector; `OnCombatHit` checks health before the blow lands). Award 5 × troop tier from `GetAgentStateProbability`, the downed-troop roll RBM still calls, including mounted killers.
- Flexible Fighter also gives cavalry its +15 Bow, Crossbow and Throwing skill (same as infantry). Archers and horse archers are unchanged. Off in MCM restores vanilla, where cavalry get nothing. RBM auto-resolve uses the same split.

## 1.0.2

- Fix stutter in fought battles (1.0.1 regression). Without RBM, every downed troop re-ran a slow search of all loaded types. It now resolves once at game start. Commodore and Flexible Fighter lookups are cached and cheaper too.

## 1.0.3

- Localization support: every player-facing string (perk tooltips and MCM) has a translation key; English template ships under `ModuleData/Languages/`. Untranslated languages still show English. Thanks to OhMyMike57 / Cartoonist57.
