# Claude Next Tasks - Assets API and Battle Data

Owner: Claude Code / backend
Consumer: Codex / Unity frontend
Status: draft for next backend round

## Context

Unity battle demo now uses text placeholders for skill buttons. The frontend can later replace these labels with backend-provided icon URLs through stable iconKey values.

## Task 1: AssetsAPI-R2 - skill icon contract

Please make sure `GET /api/v1/assets` supports the following frontend use cases:

1. Query one icon by key:
   - `GET /api/v1/assets?iconKey=skill_basic_attack`
   - `GET /api/v1/assets?iconKey=skill_poetry_attack`
   - `GET /api/v1/assets?iconKey=skill_group_damage`
   - `GET /api/v1/assets?iconKey=skill_heal`

2. Query a category list:
   - `GET /api/v1/assets?category=skill_icon`

3. Response shape should be stable:

```json
{
  "iconKey": "skill_poetry_attack",
  "displayName": "poetry attack",
  "url": "/assets/icons/skill_poetry_attack.png",
  "category": "skill_icon",
  "_placeholder": false,
  "version": 1,
  "width": 256,
  "height": 256
}
```

If an icon file is missing, keep the same shape but use `url: null` and `_placeholder: true`.

## Task 2: BattleDataAPI-R1 - demo battle config

Later the Unity battle demo should stop hardcoding battle values. Please prepare or plan one endpoint:

- `GET /api/v1/battle/demo-config`

Minimum fields needed by Unity:

```json
{
  "stageId": "1-1",
  "maxActionPoint": 3,
  "allies": [
    { "id": "liqingzhao", "name": "Li Qingzhao", "slot": 1, "hp": 1200, "attack": 160, "portraitIconKey": "char_liqingzhao" },
    { "id": "wanhe", "name": "Wanhe", "slot": 2, "hp": 1100, "attack": 140, "portraitIconKey": "char_wanhe" }
  ],
  "enemies": [
    { "id": "dream_shadow_1", "name": "Dream Shadow", "slot": 1, "hp": 520, "attack": 70, "portraitIconKey": "enemy_shadow" }
  ],
  "skills": [
    { "id": "basic", "label": "Basic", "iconKey": "skill_basic_attack", "target": "enemy_single", "multiplier": 1.0, "cooldown": 0 },
    { "id": "poetry_strike", "label": "Poetry Strike", "iconKey": "skill_poetry_attack", "target": "enemy_single", "multiplier": 1.8, "cooldown": 2 },
    { "id": "dream_area", "label": "Dream Area", "iconKey": "skill_group_damage", "target": "enemy_all", "multiplier": 0.75, "cooldown": 3 },
    { "id": "healing_verse", "label": "Healing Verse", "iconKey": "skill_heal", "target": "ally_lowest_hp", "multiplier": 1.2, "cooldown": 3 }
  ]
}
```

## Task 3: Characters and formation

Please verify whether `char_wanhe` is exposed consistently in:

1. `/api/v1/assets?iconKey=char_wanhe`
2. character list endpoint, if available
3. player formation/default party data, if available

The user reported that Wanhe did not appear in battle earlier. If backend already returns her, document which endpoint Unity should consume.

## Task 4: Suggested tests for Claude

1. Assets endpoint returns a valid object for every skill iconKey above.
2. Missing icon files return `_placeholder: true`, not 404, unless the route itself is invalid.
3. Category query returns an array/list of skill icons.
4. If battle config endpoint is implemented, it contains Li Qingzhao and Wanhe in the ally list.
5. Backend tests should not require Unity Editor.
