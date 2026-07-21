# Unity 代码阅读小抄

这份文档解释当前两个核心脚本分别做什么。

## 1. 工程代码

位置：

`Assets/_Project/Scripts/UI/HomePageRouter.cs`

它是游戏运行时会用到的代码。

作用：

- 管理五个页面
- 点击底部导航时切换页面
- 游戏开始时默认显示庭院首页

核心逻辑：

```csharp
private void ShowOnly(GameObject target)
{
    SetActive(homePage, target == homePage);
    SetActive(characterPage, target == characterPage);
    SetActive(battlePage, target == battlePage);
    SetActive(storyPage, target == storyPage);
    SetActive(activityPage, target == activityPage);
}
```

意思是：

只显示目标页面，其他页面全部隐藏。

## 2. 编辑器脚本

位置：

`Assets/_Project/Scripts/Editor/HomeUILayoutBuilder.cs`

它只在 Unity 编辑器里运行，不是玩家手机里的游戏逻辑。

作用：

- 在顶部菜单生成 `Shouyou > UI > Clean And Rebuild Prototype`
- 一键生成首页 UI
- 一键生成角色、战斗、剧情、活动页面
- 自动绑定底部导航点击事件
- 为角色、战斗、剧情、活动页面生成可继续扩展的信息卡片
- 自动生成页面内的“返回庭院”按钮
- 自动绑定首页“进入主线”按钮

## 3. 点击底部导航时发生了什么

流程如下：

```text
玩家点击“角色”
-> Nav_Character 按钮触发 onClick
-> 调用 HomePageRouter.ShowCharacter()
-> ShowOnly(characterPage)
-> 角色页显示，其他页面隐藏
-> 角色按钮背景变成选中暖色
```

对应代码：

```csharp
BindNav(bottomNav, "Nav_Character", router, router.ShowCharacter);
```

这行代码是在编辑器里自动绑定按钮事件。

运行时的 `ShowOnly` 还会同步调用 `SetNavSelected`：当前按钮显示暖色，其他按钮恢复半透明浅色。

首页“进入主线”按钮的流程是：

```text
点击“进入主线”
-> Unity 按钮事件调用 HomePageRouter.EnterMainline()
-> EnterMainline() 调用 ShowBattle()
-> 战斗页显示，其他页面隐藏
```

二级页面的“返回庭院”按钮则会调用 `ReturnHome()`，回到庭院首页。

当前第 2 轮加入的 `BuildInfoCard` 是“页面骨架组件”。它负责先把标题、说明和入口按钮摆好，但还没有连接真正的角色数据、战斗数据或活动数据。

以后接入真实功能时，可以按照这个顺序替换：

1. 保留卡片的位置和名字；
2. 在 `Scripts/Data` 增加角色、关卡或活动数据；
3. 在 `Scripts/UI` 增加点击后的页面逻辑；
4. 最后把占位文字替换成数据驱动的文字和图片。

真正运行时执行的是：

```csharp
public void ShowCharacter()
{
    ShowOnly(characterPage);
}
```

## 4. 以后怎么区分两类代码

看文件夹：

- `Scripts/UI`：游戏运行代码
- `Scripts/Core`：核心系统代码
- `Scripts/Data`：数据结构代码
- `Scripts/Editor`：Unity 编辑器工具代码

一般来说：

- 玩家运行游戏时会用到 `Scripts/UI`
- 你在 Unity 里点菜单生成内容时，会用到 `Scripts/Editor`

## 5. 本轮你需要做什么

代码修改后，Unity 需要重新编译脚本。编译完成后，在顶部菜单点击：

`Shouyou > UI > Clean And Rebuild Prototype`

然后点击 Play。点击底部五个按钮时，页面会切换，并且当前按钮会显示暖色高亮。

## 6. 第 4 轮业务骨架

本轮加入的内容主要分为四类：

- `Scripts/Data/CharacterDefinition.cs`：角色数据结构，包含姓名、等级、词意、稀有度和定位。
- `Scripts/Data/BattleStageDefinition.cs`：关卡数据结构，包含推荐等级、体力消耗、完成状态和词意提示。
- `Scripts/Data/StoryDefinition.cs`：章节和场景数据结构，关联角色、背景、CG、战斗和互动选择。
- 角色页：增加角色属性条、养成入口和羁绊入口。
- 战斗页：增加第一章 6 个关卡节点，以及前排、后排六人编队展示位。
- 剧情页：增加主线、人物传记、梦境支线，以及跳过、回看、自动播放入口占位。
- 活动页：增加限时活动、角色召集、外观收集，以及倒计时和收集进度占位。
- 剧情页：章节卡片的“查看详情”会打开 `StoryDetailOverlay` 剧情详情弹窗。

剧情资料的整理结果见：`docs/story_content_catalog.md`。

剧情详情按钮的运行流程是：

```text
点击第一章“查看详情”
-> Unity Button.onClick
-> HomePageRouter.ShowChapterOneDetail()
-> StoryDetailOverlay 显示
-> 标题和正文 Text 被替换为第一章概要
```

“开始阅读”“跳过剧情”“回看剧情”目前是原型入口，已经有反馈文字；后续再接入逐句文本播放器和存档。

第 7 轮新增的交互包括：

- 角色卡片可以打开角色详情、养成说明和羁绊说明。
- 六人编队的六个位置可以点击，空位会显示后续角色选择入口。
- 第一章六个关卡节点可以点击，显示推荐等级、体力和解锁状态。
- 第三章详情弹窗增加 7 个场景节点，可以查看每个场景的概要。
- 第一关详情保留“进入战斗”业务入口，下一步接入真正的战斗场景。
- 详情弹窗中的“进入战斗”目前会显示战斗场景接入提示，不会误认为已经完成真实战斗系统。

这些内容目前是“业务界面骨架”，不是最终的真实功能。后续真正接入时，推荐顺序是：

```text
数据结构
-> 页面控制器
-> 按钮事件
-> 存档与服务器数据
-> 正式美术资源
```
