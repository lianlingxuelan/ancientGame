# Battle Asset English Backup

本目录是战斗页可直接使用的英文命名资源备份，来源为：

`F:\AI-assets\ancientGame-assets\all_aseet\battle_cg`

## 背景图

| 英文文件名 | 原始文件名 | 用途 |
| --- | --- | --- |
| `battle_bg_first.png` | `first_battle.png` | 第一优先级战斗背景 |
| `battle_bg_second.png` | `second_battle.png` | 第二优先级战斗背景 |
| `battle_bg_variant_01.png` | `古风双人手游CG美术专项研究项目方案.png` | 备用战斗背景 |
| `battle_bg_variant_02.png` | `古风双人手游CG美术专项研究项目方案 (1).png` | 备用战斗背景 |
| `battle_bg_variant_03.png` | `古风双人手游CG美术专项研究项目方案 (2).png` | 备用战斗背景 |
| `battle_layout_reference.png` | `布局展示示意图.png` | 战斗布局参考图 |

## 头像与技能

| 英文文件名 | 原始文件名 | 用途 |
| --- | --- | --- |
| `ally_portrait_01.png` | `人物头像1.png` | 我方头像 1 |
| `ally_portrait_02.png` | `人物头像2.png` | 我方头像 2 |
| `ally_portrait_03.png` | `人物头像3.png` | 我方头像 3 |
| `ally_portrait_04.png` | `人物头像4.png` | 我方头像 4 |
| `enemy_portrait_01.png` | `对敌头像1.png` | 敌方头像 1 |
| `enemy_portrait_02.png` | `对敌头像2.png` | 敌方头像 2 |
| `enemy_portrait_03.png` | `对敌头像3.png` | 敌方头像 3 |
| `skill_button_01.png` | `底部技能按钮1.png` | 底部技能按钮 1 |
| `skill_button_02.png` | `底部技能按钮2.png` | 底部技能按钮 2 |
| `skill_button_03.png` | `底部技能按钮3.png` | 底部技能按钮 3 |
| `skill_button_04.png` | `底部技能按钮4.png` | 底部技能按钮 4 |
| `battle_element_group.png` | `battle_element_group.png` | 战斗 UI 元素集合 |

## 尺寸处理原则

- `battle_bg_first.png` 和 `battle_bg_second.png` 是 `2848x1600`，比例约 `16:9`，适合直接作为 1920x1080 横屏背景使用。
- Unity 页面使用 1920x1080 Canvas 时，不需要提前压缩裁切；先保持原图导入，运行时按容器缩放。
- 头像和技能按钮是 `2048x2048` 正方形图，适合用在正方形头像框、技能按钮、敌我单位头像占位。
