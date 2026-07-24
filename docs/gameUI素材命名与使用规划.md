# gameUI 素材命名与使用规划 v0.1

> 目的：把 `all_aseet/gameUI` 中中文标注的参考图，备份为 Unity 更安全的英文文件名，并规划它们在游戏界面、内容展示和交互中的使用方式。

## 1. Unity 使用目录

英文备份图已复制到：

`F:\AI-project\ancientGame\ShouyouPrototype\ShouyouPrototype\Assets\_Project\Art\UI\DesignReferences`

这些图主要作为“设计参考图 / 临时 UI 背景 / 风格对照图”使用。正式切图后，可以再拆到：

- `Assets/_Project/Art/UI/CommonBg`
- `Assets/_Project/Art/UI/Buttons`
- `Assets/_Project/Art/UI/Frames`
- `Assets/_Project/Art/UI/Icons`
- `Assets/_Project/Art/UI/PageBackgrounds`

## 2. 命名原则

Unity 工程内尽量使用英文文件名，避免中文路径在脚本、Git、插件或打包时出现乱码。

命名格式建议：

`主题_模块_用途_序号.png`

示例：

- `dream_chapter_task_detail_reference_01.png`
- `reality_mail_ui_reference_01.png`
- `theme_button_compare_reality_dream.png`

## 3. 已备份素材清单

| 中文原名 | 英文备份名 | 建议用途 |
|---|---|---|
| 现实与梦域按钮样式对比组.png | theme_button_compare_reality_dream.png | 现实/梦域按钮风格对照 |
| 现实与梦域背景对比组.png | theme_background_compare_reality_dream.png | 双主题背景风格对照 |
| 现实梦域对照组.png | theme_compare_reality_dream.png | 现实/梦域整体风格基准 |
| 现实梦域章节任务组件对比图.png | chapter_task_component_compare_reality_dream.png | 主线/任务组件对照 |
| 现实内容卡片UI示意图.png | reality_content_card_ui_reference.png | 现实主题内容卡片 |
| 现实活动UI示意图.png | reality_activity_ui_reference.png | 现实主题活动页 |
| 现实版icon组.png | reality_icon_set.png | 现实主题图标参考 |
| 现实版二级反馈弹窗样式示意图.png | reality_secondary_popup_reference.png | 现实主题弹窗 |
| 现实版章节任务详情示意图.png | reality_chapter_task_detail_reference_01.png | 现实章节详情 |
| 现实版章节任务详情示意图2.png | reality_chapter_task_detail_reference_02.png | 现实章节详情备选 |
| 现实邮件UI示意图1.png | reality_mail_ui_reference_01.png | 现实邮件页 |
| 现实邮件UI示意图2.png | reality_mail_ui_reference_02.png | 现实邮件页备选 |
| 现实队伍框示意图比例不对待考虑.png | reality_team_frame_reference_ratio_pending.png | 现实编队框参考，比例待修 |
| 梦域通用背景.png | dream_common_background.png | 梦域通用整页背景 |
| 梦域版icon组.png | dream_icon_set.png | 梦域主题图标参考 |
| 梦域菜单tab示意UI图.png | dream_menu_tab_ui_reference.png | 梦域左侧/顶部 Tab |
| 梦域弹框与组件规范图.png | dream_popup_component_reference.png | 梦域弹窗与通用组件 |
| 梦域版章节任务详情示意图1.png | dream_chapter_task_detail_reference_01.png | 梦域章节详情 |
| 梦域版章节任务详情示意图2.png | dream_chapter_task_detail_reference_02.png | 梦域章节详情备选 |
| 梦域组队示意图.png | dream_formation_ui_reference.png | 梦域编队页 |
| 梦域队伍框示意图比例不对待考虑.png | dream_team_frame_reference_ratio_pending.png | 梦域编队框参考，比例待修 |
| 梦域活动展示UI示意图.png | dream_activity_ui_reference.png | 梦域活动页 |
| 梦域梦蝶赠礼示意图.png | dream_butterfly_gift_reference.png | 梦域赠礼/奖励页 |
| 梦域名士如梦示意图.png | dream_famous_character_reference.png | 梦域角色/名士展示 |
| 梦域日常签到示意图.png | dream_daily_signin_reference.png | 梦域签到页 |
| 梦域日常任务示意图1.png | dream_daily_task_reference_01.png | 梦域日常任务 |
| 梦域日常任务示意图2.png | dream_daily_task_reference_02.png | 梦域日常任务备选 |
| 梦域蝶域招灵示意图.png | dream_summon_spirit_reference.png | 梦域招灵/召唤 |
| 梦域行事示意图.png | dream_affairs_task_reference.png | 梦域行事/任务 |
| 梦域邮件UI.png | dream_mail_ui_reference.png | 梦域邮件页 |
| 如梦UI1.png | rumeng_ui_reference_01.png | 梦域氛围参考 |
| 如梦UI2.png | rumeng_ui_reference_02.png | 梦域氛围参考 |
| 如梦UI3.png | rumeng_ui_reference_03.png | 梦域氛围参考 |
| 如梦UI4.png | rumeng_ui_reference_04.png | 梦域氛围参考 |
| 进度与奖励样式规范示意图.png | progress_reward_style_reference.png | 任务进度和奖励组件 |

## 4. commonBg 使用策略

`commonBg` 是通用兜底资源池。

当外层文件夹找不到专门图片时，先用 `commonBg` 替代：

- 主背景：`commonbg3.png`、`梦域通用背景.png`
- 内容框：`commonbg_contentBorder.jpg`
- 大内容框：`commonbg_contentBorder1.jpg`
- 小卡片/按钮框：`commonbg_contentBorder2.jpg`
- 章节卡兜底背景：`commonbg4.png`、`commonbg5.png`、`commonbg6.png`、`commonbg7.png`

## 5. 页面展示规划

### 5.1 庭院首页

目标：温暖、陪伴、日常回归。

优先用现实主题资源：

- 现实内容卡片 UI
- 现实版 icon 组
- 现实与梦域按钮样式对比组中的现实按钮

交互重点：

- 进入主线
- 角色养成
- 领取资源
- 梦域伏笔入口

### 5.2 主线 / 剧情闯关页

目标：清楚展示章节、关卡、推荐战力、掉落。

第一版结构：

- 左侧栏目：剧情闯关、行迹编队、行迹养成、梦域养成活动
- 中间：章节标题 + 6 个场景关卡卡片
- 下方：前往挑战 / 回看剧情
- 右上：返回庭院

资源优先级：

1. `dream_chapter_task_detail_reference_01.png`
2. `chapter_task_component_compare_reality_dream.png`
3. `commonBg` 兜底背景

### 5.3 行迹编队页

目标：展示 6 人编队、气韵加成、助阵预留。

资源优先级：

1. `dream_formation_ui_reference.png`
2. `reality_team_frame_reference_ratio_pending.png`
3. `dream_team_frame_reference_ratio_pending.png`

交互重点：

- 点击空位选择角色
- 保存编队
- 查看气韵
- 后续预留助阵位，例如九天玄女

### 5.4 行迹养成页

目标：体现强数值成长。

第一版展示：

- 等级
- 境界 / 星级
- 技能
- 行迹节点
- 气韵 / 词意 / 神识等诗词化属性

资源优先级：

- 现实内容卡片 UI
- 梦域名士如梦示意图
- 进度与奖励样式规范示意图

### 5.5 梦域养成活动页

目标：强视觉冲击，体现梦域独立主题。

资源优先级：

1. `dream_common_background.png`
2. `dream_menu_tab_ui_reference.png`
3. `dream_popup_component_reference.png`
4. `dream_activity_ui_reference.png`
5. `dream_butterfly_gift_reference.png`

交互重点：

- 梦域入口
- 梦域任务
- 梦蝶赠礼
- 梦域签到
- 梦域活跃度

### 5.6 邮件页

现实邮件和梦域邮件可以共用功能，不同主题换皮肤。

资源：

- `reality_mail_ui_reference_01.png`
- `reality_mail_ui_reference_02.png`
- `dream_mail_ui_reference.png`

### 5.7 任务 / 签到 / 奖励页

第一版建议先做梦域主题，因为梦域奖励视觉更强。

资源：

- `dream_daily_task_reference_01.png`
- `dream_daily_task_reference_02.png`
- `dream_daily_signin_reference.png`
- `progress_reward_style_reference.png`

## 6. 交互规则规划

### 6.1 左侧 Tab

左侧栏目切换时，不要只是切文字，要同时变化：

- 背景图
- 主内容面板标题
- 卡片类型
- 按钮高亮状态

第一版可以先做静态切换，后续再接真实数据。

### 6.2 主题切换

现实 / 梦域采用同布局换皮肤：

- RealityTheme：米白、淡金、竹绿、柔粉
- DreamTheme：淡紫、雾蓝、月白、鎏金

切换内容：

- 页面背景
- 面板边框
- 按钮底图
- 文字颜色
- 选中态颜色

### 6.3 关卡卡片

关卡卡片采用场景卡。

必须显示：

- 关卡名
- 小标题
- 解锁状态
- 推荐等级 / 推荐战力
- 掉落预览

点击后进入：

- 关卡详情
- 剧情播放
- 编队确认
- 战斗

## 7. 资源取舍建议

当前优先使用：

1. commonBg 通用背景
2. 章节任务详情图
3. 编队示意图
4. 按钮样式对比图
5. 进度与奖励图

暂缓深入处理：

- 邮件页
- 活动商城
- 大量招灵/召唤图
- 比例不对的队伍框
- debug / cropped 临时图

## 8. 下一步开发建议

下一轮建议做：

1. 在 Unity 建立 `UIThemeConfig`，区分 Reality / Dream；
2. 主线页左侧栏目点击后，切换中间内容和背景；
3. 给 6 个关卡卡片加推荐战力和掉落图标占位；
4. 先做“关卡详情弹窗”，再进入剧情或战斗；
5. 所有新脚本继续写中文注释。

