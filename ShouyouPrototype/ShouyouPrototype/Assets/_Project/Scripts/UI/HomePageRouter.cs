using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Shouyou.Data;
using Shouyou.Network;
using Shouyou.UI.Theme;

namespace Shouyou.UI
{
    /// <summary>
    /// 首页与主要功能页路由。
    /// 负责页面切换、主线栏目切换、详情弹窗、战斗结算弹窗等基础交互。
    /// </summary>
    public sealed class HomePageRouter : MonoBehaviour
    {
        [Header("一级页面")]
        [SerializeField] private GameObject homePage;
        [SerializeField] private GameObject characterPage;
        [SerializeField] private GameObject battlePage;
        [SerializeField] private GameObject storyPage;
        [SerializeField] private GameObject activityPage;
        [SerializeField] private GameObject mainlineChapterPage;
        [SerializeField] private GameObject formationPage;
        [SerializeField] private GameObject dreamDomainPage;

        [Header("公共 UI")]
        [SerializeField] private GameObject topBarRoot;
        [SerializeField] private GameObject bottomNavRoot;

        [Header("主线页内部栏目")]
        [SerializeField] private GameObject mainlineStoryTab;
        [SerializeField] private GameObject mainlineFormationTab;
        [SerializeField] private GameObject mainlineTrainingTab;
        [SerializeField] private GameObject mainlineDreamActivityTab;

        [Header("主线左侧栏目按钮")]
        [SerializeField] private Button mainlineStoryCategoryButton;
        [SerializeField] private Button mainlineFormationCategoryButton;
        [SerializeField] private Button mainlineTrainingCategoryButton;
        [SerializeField] private Button mainlineDreamActivityCategoryButton;

        [Header("底部导航按钮")]
        [SerializeField] private Button homeNavButton;
        [SerializeField] private Button characterNavButton;
        [SerializeField] private Button battleNavButton;
        [SerializeField] private Button storyNavButton;
        [SerializeField] private Button activityNavButton;

        [Header("详情弹窗")]
        [SerializeField] private GameObject storyDetailPanel;
        [SerializeField] private Text storyDetailTitle;
        [SerializeField] private Text storyDetailBody;
        [SerializeField] private GameObject sceneListPanel;

        [Header("详情弹窗按钮")]
        [SerializeField] private Button storyReadButton;
        [SerializeField] private Button storySkipButton;
        [SerializeField] private Button storyReplayButton;
        [SerializeField] private Button storyBattleButton;
        [SerializeField] private Button storyCloseButton;

        [Header("详情弹窗按钮文本")]
        [SerializeField] private Text storyReadButtonLabel;
        [SerializeField] private Text storySkipButtonLabel;
        [SerializeField] private Text storyReplayButtonLabel;
        [SerializeField] private Text storyBattleButtonLabel;
        [SerializeField] private Text storyCloseButtonLabel;

        [Header("导航颜色")]
        [SerializeField] private Color normalNavColor = new Color32(255, 248, 236, 95);
        [SerializeField] private Color selectedNavColor = new Color32(238, 190, 125, 220);

        /// <summary>
        /// 当前选中的主线关卡名称。
        /// </summary>
        private string currentMainlineStageName = "1-1 明水入汴京";

        /// <summary>
        /// 当前选中的主线关卡序号。
        /// UI 不再只靠标题字符串判断下一关，避免标题改名后流程失效。
        /// </summary>
        private int currentMainlineStageId = 1;

        /// <summary>
        /// 当前选中的主线关卡是否已解锁。
        /// </summary>
        private bool currentMainlineStageUnlocked = true;

        /// <summary>
        /// 当前这场战斗是否已经结算过。
        /// 防止玩家连续点击“开始本关”导致重复领奖、重复推进进度。
        /// </summary>
        private bool currentBattleAlreadySettled;

        /// <summary>
        /// 战斗结算弹窗按钮锁。
        /// 防止同一帧或连续点击多个结算按钮造成页面状态互相覆盖。
        /// </summary>
        private bool battleResultActionLocked;

        /// <summary>
        /// 剧情阅读状态只服务于当前详情弹窗，关闭详情或切换关卡后会重置。
        /// 已读记录由 LevelProgressManager 持久化，不依赖这里的临时字段。
        /// </summary>
        private bool storyReadingActive;
        private int currentStoryLineIndex = -1;
        private float storyReadingStartedAt;

        /// <summary>
        /// 进入剧情三秒后才允许跳过，保留开场的情绪铺垫。
        /// </summary>
        private const float StorySkipDelaySeconds = 3f;

        private void Awake()
        {
            EnsureRuntimeReferences();
            ConfigureStoryDetailForGeneric();
            ShowHome();
        }

        // -------------------------
        // 一级页面切换
        // -------------------------

        public void ShowHome()
        {
            ShowOnly(homePage);
        }

        public void ShowCharacter()
        {
            ShowOnly(characterPage);
        }

        public void ShowBattle()
        {
            currentBattleAlreadySettled = false;
            battleResultActionLocked = false;
            ShowOnly(battlePage);

            BattleDemoController battleController = battlePage == null ? null : battlePage.GetComponent<BattleDemoController>();
            if (battlePage != null && battleController == null)
            {
                battleController = battlePage.AddComponent<BattleDemoController>();
            }

            if (battleController != null)
            {
                // 战斗页需要知道从哪一关进入，结算才能回写正确的主线进度。
                battleController.ConfigureStageContext(currentMainlineStageId, currentMainlineStageName);
                battleController.ResetDemoBattle();
            }
        }

        public void ShowStory()
        {
            ShowOnly(storyPage);
        }

        public void ShowActivity()
        {
            ShowOnly(activityPage);
        }

        public void ShowMainlineChapter()
        {
            ShowOnly(mainlineChapterPage);
            ShowMainlineStoryTab();
        }

        public void ShowFormation()
        {
            ShowOnly(formationPage);

            FormationDemoController formationController = EnsureFormationController();
            if (formationController != null)
            {
                formationController.LoadFormationFromBackendCache();
            }
        }

        public void ShowDreamDomain()
        {
            ShowOnly(dreamDomainPage);
        }

        public void ReturnMainlineDreamTab()
        {
            ShowOnly(mainlineChapterPage);
            ShowMainlineDreamActivityTab();
        }

        public void EnterMainline()
        {
            ShowMainlineChapter();
        }

        public void ReturnHome()
        {
            ShowHome();
        }

        // -------------------------
        // 主线页签切换
        // -------------------------

        public void ShowMainlineStoryTab()
        {
            ShowMainlineTab(mainlineStoryTab);
        }

        public void ShowMainlineFormationTab()
        {
            ShowMainlineTab(mainlineFormationTab);
        }

        public void ShowMainlineTrainingTab()
        {
            ShowMainlineTab(mainlineTrainingTab);
        }

        public void ShowMainlineDreamActivityTab()
        {
            ShowMainlineTab(mainlineDreamActivityTab);
        }

        public void ShowTrainingCategory()
        {
            ShowMainlineTrainingTab();
        }

        public void ShowActivityCategory()
        {
            ShowMainlineDreamActivityTab();
        }

        // -------------------------
        // 梦域与主题
        // -------------------------

        public void ShowDreamNodeDetail()
        {
            ShowStoryDetail(
                "梦域节点",
                "这里会进入梦域记忆节点。\n\n第一版规划：\n1. 选择记忆节点\n2. 触发剧情或轻战斗\n3. 获得梦蝶赠礼、神识、角色行迹材料\n\n正式版会根据主线进度逐步解锁。"
            );
        }

        public void ToggleThemeForTest()
        {
            ShowDebugStatus();
        }

        public void ShowDebugStatus()
        {
            string body =
                "【开发状态面板】" +
                "\n\n当前页面关卡：" + currentMainlineStageName +
                "\n当前关卡序号：" + currentMainlineStageId +
                "\n当前关卡状态：" + LevelProgressManager.Instance.GetStageStateLabel(currentMainlineStageId) +
                "\n本地最高通关：" + LevelProgressManager.Instance.GetHighestClearedStageId() +
                "\n本场战斗是否已结算：" + (currentBattleAlreadySettled ? "是" : "否") +
                "\n结算按钮锁：" + (battleResultActionLocked ? "已锁定" : "未锁定") +
                "\n\n" + ShouyouBackendBootstrap.GetDebugSummary() +
                "\n\n提示：这个面板是开发调试用，后续正式 UI 会隐藏。";

            ShowStoryDetail("开发状态", body);
        }

        public void ToggleThemeOnlyForTest()
        {
            UIThemeApplier themeApplier = GetComponent<UIThemeApplier>();
            if (themeApplier == null)
            {
                ShowStoryDetail(
                    "主题切换",
                    "当前 Canvas 上没有找到 UIThemeApplier。\n\n请先执行 Shouyou > UI > Clean And Rebuild Prototype 重新生成 UI。"
                );
                return;
            }

            themeApplier.ToggleTheme();
        }

        // -------------------------
        // 剧情详情
        // -------------------------

        public void ShowChapterOneDetail()
        {
            ShowStoryDetail(
                "第一章：竹堂初语",
                "李清照十五岁，获父亲李格非允许，与婉禾一同赴汴京雅集。\n\n本章重点：竹堂父女谈话、少女的期待、雅集目标建立。\n\n资源：李府竹堂、翠竹院落、父女立绘、竹影 CG。\n\n状态：已解锁"
            );
        }

        public void ShowChapterTwoDetail()
        {
            ShowStoryDetail(
                "第二章：灯下共稿（暂定）",
                "婉禾带着新作来访，两人在临窗小室共同推敲词稿。李清照从闺中闲趣想到更广阔的山河风月。\n\n本章重点：婉禾来访、灯下共稿、李清照夜里独自修改词作。\n\n资源：李府院门、回廊、临窗小室、烛下共稿 CG。\n\n状态：已解锁"
            );
        }

        public void ShowChapterThreeDetail()
        {
            ShowStoryDetail(
                "第三章：汴京雅集，初逢群英",
                "3-1 清晨赴会·街巷同行\n3-2 入园落座·初次被围观\n3-3 众贤落笔·词作交流\n3-4 前辈主动搭话\n3-5 献词全场\n3-6 全场惊艳\n3-7 雅集尾声·心境升华\n\n核心：李清照以《浣溪沙》回应质疑，白衣少年在终场埋下伏笔。\n\n状态：未开始"
            );

            SetActive(sceneListPanel, true);
        }

        public void CloseStoryDetail()
        {
            storyReadingActive = false;
            currentStoryLineIndex = -1;
            SetActive(storyDetailPanel, false);
        }

        public void StartStoryReading()
        {
            if (!currentMainlineStageUnlocked)
            {
                SetStoryBody(currentMainlineStageName + "\n\n该关卡暂未解锁。\n\n正式版本会根据主线进度、角色等级和前置关卡判断是否可读。");
                return;
            }

            currentStoryLineIndex = 0;
            storyReadingActive = true;
            storyReadingStartedAt = Time.unscaledTime;
            RenderCurrentStoryLine();
            ConfigureStoryDetailForReading();
        }

        public void SkipStory()
        {
            if (!currentMainlineStageUnlocked)
            {
                ShowLockedStageHint();
                return;
            }

            if (!storyReadingActive)
            {
                StartStoryReading();
                return;
            }

            float elapsed = Time.unscaledTime - storyReadingStartedAt;
            if (elapsed < StorySkipDelaySeconds)
            {
                int remaining = Mathf.CeilToInt(StorySkipDelaySeconds - elapsed);
                SetStoryBody("剧情正在展开。\n\n" + remaining + " 秒后可跳过；也可以点击“下一句”继续阅读。");
                return;
            }

            CompleteStoryReading(true);
        }

        public void ReplayStory()
        {
            if (!currentMainlineStageUnlocked)
            {
                ShowLockedStageHint();
                return;
            }

            StartStoryReading();
        }

        // -------------------------
        // 角色相关
        // -------------------------

        public void ShowCharacterDetail()
        {
            ShowStoryDetail(
                "李清照 · 角色详情",
                "稀有度：SSR\n定位：词意输出 / 群体辅助\n等级：Lv.1 / 60\n词意：如梦令\n生命：1200    攻击：180    防御：95\n\n她的成长主题是：以笔墨突破闺阁边界。"
            );
        }

        public void ShowTrainingInfo()
        {
            ShowStoryDetail(
                "角色养成",
                "升级：提升基础属性\n突破：提高等级上限\n技能：解锁词意效果\n装备：强化战斗定位\n\n当前版本先展示入口，后续接入材料和数值消耗。"
            );
        }

        public void ShowBondInfo()
        {
            ShowStoryDetail(
                "角色羁绊",
                "与婉禾共同完成剧情，可解锁羁绊等级、专属对话和梦境支线。\n\n正式版本将在这里显示角色关系网。"
            );
        }

        // -------------------------
        // 编队相关
        // -------------------------

        public void ShowFormationSlotOne() { SelectFormationSlot(1); }
        public void ShowFormationSlotTwo() { SelectFormationSlot(2); }
        public void ShowFormationSlotThree() { SelectFormationSlot(3); }
        public void ShowFormationSlotFour() { SelectFormationSlot(4); }
        public void ShowFormationSlotFive() { SelectFormationSlot(5); }
        public void ShowFormationSlotSix() { SelectFormationSlot(6); }

        public void EditFormation()
        {
            ShowStoryDetail(
                "编辑阵容",
                "正式版本将在这里打开角色选择列表。\n\n当前 Demo 已预留六个位置：前排 3 人、后排 3 人。\n点击空位可以继续接入角色选择。\n\n当前队伍：" +
                ShouyouBackendBootstrap.GetFormationSummary() +
                "\n当前战力：" + ShouyouBackendBootstrap.GetFormationPower()
            );
        }

        public void SaveFormation()
        {
            FormationDemoController formationController = EnsureFormationController();
            if (formationController != null)
            {
                formationController.SaveCurrentFormation();
            }
            else
            {
                ShouyouBackendBootstrap.SaveCurrentDemoFormation();
            }

            ShowStoryDetail(
                "保存编队",
                "正在保存到本地后端。\n\n如果 ShouyouServer 已启动，Unity Console 会看到“编队已保存到后端”。\n\n当前保存内容：\n" +
                ShouyouBackendBootstrap.GetFormationSummary()
            );
        }

        public void PreviewBond()
        {
            ShowStoryDetail(
                "羁绊预览",
                "同调角色共同出战时，可以触发气韵增益。\n\n李清照：词意输出\n婉禾：辅助与协奏\n\n后续会在这里显示角色关系、羁绊等级和梦境解锁条件。"
            );
        }

        // -------------------------
        // 主线关卡与战斗
        // -------------------------

        public void ShowStageOne() { ShowStageDetail(1); }
        public void ShowStageTwo() { ShowStageDetail(2); }
        public void ShowStageThree() { ShowStageDetail(3); }
        public void ShowStageFour() { ShowStageDetail(4); }
        public void ShowStageFive() { ShowStageDetail(5); }
        public void ShowStageSix() { ShowStageDetail(6); }

        public void ShowMainlineStageOne() { ShowMainlineStageDetail(MainlineStageCatalog.Get(1)); }
        public void ShowMainlineStageTwo() { ShowMainlineStageDetail(MainlineStageCatalog.Get(2)); }
        public void ShowMainlineStageThree() { ShowMainlineStageDetail(MainlineStageCatalog.Get(3)); }
        public void ShowMainlineStageFour() { ShowMainlineStageDetail(MainlineStageCatalog.Get(4)); }
        public void ShowMainlineStageFive() { ShowMainlineStageDetail(MainlineStageCatalog.Get(5)); }
        public void ShowMainlineStageSix() { ShowMainlineStageDetail(MainlineStageCatalog.Get(6)); }

        public void EnterBattlePrototype()
        {
            if (!currentMainlineStageUnlocked)
            {
                ShowStoryDetail(
                    "进入战斗",
                    currentMainlineStageName + "\n\n该关卡暂未解锁，不能进入战斗。\n\n正式版本会提示玩家先完成前置剧情或提升角色等级。"
                );
                return;
            }

            if (!ShouyouBackendBootstrap.HasBattleReadyFormation())
            {
                ShowStoryDetail(
                    "进入战斗",
                    currentMainlineStageName +
                    "\n\n当前没有可出战角色，不能开始战斗。\n\n请先进入“行迹编队”至少放入 1 名角色。"
                );
                return;
            }

            ShowBattle();
        }

        public void ResolveBattleVictory()
        {
            if (currentBattleAlreadySettled)
            {
                return;
            }

            currentBattleAlreadySettled = true;
            ShowBattleVictoryDetail();
        }

        public void ResolveBattleDefeat()
        {
            if (currentBattleAlreadySettled)
            {
                return;
            }

            currentBattleAlreadySettled = true;
            ShowStoryDetail(
                "\u6218\u6597\u5931\u8d25",
                currentMainlineStageName +
                "\n\n\u6211\u65b9\u5168\u5458\u5df2\u65e0\u6cd5\u7ee7\u7eed\u884c\u52a8\u3002\n\n\u5f53\u524d Demo \u4e0d\u6263\u9664\u8d44\u6e90\u3002\u5efa\u8bae\u5148\u8c03\u6574\u7f16\u961f\uff0c\u6216\u76f4\u63a5\u91cd\u6218\u672c\u5173\u3002"
            );
            ConfigureStoryDetailForBattleDefeat();
        }

        // -------------------------
        // 第三章场景剧情快速预览入口（仅演示用）
        // -------------------------

        public void ShowScene31() { SetStoryBody("3-1 清晨赴会·街巷同行\n李清照与婉禾第一次走入汴京文坛社交场。"); }
        public void ShowScene32() { SetStoryBody("3-2 入园落座·初次被围观\n二人因闺阁女子身份受到好奇与轻视。"); }
        public void ShowScene33() { SetStoryBody("3-3 众贤落笔·词作交流\n雅集以春日风物为题，众人即兴填词。"); }
        public void ShowScene34() { SetStoryBody("3-4 前辈主动搭话\n周学士主动与李清照一对一论词。"); }
        public void ShowScene35() { SetStoryBody("3-5 献词全场\n李清照呈上《浣溪沙》，回应全场质疑。"); }
        public void ShowScene36() { SetStoryBody("3-6 全场惊艳\n众人改变态度，认可李清照的才华。"); }
        public void ShowScene37() { SetStoryBody("3-7 雅集尾声·心境升华\n深度词学交流结束，白衣少年伏笔出现。"); }

        // -------------------------
        // 内部实现
        // -------------------------

        /// <summary>
        /// 只显示目标一级页面，其余一级页面全部隐藏。
        /// 同时关闭详情弹窗，避免跨页残留。
        /// </summary>
        private void ShowOnly(GameObject target)
        {
            SetActive(homePage, target == homePage);
            SetActive(characterPage, target == characterPage);
            SetActive(battlePage, target == battlePage);
            SetActive(storyPage, target == storyPage);
            SetActive(activityPage, target == activityPage);
            SetActive(mainlineChapterPage, target == mainlineChapterPage);
            SetActive(formationPage, target == formationPage);
            SetActive(dreamDomainPage, target == dreamDomainPage);
            SetActive(storyDetailPanel, false);

            ApplySharedChromeVisibility(target);

            SetNavSelected(homeNavButton, target == homePage);
            SetNavSelected(characterNavButton, target == characterPage);
            SetNavSelected(battleNavButton, target == battlePage);
            SetNavSelected(storyNavButton, target == storyPage);
            SetNavSelected(activityNavButton, target == activityPage);
        }

        /// <summary>
        /// 控制顶部栏和底部导航是否显示。
        /// 庭院是主入口，可以显示公共 UI；战斗、主线、编队、梦域等全屏模块需要沉浸展示，
        /// 不能继续露出半透明顶栏和底栏，避免破坏画面和遮挡模块自己的操作区。
        /// </summary>
        private void ApplySharedChromeVisibility(GameObject target)
        {
            bool showSharedChrome = target == homePage;
            SetActive(topBarRoot, showSharedChrome);
            SetActive(bottomNavRoot, showSharedChrome);
        }

        /// <summary>
        /// 切换主线页内栏目。
        /// </summary>
        private void ShowMainlineTab(GameObject target)
        {
            SetActive(mainlineStoryTab, target == mainlineStoryTab);
            SetActive(mainlineFormationTab, target == mainlineFormationTab);
            SetActive(mainlineTrainingTab, target == mainlineTrainingTab);
            SetActive(mainlineDreamActivityTab, target == mainlineDreamActivityTab);

            SetMainlineCategorySelected(mainlineStoryCategoryButton, target == mainlineStoryTab);
            SetMainlineCategorySelected(mainlineFormationCategoryButton, target == mainlineFormationTab);
            SetMainlineCategorySelected(mainlineTrainingCategoryButton, target == mainlineTrainingTab);
            SetMainlineCategorySelected(mainlineDreamActivityCategoryButton, target == mainlineDreamActivityTab);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private void SetNavSelected(Button button, bool selected)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? selectedNavColor : normalNavColor;
            }
        }

        private void SetMainlineCategorySelected(Button button, bool selected)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? selectedNavColor : normalNavColor;
            }
        }

        /// <summary>
        /// 通用详情弹窗。
        /// 默认使用剧情/说明类按钮布局。
        /// </summary>
        private void ShowStoryDetail(string title, string body)
        {
            if (storyDetailPanel == null)
            {
                return;
            }

            ConfigureStoryDetailForGeneric();
            storyDetailPanel.SetActive(true);
            SetActive(sceneListPanel, false);
            SetStoryText(storyDetailTitle, title);
            SetStoryText(storyDetailBody, body);
        }

        /// <summary>
        /// 战斗胜利后的结算弹窗。
        /// 仍复用当前详情弹窗容器，但会替换按钮语义和去向。
        /// </summary>
        private void ShowBattleVictoryDetail()
        {
            if (storyDetailPanel == null)
            {
                return;
            }

            bool progressAdvanced = LevelProgressManager.Instance.CompleteStage(currentMainlineStageId);
            ShouyouBackendBootstrap.CompleteMainlineStage(currentMainlineStageId);
            int nextStageId = LevelProgressManager.Instance.GetNextStageId(currentMainlineStageId);
            MainlineStageInfo nextStage = MainlineStageCatalog.Get(nextStageId);
            MainlineStageInfo completedStage = MainlineStageCatalog.Get(currentMainlineStageId);
            RewardItem[] stageRewards = MainlineStageCatalog.GetRewards(completedStage.id);
            // 结算奖励实际入账：通关后立即写入本地资源钱包，不再只是展示文字。
            PlayerResourceManager.Instance.GrantRewards(stageRewards);
            string rewardText = BuildBattleRewardText(stageRewards, completedStage.rewardPreview);
            string balanceText = BuildResourceBalanceText(stageRewards);
            string rewardSection = string.IsNullOrEmpty(balanceText)
                ? rewardText
                : rewardText + "\n" + balanceText;
            string progressText = progressAdvanced
                ? "主线进度已推进，下一关已解锁：" + nextStage.title
                : "该关卡此前已通关，本次为重复挑战，不重复推进主线进度。";

            battleResultActionLocked = false;
            storyDetailPanel.SetActive(true);
            SetActive(sceneListPanel, false);
            SetStoryText(storyDetailTitle, "战斗胜利");
            SetStoryText(
                storyDetailBody,
                currentMainlineStageName +
                "\n\n李清照发动词意：如梦令。\n队伍获得气韵增益，顺利完成本次 PVE 试炼。" +
                "\n\n出战队伍：" + ShouyouBackendBootstrap.GetFormationSummary() +
                "\n队伍战力：" + ShouyouBackendBootstrap.GetFormationPower() +
                "\n\n结算奖励：\n" + rewardSection + "\n主线进度 +1" +
                "\n\n" + progressText +
                "\n\n下一步你可以返回主线继续选关，也可以先去编队调整阵容。"
            );
            ConfigureStoryDetailForBattleVictory();
        }

        /// <summary>
        /// 奖励列表可用时逐条显示；缺失、为空或没有有效项时保留旧的 rewardPreview 兜底。
        /// </summary>
        private string BuildBattleRewardText(RewardItem[] rewards, string rewardPreview)
        {
            string renderedRewards = string.Empty;
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Length; i++)
                {
                    RewardItem reward = rewards[i];
                    if (reward == null || string.IsNullOrEmpty(reward.name) || reward.amount <= 0)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(renderedRewards))
                    {
                        renderedRewards += "\n";
                    }

                    renderedRewards += reward.name + " ×" + reward.amount;
                }
            }

            return string.IsNullOrEmpty(renderedRewards)
                ? (string.IsNullOrEmpty(rewardPreview) ? "暂无奖励信息" : rewardPreview)
                : renderedRewards;
        }

        /// <summary>
        /// 生成结算后各类资源的当前持有数量文本。
        ///
        /// 按奖励的 id 去重，逐条读取 PlayerResourceManager 的入账后余额。
        /// 奖励列表为空或全部无效时返回空字符串，由调用方决定是否拼接。
        /// </summary>
        private string BuildResourceBalanceText(RewardItem[] rewards)
        {
            if (rewards == null)
            {
                return string.Empty;
            }

            var seenIds = new HashSet<string>();
            var balanceLines = new List<string>();
            for (int i = 0; i < rewards.Length; i++)
            {
                RewardItem reward = rewards[i];
                if (reward == null || string.IsNullOrEmpty(reward.id) || reward.amount <= 0)
                {
                    continue;
                }

                if (!seenIds.Add(reward.id))
                {
                    continue;
                }

                balanceLines.Add("当前持有：" + reward.name + " ×" + PlayerResourceManager.Instance.GetCount(reward.id));
            }

            return balanceLines.Count == 0 ? string.Empty : string.Join("\n", balanceLines);
        }

        /// <summary>
        /// 通用剧情弹窗按钮配置。
        /// </summary>
        private void ConfigureStoryDetailForGeneric()
        {
            ConfigureDetailButton(storyReadButton, storyReadButtonLabel, "开始阅读", true, StartStoryReading);
            ConfigureDetailButton(storySkipButton, storySkipButtonLabel, "跳过剧情", true, SkipStory);
            ConfigureDetailButton(storyReplayButton, storyReplayButtonLabel, "回看剧情", true, ReplayStory);
            ConfigureDetailButton(storyBattleButton, storyBattleButtonLabel, "进入战斗", true, EnterBattlePrototype);
            ConfigureDetailButton(storyCloseButton, storyCloseButtonLabel, "关闭详情", true, CloseStoryDetail);
        }

        /// <summary>
        /// 主线关卡详情弹窗按钮配置。
        ///
        /// 未解锁关卡只保留提示和关闭入口，避免玩家误点“进入战斗”后以为功能失效。
        /// 已通关关卡仍允许重复挑战，但不会重复推进主线进度。
        /// </summary>
        private void ConfigureStoryDetailForMainlineStage(bool unlocked, bool cleared)
        {
            bool storyRead = unlocked && LevelProgressManager.Instance.IsStoryRead(currentMainlineStageId);
            UnityEngine.Events.UnityAction readAction = unlocked
                ? new UnityEngine.Events.UnityAction(StartStoryReading)
                : new UnityEngine.Events.UnityAction(ShowLockedStageHint);

            ConfigureDetailButton(
                storyReadButton,
                storyReadButtonLabel,
                unlocked ? (storyRead ? "重读剧情" : "开始阅读") : "未解锁",
                true,
                readAction);

            ConfigureDetailButton(
                storySkipButton,
                storySkipButtonLabel,
                "跳过剧情",
                unlocked,
                SkipStory);

            ConfigureDetailButton(
                storyReplayButton,
                storyReplayButtonLabel,
                "回看剧情",
                storyRead,
                ReplayStory);

            ConfigureDetailButton(
                storyBattleButton,
                storyBattleButtonLabel,
                cleared ? "再次战斗" : "进入战斗",
                unlocked,
                EnterBattlePrototype);

            ConfigureDetailButton(storyCloseButton, storyCloseButtonLabel, "关闭详情", true, CloseStoryDetail);
        }

        /// <summary>
        /// 战斗结算弹窗按钮配置。
        /// </summary>
        private void ConfigureStoryDetailForBattleVictory()
        {
            ConfigureDetailButton(storyReadButton, storyReadButtonLabel, "\u8fd4\u56de\u5173\u5361", true, OnBattleResultReturnMainline);
            ConfigureDetailButton(storySkipButton, storySkipButtonLabel, "\u8c03\u6574\u7f16\u961f", true, OnBattleResultOpenFormation);
            ConfigureDetailButton(storyReplayButton, storyReplayButtonLabel, "\u91cd\u6218\u672c\u5173", true, OnBattleResultReplay);
            ConfigureDetailButton(storyBattleButton, storyBattleButtonLabel, "\u4e0b\u4e00\u5173", true, OnBattleResultContinueNext);
            ConfigureDetailButton(storyCloseButton, storyCloseButtonLabel, "\u6536\u8d77\u7ed3\u7b97", true, OnBattleResultClose);
        }

        /// <summary>
        /// 战斗失败结算弹窗按钮配置。
        /// 失败不推进主线进度，只保留返回/编队/重战/收起入口。
        /// </summary>
        private void ConfigureStoryDetailForBattleDefeat()
        {
            ConfigureDetailButton(storyReadButton, storyReadButtonLabel, "\u8fd4\u56de\u5173\u5361", true, OnBattleResultReturnMainline);
            ConfigureDetailButton(storySkipButton, storySkipButtonLabel, "\u8c03\u6574\u7f16\u961f", true, OnBattleResultOpenFormation);
            ConfigureDetailButton(storyReplayButton, storyReplayButtonLabel, "\u91cd\u6218\u672c\u5173", true, OnBattleResultReplay);
            ConfigureDetailButton(storyBattleButton, storyBattleButtonLabel, "\u7ee7\u7eed\u67e5\u770b", false, OnBattleResultClose);
            ConfigureDetailButton(storyCloseButton, storyCloseButtonLabel, "\u6536\u8d77\u7ed3\u7b97", true, OnBattleResultClose);
        }

        /// <summary>
        /// 战斗结算后返回主线章节页。
        /// </summary>
        private void ReturnToMainlineAfterBattle()
        {
            ShowMainlineChapter();
        }

        /// <summary>
        /// 继续下一关。
        /// 第一版先进入下一关的详情弹窗。
        /// </summary>
        private void ContinueToNextMainlineStage()
        {
            int nextId = LevelProgressManager.Instance.GetNextStageId(currentMainlineStageId);
            ShowMainlineChapter();
            ShowMainlineStageDetail(MainlineStageCatalog.Get(nextId));
        }

        /// <summary>
        /// 战斗结算按钮：返回主线。
        /// </summary>
        private void OnBattleResultReturnMainline()
        {
            if (TryLockBattleResultAction())
            {
                ReturnToMainlineAfterBattle();
            }
        }

        /// <summary>
        /// 战斗结算按钮：进入编队。
        /// </summary>
        private void OnBattleResultOpenFormation()
        {
            if (TryLockBattleResultAction())
            {
                ShowFormation();
            }
        }

        /// <summary>
        /// 战斗结算按钮：再来一战。
        /// </summary>
        private void OnBattleResultReplay()
        {
            if (TryLockBattleResultAction())
            {
                ShowBattle();
            }
        }

        /// <summary>
        /// 战斗结算按钮：继续下一关。
        /// </summary>
        private void OnBattleResultContinueNext()
        {
            if (TryLockBattleResultAction())
            {
                ContinueToNextMainlineStage();
            }
        }

        /// <summary>
        /// 战斗结算按钮：关闭结算。
        /// </summary>
        private void OnBattleResultClose()
        {
            if (TryLockBattleResultAction())
            {
                CloseStoryDetail();
            }
        }

        /// <summary>
        /// 尝试锁住战斗结算按钮。
        /// 返回 false 表示本次点击已经被其它按钮处理过，需要忽略。
        /// </summary>
        private bool TryLockBattleResultAction()
        {
            if (battleResultActionLocked)
            {
                return false;
            }

            battleResultActionLocked = true;
            return true;
        }

        /// <summary>
        /// 安全配置详情弹窗按钮。
        /// 负责显示、文案和点击事件绑定。
        /// </summary>
        private void ConfigureDetailButton(Button button, Text label, string text, bool visible, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            button.onClick.RemoveAllListeners();
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            SetStoryText(label, text);
        }

        private void SelectFormationSlot(int slotIndex)
        {
            FormationDemoController formationController = EnsureFormationController();
            if (formationController != null)
            {
                if (slotIndex == 1) formationController.SelectSlotOne();
                if (slotIndex == 2) formationController.SelectSlotTwo();
                if (slotIndex == 3) formationController.SelectSlotThree();
                if (slotIndex == 4) formationController.SelectSlotFour();
                if (slotIndex == 5) formationController.SelectSlotFive();
                if (slotIndex == 6) formationController.SelectSlotSix();
                return;
            }

            ShowFormationSlot(GetFormationSlotPositionLabel(slotIndex) + "：" + GetFormationSlotLabel(slotIndex));
        }

        private void ShowFormationSlot(string slotName)
        {
            ShowStoryDetail("编队位置", slotName + "\n\n点击空位后，正式版本会打开角色选择列表。\n当前先保留位置反馈。");
        }

        private string GetFormationSlotPositionLabel(int slotIndex)
        {
            return slotIndex <= 3 ? "前排 " + slotIndex : "后排 " + (slotIndex - 3);
        }

        private string GetFormationSlotLabel(int slotIndex)
        {
            string[] labels = ShouyouBackendBootstrap.GetFormationSummary().Split('/');
            if (slotIndex <= 0 || slotIndex > labels.Length)
            {
                return "空位";
            }

            return labels[slotIndex - 1].Trim();
        }

        /// <summary>
        /// 未解锁关卡提示。
        /// 这个方法只更新弹窗正文，不切换页面。
        /// </summary>
        private void ShowLockedStageHint()
        {
            SetStoryBody(
                currentMainlineStageName +
                "\n\n该关卡暂未解锁。\n\n请先完成前置关卡。正式版本还会检查角色等级、体力和剧情前置条件。"
            );
        }

        /// <summary>
        /// 旧版关卡按钮入口。
        ///
        /// 场景里可能还有按钮绑定 ShowStageOne/Two 这一组旧方法，
        /// 所以这里不删除公开入口，只把它们统一转到新版主线关卡数据。
        /// </summary>
        private void ShowStageDetail(int stageId)
        {
            ShowMainlineStageDetail(MainlineStageCatalog.Get(stageId));
        }

        private void ShowMainlineStageDetail(MainlineStageInfo stage)
        {
            currentMainlineStageId = stage.id;
            currentMainlineStageName = stage.title;
            currentMainlineStageUnlocked = LevelProgressManager.Instance.IsStageUnlocked(stage.id);
            bool cleared = LevelProgressManager.Instance.IsStageCleared(stage.id);
            bool storyRead = LevelProgressManager.Instance.IsStoryRead(stage.id);
            storyReadingActive = false;
            currentStoryLineIndex = -1;

            string actionHint = currentMainlineStageUnlocked
                ? (cleared ? "可操作：回看剧情 / 重复挑战 / 再次战斗" : "可操作：开始阅读 / 进入战斗")
                : "可操作：查看信息。阅读和战斗需要先完成前置条件。";

            string body =
                "推荐等级：Lv." + stage.recommendLevel +
                "\n推荐战力：" + stage.recommendPower +
                "\n关卡目标：" + stage.objective +
                "\n奖励：" + stage.rewardPreview +
                "\n\n体力消耗：6" +
                "\n关卡类型：剧情 + PVE" +
                "\n状态：" + LevelProgressManager.Instance.GetStageStateLabel(stage.id) +
                "\n剧情记录：" + (storyRead ? "已阅读" : "未阅读") +
                "\n通关记录：" + (cleared ? "已记录" : "未通关") +
                "\n\n" + actionHint;

            ShowStoryDetail(stage.title, body);
            ConfigureStoryDetailForMainlineStage(currentMainlineStageUnlocked, cleared);
        }

        /// <summary>
        /// 渲染当前剧情句子。剧情文本由 MainlineStoryCatalog 按关卡编号提供。
        /// </summary>
        private void RenderCurrentStoryLine()
        {
            MainlineStorySequence sequence = MainlineStoryCatalog.Get(currentMainlineStageId);
            if (sequence.lines == null || sequence.lines.Length == 0)
            {
                CompleteStoryReading(false);
                return;
            }

            currentStoryLineIndex = Mathf.Clamp(currentStoryLineIndex, 0, sequence.lines.Length - 1);
            bool canSkip = Time.unscaledTime - storyReadingStartedAt >= StorySkipDelaySeconds;
            string skipHint = canSkip ? "现在可跳过剧情。" : "3 秒后将开放跳过。";

            SetStoryText(storyDetailTitle, currentMainlineStageName + " · " + sequence.title);
            SetStoryBody(
                sequence.lines[currentStoryLineIndex] +
                "\n\n—— " + (currentStoryLineIndex + 1) + " / " + sequence.lines.Length + " ——" +
                "\n" + skipHint
            );
        }

        /// <summary>
        /// 读取下一句；最后一句结束后写入“剧情已读”记录。
        /// </summary>
        private void AdvanceStoryReading()
        {
            if (!storyReadingActive)
            {
                StartStoryReading();
                return;
            }

            MainlineStorySequence sequence = MainlineStoryCatalog.Get(currentMainlineStageId);
            currentStoryLineIndex++;
            if (sequence.lines == null || currentStoryLineIndex >= sequence.lines.Length)
            {
                CompleteStoryReading(false);
                return;
            }

            RenderCurrentStoryLine();
        }

        /// <summary>
        /// 完成或跳过当前剧情。只记录已读状态，不把“看剧情”等同于“通关战斗”。
        /// </summary>
        private void CompleteStoryReading(bool skipped)
        {
            storyReadingActive = false;
            currentStoryLineIndex = -1;
            LevelProgressManager.Instance.MarkStoryRead(currentMainlineStageId);

            string result = skipped ? "已跳过本段剧情，仍可随时回看。" : "本段剧情阅读完成，已收入剧情记录。";
            SetStoryText(storyDetailTitle, currentMainlineStageName);
            SetStoryBody(
                result +
                "\n\n下一步可进入战斗，也可以回看本关剧情。\n" +
                "提示：剧情已读与战斗通关是两条独立进度。"
            );

            ConfigureStoryDetailForMainlineStage(
                currentMainlineStageUnlocked,
                LevelProgressManager.Instance.IsStageCleared(currentMainlineStageId)
            );
        }

        /// <summary>
        /// 剧情阅读期间的按钮语义：阅读按钮变成下一句，跳过按钮保留延迟判定。
        /// </summary>
        private void ConfigureStoryDetailForReading()
        {
            ConfigureDetailButton(storyReadButton, storyReadButtonLabel, "下一句", true, AdvanceStoryReading);
            ConfigureDetailButton(storySkipButton, storySkipButtonLabel, "跳过剧情", true, SkipStory);
            ConfigureDetailButton(storyReplayButton, storyReplayButtonLabel, "重新开始", true, ReplayStory);
            ConfigureDetailButton(storyBattleButton, storyBattleButtonLabel, "进入战斗", true, EnterBattlePrototype);
            ConfigureDetailButton(storyCloseButton, storyCloseButtonLabel, "关闭详情", true, CloseStoryDetail);
        }

        private void SetStoryBody(string body)
        {
            SetStoryText(storyDetailBody, body);
        }

        /// <summary>
        /// 运行时补齐丢失的 Inspector 引用。
        /// 这样即使场景序列化丢了部分字段，也能尽量自愈。
        /// </summary>
        private void EnsureRuntimeReferences()
        {
            if (topBarRoot == null)
            {
                Transform topBar = FindChildRecursive(transform, "TopBar");
                if (topBar != null)
                {
                    topBarRoot = topBar.gameObject;
                }
            }

            if (bottomNavRoot == null)
            {
                Transform bottomNav = FindChildRecursive(transform, "BottomNav");
                if (bottomNav != null)
                {
                    bottomNavRoot = bottomNav.gameObject;
                }
            }

            if (storyDetailPanel == null)
            {
                Transform overlay = FindChildRecursive(transform, "StoryDetailOverlay");
                if (overlay != null)
                {
                    storyDetailPanel = overlay.gameObject;
                }
            }

            Transform detailPanel = storyDetailPanel != null ? storyDetailPanel.transform.Find("DetailPanel") : null;

            if (sceneListPanel == null)
            {
                Transform panel = detailPanel != null ? detailPanel.Find("SceneList") : null;
                if (panel != null)
                {
                    sceneListPanel = panel.gameObject;
                }
            }

            if (storyDetailTitle == null)
            {
                Transform title = detailPanel != null ? detailPanel.Find("Title/Label") : null;
                if (title != null)
                {
                    storyDetailTitle = title.GetComponent<Text>();
                }
            }

            if (storyDetailBody == null)
            {
                Transform body = detailPanel != null ? detailPanel.Find("Body/Label") : null;
                if (body != null)
                {
                    storyDetailBody = body.GetComponent<Text>();
                }
            }

            if (storyReadButton == null) storyReadButton = FindButton(detailPanel, "ReadButton");
            if (storySkipButton == null) storySkipButton = FindButton(detailPanel, "SkipButton");
            if (storyReplayButton == null) storyReplayButton = FindButton(detailPanel, "ReplayButton");
            if (storyBattleButton == null) storyBattleButton = FindButton(detailPanel, "BattleButton");
            if (storyCloseButton == null) storyCloseButton = FindButton(detailPanel, "CloseButton");

            if (storyReadButtonLabel == null) storyReadButtonLabel = FindButtonLabel(storyReadButton);
            if (storySkipButtonLabel == null) storySkipButtonLabel = FindButtonLabel(storySkipButton);
            if (storyReplayButtonLabel == null) storyReplayButtonLabel = FindButtonLabel(storyReplayButton);
            if (storyBattleButtonLabel == null) storyBattleButtonLabel = FindButtonLabel(storyBattleButton);
            if (storyCloseButtonLabel == null) storyCloseButtonLabel = FindButtonLabel(storyCloseButton);
        }

        private static Button FindButton(Transform root, string childName)
        {
            Transform target = root != null ? root.Find(childName) : null;
            return target != null ? target.GetComponent<Button>() : null;
        }

        private static Text FindButtonLabel(Button button)
        {
            if (button == null)
            {
                return null;
            }

            Transform label = button.transform.Find("Label");
            return label != null ? label.GetComponent<Text>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            Transform direct = root.Find(childName);
            if (direct != null)
            {
                return direct;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildRecursive(root.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void SetStoryText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private FormationDemoController EnsureFormationController()
        {
            if (formationPage == null)
            {
                return null;
            }

            FormationDemoController controller = formationPage.GetComponent<FormationDemoController>();
            if (controller == null)
            {
                controller = formationPage.AddComponent<FormationDemoController>();
            }

            return controller;
        }
    }
}
