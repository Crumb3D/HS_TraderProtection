# HS Trader Protection — YouTuber quick start (admin)

Drop the **HS_TraderProtection** folder into `Mods`. Needs **Harmony** (`0_TFP_Harmony`, ships with 7DTD).

On a **dedicated server**, the same folder must be in the **server Mods** and in **every player’s Mods**. JSON is host-side: `HSTraderProtectionSettings.json` next to the DLL, or a copy in `%AppData%\Roaming\7DaysToDie\HSTraderProtection\` on the host.

Restart the matching dedicated server and the matching client after install.

Works on 7 Days To Die **3.2 and 3.3**. Same DLL.

---

## Start a mission (F1 console)

Permission 1000. On a dedicated server, join first, then type these **in-game**.

```
hs_tp start
hs_tp status
hs_tp stop
hs_tp resetstats
```

Stand **inside a trader compound**, then `hs_tp start`. The HUD on the left of the toolbelt says **PROTECTED** and **RAID IN N DAYS hh:mm**.

Optional: `hs_tp start 12345` with a trader entity id if you are not standing in the area.

---

## Demo in minutes (skip prep)

1. `hs_tp start` at a trader.
2. `hs_tp debug raid` — raid starts now.
3. Watch the HUD: **UNDER ATTACK**, wave timer, survival clock, health bar.
4. `hs_tp debug damage 200` — apply enemy-source damage (testing).
5. `hs_tp debug kill` — fake a lethal hit, then the compound reloads and the results screen opens.

Do not use the debug commands on a live player world unless you mean it.

---

## What to show on camera

- Prep HUD countdown (**RAID IN N DAYS**).
- Looking at the trader: trading is closed.
- Building **inside** the compound (protect volume is off). Wall the trader in steel if you want. He still cannot take damage until the raid.
- Raid: zombies walking into the trader, not only the players.
- Health bar, then the pulse after 75% of the 24-hour fight.
- Results: SUCCESS / FAILURE, team buckets, per-player K / Dmg / HS / ammo.

---

## JSON that matters for a take

`HSTraderProtectionSettings.json` next to the DLL (host file wins):

| Key | Default | Demo tip |
| --- | --- | --- |
| PrepDays | 7 | Set to 1 if you want a short wait without `debug raid` |
| ObjectiveHours | 24 | Leave at 24 for a real fight; drop it only for a cut |
| WaveIntervalHours | 1 | 1 in-game hour per wave |
| MaxAlive | 24 | Lower if the box is tiny |
| PulseStartPercent | 75 | HUD pulse starts here |
| Bands | (see file) | Wave mix by raid hour |

Restart the **server** after you change the JSON.

---

## Admin extras

| Command | What |
| --- | --- |
| `hs_tp` | Help |
| `hs_tp start` | Preparation at the trader you are with |
| `hs_tp stop` | Reload compound, restore trader, idle (no results) |
| `hs_tp status` | Countdown, or raid health / wave |
| `hs_tp resetstats` | Clear stats while idle |
| `hs_tp debug raid` | Skip remaining prep |
| `hs_tp debug damage [n]` | Enemy-source damage |
| `hs_tp debug kill` | Fake lethal hit |

Full write-up: `PLAYER_HANDBOOK.md` / `Handbook.html`.
