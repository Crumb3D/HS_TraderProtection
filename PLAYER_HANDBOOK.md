# HS Trader Protection — Player Handbook

**Defend the trader**

An admin starts a mission at a trader compound. You get in-game days to build, then the raid hits. Keep the trader alive for **24 in-game hours**. Trading is closed until it is over.

A printable copy is `Handbook.html` in this mod folder (open in a browser, then Print / save as PDF).

Works on 7 Days To Die **3.2 and 3.3**. Load this mod on the dedicated server and every client.

---

## How a mission starts

Someone with admin rights stands **inside a trader compound** and types `hs_tp start` in the F1 console.

You do not start this yourself. You wait for the HUD in the **top left**.

The shop NPC stays. The **vanilla protect volume is off** for this compound until the mission ends: you can smash shop blocks and **wall the trader in with steel** if you want. The trader still cannot take damage until the raid starts. Other traders stay protected.

---

## Preparation

Default: **7 in-game days**.

- The top-left HUD says **PROTECTED** and counts down **RAID IN N DAYS hh:mm**.
- The trader cannot take damage.
- Trading is closed. Looking at the trader says they are busy with the protection mission.
- You are not kicked out of the compound (teleport-out is off while the mission runs).

Use the time. Walls, turrets, traps, ammo, meds. Box the doorway, wrap the trader in steel, whatever you like. The shop prefab reloads when the mission ends, so anything you built **inside** the vanilla shop is wiped. Steel you put **around** the compound stays until you tear it down (or the raid does).

`hs_tp status` on the server also prints the same countdown.

---

## The raid

When the timer hits zero the HUD flips to **UNDER ATTACK**.

- The trader can take **zombie damage only**. Players, turrets, and traps do not hurt the trader.
- Zombies spawn in a ring around the trader (default 20–50 blocks) and will melee the trader, not only the players.
- Waves step up about once per in-game hour, for 24 hours:

| Raid hours | Mix |
| --- | --- |
| 0–4 | Normal |
| 4–8 | Normal + feral |
| 8–12 | Feral + radiated |
| 12–16 | Radiated + charged (rare demolisher) |
| 16–20 | Charged + infernal |
| 20–24 | Charged + infernal + demolisher |

The HUD shows the wave, time until the next wave, **TRADER SURVIVED FOR hh:mm**, and a health bar. After **75%** of the 24-hour objective the panel starts to pulse. It gets stronger the closer you are to the end.

Cap: 24 mission zombies alive at once (JSON `MaxAlive`). Extra spawns wait until something dies.

---

## Winning and losing

**Success:** the trader is still up after 24 in-game hours.

**Failure:** a zombie lands a lethal hit, or the trader vanishes.

Either way:

1. Mission zombies are cleared.
2. The trader compound prefab reloads (vanilla shop blocks come back).
3. The trader is restored (full health, god mode back to normal).
4. The **results screen** opens for everyone.

Close the results screen. The mod sits idle until an admin starts another mission.

`hs_tp stop` during prep or raid reloads the compound and goes idle **without** a results screen.

---

## Results screen

Lead block: survival time, ending health, damage taken, waves survived, whether the 24-hour objective was met, cause of death, final attacker.

Team totals, then per-player lines. Kill buckets are **exclusive** — a demolisher is only a demolisher, not also infernal / charged / radiated / feral.

| Stat | Meaning |
| --- | --- |
| Total / player / turret / trap kills | Who got the last hit |
| Normal, feral, radiated, charged, infernal, demolisher | Exclusive type buckets |
| Headshots | Head hits that counted |
| Ammo fired / hits | Shots while the mission is running |
| Player deaths | Deaths inside the mission radius |
| Defence blocks | Placed / upgraded / destroyed around the compound |

There is no per-zombie kill feed. One kill, one bucket.

---

## What you cannot do

- Trade until the mission is idle again.
- Hurt the trader yourself (or with your turrets).
- Keep player-built walls **inside** the vanilla shop prefab after the reload — that part of the compound is restored.
- Start a second mission while one is running, or while the results screen is still up.

---

## Quick tips

- Nail the outer ring first. Demolishers show up late.
- Turrets and traps count on the results screen. Use them.
- The top-left HUD stays up for the whole mission. If you do not see it, this folder is missing from **your** Mods as well as the server.
- After an update, restart the **matching dedicated server and the matching client**.
