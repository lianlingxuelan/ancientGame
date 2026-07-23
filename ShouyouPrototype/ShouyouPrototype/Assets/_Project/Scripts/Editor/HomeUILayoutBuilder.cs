using Shouyou.UI;
using Shouyou.UI.Theme;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Shouyou.EditorTools
{
    // 首页 UI 自动生成工具。
    // 这个类只在 Unity 编辑器里运行，用来帮我们一键生成页面、按钮和布局。
    // 它不是玩家手机上运行的游戏逻辑，所以放在 Scripts/Editor 目录下。
    public static class HomeUILayoutBuilder
    {
        // 这里集中管理第一版 UI 的颜色，后面想改整体风格时，优先改这里。
        private static readonly Color InkColor = new Color32(70, 48, 36, 255);
        private static readonly Color TopBarColor = new Color32(255, 244, 230, 150);
        private static readonly Color BottomBarColor = new Color32(255, 236, 214, 180);
        private static readonly Color SoftPanelColor = new Color32(255, 248, 236, 190);
        private static readonly Color ButtonColor = new Color32(238, 190, 125, 215);
        private static readonly Color PagePanelColor = new Color32(255, 248, 236, 210);
        // 通用按钮文字颜色，来自 operate_icon/font-color.txt 的“按钮文字”。
        private static readonly Color ButtonTextColor = new Color32(201, 154, 90, 255);

        // Unity 顶部菜单入口：Shouyou > UI > Clean And Rebuild Prototype。
        // 点击后会清理旧 UI，然后重新生成首页、五个页面和底部导航。
        [MenuItem("Shouyou/UI/Clean And Rebuild Prototype")]
        public static void CleanAndRebuildPrototype()
        {
            Canvas canvas = FindOrCreateCanvas();
            Sprite backgroundSprite = GetBackgroundSprite(canvas.transform);

            // 先删除旧节点，避免之前手动创建的对象残留，导致重复或报错。
            DestroyChildIfExists(canvas.transform, "BG_MainCourtyard");
            DestroyChildIfExists(canvas.transform, "CenterPanel");
            DestroyChildIfExists(canvas.transform, "TopBar");
            DestroyChildIfExists(canvas.transform, "PageRoot");
            DestroyChildIfExists(canvas.transform, "BottomNav");

            // 重新创建主背景，并尽量保留之前绑定过的庭院图片。
            Image bg = FindOrCreateImage(canvas.transform, "BG_MainCourtyard");
            StretchFull(bg.rectTransform);
            bg.sprite = backgroundSprite;
            // 背景只负责显示，不参与点击，避免挡住下面的 UI 按钮。
            bg.raycastTarget = false;
            // 明确把庭院背景放到 Canvas 的最底层，避免被页面容器盖住。
            bg.transform.SetAsFirstSibling();

            // 分三块生成：顶部栏、页面区、底部导航栏。
            RectTransform topBar = BuildTopBar(canvas.transform);
            RectTransform pageRoot = BuildPages(canvas.transform);
            RectTransform storyDetail = BuildStoryDetail(canvas.transform);
            RectTransform bottomNav = BuildBottomNav(canvas.transform);

            // 固定显示层级：背景 < 页面 < 顶栏 < 底栏。
            bg.transform.SetSiblingIndex(0);
            pageRoot.SetSiblingIndex(1);

            // 给 Canvas 挂上运行时页面路由脚本，让按钮点击后可以切页面。
            HomePageRouter router = canvas.GetComponent<HomePageRouter>();
            if (router == null)
            {
                router = canvas.gameObject.AddComponent<HomePageRouter>();
            }

            // 自动把五个页面对象绑定到 HomePageRouter 的 private 字段上。
            SerializedObject serializedRouter = new SerializedObject(router);
            SetObject(serializedRouter, "homePage", pageRoot.Find("Page_Home").gameObject);
            SetObject(serializedRouter, "characterPage", pageRoot.Find("Page_Character").gameObject);
            SetObject(serializedRouter, "battlePage", pageRoot.Find("Page_Battle").gameObject);
            SetObject(serializedRouter, "storyPage", pageRoot.Find("Page_Story").gameObject);
            SetObject(serializedRouter, "activityPage", pageRoot.Find("Page_Activity").gameObject);
            SetObject(serializedRouter, "mainlineChapterPage", pageRoot.Find("Page_MainlineChapter").gameObject);
            SetObject(serializedRouter, "formationPage", pageRoot.Find("Page_Formation").gameObject);
            SetObject(serializedRouter, "dreamDomainPage", pageRoot.Find("Page_DreamDomain").gameObject);
            SetObject(serializedRouter, "mainlineStoryTab", pageRoot.Find("Page_MainlineChapter/MainlinePanel/Tab_Story").gameObject);
            SetObject(serializedRouter, "mainlineFormationTab", pageRoot.Find("Page_MainlineChapter/MainlinePanel/Tab_Formation").gameObject);
            SetObject(serializedRouter, "mainlineTrainingTab", pageRoot.Find("Page_MainlineChapter/MainlinePanel/Tab_Training").gameObject);
            SetObject(serializedRouter, "mainlineDreamActivityTab", pageRoot.Find("Page_MainlineChapter/MainlinePanel/Tab_DreamActivity").gameObject);
            SetObject(serializedRouter, "mainlineStoryCategoryButton", pageRoot.Find("Page_MainlineChapter/MainlinePanel/Category_Story").GetComponent<Button>());
            SetObject(serializedRouter, "mainlineFormationCategoryButton", pageRoot.Find("Page_MainlineChapter/MainlinePanel/Category_Formation").GetComponent<Button>());
            SetObject(serializedRouter, "mainlineTrainingCategoryButton", pageRoot.Find("Page_MainlineChapter/MainlinePanel/Category_Training").GetComponent<Button>());
            SetObject(serializedRouter, "mainlineDreamActivityCategoryButton", pageRoot.Find("Page_MainlineChapter/MainlinePanel/Category_DreamActivity").GetComponent<Button>());

            // 自动绑定五个底部导航按钮，让运行时可以同步显示当前选中状态。
            SetObject(serializedRouter, "homeNavButton", bottomNav.Find("Nav_Home").GetComponent<Button>());
            SetObject(serializedRouter, "characterNavButton", bottomNav.Find("Nav_Character").GetComponent<Button>());
            SetObject(serializedRouter, "battleNavButton", bottomNav.Find("Nav_Battle").GetComponent<Button>());
            SetObject(serializedRouter, "storyNavButton", bottomNav.Find("Nav_Story").GetComponent<Button>());
            SetObject(serializedRouter, "activityNavButton", bottomNav.Find("Nav_Activity").GetComponent<Button>());
            SetObject(serializedRouter, "storyDetailPanel", storyDetail.gameObject);
            SetObject(serializedRouter, "storyDetailTitle", storyDetail.Find("DetailPanel/Title").GetComponent<Text>());
            SetObject(serializedRouter, "storyDetailBody", storyDetail.Find("DetailPanel/Body").GetComponent<Text>());
            SetObject(serializedRouter, "sceneListPanel", storyDetail.Find("DetailPanel/SceneList").gameObject);
            serializedRouter.ApplyModifiedPropertiesWithoutUndo();

            // 自动创建并绑定 UI 主题系统。
            // 现实/梦域两套主题先作为配置资源保存，之后你可以在 Project 面板里直接改。
            UIThemeApplier themeApplier = canvas.GetComponent<UIThemeApplier>();
            if (themeApplier == null)
            {
                themeApplier = canvas.gameObject.AddComponent<UIThemeApplier>();
            }

            CreateOrUpdateThemeConfigs(out UIThemeConfig realityTheme, out UIThemeConfig dreamTheme);
            SerializedObject serializedTheme = new SerializedObject(themeApplier);
            SetObject(serializedTheme, "realityTheme", realityTheme);
            SetObject(serializedTheme, "dreamTheme", dreamTheme);
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();

            // 自动绑定底部导航按钮的点击事件。
            BindNav(bottomNav, "Nav_Home", router, router.ShowHome);
            BindNav(bottomNav, "Nav_Character", router, router.ShowCharacter);
            BindNav(bottomNav, "Nav_Battle", router, router.ShowBattle);
            BindNav(bottomNav, "Nav_Story", router, router.ShowStory);
            BindNav(bottomNav, "Nav_Activity", router, router.ShowActivity);

            // 自动绑定首页主线入口和其他页面的返回按钮。
            BindButton(pageRoot.Find("Page_Home"), "MainActionButton", router, router.EnterMainline);
            BindButton(pageRoot.Find("Page_Character"), "BackHomeButton", router, router.ReturnHome);
            BindButton(pageRoot.Find("Page_Battle"), "BackHomeButton", router, router.ReturnHome);
            BindButton(pageRoot.Find("Page_Battle"), "BackMainlineButton", router, router.ShowMainlineChapter);
            BindButton(pageRoot.Find("Page_Battle"), "StartBattleButton", router, router.ResolveBattleVictory);
            BindButton(pageRoot.Find("Page_Story"), "BackHomeButton", router, router.ReturnHome);
            BindButton(pageRoot.Find("Page_Activity"), "BackHomeButton", router, router.ReturnHome);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "BackHomeButton", router, router.ReturnHome);
            BindButton(pageRoot.Find("Page_Formation"), "BackHomeButton", router, router.ReturnHome);
            // 临时测试：右上角设置按钮先作为现实/梦域主题切换入口。
            // 后续真正设置页做好后，再把这里改成打开设置页。
            BindButton(topBar, "SettingButton", router, router.ToggleThemeForTest);

            // 主线关卡页：左侧分类、六个关卡和进入编队入口。
            BindButton(pageRoot.Find("Page_MainlineChapter"), "Category_Story", router, router.ShowMainlineStoryTab);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "Category_Formation", router, router.ShowMainlineFormationTab);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "Category_Training", router, router.ShowTrainingCategory);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "Category_DreamActivity", router, router.ShowDreamDomain);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "StageCard_1", router, router.ShowMainlineStageOne);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "StageCard_2", router, router.ShowMainlineStageTwo);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "StageCard_3", router, router.ShowMainlineStageThree);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "StageCard_4", router, router.ShowMainlineStageFour);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "StageCard_5", router, router.ShowMainlineStageFive);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "StageCard_6", router, router.ShowMainlineStageSix);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "ChallengeButton", router, router.ShowFormation);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "FormationTabActionButton", router, router.ShowFormation);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "TrainingTabActionButton", router, router.ShowCharacter);
            BindButton(pageRoot.Find("Page_MainlineChapter"), "DreamTabActionButton", router, router.ShowDreamDomain);
            BindButton(pageRoot.Find("Page_DreamDomain"), "BackMainlineButton", router, router.ReturnMainlineDreamTab);
            BindButton(pageRoot.Find("Page_DreamDomain"), "DreamNode_1", router, router.ShowDreamNodeDetail);
            BindButton(pageRoot.Find("Page_DreamDomain"), "DreamNode_2", router, router.ShowDreamNodeDetail);
            BindButton(pageRoot.Find("Page_DreamDomain"), "DreamNode_3", router, router.ShowDreamNodeDetail);
            BindButton(pageRoot.Find("Page_DreamDomain"), "DreamNode_4", router, router.ShowDreamNodeDetail);
            BindButton(pageRoot.Find("Page_DreamDomain"), "EnterDreamButton", router, router.ShowDreamNodeDetail);

            // 编队页：六个位置和底部业务按钮。
            BindButton(pageRoot.Find("Page_Formation"), "FormationSlot_1", router, router.ShowFormationSlotOne);
            BindButton(pageRoot.Find("Page_Formation"), "FormationSlot_2", router, router.ShowFormationSlotTwo);
            BindButton(pageRoot.Find("Page_Formation"), "FormationSlot_3", router, router.ShowFormationSlotThree);
            BindButton(pageRoot.Find("Page_Formation"), "FormationSlot_4", router, router.ShowFormationSlotFour);
            BindButton(pageRoot.Find("Page_Formation"), "FormationSlot_5", router, router.ShowFormationSlotFive);
            BindButton(pageRoot.Find("Page_Formation"), "FormationSlot_6", router, router.ShowFormationSlotSix);
            BindButton(pageRoot.Find("Page_Formation"), "EditFormationButton", router, router.EditFormation);
            BindButton(pageRoot.Find("Page_Formation"), "SaveFormationButton", router, router.SaveFormation);
            BindButton(pageRoot.Find("Page_Formation"), "BondPreviewButton", router, router.PreviewBond);

            // 剧情页三张章节卡片的详情按钮。
            BindButton(pageRoot.Find("Page_Story/StoryCard_Main"), "EnterButton", router, router.ShowChapterOneDetail);
            BindButton(pageRoot.Find("Page_Story/StoryCard_ChapterTwo"), "EnterButton", router, router.ShowChapterTwoDetail);
            BindButton(pageRoot.Find("Page_Story/StoryCard_ChapterThree"), "EnterButton", router, router.ShowChapterThreeDetail);
            BindButton(pageRoot.Find("Page_Character/CharacterCard_Main"), "EnterButton", router, router.ShowCharacterDetail);
            BindButton(pageRoot.Find("Page_Character/CharacterCard_Training"), "EnterButton", router, router.ShowTrainingInfo);
            BindButton(pageRoot.Find("Page_Character/CharacterCard_Bond"), "EnterButton", router, router.ShowBondInfo);
            BindButton(pageRoot.Find("Page_Battle/FormationPanel"), "FormationSlot_1", router, router.ShowFormationSlotOne);
            BindButton(pageRoot.Find("Page_Battle/FormationPanel"), "FormationSlot_2", router, router.ShowFormationSlotTwo);
            BindButton(pageRoot.Find("Page_Battle/FormationPanel"), "FormationSlot_3", router, router.ShowFormationSlotThree);
            BindButton(pageRoot.Find("Page_Battle/FormationPanel"), "FormationSlot_4", router, router.ShowFormationSlotFour);
            BindButton(pageRoot.Find("Page_Battle/FormationPanel"), "FormationSlot_5", router, router.ShowFormationSlotFive);
            BindButton(pageRoot.Find("Page_Battle/FormationPanel"), "FormationSlot_6", router, router.ShowFormationSlotSix);
            BindButton(pageRoot.Find("Page_Battle/StagePanel"), "StageNode_1", router, router.ShowStageOne);
            BindButton(pageRoot.Find("Page_Battle/StagePanel"), "StageNode_2", router, router.ShowStageTwo);
            BindButton(pageRoot.Find("Page_Battle/StagePanel"), "StageNode_3", router, router.ShowStageThree);
            BindButton(pageRoot.Find("Page_Battle/StagePanel"), "StageNode_4", router, router.ShowStageFour);
            BindButton(pageRoot.Find("Page_Battle/StagePanel"), "StageNode_5", router, router.ShowStageFive);
            BindButton(pageRoot.Find("Page_Battle/StagePanel"), "StageNode_6", router, router.ShowStageSix);
            BindButton(storyDetail.Find("DetailPanel"), "ReadButton", router, router.StartStoryReading);
            BindButton(storyDetail.Find("DetailPanel"), "SkipButton", router, router.SkipStory);
            BindButton(storyDetail.Find("DetailPanel"), "ReplayButton", router, router.ReplayStory);
            BindButton(storyDetail.Find("DetailPanel"), "BattleButton", router, router.EnterBattlePrototype);
            BindButton(storyDetail.Find("DetailPanel"), "SceneButton_1", router, router.ShowScene31);
            BindButton(storyDetail.Find("DetailPanel"), "SceneButton_2", router, router.ShowScene32);
            BindButton(storyDetail.Find("DetailPanel"), "SceneButton_3", router, router.ShowScene33);
            BindButton(storyDetail.Find("DetailPanel"), "SceneButton_4", router, router.ShowScene34);
            BindButton(storyDetail.Find("DetailPanel"), "SceneButton_5", router, router.ShowScene35);
            BindButton(storyDetail.Find("DetailPanel"), "SceneButton_6", router, router.ShowScene36);
            BindButton(storyDetail.Find("DetailPanel"), "SceneButton_7", router, router.ShowScene37);
            BindButton(storyDetail, "CloseButton", router, router.CloseStoryDetail);

            // 顶栏和底栏放到最上层，避免被页面内容挡住。
            topBar.SetAsLastSibling();
            bottomNav.SetAsLastSibling();

            Selection.activeGameObject = canvas.gameObject;
            EditorUtility.SetDirty(canvas.gameObject);
        }

        [MenuItem("Shouyou/UI/Rebuild Home Layout")]
        public static void RebuildHomeLayout()
        {
            CleanAndRebuildPrototype();
        }

        [MenuItem("Shouyou/UI/Clean And Rebuild Home Layout")]
        public static void CleanAndRebuildHomeLayout()
        {
            CleanAndRebuildPrototype();
        }

        private static RectTransform BuildTopBar(Transform canvas)
        {
            // 顶部栏：头像、玩家名、等级、货币、设置入口。
            RectTransform topBar = FindOrCreateRect(canvas, "TopBar");
            SetTop(topBar, 160);

            Image topBg = FindOrCreateImage(topBar, "TopBar_BG");
            StretchFull(topBg.rectTransform);
            topBg.color = TopBarColor;
            topBg.raycastTarget = false;

            RectTransform avatar = FindOrCreateRect(topBar, "Avatar");
            SetRect(avatar, -770, 0, 90, 90);
            AddPanelImage(avatar, SoftPanelColor);
            ApplySpriteToImage(avatar.GetComponent<Image>(), "icon_avatar.png");

            RectTransform playerName = FindOrCreateRect(topBar, "PlayerName");
            SetRect(playerName, -620, 22, 220, 48);
            SetupLabel(playerName, "玩家名", 32, TextAnchor.MiddleLeft);

            RectTransform level = FindOrCreateRect(topBar, "LevelText");
            SetRect(level, -620, -24, 220, 42);
            SetupLabel(level, "Lv.1", 26, TextAnchor.MiddleLeft);

            RectTransform currency = FindOrCreateRect(topBar, "CurrencyRoot");
            SetRect(currency, 380, 0, 480, 86);

            RectTransform gold = FindOrCreateRect(currency, "GoldItem");
            SetRect(gold, -130, 0, 210, 56);
            AddPanelImage(gold, SoftPanelColor);
            SetupLabel(gold, "铜钱 9999", 28, TextAnchor.MiddleCenter);

            RectTransform jade = FindOrCreateRect(currency, "JadeItem");
            SetRect(jade, 120, 0, 170, 56);
            AddPanelImage(jade, SoftPanelColor);
            SetupLabel(jade, "玉 120", 28, TextAnchor.MiddleCenter);

            RectTransform settings = FindOrCreateRect(topBar, "SettingButton");
            SetRect(settings, 770, 0, 86, 86);
            AddPanelImage(settings, SoftPanelColor);
            ApplySpriteToImage(settings.GetComponent<Image>(), "icon_setting.png");

            return topBar;
        }

        private static RectTransform BuildPages(Transform canvas)
        {
            // PageRoot 是所有页面的父节点。
            // 后面切页面时，会显示其中一个 Page_Xxx，隐藏其他页面。
            RectTransform pageRoot = FindOrCreateRect(canvas, "PageRoot");
            StretchFull(pageRoot);

            // 首页比较特殊：它不放大面板，只在庭院背景上叠主按钮和提示。
            RectTransform home = BuildPage(pageRoot, "Page_Home", "");
            RectTransform center = FindOrCreateRect(home, "CenterPanel");
            SetRect(center, 0, -20, 1400, 720);
            SetRect(FindOrCreateRect(center, "CharacterDisplay"), 280, 40, 420, 620);

            RectTransform mainAction = FindOrCreateRect(center, "MainActionButton");
            SetRect(mainAction, 470, -120, 280, 92);
            AddCommonButtonImage(mainAction);
            Button mainActionButton = mainAction.GetComponent<Button>();
            if (mainActionButton == null)
            {
                mainActionButton = mainAction.gameObject.AddComponent<Button>();
            }

            mainActionButton.targetGraphic = mainAction.GetComponent<Image>();
            SetupButtonLabel(mainAction, "进入主线", 34, TextAnchor.MiddleCenter);

            RectTransform storyHint = FindOrCreateRect(center, "StoryHint");
            SetRect(storyHint, -430, -145, 620, 74);
            AddPanelImage(storyHint, SoftPanelColor);
            SetupLabel(storyHint, "第一章：竹堂初语，雅集将启", 30, TextAnchor.MiddleCenter);

            RectTransform notice = FindOrCreateRect(center, "NoticeArea");
            SetRect(notice, -430, -220, 720, 60);
            AddPanelImage(notice, SoftPanelColor);
            SetupLabel(notice, "今日任务：完成一次词意试炼    主线进度：0 / 6", 26, TextAnchor.MiddleCenter);

            // 角色页：先搭建养成信息的视觉骨架，真实数值以后接数据表。
            RectTransform character = BuildPage(pageRoot, "Page_Character", "角色养成");
            BuildInfoCard(character, "CharacterCard_Main", "李清照", "等级 Lv.1    词意：如梦令\n点击后接入立绘、升级、技能和羁绊", -360, 80, 620, 300);
            BuildInfoCard(character, "CharacterCard_Training", "养成入口", "升级 / 突破 / 技能 / 装备", 360, 80, 620, 300);
            BuildCharacterStatStrip(character, -120);
            BuildInfoCard(character, "CharacterCard_Bond", "角色羁绊", "与其他人物共同出战，解锁传记与梦境", 0, -300, 620, 220);

            // 战斗页：先搭建关卡选择和编队入口。
            RectTransform battle = BuildPage(pageRoot, "Page_Battle", "回合 PVE");
            BuildInfoCard(battle, "BattleCard_Main", "第一章：春日庭院", "推荐等级 Lv.1    关卡进度 0 / 6\n词意相生，回合制自动战斗\n点击下方“开始本关”结算试炼", -360, 80, 620, 300);
            BuildInfoCard(battle, "BattleCard_Team", "六人编队", "前排 / 后排 / 词意搭配\n点击后接入编队页面", 360, 80, 620, 300);
            BuildStagePanel(battle, -360, -300);
            BuildFormationPanel(battle, 360, -300);
            // 战斗页业务按钮要避开底部导航栏。
            // 之前放得太低，容易和底部“战斗”导航混在一起，导致玩家误以为点了开始战斗。
            BuildActionButton(battle, "StartBattleButton", "开始本关", 170, -220, 230, 68, 24);
            BuildActionButton(battle, "BackMainlineButton", "返回主线", 450, -220, 210, 68, 22);

            // 剧情页：把大段剧情拆成主线、传记和支线三个入口。
            RectTransform story = BuildPage(pageRoot, "Page_Story", "故事与传记");
            BuildInfoCard(story, "StoryCard_Main", "第一章：竹堂初语", "李清照获准赴汴京雅集，支持跳过与回看", -360, 80, 620, 300);
            BuildInfoCard(story, "StoryCard_ChapterTwo", "第二章：灯下共稿（暂定）", "李清照与婉禾连夜推敲词稿，准备赴会", 360, 80, 620, 300);
            BuildInfoCard(story, "StoryCard_ChapterThree", "第三章：汴京雅集，初逢群英", "公开献词、群英互动、白衣少年伏笔", 0, -300, 620, 220);
            BuildStoryActionBar(story);

            // 活动页：先预留限时内容的位置，方便后续扩展活动和皮肤。
            RectTransform activity = BuildPage(pageRoot, "Page_Activity", "活动与收集");
            BuildInfoCard(activity, "ActivityCard_Event", "春日活动", "限时任务、活动剧情、活动商店", -360, 80, 620, 300);
            BuildInfoCard(activity, "ActivityCard_Summon", "角色召集", "新人物、概率说明、保底规则", 360, 80, 620, 300);
            BuildInfoCard(activity, "ActivityCard_Collection", "CG 与外观", "情侣立绘、CG、节日皮肤收集", 0, -300, 620, 220);
            BuildActivityProgress(activity);

            // 主线章节页：对应“卷一·汴京春深”的关卡选择界面。
            BuildMainlineChapterPage(pageRoot);

            // 编队页：对应六人行迹编队和气韵提示界面。
            BuildFormationPage(pageRoot);

            // 梦域页：对应现实主线之外的梦境节点选择。
            BuildDreamDomainPage(pageRoot);

            return pageRoot;
        }

        private static void BuildMainlineChapterPage(RectTransform pageRoot)
        {
            // 主线章节页：让玩家在剧情、关卡和编队之间形成清晰的操作路径。
            RectTransform page = BuildPage(pageRoot, "Page_MainlineChapter", "");
            GameObject defaultPanel = page.Find("ContentPanel") != null ? page.Find("ContentPanel").gameObject : null;
            if (defaultPanel != null)
            {
                defaultPanel.SetActive(false);
            }

            // 主线页总背景：先使用 commonBg 的梦境紫蓝背景，让“进入主线”后的画面不再单调。
            Image dreamBackground = FindOrCreateImage(page, "MainlineDreamBackground");
            StretchFull(dreamBackground.rectTransform);
            dreamBackground.sprite = LoadCommonBgSprite("commonbg3.png");
            dreamBackground.color = new Color(1f, 1f, 1f, 0.96f);
            dreamBackground.preserveAspect = false;
            dreamBackground.raycastTarget = false;
            AddThemeElement(dreamBackground.rectTransform, UIThemeElementRole.PageBackground, true, true);
            dreamBackground.transform.SetAsFirstSibling();

            // 右侧独立背景：主线内容未铺满屏幕时，仍然保持完整的古风画面。
            Image rightBackground = FindOrCreateImage(page, "MainlineRightBackground");
            SetRect(rightBackground.rectTransform, 770, 0, 380, 780);
            rightBackground.sprite = LoadCommonBgSprite("commonbg7.png");
            rightBackground.color = new Color(1f, 1f, 1f, 0.36f);
            rightBackground.preserveAspect = false;
            rightBackground.raycastTarget = false;
            AddThemeElement(rightBackground.rectTransform, UIThemeElementRole.PageBackground, true, false);
            rightBackground.transform.SetSiblingIndex(1);

            RectTransform panel = FindOrCreateRect(page, "MainlinePanel");
            SetRect(panel, 0, 0, 1500, 780);
            AddMainlineContentFrame(panel);

            RectTransform title = FindOrCreateRect(panel, "Title");
            SetRect(title, 0, 320, 900, 70);
            SetupLabel(title, "卷一·汴京春深", 42, TextAnchor.MiddleCenter);

            RectTransform subtitle = FindOrCreateRect(panel, "Subtitle");
            SetRect(subtitle, 0, 270, 700, 50);
            SetupLabel(subtitle, "雅集初会", 28, TextAnchor.MiddleCenter);

            string[] categories = { "剧情闯关", "行迹编队", "行迹养成", "梦域养成活动" };
            for (int i = 0; i < categories.Length; i++)
            {
                RectTransform category = FindOrCreateRect(panel, "Category_" + (i == 0 ? "Story" : i == 1 ? "Formation" : i == 2 ? "Training" : "DreamActivity"));
                SetRect(category, -650, 185 - i * 115, 230, 78);
                AddMainlineCategoryImage(category, i == 0);
                SetupButtonLabel(category, categories[i], 24, TextAnchor.MiddleCenter);
                category.SetAsLastSibling();
            }

            RectTransform storyTab = FindOrCreateRect(panel, "Tab_Story");
            SetRect(storyTab, 80, -20, 1180, 560);

            RectTransform formationTab = FindOrCreateRect(panel, "Tab_Formation");
            SetRect(formationTab, 80, -20, 1180, 560);

            RectTransform trainingTab = FindOrCreateRect(panel, "Tab_Training");
            SetRect(trainingTab, 80, -20, 1180, 560);

            RectTransform dreamActivityTab = FindOrCreateRect(panel, "Tab_DreamActivity");
            SetRect(dreamActivityTab, 80, -20, 1180, 560);

            string[] stageNames = { "关卡一\n明水入汴京", "关卡二\n雅集赴会", "关卡三\n词论初临", "关卡四\n风雨前夜", "关卡五\n故人入梦", "关卡六\n潮声再起" };
            for (int i = 0; i < stageNames.Length; i++)
            {
                // 按参考 UI 排成五列两行，第一版只放六个已规划关卡。
                float x = -470 + (i % 5) * 210;
                float y = 135 - (i / 5) * 265;
                BuildStageCard(storyTab, "StageCard_" + (i + 1), stageNames[i], x, y, i < 2, i);
            }

            BuildActionButton(storyTab, "ChallengeButton", "前往挑战", 0, -285, 250, 76, 28);
            RectTransform replay = FindOrCreateRect(storyTab, "ReplayStoryButton");
            SetRect(replay, 520, -285, 190, 64);
            AddCommonButtonImage(replay);
            SetupButtonLabel(replay, "回看剧情", 22, TextAnchor.MiddleCenter);
            BuildMainlineTabPreview(formationTab, "行迹编队", "六人编队 · 前排 / 后排 / 气韵加成", "当前队伍：李清照 / 空位 / 空位 / 空位 / 空位 / 空位\n\n后续：点击空位选择角色，保存编队后写入本地后端。", "进入编队页", "FormationTabActionButton");
            BuildMainlineTabPreview(trainingTab, "行迹养成", "等级 · 突破 · 技能 · 词意节点", "李清照 Lv.1\n词意：如梦令\n定位：词意输出 / 群体辅助\n\n后续：这里接入升级材料、突破消耗、技能等级和行迹节点。", "查看养成", "TrainingTabActionButton");
            BuildMainlineTabPreview(dreamActivityTab, "梦域养成活动", "神识波动 · 梦蝶赠礼 · 梦域任务", "第一章先出现梦域伏笔。\n第一卷末章正式开启梦域系统。\n\n后续：这里接入梦域节点、梦蝶赠礼、签到、活跃度和限定活动。", "查看梦域", "DreamTabActionButton");

            storyTab.gameObject.SetActive(true);
            formationTab.gameObject.SetActive(false);
            trainingTab.gameObject.SetActive(false);
            dreamActivityTab.gameObject.SetActive(false);
            BuildActionButton(page, "BackHomeButton", "返回庭院", 720, 320, 210, 68, 22);
        }

        private static void BuildMainlineTabPreview(RectTransform parent, string titleText, string subtitleText, string bodyText, string buttonText, string buttonName)
        {
            // 主线页栏目预览：用于左侧栏目切换时展示对应模块的摘要。
            // 第一版先做清晰的信息卡，后续再替换成完整玩法界面。
            RectTransform card = FindOrCreateRect(parent, "PreviewCard");
            SetRect(card, 0, 40, 900, 390);
            AddMainlineContentFrame(card);

            RectTransform title = FindOrCreateRect(card, "Title");
            SetRect(title, 0, 125, 760, 60);
            SetupLabel(title, titleText, 38, TextAnchor.MiddleCenter);

            RectTransform subtitle = FindOrCreateRect(card, "Subtitle");
            SetRect(subtitle, 0, 70, 760, 45);
            SetupLabel(subtitle, subtitleText, 24, TextAnchor.MiddleCenter);

            RectTransform body = FindOrCreateRect(card, "Body");
            SetRect(body, 0, -10, 760, 120);
            SetupLabel(body, bodyText, 22, TextAnchor.MiddleCenter);

            // 三个信息小卡用于把“占位说明”推进成可读的玩法模块。
            // 这些不是最终系统，只是让 Demo 阶段先看见模块结构，后续可以逐个接入真实数据。
            string[] featureTexts = GetMainlineTabFeatureTexts(titleText);
            for (int i = 0; i < featureTexts.Length; i++)
            {
                float x = -260 + i * 260;
                BuildSmallInfoCard(card, "FeatureCard_" + (i + 1), featureTexts[i], x, -135);
            }

            BuildActionButton(parent, buttonName, buttonText, 0, -255, 230, 68, 24);
        }

        private static string[] GetMainlineTabFeatureTexts(string titleText)
        {
            // 根据当前栏目标题返回对应的小模块说明。
            // 这里先写在 UI 构建器里，等玩法稳定后再抽成配置表或后端数据。
            if (titleText.Contains("编队"))
            {
                return new[]
                {
                    "前排 3 位\n承伤 / 控制",
                    "后排 3 位\n输出 / 辅助",
                    "助阵预留\n九天玄女等"
                };
            }

            if (titleText.Contains("养成"))
            {
                return new[]
                {
                    "等级突破\n基础数值",
                    "词意技能\n角色特有",
                    "行迹节点\n支线解锁"
                };
            }

            if (titleText.Contains("梦域"))
            {
                return new[]
                {
                    "神识波动\n章节伏笔",
                    "梦蝶赠礼\n日常资源",
                    "命运节点\n末章开启"
                };
            }

            return new[]
            {
                "模块一\n待配置",
                "模块二\n待配置",
                "模块三\n待配置"
            };
        }

        private static void BuildSmallInfoCard(RectTransform parent, string name, string label, float x, float y)
        {
            // 小信息卡：用于在同一栏目里并排展示 3 个核心业务点。
            RectTransform card = FindOrCreateRect(parent, name);
            SetRect(card, x, y, 210, 86);
            AddPanelImage(card, SoftPanelColor);
            SetupLabel(card, label, 20, TextAnchor.MiddleCenter);
            AddThemeElement(card, UIThemeElementRole.MainPanel, true, true);
        }

        private static void BuildStageCard(RectTransform parent, string name, string title, float x, float y, bool unlocked, int stageIndex)
        {
            // 关卡卡片：每一关先铺一张背景图，再叠加章节边框和文字。
            RectTransform card = FindOrCreateRect(parent, name);
            SetRect(card, x, y, 190, 235);

            Image stageBackground = FindOrCreateImage(card, "StageBackground");
            StretchFull(stageBackground.rectTransform);
            string[] stageBackgrounds =
            {
                "commonbg4.png",
                "commonbg5.png",
                "commonbg6.png",
                "commonbg7.png",
                "commonbg3.png",
                "commonbg4.png"
            };
            stageBackground.sprite = LoadCommonBgSprite(stageBackgrounds[stageIndex % stageBackgrounds.Length]);
            stageBackground.color = unlocked ? new Color(1f, 1f, 1f, 0.88f) : new Color(0.78f, 0.72f, 1f, 0.58f);
            stageBackground.preserveAspect = false;
            stageBackground.raycastTarget = false;
            stageBackground.transform.SetAsFirstSibling();

            Image frame = FindOrCreateImage(card, "ChapterFrame");
            StretchFull(frame.rectTransform);
            frame.sprite = LoadReferenceSprite("章节背景框.png");
            frame.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.78f);
            frame.preserveAspect = false;
            frame.raycastTarget = true;
            Button button = card.GetComponent<Button>();
            if (button == null)
            {
                button = card.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = frame;

            // 卡片文字先做成“场景关卡卡”的信息结构：
            // 关卡名 + 推荐战力 + 掉落预览。后续有真实配置表后，再从数据里读取。
            int recommendPower = 800 + stageIndex * 180;
            string dropPreview = stageIndex == 4 ? "梦境碎片" : stageIndex == 5 ? "玉 / CG" : "铜钱 / 词意";
            string lockText = unlocked ? "已解锁" : "未解锁";
            SetupButtonLabel(card, title + "\n" + lockText + " · 战力 " + recommendPower + "\n掉落：" + dropPreview, 16, TextAnchor.MiddleCenter);
            card.SetAsLastSibling();
        }

        private static void AddMainlineContentFrame(RectTransform rect)
        {
            // 主线内容框：用 commonBg 里的大边框图做主体容器，后续可以替换成九宫格切图。
            Image image = FindOrCreateImage(rect, "Background");
            StretchFull(image.rectTransform);
            image.sprite = LoadCommonBgSprite("commonbg_contentBorder1.jpg");
            image.color = new Color(1f, 1f, 1f, 0.88f);
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.raycastTarget = false;
            AddThemeElement(rect, UIThemeElementRole.MainPanel, true, true);
            image.transform.SetAsFirstSibling();
        }

        private static void AddMainlineCategoryImage(RectTransform rect, bool selected)
        {
            // 左侧栏目按钮：选中项偏粉金，普通项偏梦域紫，先把风格方向跑通。
            Image image = FindOrCreateImage(rect, "Background");
            StretchFull(image.rectTransform);
            image.sprite = LoadCommonBgSprite(selected ? "commonbg_contentBorder2.jpg" : "commonbg_contentBorder.jpg");
            image.color = selected ? new Color(1f, 0.78f, 0.86f, 0.92f) : new Color(0.84f, 0.78f, 1f, 0.78f);
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.raycastTarget = true;
            AddThemeElement(rect, selected ? UIThemeElementRole.SelectedButton : UIThemeElementRole.NormalButton, true, true);
            image.transform.SetAsFirstSibling();

            Button button = rect.GetComponent<Button>();
            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = image;
        }

        private static void BuildFormationPage(RectTransform pageRoot)
        {
            // 六人编队页：保持当前设计的前排 3 人、后排 3 人。
            RectTransform page = BuildPage(pageRoot, "Page_Formation", "");
            GameObject defaultPanel = page.Find("ContentPanel") != null ? page.Find("ContentPanel").gameObject : null;
            if (defaultPanel != null)
            {
                defaultPanel.SetActive(false);
            }

            RectTransform panel = FindOrCreateRect(page, "FormationMainPanel");
            SetRect(panel, 0, 0, 1500, 780);
            AddPanelImage(panel, PagePanelColor);
            panel.GetComponent<Image>().raycastTarget = false;

            RectTransform title = FindOrCreateRect(panel, "Title");
            SetRect(title, -120, 320, 900, 70);
            SetupLabel(title, "行迹编队", 42, TextAnchor.MiddleCenter);

            string[] slots = { "李清照", "待入阵", "待入阵", "待入阵", "待入阵", "待入阵" };
            for (int i = 0; i < slots.Length; i++)
            {
                float x = -390 + (i % 3) * 270;
                float y = 145 - (i / 3) * 190;
                BuildFormationSlotButton(panel, "FormationSlot_" + (i + 1), slots[i], x, y, i == 0);
            }

            RectTransform qiYun = FindOrCreateRect(panel, "CurrentQiYun");
            SetRect(qiYun, 570, 155, 360, 210);
            AddPanelImage(qiYun, SoftPanelColor);
            qiYun.GetComponent<Image>().raycastTarget = false;
            SetupLabel(qiYun, "当前气韵\n\n24800\n\n同调角色可触发气韵增益", 24, TextAnchor.MiddleCenter);

            RectTransform hint = FindOrCreateRect(panel, "BondHint");
            SetRect(hint, 570, -95, 360, 220);
            AddPanelImage(hint, SoftPanelColor);
            hint.GetComponent<Image>().raycastTarget = false;
            SetupLabel(hint, "羁绊提示\n\n李清照：词意输出\n婉禾：辅助与协奏", 22, TextAnchor.MiddleCenter);

            BuildActionButton(panel, "EditFormationButton", "编辑阵容", -260, -325, 210, 70, 24);
            BuildActionButton(panel, "SaveFormationButton", "保存编队", 0, -325, 210, 70, 24);
            BuildActionButton(panel, "BondPreviewButton", "羁绊预览", 260, -325, 210, 70, 24);
            BuildActionButton(page, "BackHomeButton", "返回庭院", 720, 320, 210, 68, 22);
        }

        private static void BuildDreamDomainPage(RectTransform pageRoot)
        {
            // 梦域全屏页：从“弹窗入口”升级成可停留、可点节点的独立页面。
            // 这一页先做系统雏形，不急着接复杂战斗，重点是建立“进入梦域”的仪式感。
            RectTransform page = BuildPage(pageRoot, "Page_DreamDomain", "");
            GameObject defaultPanel = page.Find("ContentPanel") != null ? page.Find("ContentPanel").gameObject : null;
            if (defaultPanel != null)
            {
                defaultPanel.SetActive(false);
            }

            Image background = FindOrCreateImage(page, "DreamDomainBackground");
            StretchFull(background.rectTransform);
            background.sprite = LoadUISprite("commonbg3.png");
            background.color = new Color32(125, 98, 210, 230);
            background.raycastTarget = false;
            AddThemeElement(background.rectTransform, UIThemeElementRole.PageBackground, true, true);
            background.transform.SetAsFirstSibling();

            RectTransform title = FindOrCreateRect(page, "DreamTitle");
            SetRect(title, 0, 310, 720, 90);
            SetupLabel(title, "梦域裂隙", 52, TextAnchor.MiddleCenter);

            RectTransform subtitle = FindOrCreateRect(page, "DreamSubtitle");
            SetRect(subtitle, 0, 250, 900, 54);
            SetupLabel(subtitle, "选择记忆，也选择命运", 28, TextAnchor.MiddleCenter);

            RectTransform nodePanel = FindOrCreateRect(page, "DreamNodePanel");
            SetRect(nodePanel, 0, 10, 1020, 470);
            AddMainlineContentFrame(nodePanel);

            BuildDreamNode(nodePanel, "DreamNode_1", "少年诗意", -330, 120, true);
            BuildDreamNode(nodePanel, "DreamNode_2", "花叶离舟", 330, 120, false);
            BuildDreamNode(nodePanel, "DreamNode_3", "化蝶之境", -230, -95, false);
            BuildDreamNode(nodePanel, "DreamNode_4", "归梦成双", 230, -95, false);

            RectTransform hint = FindOrCreateRect(page, "DreamHint");
            SetRect(hint, 610, 80, 360, 210);
            AddPanelImage(hint, new Color32(246, 238, 255, 210));
            SetupLabel(hint, "通关梦境后\n可解锁命运分支\n\n当前 Demo：先开放节点预览", 24, TextAnchor.MiddleCenter);
            AddThemeElement(hint, UIThemeElementRole.MainPanel, true, true);

            BuildActionButton(page, "EnterDreamButton", "进入梦域", 0, -340, 280, 82, 30);
            BuildActionButton(page, "BackMainlineButton", "返回主线", 720, 320, 210, 68, 22);
        }

        private static void BuildDreamNode(RectTransform parent, string name, string label, float x, float y, bool unlocked)
        {
            // 梦域节点：第一版用按钮表示可交互节点。
            // 后续可以替换成星轨、蝴蝶、梦境碎片等更精致的图标。
            RectTransform node = FindOrCreateRect(parent, name);
            SetRect(node, x, y, 240, 78);
            AddCommonButtonImage(node);
            SetupButtonLabel(node, unlocked ? label + "\n已开启" : label + "\n待唤醒", 22, TextAnchor.MiddleCenter);

            Button button = node.GetComponent<Button>();
            if (button == null)
            {
                button = node.gameObject.AddComponent<Button>();
            }

            button.interactable = true;
        }

        private static void BuildFormationSlotButton(RectTransform parent, string name, string label, float x, float y, bool occupied)
        {
            // 编队槽位按钮：第一版用姓名和空位表示，后续接入角色头像和稀有度边框。
            RectTransform slot = FindOrCreateRect(parent, name);
            SetRect(slot, x, y, 235, 150);
            AddCommonButtonImage(slot);
            Button button = slot.GetComponent<Button>();
            if (button == null)
            {
                button = slot.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = slot.GetComponent<Image>();
            SetupButtonLabel(slot, label, 24, TextAnchor.MiddleCenter);
            slot.SetAsLastSibling();
        }

        private static void BuildActionButton(RectTransform parent, string name, string label, float x, float y, float width, float height, int fontSize)
        {
            // 页面底部的通用业务按钮，统一使用 common-button.png。
            RectTransform buttonRect = FindOrCreateRect(parent, name);
            SetRect(buttonRect, x, y, width, height);
            AddCommonButtonImage(buttonRect);
            Button button = buttonRect.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRect.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = buttonRect.GetComponent<Image>();
            SetupButtonLabel(buttonRect, label, fontSize, TextAnchor.MiddleCenter);
            buttonRect.SetAsLastSibling();
        }

        private static void BuildInfoCard(RectTransform page, string name, string title, string description, float x, float y, float width, float height)
        {
            // 信息卡片是当前 Demo 的占位组件：先确认信息层级和布局，再接真实功能。
            RectTransform card = FindOrCreateRect(page, name);
            SetRect(card, x, y, width, height);
            AddPanelImage(card, PagePanelColor);
            // 卡片背景只负责显示，真正接收点击的是内部按钮。
            card.GetComponent<Image>().raycastTarget = false;

            RectTransform titleRect = FindOrCreateRect(card, "Title");
            SetRect(titleRect, 0, 70, width - 50, 58);
            SetupLabel(titleRect, title, 32, TextAnchor.MiddleCenter);

            RectTransform descriptionRect = FindOrCreateRect(card, "Description");
            SetRect(descriptionRect, 0, -30, width - 70, 110);
            SetupLabel(descriptionRect, description, 24, TextAnchor.MiddleCenter);

            // 先生成一个视觉上的入口按钮，后续再绑定具体功能。
            RectTransform enterButton = FindOrCreateRect(card, "EnterButton");
            SetRect(enterButton, 0, -105, 220, 52);
            AddCommonButtonImage(enterButton);
            Button button = enterButton.GetComponent<Button>();
            if (button == null)
            {
                button = enterButton.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = enterButton.GetComponent<Image>();
            SetupButtonLabel(enterButton, "查看详情", 22, TextAnchor.MiddleCenter);
            // 确保按钮在卡片背景和文字之上，优先接收鼠标点击。
            enterButton.SetAsLastSibling();
            card.SetAsLastSibling();
        }

        private static void BuildCharacterStatStrip(RectTransform page, float y)
        {
            // 角色属性条：把玩家最常看的等级、生命、攻击和词意集中展示。
            RectTransform strip = FindOrCreateRect(page, "CharacterStatStrip");
            SetRect(strip, 0, y, 1240, 82);
            AddPanelImage(strip, SoftPanelColor);
            strip.GetComponent<Image>().raycastTarget = false;
            SetupLabel(strip, "等级 1 / 60     生命 1200     攻击 180     防御 95     词意：如梦令", 24, TextAnchor.MiddleCenter);
        }

        private static void BuildStagePanel(RectTransform page, float x, float y)
        {
            // 第一章六个关卡节点：先用文字节点模拟地图，后续可以换成真正的章节地图。
            RectTransform panel = FindOrCreateRect(page, "StagePanel");
            SetRect(panel, x, y, 620, 220);
            AddPanelImage(panel, PagePanelColor);
            panel.GetComponent<Image>().raycastTarget = false;
            SetupLabel(panel, "第一章关卡    体力：6 / 6", 24, TextAnchor.UpperCenter);

            string[] stages = { "1-1 初见", "1-2 试探", "1-3 词意", "1-4 夜雨", "1-5 梦境", "1-6 庭院" };
            for (int i = 0; i < stages.Length; i++)
            {
                RectTransform node = FindOrCreateRect(panel, "StageNode_" + (i + 1));
                SetRect(node, -220 + (i % 3) * 220, 35 - (i / 3) * 72, 190, 48);
                AddCommonButtonImage(node);
                Button stageButton = node.GetComponent<Button>();
                if (stageButton == null)
                {
                    stageButton = node.gameObject.AddComponent<Button>();
                }

                stageButton.targetGraphic = node.GetComponent<Image>();
                SetupButtonLabel(node, stages[i] + "  Lv." + (i + 1), 18, TextAnchor.MiddleCenter);
                node.SetAsLastSibling();
            }
        }

        private static void BuildFormationPanel(RectTransform page, float x, float y)
        {
            // 六人编队展示位：前三个是前排，后三个是后排。
            RectTransform panel = FindOrCreateRect(page, "FormationPanel");
            SetRect(panel, x, y, 620, 220);
            AddPanelImage(panel, PagePanelColor);
            panel.GetComponent<Image>().raycastTarget = false;
            SetupLabel(panel, "六人编队    前排 / 后排 / 词意搭配", 24, TextAnchor.UpperCenter);

            for (int i = 0; i < 6; i++)
            {
                RectTransform slot = FindOrCreateRect(panel, "FormationSlot_" + (i + 1));
                SetRect(slot, -220 + (i % 3) * 220, 25 - (i / 3) * 75, 170, 52);
                AddCommonButtonImage(slot);
                Button slotButton = slot.GetComponent<Button>();
                if (slotButton == null)
                {
                    slotButton = slot.gameObject.AddComponent<Button>();
                }

                slotButton.targetGraphic = slot.GetComponent<Image>();
                SetupButtonLabel(slot, i == 0 ? "李清照" : "空位 " + (i + 1), 18, TextAnchor.MiddleCenter);
                slot.SetAsLastSibling();
            }
        }

        private static void BuildStoryActionBar(RectTransform page)
        {
            // 剧情操作入口占位：正式接入剧情播放器后，再绑定跳过和回看逻辑。
            RectTransform bar = FindOrCreateRect(page, "StoryActionBar");
            SetRect(bar, 0, -455, 760, 64);
            AddPanelImage(bar, SoftPanelColor);
            bar.GetComponent<Image>().raycastTarget = false;
            SetupLabel(bar, "剧情操作：跳过剧情    回看剧情    自动播放", 22, TextAnchor.MiddleCenter);
        }

        private static void BuildActivityProgress(RectTransform page)
        {
            // 活动页底部状态条：预留倒计时、收集进度和活动积分。
            RectTransform progress = FindOrCreateRect(page, "ActivityProgress");
            SetRect(progress, 0, -455, 900, 64);
            AddPanelImage(progress, SoftPanelColor);
            progress.GetComponent<Image>().raycastTarget = false;
            SetupLabel(progress, "春日活动剩余 6 天    活动积分 0 / 1000    CG 收集 0 / 12", 22, TextAnchor.MiddleCenter);
        }

        private static RectTransform BuildStoryDetail(Transform canvas)
        {
            // 剧情详情弹窗：显示章节概要，后续可以替换成真正的逐句剧情播放器。
            RectTransform overlay = FindOrCreateRect(canvas, "StoryDetailOverlay");
            StretchFull(overlay);

            Image overlayImage = FindOrCreateImage(overlay, "OverlayMask");
            StretchFull(overlayImage.rectTransform);
            overlayImage.color = new Color32(45, 32, 24, 120);
            overlayImage.raycastTarget = false;

            RectTransform detailPanel = FindOrCreateRect(overlay, "DetailPanel");
            SetRect(detailPanel, 0, 20, 1080, 650);
            AddPanelImage(detailPanel, PagePanelColor);

            RectTransform title = FindOrCreateRect(detailPanel, "Title");
            SetRect(title, 0, 245, 920, 70);
            SetupLabel(title, "剧情详情", 38, TextAnchor.MiddleCenter);

            RectTransform body = FindOrCreateRect(detailPanel, "Body");
            SetRect(body, 0, 20, 900, 360);
            SetupLabel(body, "章节概要将在这里显示。", 24, TextAnchor.UpperLeft);

            RectTransform sceneList = FindOrCreateRect(detailPanel, "SceneList");
            SetRect(sceneList, 0, 120, 900, 100);
            AddPanelImage(sceneList, new Color32(255, 248, 236, 80));
            sceneList.GetComponent<Image>().raycastTarget = false;
            string[] sceneNames = { "3-1", "3-2", "3-3", "3-4", "3-5", "3-6", "3-7" };
            for (int i = 0; i < sceneNames.Length; i++)
            {
                RectTransform sceneButton = FindOrCreateRect(sceneList, "SceneButton_" + (i + 1));
                SetRect(sceneButton, -330 + (i % 4) * 220, 22 - (i / 4) * 48, 190, 38);
                AddCommonButtonImage(sceneButton);
                Button button = sceneButton.GetComponent<Button>();
                if (button == null)
                {
                    button = sceneButton.gameObject.AddComponent<Button>();
                }

                button.targetGraphic = sceneButton.GetComponent<Image>();
                SetupButtonLabel(sceneButton, sceneNames[i], 18, TextAnchor.MiddleCenter);
                sceneButton.SetAsLastSibling();
            }

            BuildDetailButton(detailPanel, "ReadButton", "开始阅读", -330, -245);
            BuildDetailButton(detailPanel, "SkipButton", "跳过剧情", -110, -245);
            BuildDetailButton(detailPanel, "ReplayButton", "回看剧情", 110, -245);
            BuildDetailButton(detailPanel, "CloseButton", "关闭详情", 330, -245);
            BuildDetailButton(detailPanel, "BattleButton", "进入战斗", 0, -285);

            // 编辑器生成时默认隐藏，只有点击章节详情后才显示。
            sceneList.gameObject.SetActive(false);
            overlay.gameObject.SetActive(false);
            return overlay;
        }

        private static void BuildDetailButton(RectTransform parent, string name, string label, float x, float y)
        {
            // 详情弹窗底部按钮的统一生成方法。
            RectTransform buttonRect = FindOrCreateRect(parent, name);
            SetRect(buttonRect, x, y, 190, 58);
            AddCommonButtonImage(buttonRect);
            Button button = buttonRect.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRect.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = buttonRect.GetComponent<Image>();
            SetupButtonLabel(buttonRect, label, 22, TextAnchor.MiddleCenter);
            buttonRect.SetAsLastSibling();
        }

        private static RectTransform BuildPage(Transform parent, string name, string title)
        {
            // 创建一个完整页面。title 为空时，只创建空页面容器。
            RectTransform page = FindOrCreateRect(parent, name);
            StretchFull(page);

            if (!string.IsNullOrEmpty(title))
            {
                RectTransform panel = FindOrCreateRect(page, "ContentPanel");
                SetRect(panel, 0, 0, 760, 280);
                AddPanelImage(panel, PagePanelColor);
                panel.GetComponent<Image>().raycastTarget = false;
                SetupLabel(panel, title, 36, TextAnchor.MiddleCenter);

                // 每个二级页面都放一个统一位置的返回按钮，方便测试页面流程。
                RectTransform backButton = FindOrCreateRect(page, "BackHomeButton");
                // 放在顶栏下方，避免和右上角“设置”区域重叠。
                SetRect(backButton, 720, 300, 220, 60);
                AddCommonButtonImage(backButton);
                Button button = backButton.GetComponent<Button>();
                if (button == null)
                {
                    button = backButton.gameObject.AddComponent<Button>();
                }

                button.targetGraphic = backButton.GetComponent<Image>();
                SetupButtonLabel(backButton, "返回庭院", 24, TextAnchor.MiddleCenter);
                backButton.SetAsLastSibling();
            }

            return page;
        }

        private static RectTransform BuildBottomNav(Transform canvas)
        {
            // 底部导航栏：玩家主要通过这里切换五个顶层页面。
            RectTransform bottom = FindOrCreateRect(canvas, "BottomNav");
            SetBottom(bottom, 140);

            Image bottomBg = FindOrCreateImage(bottom, "BottomNav_BG");
            StretchFull(bottomBg.rectTransform);
            bottomBg.color = BottomBarColor;
            bottomBg.raycastTarget = false;

            // 按 UI 参考图统一为较矮的胶囊按钮，避免之前 220x120 过高、视觉拥挤。
            SetupNav(bottom, "Nav_Home", "庭院", -640, "nav_home.png", 250, 108);
            SetupNav(bottom, "Nav_Character", "角色", -320, "nav_character.png", 250, 108);
            SetupNav(bottom, "Nav_Battle", "战斗", 0, "nav_battle.png", 250, 108);
            SetupNav(bottom, "Nav_Story", "剧情", 320, "nav_story.png", 250, 108);
            SetupNav(bottom, "Nav_Activity", "活动", 640, "nav_activity.png", 250, 108);

            return bottom;
        }

        private static Canvas FindOrCreateCanvas()
        {
            // 查找场景里已有的 Canvas；没有就新建一个。
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                EnsureCanvasScaler(canvas);
                return canvas;
            }

            GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            EnsureCanvasScaler(canvas);
            return canvas;
        }

        private static Sprite GetBackgroundSprite(Transform canvas)
        {
            Image bg = canvas.Find("BG_MainCourtyard") != null ? canvas.Find("BG_MainCourtyard").GetComponent<Image>() : null;
            return bg != null ? bg.sprite : null;
        }

        private static void EnsureCanvasScaler(Canvas canvas)
        {
            // CanvasScaler 用来做不同屏幕分辨率适配。
            // 这里先按横屏 1920x1080 做第一版 Demo。
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static RectTransform FindOrCreateRect(Transform parent, string name)
        {
            // 查找或创建 UI 节点。
            // 如果之前手动建的是普通 Empty，这里会自动替换成 RectTransform。
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                RectTransform existingRect = existing.GetComponent<RectTransform>();
                if (existingRect != null)
                {
                    return existingRect;
                }

                int index = existing.GetSiblingIndex();
                GameObject replacement = new GameObject(name, typeof(RectTransform));
                replacement.transform.SetParent(parent, false);
                replacement.transform.SetSiblingIndex(index);

                while (existing.childCount > 0)
                {
                    existing.GetChild(0).SetParent(replacement.transform, false);
                }

                Object.DestroyImmediate(existing.gameObject);
                return replacement.GetComponent<RectTransform>();
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image FindOrCreateImage(Transform parent, string name)
        {
            RectTransform rect = FindOrCreateRect(parent, name);
            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            return image;
        }

        private static void SetupNav(RectTransform parent, string nodeName, string label, float x, string spriteName, float width, float height)
        {
            // 创建一个底部导航按钮，并给它加 Image、Button 和文字。
            RectTransform nav = FindOrCreateRect(parent, nodeName);
            SetRect(nav, x, 0, width, height);
            AddPanelImage(nav, new Color32(255, 248, 236, 95));
            Button button = nav.GetComponent<Button>();
            if (button == null)
            {
                button = nav.gameObject.AddComponent<Button>();
            }

            button.targetGraphic = nav.GetComponent<Image>();
            Sprite sprite = LoadUISprite(spriteName);
            if (sprite != null)
            {
                // 处理后的导航图已经包含边框和中文，直接作为完整按钮皮肤使用。
                nav.GetComponent<Image>().sprite = sprite;
                nav.GetComponent<Image>().color = Color.white;
                nav.GetComponent<Image>().type = Image.Type.Simple;
                nav.GetComponent<Image>().preserveAspect = true;
            }
            else
            {
                // 如果图片还没导入，保留文字版作为安全回退。
                SetupLabel(nav, label, 32, TextAnchor.MiddleCenter);
            }
        }

        private static void BindNav(RectTransform bottomNav, string nodeName, Object target, UnityAction action)
        {
            // 把底部按钮和 HomePageRouter 的 ShowXxx 方法绑定起来。
            // 这样玩家点击按钮时，就会切换对应页面。
            Transform nav = bottomNav.Find(nodeName);
            if (nav == null)
            {
                return;
            }

            Button button = nav.GetComponent<Button>();
            if (button == null)
            {
                button = nav.gameObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static void BindButton(Transform root, string nodeName, Object target, UnityAction action)
        {
            // 通用按钮绑定方法：用于页面内部按钮，不只限于底部导航。
            if (root == null)
            {
                return;
            }

            // 有些按钮位于 Page_Home/CenterPanel 这种更深层级，不能只查直接子节点。
            Transform node = FindDescendant(root, nodeName);
            if (node == null)
            {
                return;
            }

            Button button = node.GetComponent<Button>();
            if (button == null)
            {
                button = node.gameObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static Transform FindDescendant(Transform root, string nodeName)
        {
            // 先查直接子节点，再递归查找所有后代节点。
            Transform direct = root.Find(nodeName);
            if (direct != null)
            {
                return direct;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDescendant(root.GetChild(i), nodeName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void CreateOrUpdateThemeConfigs(out UIThemeConfig realityTheme, out UIThemeConfig dreamTheme)
        {
            // 主题配置资源保存位置。
            // 这里用 ScriptableObject，是为了以后你不写代码也能在 Inspector 里换图和改颜色。
            const string themeFolder = "Assets/_Project/Resources/UIThemes";
            EnsureAssetFolder("Assets/_Project", "Resources");
            EnsureAssetFolder("Assets/_Project/Resources", "UIThemes");

            realityTheme = LoadOrCreateTheme(themeFolder + "/RealityTheme.asset");
            realityTheme.themeType = UIThemeType.Reality;
            realityTheme.displayName = "现实清雅主题";
            realityTheme.pageBackground = LoadReferenceSprite("bg1.png");
            realityTheme.mainPanelSprite = LoadCommonBgSprite("commonbg_contentBorder1.jpg");
            realityTheme.cardSprite = LoadCommonBgSprite("commonbg_contentBorder2.jpg");
            realityTheme.normalButtonSprite = LoadUISprite("common-button.png");
            realityTheme.selectedButtonSprite = LoadUISprite("common-button.png");
            realityTheme.titleColor = new Color32(122, 79, 40, 255);
            realityTheme.bodyTextColor = InkColor;
            realityTheme.buttonTextColor = ButtonTextColor;
            realityTheme.panelFallbackColor = PagePanelColor;
            realityTheme.selectedFallbackColor = ButtonColor;
            EditorUtility.SetDirty(realityTheme);

            dreamTheme = LoadOrCreateTheme(themeFolder + "/DreamTheme.asset");
            dreamTheme.themeType = UIThemeType.Dream;
            dreamTheme.displayName = "梦域星蝶主题";
            dreamTheme.pageBackground = LoadCommonBgSprite("commonbg3.png");
            dreamTheme.mainPanelSprite = LoadCommonBgSprite("commonbg_contentBorder1.jpg");
            dreamTheme.cardSprite = LoadCommonBgSprite("commonbg_contentBorder2.jpg");
            dreamTheme.normalButtonSprite = LoadUISprite("common-button.png");
            dreamTheme.selectedButtonSprite = LoadUISprite("common-button.png");
            dreamTheme.titleColor = new Color32(255, 249, 224, 255);
            dreamTheme.bodyTextColor = new Color32(255, 249, 224, 255);
            dreamTheme.buttonTextColor = new Color32(255, 249, 224, 255);
            dreamTheme.panelFallbackColor = new Color32(225, 214, 255, 170);
            dreamTheme.selectedFallbackColor = new Color32(178, 142, 255, 220);
            EditorUtility.SetDirty(dreamTheme);

            AssetDatabase.SaveAssets();
        }

        private static UIThemeConfig LoadOrCreateTheme(string assetPath)
        {
            UIThemeConfig config = AssetDatabase.LoadAssetAtPath<UIThemeConfig>(assetPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<UIThemeConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            return config;
        }

        private static void EnsureAssetFolder(string parent, string child)
        {
            // AssetDatabase.CreateFolder 只能一级一级创建，所以这里做一个安全包装。
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void AddPanelImage(RectTransform rect, Color color)
        {
            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = true;
        }

        private static void AddCommonButtonImage(RectTransform rect)
        {
            // 使用通用按钮底图，并启用九宫格，保证文字变长时边框仍然保持圆润。
            AddPanelImage(rect, Color.white);
            AddThemeElement(rect, UIThemeElementRole.NormalButton, true, true);
            Image image = rect.GetComponent<Image>();
            Sprite sprite = LoadUISprite("common-button.png");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.fillCenter = true;
            }
        }

        private static void ApplySpriteToImage(Image image, string spriteName)
        {
            // 给头像、设置等单独图标绑定处理后的 Sprite。
            Sprite sprite = LoadUISprite(spriteName);
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
            }
        }

        private static Sprite LoadUISprite(string spriteName)
        {
            // Unity 默认可能把新导入 PNG 当作普通纹理，这里自动改成 Sprite。
            return LoadSpriteAtPath("Assets/_Project/Art/UI/OperateIcons/" + spriteName, spriteName);
        }

        private static Sprite LoadReferenceSprite(string spriteName)
        {
            // 加载主线、编队参考素材目录中的背景或章节框。
            return LoadSpriteAtPath("Assets/_Project/Art/UI/TabNavBackgrounds/" + spriteName, spriteName);
        }

        private static Sprite LoadCommonBgSprite(string spriteName)
        {
            // commonBg 是当前梦域/主线通用背景资源目录，先统一从这里取图。
            return LoadSpriteAtPath("Assets/_Project/Art/UI/CommonBg/" + spriteName, spriteName);
        }

        private static Sprite LoadSpriteAtPath(string assetPath, string spriteName)
        {
            // 统一把图片导入为 Sprite，避免 Image 无法显示普通 Texture。
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                if (spriteName == "common-button.png")
                {
                    // 给通用胶囊按钮设置九宫格边距，拉伸时只拉伸中间区域。
                    importer.spriteBorder = new Vector4(280f, 220f, 280f, 220f);
                }
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void AddThemeElement(RectTransform rect, UIThemeElementRole role, bool applySprite, bool applyColor)
        {
            // 给 UI 对象添加主题标记。
            // 以后切换现实/梦域主题时，UIThemeApplier 会根据这个标记统一换图和换颜色。
            UIThemeElement element = rect.GetComponent<UIThemeElement>();
            if (element == null)
            {
                element = rect.gameObject.AddComponent<UIThemeElement>();
            }

            element.role = role;
            element.applySprite = applySprite;
            element.applyColor = applyColor;
        }

        private static void SetupLabel(RectTransform parent, string text, int size, TextAnchor alignment)
        {
            // 给某个 UI 节点创建或更新文字。
            // 统一用 Label 作为文字子节点，方便后面继续改样式。
            RectTransform labelRect = FindOrCreateRect(parent, "Label");
            StretchFull(labelRect);

            Text label = labelRect.GetComponent<Text>();
            if (label == null)
            {
                label = labelRect.gameObject.AddComponent<Text>();
            }

            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = InkColor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            AddThemeElement(labelRect, UIThemeElementRole.BodyText, false, true);

            if (label.font == null)
            {
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        private static void SetupButtonLabel(RectTransform parent, string text, int size, TextAnchor alignment)
        {
            // 普通按钮统一使用暖金色文字，和 font-color.txt 的配置保持一致。
            SetupLabel(parent, text, size, alignment);
            Text label = parent.Find("Label") != null ? parent.Find("Label").GetComponent<Text>() : null;
            if (label != null)
            {
                label.color = ButtonTextColor;
                AddThemeElement(label.rectTransform, UIThemeElementRole.ButtonText, false, true);
            }
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void DestroyChildIfExists(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTop(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, height);
        }

        private static void SetBottom(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, height);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
    }
}
