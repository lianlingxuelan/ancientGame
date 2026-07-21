using UnityEngine;
using UnityEngine.UI;

namespace Shouyou.UI
{
    // 首页页面路由器：负责在游戏运行时切换“庭院 / 角色 / 战斗 / 剧情 / 活动”页面。
    // 这个脚本会挂在 Canvas 上，底部导航按钮会调用下面的 ShowXxx 方法。
    public sealed class HomePageRouter : MonoBehaviour
    {
        // 下面这些字段对应 Unity 场景里的页面对象。
        // [SerializeField] 的意思是：字段虽然是 private，但仍然可以在 Inspector 面板里绑定。
        [SerializeField] private GameObject homePage;
        [SerializeField] private GameObject characterPage;
        [SerializeField] private GameObject battlePage;
        [SerializeField] private GameObject storyPage;
        [SerializeField] private GameObject activityPage;
        [SerializeField] private GameObject mainlineChapterPage;
        [SerializeField] private GameObject formationPage;

        // 下面五个按钮用于显示当前所在页面。
        // 这些引用也会由编辑器工具自动绑定，不需要你手动拖拽。
        [SerializeField] private Button homeNavButton;
        [SerializeField] private Button characterNavButton;
        [SerializeField] private Button battleNavButton;
        [SerializeField] private Button storyNavButton;
        [SerializeField] private Button activityNavButton;

        // 剧情详情弹窗及其中的文字引用。
        [SerializeField] private GameObject storyDetailPanel;
        [SerializeField] private Text storyDetailTitle;
        [SerializeField] private Text storyDetailBody;
        [SerializeField] private GameObject sceneListPanel;

        // 未选中和选中时的底部导航颜色。
        [SerializeField] private Color normalNavColor = new Color32(255, 248, 236, 95);
        [SerializeField] private Color selectedNavColor = new Color32(238, 190, 125, 220);

        // 显示庭院首页。底部“庭院”按钮会调用这里。
        public void ShowHome()
        {
            ShowOnly(homePage);
        }

        // 显示角色页。底部“角色”按钮会调用这里。
        public void ShowCharacter()
        {
            ShowOnly(characterPage);
        }

        // 显示战斗页。底部“战斗”按钮会调用这里。
        public void ShowBattle()
        {
            ShowOnly(battlePage);
        }

        // 显示剧情页。底部“剧情”按钮会调用这里。
        public void ShowStory()
        {
            ShowOnly(storyPage);
        }

        // 显示活动页。底部“活动”按钮会调用这里。
        public void ShowActivity()
        {
            ShowOnly(activityPage);
        }

        // 显示主线章节页：玩家从庭院点击“进入主线”后会来到这里。
        public void ShowMainlineChapter()
        {
            ShowOnly(mainlineChapterPage);
        }

        // 显示六人编队页：玩家可以从主线页或战斗页进入这里。
        public void ShowFormation()
        {
            ShowOnly(formationPage);
        }

        // 梦域入口：第一版先用详情弹窗说明解锁规则，后续接入梦域全屏页面。
        public void ShowDreamDomain()
        {
            ShowStoryDetail("梦域", "梦域是主线之外的情绪支线。\n\n通关特定章节、完成角色羁绊或收集梦境碎片后，可以解锁新的命运节点。\n\n当前 Demo 已预留入口，后续接入梦域全屏页面和节点选择。");
        }

        // 主线页的行迹养成分类暂时跳转到角色页。
        public void ShowTrainingCategory()
        {
            ShowCharacter();
        }

        // 主线页的雅集活动分类暂时跳转到活动页。
        public void ShowActivityCategory()
        {
            ShowActivity();
        }

        // 首页“进入主线”按钮的入口。
        // 现在先跳到战斗页，后续可以在这里接入章节选择或剧情前置判断。
        public void EnterMainline()
        {
            ShowMainlineChapter();
        }

        // 页面内部“返回庭院”按钮的入口。
        public void ReturnHome()
        {
            ShowHome();
        }

        // 显示第一章的剧情详情。
        public void ShowChapterOneDetail()
        {
            ShowStoryDetail("第一章：竹堂初语", "李清照十五岁，获父亲李格非允许，与婉禾一同赴汴京雅集。\n\n本章重点：竹堂父女谈话、少女的期待、雅集目标建立。\n\n资源：李府竹堂、翠竹院落、父女立绘、竹影 CG。\n\n状态：已解锁");
        }

        // 显示第二章的剧情详情。
        public void ShowChapterTwoDetail()
        {
            ShowStoryDetail("第二章：灯下共稿（暂定）", "婉禾带着新作来访，两人在临窗小室共同推敲词稿。李清照从闺中闲趣想到更广阔的山河风月。\n\n本章重点：婉禾来访、灯下共稿、李清照夜里独自修改词作。\n\n资源：李府院门、回廊、临窗小室、烛下共稿 CG。\n\n状态：已解锁");
        }

        // 显示第三章的剧情详情，正文拆成七个场景节点。
        public void ShowChapterThreeDetail()
        {
            ShowStoryDetail("第三章：汴京雅集，初逢群英", "3-1 清晨赴会·街巷同行\n3-2 入园落座·初次被围观\n3-3 众贤落笔·词作交流\n3-4 前辈主动搭话\n3-5 献词全场\n3-6 全场惊艳\n3-7 雅集尾声·心境升华\n\n核心：李清照以《浣溪沙》回应质疑，白衣少年在终场埋下伏笔。\n\n状态：未开始");
            SetActive(sceneListPanel, true);
        }

        // 关闭剧情详情弹窗。
        public void CloseStoryDetail()
        {
            SetActive(storyDetailPanel, false);
        }

        // “开始阅读”目前先显示原型提示，后续接入逐句文本播放器。
        public void StartStoryReading()
        {
            SetStoryBody("阅读入口已准备好。下一步会接入逐句文本、角色头像、自动播放和语音。\n\n当前版本先完成章节详情展示。");
        }

        // “跳过剧情”原型按钮：记录入口位置，后续接入存档和奖励结算。
        public void SkipStory()
        {
            SetStoryBody("已跳过本段剧情。正式版本将在这里记录跳过状态，并发放已配置的剧情奖励。");
        }

        // “回看剧情”原型按钮：后续接入剧情回看列表。
        public void ReplayStory()
        {
            SetStoryBody("剧情回看入口已打开。正式版本将在这里显示已解锁的章节和场景列表。");
        }

        // 角色卡片详情：展示李清照的第一版养成信息。
        public void ShowCharacterDetail()
        {
            ShowStoryDetail("李清照 · 角色详情", "稀有度：SSR\n定位：词意输出 / 群体辅助\n等级：Lv.1 / 60\n词意：如梦令\n生命：1200    攻击：180    防御：95\n\n她的成长主题是：以笔墨突破闺阁边界。");
        }

        // 角色养成入口的原型反馈。
        public void ShowTrainingInfo()
        {
            ShowStoryDetail("角色养成", "升级：提升基础属性\n突破：提高等级上限\n技能：解锁词意效果\n装备：强化战斗定位\n\n当前版本先展示入口，后续接入材料和数值消耗。");
        }

        // 角色羁绊入口的原型反馈。
        public void ShowBondInfo()
        {
            ShowStoryDetail("角色羁绊", "与婉禾共同完成剧情，可解锁羁绊等级、专属对话和梦境支线。\n\n正式版本将在这里显示角色关系网。");
        }

        // 六人编队六个位置的点击反馈。
        public void ShowFormationSlotOne() { ShowFormationSlot("前排 1：李清照"); }
        public void ShowFormationSlotTwo() { ShowFormationSlot("前排 2：空位"); }
        public void ShowFormationSlotThree() { ShowFormationSlot("前排 3：空位"); }
        public void ShowFormationSlotFour() { ShowFormationSlot("后排 1：空位"); }
        public void ShowFormationSlotFive() { ShowFormationSlot("后排 2：空位"); }
        public void ShowFormationSlotSix() { ShowFormationSlot("后排 3：空位"); }

        // 六个关卡节点的点击反馈。
        public void ShowStageOne() { ShowStageDetail("1-1 初见", true); }
        public void ShowStageTwo() { ShowStageDetail("1-2 试探", false); }
        public void ShowStageThree() { ShowStageDetail("1-3 词意", false); }
        public void ShowStageFour() { ShowStageDetail("1-4 夜雨", false); }
        public void ShowStageFive() { ShowStageDetail("1-5 梦境", false); }
        public void ShowStageSix() { ShowStageDetail("1-6 庭院", false); }

        // 主线章节页六个关卡的详情入口。
        public void ShowMainlineStageOne() { ShowMainlineStageDetail("1-1 明水入汴京", "推荐等级：Lv.1\n关卡目标：前往李府庭院，触发第一段剧情。\n奖励：铜钱 1200、词意经验 80", true); }
        public void ShowMainlineStageTwo() { ShowMainlineStageDetail("1-2 雅集赴会", "推荐等级：Lv.1\n关卡目标：完成第一次词意试炼。\n奖励：铜钱 1200、名士信笺 1", true); }
        public void ShowMainlineStageThree() { ShowMainlineStageDetail("1-3 词论初临", "推荐等级：Lv.2\n关卡目标：在雅集中回应前辈论词。\n奖励：铜钱 1500、突破材料 1", false); }
        public void ShowMainlineStageFour() { ShowMainlineStageDetail("1-4 风雨前夜", "推荐等级：Lv.3\n关卡目标：完成雨夜前的准备。\n奖励：铜钱 1500、词意经验 120", false); }
        public void ShowMainlineStageFive() { ShowMainlineStageDetail("1-5 故人入梦", "推荐等级：Lv.4\n关卡目标：进入李清照的梦境支线。\n奖励：梦境碎片 1、铜钱 1800", false); }
        public void ShowMainlineStageSix() { ShowMainlineStageDetail("1-6 潮声再起", "推荐等级：Lv.5\n关卡目标：完成第一卷收束战。\n奖励：玉 60、CG 解锁进度 1", false); }

        // 编队页底部“编辑阵容”按钮的原型反馈。
        public void EditFormation()
        {
            ShowStoryDetail("编辑阵容", "正式版本将在这里打开角色选择列表。\n\n当前 Demo 已预留六个位置：前排 3 人、后排 3 人。\n点击空位可以继续接入角色选择。\n\n当前队伍：李清照 / 空位 / 空位 / 空位 / 空位 / 空位");
        }

        // 编队页“保存编队”按钮的原型反馈。
        public void SaveFormation()
        {
            ShowStoryDetail("保存编队", "编队已保存。\n\n正式版本会记录多套队伍，并根据关卡自动推荐词意搭配。\n当前 Demo 暂不写入本地存档。");
        }

        // 编队页“羁绊预览”按钮的原型反馈。
        public void PreviewBond()
        {
            ShowStoryDetail("羁绊预览", "同调角色共同出战时，可以触发气韵增益。\n\n李清照：词意输出\n婉禾：辅助与协奏\n\n后续会在这里显示角色关系、羁绊等级和梦境解锁条件。");
        }

        // 第一关的进入战斗入口占位。
        public void EnterBattlePrototype()
        {
            ShowStoryDetail("进入战斗", "第一章 1-1 初见\n\n这里将进入六人编队和回合制 PVE 战斗场景。\n当前版本先完成入口，下一轮接入战斗场景。");
        }

        // 第三章七个场景节点的点击反馈。
        public void ShowScene31() { SetStoryBody("3-1 清晨赴会·街巷同行\n李清照与婉禾第一次走入汴京文坛社交场。"); }
        public void ShowScene32() { SetStoryBody("3-2 入园落座·初次被围观\n二人因闺阁女子身份受到好奇与轻视。"); }
        public void ShowScene33() { SetStoryBody("3-3 众贤落笔·词作交流\n雅集以春日风物为题，众人即兴填词。"); }
        public void ShowScene34() { SetStoryBody("3-4 前辈主动搭话\n周学士主动与李清照一对一论词。"); }
        public void ShowScene35() { SetStoryBody("3-5 献词全场\n李清照呈上《浣溪沙》，回应全场质疑。"); }
        public void ShowScene36() { SetStoryBody("3-6 全场惊艳\n众人改变态度，认可李清照的才华。"); }
        public void ShowScene37() { SetStoryBody("3-7 雅集尾声·心境升华\n深度词学交流结束，白衣少年伏笔出现。"); }

        // Awake 会在场景开始运行时自动执行。
        // 这里默认先显示庭院首页，避免一进游戏看到空白。
        private void Awake()
        {
            ShowHome();
        }

        // 核心切页逻辑：只打开目标页面，其他页面全部隐藏。
        private void ShowOnly(GameObject target)
        {
            SetActive(homePage, target == homePage);
            SetActive(characterPage, target == characterPage);
            SetActive(battlePage, target == battlePage);
            SetActive(storyPage, target == storyPage);
            SetActive(activityPage, target == activityPage);
            SetActive(mainlineChapterPage, target == mainlineChapterPage);
            SetActive(formationPage, target == formationPage);
            SetActive(storyDetailPanel, false);

            // 页面切换完成后，同步更新底部导航的选中状态。
            SetNavSelected(homeNavButton, target == homePage);
            SetNavSelected(characterNavButton, target == characterPage);
            SetNavSelected(battleNavButton, target == battlePage);
            SetNavSelected(storyNavButton, target == storyPage);
            SetNavSelected(activityNavButton, target == activityPage);
        }

        // 安全地显示或隐藏对象。
        // 先判断 null，是为了防止某个页面还没绑定时直接报错。
        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        // 修改按钮背景颜色，让玩家知道当前位于哪个页面。
        private void SetNavSelected(Button button, bool selected)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? selectedNavColor : normalNavColor;
            }
        }

        private void ShowStoryDetail(string title, string body)
        {
            if (storyDetailPanel == null)
            {
                return;
            }

            storyDetailPanel.SetActive(true);
            SetActive(sceneListPanel, false);
            SetStoryText(storyDetailTitle, title);
            SetStoryText(storyDetailBody, body);
        }

        private void ShowFormationSlot(string slotName)
        {
            ShowStoryDetail("编队位置", slotName + "\n\n点击空位后，正式版本会打开角色选择列表。\n当前先保留位置反馈。");
        }

        private void ShowStageDetail(string stageName, bool unlocked)
        {
            ShowStoryDetail(stageName, "推荐等级：Lv." + (stageName == "1-1 初见" ? "1" : "2") + "\n体力消耗：6\n词意提示：以春日风物试探角色配合。\n\n状态：" + (unlocked ? "已解锁" : "未解锁"));
        }

        private void ShowMainlineStageDetail(string stageName, string body, bool unlocked)
        {
            ShowStoryDetail(stageName, body + "\n\n状态：" + (unlocked ? "已解锁" : "暂未解锁"));
        }

        private void SetStoryBody(string body)
        {
            SetStoryText(storyDetailBody, body);
        }

        private static void SetStoryText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
