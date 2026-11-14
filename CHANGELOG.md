# HS Trader Protection — Changelog

Works on 7 Days To Die **3.2, 3.3 and 3.4** with the same download. Install on the dedicated server and on every client, and keep them on the same version.

## 0.1.1

- **Runs on 7DTD 3.4.** Same download still runs on 3.2 and 3.3. It used to refuse to load on 3.4. 3.4 uses the 3.3 network and trader-lock path.

## 0.1.0

- **First release.** Admin `hs_tp start` at a trader compound: 7 in-game days of preparation (trader invulnerable, trading closed, HUD **RAID IN N DAYS hh:mm**), then a 24 in-game hour raid.
- **Protect volume off during the mission.** Players can build and break inside that compound, including boxing the trader in steel. The NPC still cannot take damage until the raid. Other traders stay protected. The shop prefab reloads at the end.
- **Charged waves use live vanilla zombies.** `zombieFatCopCharged` and `zombieSoldierCharged` are commented out in 7DTD and broke `entitygroups.xml` load.
- **HUD sits top-left**, not next to the buffs on the toolbelt.
- **Raid.** Trader takes zombie damage only. Waves escalate through feral, radiated, charged, infernal and demolisher. Zombies melee the trader. HUD shows wave, survival time, health bar, and a pulse from 75% of the objective.
- **End.** Compound prefab reloads, trader is restored, results screen shows team and player stats with exclusive kill buckets. Idle until an admin starts again.
- **JSON config.** `HSTraderProtectionSettings.json` next to the DLL (user-data copy in `HSTraderProtection` overrides it). Prep days, objective hours, wave table, caps, radii, HUD pulse.
- **Player guide.** `PLAYER_HANDBOOK.md` sits next to `Handbook.html` — same walkthrough. Admin/demo notes in `QUICKSTART.md`.
