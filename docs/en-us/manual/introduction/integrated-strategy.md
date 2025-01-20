---
order: 7
---

# Integrated Strategy (I.S.)

::: important This page may be outdated.
:::

MAA selects the latest theme by default, and can be changed in `Auto I.S.` - `General`.

::: warning
All features involving auto-battle require a stable game frame rate of at least 60 frames, including but not limited to Copilot and Auto I.S..
:::

- Please pin the corresponding I.S. theme to the terminal of the game. Although automatic navigation is currently available, long-term usability is not guaranteed.
- If there is an exploration of non-target themes (such as if you plan to use MAA to auto Mizuki, but there is still an unfinished exploration of Phantom), please end it manually.
- MAA will not automatically select the difficulty. If the difficulty is not selected, it will get stuck/repeatedly enter and exit the difficulty selection interface.
- In the settings, you can choose the team, starting operator (only one operator name), etc.

## Recommended Starting Strategies

| Theme   | Difficulty           | Squad                                           | Strategy                      | Operators             | Notes                                                                                     |
| ------- | -------------------- | ----------------------------------------------- | ----------------------------- | --------------------- | ----------------------------------------------------------------------------------------- |
| Phantom | Formal Investigation | Leader Squad / Tactical Assault Squad           | Overcoming Your Weaknesses    | Thorns                |                                                                                           |
| Mizuki  | Surging Waves·3      | People-Oriented Squad / Mind Over Matter Squad  | Overcoming Your Weaknesses    | Gavial the Invincible | High-level builds work best with People-Oriented; Mind Over Matter requires a bit of luck |
| Sami    | Braving Nature·4     | Special Training Squad / Tactical Ranged Squad  | Slow and Steady Wins the Race | Wiš'adel              |                                                                                           |
| Sarkaz  | TODO TRANSLATION·4   | Blueprint Mapping Squad / Tactical Ranged Squad | Slow and Steady Wins the Race | Wiš'adel              | Selecting the Blueprint Mapping Squad results in a completely stealth-oriented strategy   |

What you might want to know: `Auto I.S.` → `Advanced` → `Select "Starting Operator" from support unit list`

## Battle Strategy

MAA does not have AI capabilities; all operations in automatic Integrated Strategy are pre-set strategies, and all stage battles use built-in job files.

For more details, please refer to the [Integrated Strategy Schema](../../protocol/integrated-strategy-schema.md).

- It supports automatic recognition of operators and proficiency and automatically selects better operators and skills.
- It supports identifying store items and prioritizes purchasing more powerful relics.

## Abnormal Detection

- It supports reconnection after disconnection and supports continuation to auto after 4 a.m. server reset.
- Encounters longer than 5 minutes will automatically retreat all ground units, and longer than 6 minutes will automatically abandon the operation to avoid wasting time/getting stuck.
- If the exploration gets stuck, it will automatically abandon and retry.

However, if it often gets stuck in a certain place and then gives up, seriously affecting efficiency, please feel free to submit an issue for feedback.
