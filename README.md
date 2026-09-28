# ⚔️ ETERNAL CLASH

> **Genre:** 2D Auto-Scroller Action RPG
> **Platform:** Mobile (iOS / Android) — Portrait Mode (optimized for 1-handed play)
> **Inspiration:** Postknight

---

## 📖 Overview

**Eternal Clash** puts players in the shoes of a young Courier belonging to the *Wandering Couriers Guild*, journeying across the war-torn continent of Aethelgard. The core objective is to brave intense monster encounters to deliver goods, hand-written letters, and gifts to their beloved stranded in the distant capital.

---

## 🔄 Core Loop

**[In-Match Combat] ➔ [Accumulate Gold & Materials] ➔ [Return to Town] ➔ [Craft Gear & Send Letters] ➔ [Boost Bonds & Power]**

**1. IN-MATCH (Action Loop):** 
The character automatically runs forward (+X). Players do not navigate movement; instead, they focus on timing 3 skill buttons to survive and complete deliveries (1–2 minutes per run).

**2. OUT-OF-MATCH (Meta & Bonding):** 
Use Gold and Materials (Wood, Ore, Leather) at the Blacksmith to upgrade equipment. Send letters and gifts to increase Bond points, unlocking story snippets and receiving permanent buffs.

---

## ✨ Unique Selling Propositions (USP)

*   **Bite-Sized Combat:** Each stage lasts 1–2 minutes, perfectly tailored for quick mobile gaming sessions.
*   **Visual Progression:** Upgrading equipment (from Wooden Sword to Iron Sword, Cloth Armor to Knight Armor) instantly updates the character's appearance in-game.
*   **Emotional Motivation:** Progression is driven by emotional connections via reply letters and gifts (e.g., Wild Daisy, Orc Fang Necklace).

---

## 🎮 Combat Skills

*   **Charge:** (3.0s Cooldown) Dash forward (+5 Units) and deal 20 DMG. Tactical role is to close the gap and interrupt ranged monsters.
*   **Shield:** (5.0s Cooldown) Pause for 1s, block 80% DMG, and reflect 5 DMG[cite: 5]. Tactical role is to block ranged projectiles or heavy boss charges.
*   **Potion:** (15.0s Cooldown) Instantly restore 50 HP. Tactical role is an emergency save (limited uses per run).

---

## 📊 Stat Progression

Each level-up grants **3 Stat Points** to distribute freely:

*   **STR (Strength):** Grants +2 Physical Damage per point. Ideal for a Clearing/Burst build.
*   **INT (Intelligence):** Grants +5% Bonus EXP Drop per point. Ideal for a fast leveling build.
*   **VIT (Vitality):** Grants +15 Max HP and +2 HP recovery from Potions per point. Ideal for a Tanker build.
*   **LUCK (Luck):** Grants +1% Rare Drop Rate and +0.5% Crit Rate per point. Ideal for a material farming build.

---

## 🛠️ Technical Specs & Architecture

*   **Engine:** Unity 2D (Vector Art style).
*   **Physics:** `BoxCollider2D`, Knockback mechanic triggered on hitbox collision.
*   **Movement:** Constant auto-translation along `Vector2.right (+X)` at a fixed speed.
*   **Monetization:** Rewarded Ads (2x Clear Rewards, Emergency Revival) & IAP (Remove-Ads Package, Gold/Gems).
