# UI 图标资源映射

## 原始与处理目录

- 原始素材：`F:/AI-project/shouyou/all_aseet/operate_icon`
- 原图备份：`F:/AI-project/shouyou/all_aseet/operate_icon/originals_backup_v1`
- 用户处理素材：`F:/AI-project/shouyou/all_aseet/operate_icon/processed_v2`
- Unity 透明 PNG：`F:/AI-project/shouyou/all_aseet/operate_icon/processed_v2_unity`
- Unity 使用目录：`Assets/_Project/Art/UI/OperateIcons`
- 任务详情原图目录：`Assets/_Project/Art/UI/TaskSetting`
- 主线与编队参考图：`Assets/_Project/Art/UI/References`
- 梦域参考图：`Assets/_Project/Art/UI/References/DreamDomain`
- 主线/编队背景素材：`Assets/_Project/Art/UI/TabNavBackgrounds`

## Unity 资源命名

| Unity 文件 | 用途 | 尺寸 |
|---|---|---:|
| `nav_home.png` | 底部庭院按钮 | 1024×512 |
| `nav_character.png` | 底部角色按钮 | 1024×512 |
| `nav_battle.png` | 底部战斗按钮 | 1024×512 |
| `nav_story.png` | 底部剧情按钮 | 1024×512 |
| `nav_activity.png` | 底部活动按钮 | 1024×512 |
| `common-button.png` | 通用文字按钮底图：进入主线、查看详情、返回庭院等 | 1708×796 |
| `icon_setting.png` | 顶部设置 | 512×512 |
| `icon_avatar.png` | 顶部头像 | 512×512 |
| `icon_gold.png` | 铜钱资源图标，暂未替换文字 | 512×512 |
| `icon_jade.png` | 玉资源图标，暂未替换文字 | 512×512 |
| `icon_notification.png` | 通知/邮件入口备用图标 | 512×512 |

## 使用原则

导航图片已经包含按钮边框和中文，所以 Unity 不再额外叠加导航文字。

`common-button.png` 不包含文字，Unity 会在它上面创建 `Label`，文字颜色统一使用 `font-color.txt` 中的按钮文字色 `#C99A5A`。原始 JPG/PNG 的黑色外部底色已转为透明，Unity 中使用 `Image.Type.Sliced`，避免按钮文字变长时拉坏边框。

底部导航按照 UI 参考图使用约 `250×108` 的胶囊比例；普通功能按钮和底部导航是两套资源，不共用尺寸。

任务详情素材目前按原图整组放入 `TaskSetting`，没有切图、抠图或压缩，后续由美术处理后再决定哪些作为背景、面板或参考图。

主线关卡页、编队页和梦域页参考图也按原图保存，当前只作为设计参考，不直接覆盖 Unity 的运行时 UI。

`TabNavBackgrounds` 中的 `bg1` 至 `bg4` 是页面背景，`章节背景框.png` 是章节卡片框，带字按钮和通用按钮分别用于已有文字按钮与后续代码动态配字按钮。

圆形图标使用透明边缘，适合放在头像、设置和资源区域。

原始图片和用户的 `processed_v2` 都没有被覆盖；Unity 使用的 PNG 是从 `processed_v2` 转换得到的透明版本，黑色背景已转为透明。
