# ⚔️ ETERNAL CLASH[cite: 5]

> **Genre:** 2D Auto-Scroller Action RPG[cite: 5]
> **Platform:** Mobile (iOS / Android) — Portrait Mode (optimized for 1-handed play)[cite: 5]
> **Inspiration:** Postknight[cite: 5]

---

## 📖 Overview

**Eternal Clash** puts players in the shoes of a young Courier belonging to the *Wandering Couriers Guild*, journeying across the war-torn continent of Aethelgard[cite: 5]. The core objective is to brave intense monster encounters to deliver goods, hand-written letters, and gifts to their beloved stranded in the distant capital[cite: 5].

---

## 🔄 Core Loop

**[In-Match Combat] ➔ [Accumulate Gold & Materials] ➔ [Return to Town] ➔ [Craft Gear & Send Letters] ➔ [Boost Bonds & Power]**[cite: 5]

**1. IN-MATCH (Action Loop):** 
The character automatically runs forward (+X)[cite: 5]. Players do not navigate movement; instead, they focus on timing 3 skill buttons to survive and complete deliveries (1–2 minutes per run)[cite: 5].

**2. OUT-OF-MATCH (Meta & Bonding):** 
Use Gold and Materials (Wood, Ore, Leather) at the Blacksmith to upgrade equipment[cite: 5]. Send letters and gifts to increase Bond points, unlocking story snippets and receiving permanent buffs[cite: 5].

---

## ✨ Unique Selling Propositions (USP)

*   **Bite-Sized Combat:** Each stage lasts 1–2 minutes, perfectly tailored for quick mobile gaming sessions[cite: 5].
*   **Visual Progression:** Upgrading equipment (from Wooden Sword to Iron Sword, Cloth Armor to Knight Armor) instantly updates the character's appearance in-game[cite: 5].
*   **Emotional Motivation:** Progression is driven by emotional connections via reply letters and gifts (e.g., Wild Daisy, Orc Fang Necklace)[cite: 5].

---

## 🎮 Combat Skills

*   **Charge:** (3.0s Cooldown) Dash forward (+5 Units) and deal 20 DMG[cite: 5]. Tactical role is to close the gap and interrupt ranged monsters[cite: 5].
*   **Shield:** (5.0s Cooldown) Pause for 1s, block 80% DMG, and reflect 5 DMG[cite: 5]. Tactical role is to block ranged projectiles or heavy boss charges[cite: 5].
*   **Potion:** (15.0s Cooldown) Instantly restore 50 HP[cite: 5]. Tactical role is an emergency save (limited uses per run)[cite: 5].

---

## 📊 Stat Progression

Each level-up grants **3 Stat Points** to distribute freely[cite: 5]:

*   **STR (Strength):** Grants +2 Physical Damage per point[cite: 5]. Ideal for a Clearing/Burst build[cite: 5].
*   **INT (Intelligence):** Grants +5% Bonus EXP Drop per point[cite: 5]. Ideal for a fast leveling build[cite: 5].
*   **VIT (Vitality):** Grants +15 Max HP and +2 HP recovery from Potions per point[cite: 5]. Ideal for a Tanker build[cite: 5].
*   **LUCK (Luck):** Grants +1% Rare Drop Rate and +0.5% Crit Rate per point[cite: 5]. Ideal for a material farming build[cite: 5].

---

## 🛠️ Technical Specs & Architecture

*   **Engine:** Unity 2D (Vector Art style)[cite: 5].
*   **Physics:** `BoxCollider2D`, Knockback mechanic triggered on hitbox collision[cite: 5].
*   **Movement:** Constant auto-translation along `Vector2.right (+X)` at a fixed speed[cite: 5].
*   **Monetization:** Rewarded Ads (2x Clear Rewards, Emergency Revival) & IAP (Remove-Ads Package, Gold/Gems)[cite: 5].
