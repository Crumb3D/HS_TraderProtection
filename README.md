# HS Trader Protection

**Player guide:** `PLAYER_HANDBOOK.md` in this folder, or open `Handbook.html` to print. Defend the trader: prep, raid waves, left HUD, results screen.

An admin starts a mission at a trader compound. Players prepare, then defend the trader through escalating zombie waves. The trader must survive **24 in-game hours** after the raid begins. Works on 7 Days To Die **3.2 and 3.3**. Same DLL on the client and the dedicated server.

Admin/demo notes: `QUICKSTART.md`.

## Commands

Type these in the in-game F1 console (permission 1000). On a dedicated server, join first.

```
hs_tp start              start preparation at the trader you are standing with
hs_tp stop               reload the compound, restore the trader, idle (no results screen)
hs_tp status             RAID IN N DAYS hh:mm during prep, or raid health / wave
hs_tp resetstats         clear stats while idle
hs_tp debug raid         skip remaining prep (testing)
hs_tp debug damage [n]   apply enemy-source damage (testing)
hs_tp debug kill         fake a lethal hit (testing)
```

## JSON config

`HSTraderProtectionSettings.json` next to this mod. A copy in the user data folder `HSTraderProtection` overrides it. The host file is the one that counts.

| Key | Default | Meaning |
|-----|---------|---------|
| PrepDays | 7 | In-game days of preparation |
| ObjectiveHours | 24 | In-game hours the trader must survive |
| WaveIntervalHours | 1 | In-game hours between waves |
| MaxAlive | 24 | Mission zombie cap |
| SpawnMinRadius / SpawnMaxRadius | 20 / 50 | Spawn ring around the trader |
| MissionRadius | 64 | Stats and teleport-suppression radius |
| PulseStartPercent | 75 | HUD pulse starts at this percent of the 24-hour objective |
| SuppressTeleport | true | Do not eject players from the compound during the mission |
| SpawnsPerTick | 2 | Zombies created per server tick |
| Bands | (see file) | Wave composition by raid hour |

## Play

1. Stand in a trader compound and run `hs_tp start`.
2. Preparation: trader stays invulnerable, trading is closed, HUD counts down **RAID IN N DAYS hh:mm**.
3. Raid: trader takes zombie damage only. Survive 24 in-game hours.
4. On success or death the compound is rebuilt, the trader is restored, and the results screen opens.
5. Close the results screen. The mod sits idle until an admin starts again.

Build defences **outside** the vanilla trader protect volume. The shop itself cannot be damaged or built in.

See `CHANGELOG.md` for what changed in each version.
