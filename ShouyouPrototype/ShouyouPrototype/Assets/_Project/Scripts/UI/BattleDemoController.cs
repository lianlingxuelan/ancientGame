using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Shouyou.Data;
using Shouyou.Network;

namespace Shouyou.UI
{
    /// <summary>
    /// 第一版战斗 Demo 控制器。
    /// 只负责“能打、能扣血、能阵亡、能胜负结算”的最小闭环。
    /// 后续真正做技能、Buff、行动条、后端战报时，可以把这里拆成更正式的 BattleSystem。
    /// </summary>
    public sealed class BattleDemoController : MonoBehaviour
    {
        /// <summary>
        /// 头像攻击表现的请求数据。
        /// 仅描述“谁用什么技能对谁发起了表现”，不参与伤害、命中或结算计算。
        /// 后续可由 Animator、Timeline、粒子或 Spine 表现层订阅并播放对应动画。
        /// </summary>
        public sealed class BattlePortraitEffectRequest
        {
            public readonly int attackerSlotIndex;
            public readonly bool attackerIsAlly;
            public readonly string attackerName;
            public readonly string skillId;
            public readonly int targetSlotIndex;
            public readonly bool targetIsAlly;
            public readonly string targetName;
            public readonly bool hitsAllTargets;

            public BattlePortraitEffectRequest(
                int attackerSlotIndex,
                bool attackerIsAlly,
                string attackerName,
                string skillId,
                int targetSlotIndex,
                bool targetIsAlly,
                string targetName,
                bool hitsAllTargets)
            {
                this.attackerSlotIndex = attackerSlotIndex;
                this.attackerIsAlly = attackerIsAlly;
                this.attackerName = attackerName;
                this.skillId = skillId;
                this.targetSlotIndex = targetSlotIndex;
                this.targetIsAlly = targetIsAlly;
                this.targetName = targetName;
                this.hitsAllTargets = hitsAllTargets;
            }
        }

        /// <summary>
        /// 角色开始攻击时触发。未来的头像特效表现层订阅此事件即可，
        /// 无需反向修改伤害、技能或战斗结算逻辑。
        /// </summary>
        public event System.Action<BattlePortraitEffectRequest> PortraitAttackEffectRequested;

        private const int UnitCount = 6;
        // 顶部仅预览当前回合中尚未行动的前四位，不创建真正的行动条或改变行动权。
        private const int ActionPreviewCount = 4;
        private const int FallbackActionPointMax = 3;
        private const string BattleApiBaseUrl = "http://127.0.0.1:5188";
        private const float HpBarMaxWidth = 86f;
        // 飘字总可见时长；受击表现（白闪 + 颜色脉冲）与飘字上浮段之和应等于该值。
        private const float DamageTextVisibleSeconds = 0.8f;
        // 攻击者施法高亮总时长（上抬 + 回落）。
        private const float AttackPresentationSeconds = 0.22f;
        // 受击瞬间白闪时长。
        private const float ImpactWhiteFlashSeconds = 0.08f;
        // 受击颜色脉冲时长（与白闪合计 0.42s 受击总时长）。
        private const float HitColorPulseSeconds = 0.34f;
        // 飘字上浮 + 淡出时长（0.08 + 0.34 + 0.38 = 0.8 = DamageTextVisibleSeconds）。
        private const float FloatingTextRiseSeconds = 0.38f;
        // 阵亡淡出时长。
        private const int BasicSkillCost = 0;
        private const int PoetryStrikeCost = 1;
        private const int DreamAreaCost = 2;
        private const int HealingVerseCost = 2;

        private readonly BattleUnitState[] allyUnits = new BattleUnitState[UnitCount];
        private readonly BattleUnitState[] enemyUnits = new BattleUnitState[UnitCount];
        private readonly BattleUnitView[] allyViews = new BattleUnitView[UnitCount];
        private readonly BattleUnitView[] enemyViews = new BattleUnitView[UnitCount];
        private readonly Dictionary<string, Sprite> skillIconCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();
        // 每个角色最多预选一个大招。预选不立即扣行动点或进入冷却，等该角色下次行动时才结算。
        private readonly Dictionary<BattleUnitState, QueuedSkillState> queuedSkills = new Dictionary<BattleUnitState, QueuedSkillState>();
        private readonly List<BattleUnitState> actionOrder = new List<BattleUnitState>();
        // 表现队列只记录已经结算过的视觉事件，不参与伤害、行动值或胜负判定。
        private readonly Queue<BattlePresentationEvent> presentationEvents = new Queue<BattlePresentationEvent>();

        private BattleDemoConfigResponse battleConfig;
        private BattleSkillDto[] backendSkills;
        private int actionPointMax = FallbackActionPointMax;
        private bool backendBattleConfigLoaded;
        private bool backendBattleConfigLoading;

        private HomePageRouter router;
        private Text roundTipText;
        private Text actionPointText;
        private Text battleMessageText;
        private Button startBattleButton;
        private Button autoBattleButton;
        private Button retreatButton;
        private Button basicSkillButton;
        private Button poetryStrikeButton;
        private Button dreamAreaButton;
        private Button healSkillButton;

        private int selectedEnemyIndex;
        private int selectedAllyIndex;
        private int actionCursor;
        private BattleUnitState currentActor;
        private int roundIndex = 1;
        private int actionPoint = FallbackActionPointMax;
        private bool battleEnded;
        private bool resolvingEnemyTurn;
        private bool referencesBound;
        private bool isPlayingPresentation;
        private Coroutine presentationCoroutine;
        // 自动战斗必须等待当前表现队列播完，不能把多次伤害在同一帧内全部结算给玩家看。
        private bool isAutoBattleRunning;
        private Coroutine autoBattleCoroutine;
        // 玩家出手后的敌方行动、预选技能必须逐段等待表现结束，不能同步循环结算。
        private Coroutine followUpResolutionCoroutine;

        /// <summary>
        /// 从主线详情页进入战斗时写入。战斗控制器不决定关卡进度，
        /// 仅保存上下文供提示和结算路由使用。
        /// </summary>
        private int activeStageId = 1;
        private string activeStageTitle = "1-1 明水入汴京";

        private void Awake()
        {
            BindRuntimeReferences();
            ResetDemoBattle();
            StartCoroutine(LoadBackendBattleConfig());
        }

        private void OnEnable()
        {
            BindRuntimeReferences();
            ResetDemoBattle();
            StartCoroutine(LoadBackendBattleConfig());
        }

        private void OnDisable()
        {
            // 页面离开后不再播放旧战斗的表现事件，避免回到战斗页时残留飘字或高亮。
            StopAutoBattleRoutine();
            StopFollowUpResolutionRoutine();
            ClearPresentationQueue();
        }

        /// <summary>
        /// 设置本场战斗对应的主线关卡。
        /// 由 HomePageRouter 在切入战斗页前调用，避免战斗页自行猜测关卡。
        /// </summary>
        public void ConfigureStageContext(int stageId, string stageTitle)
        {
            activeStageId = Mathf.Max(1, stageId);
            if (!string.IsNullOrEmpty(stageTitle))
            {
                activeStageTitle = stageTitle;
            }
        }

        /// <summary>
        /// 每次进入战斗页时重置 Demo 战斗。
        /// 当前没有接正式战报，所以先保证每次进入都从完整血量开始。
        /// </summary>
        public void ResetDemoBattle()
        {
            StopAutoBattleRoutine();
            StopFollowUpResolutionRoutine();
            ClearPresentationQueue();
            ResetAllUnitViewRemovalState();
            selectedEnemyIndex = 0;
            selectedAllyIndex = 0;
            roundIndex = 1;
            actionPoint = actionPointMax;
            battleEnded = false;
            resolvingEnemyTurn = false;
            skillCooldowns.Clear();
            queuedSkills.Clear();

            for (int i = 0; i < UnitCount; i++)
            {
                allyUnits[i] = CreateAllyUnit(i);
                enemyUnits[i] = CreateEnemyUnit(i);
            }

            BuildActionOrder();
            MoveToNextAvailableActor(false);

            SetBattleMessage(
                "当前关卡：" + activeStageTitle + "（" + activeStageId + "）" +
                "\n第一回合：我方行动。选择敌方头像，或直接点击“开始战斗”。" +
                "\n当前阵容：" + GetFormationSummaryForBattle()
            );
            RefreshAllViews();
        }

        /// <summary>
        /// 主战斗按钮：战斗未结束时用于发动普攻；战斗结束后点击重新开始本场战斗。
        /// </summary>
        public void PressMainBattleButton()
        {
            BindRuntimeReferences();

            if (IsBattleInputLocked())
            {
                return;
            }

            if (battleEnded)
            {
                ResetDemoBattle();
                return;
            }

            if (!ValidateBattleStartup())
            {
                return;
            }

            PerformPlayerAttack();
        }

        public void PerformPlayerAttack()
        {
            if (IsBattleInputLocked())
            {
                return;
            }

            PerformPlayerAttackInternal();
        }

        /// <summary>
        /// 普攻的实际结算入口。自动战斗会在同一段逻辑内连续生成多个表现事件，
        /// 因此不能再次经过“表现队列是否锁定”的用户输入判断。
        /// </summary>
        private void PerformPlayerAttackInternal()
        {

            BattleUnitState attacker;
            BattleUnitState target;
            if (!TryGetBattleActionContext(BasicSkillCost, "basic", out attacker, out target))
            {
                return;
            }

            RequestPortraitAttackEffect(attacker, target, "basic", false);
            int damage = CalculateDamage(attacker, target);
            bool damageApplied;
            bool targetDefeated = ApplyDamage(target, damage, out damageApplied);
            if (damageApplied)
            {
                ShowDamageText(target, damage, targetDefeated);
                QueueDefeatPresentation(target, targetDefeated);
            }
            CompletePlayerAction(BuildAttackMessage(attacker, target, damage, targetDefeated));
        }

        /// <summary>
        /// 词意连击（单体高伤大招）：当前行动角色立即结算；
        /// 非当前角色仅登记预选，轮到该角色下次行动时自动释放。
        /// </summary>
        public void CastPoetryStrike()
        {
            if (IsBattleInputLocked())
            {
                return;
            }

            UseOrQueueSkill(PoetryStrikeCost, "poetry_strike", true);
        }

        /// <summary>
        /// 如梦令（群体大招）：当前行动角色立即结算；
        /// 非当前角色仅登记预选，轮到该角色下次行动时自动释放。
        /// </summary>
        public void CastDreamAreaAttack()
        {
            if (IsBattleInputLocked())
            {
                return;
            }

            UseOrQueueSkill(DreamAreaCost, "dream_area", false);
        }

        /// <summary>
        /// 疗愈（治疗大招）：当前行动角色立即结算；
        /// 非当前角色仅登记预选，轮到该角色下次行动时自动释放。
        /// </summary>
        public void CastHealingVerse()
        {
            if (IsBattleInputLocked())
            {
                return;
            }

            UseOrQueueSkill(HealingVerseCost, "healing_verse", false);
        }

        /// <summary>
        /// 临时自动战斗：自动执行可行动的我方角色。
        /// 每次行动后都等待攻击、受击、飘字与退场表现结束，避免多名角色看起来同时结算。
        /// </summary>
        public void PerformAutoAttacks()
        {
            if (IsBattleInputLocked() || isAutoBattleRunning)
            {
                return;
            }

            if (!ValidateBattleStartup())
            {
                return;
            }

            autoBattleCoroutine = StartCoroutine(PerformAutoAttacksRoutine());
        }

        /// <summary>
        /// 自动战斗的逐段执行协程。数值结算继续复用既有逻辑，
        /// 本协程只在每一个行动批次之间等待表现队列清空。
        /// </summary>
        private IEnumerator PerformAutoAttacksRoutine()
        {
            isAutoBattleRunning = true;
            RefreshBattleControls();

            // 自动战斗可以跨越多个完整行动链继续执行；安全上限仅防止异常状态下无限循环。
            int safety = UnitCount * UnitCount;
            while (!battleEnded && safety-- > 0)
            {
                yield return WaitForPresentationQueueToFinish();
                yield return WaitForFollowUpResolutionToFinish();
                if (battleEnded)
                {
                    break;
                }

                // 正常情况下后续行动链会停在下一名可操作的我方角色；
                // 若战斗状态异常或不再属于我方行动，则安全结束本轮自动执行。
                if (!IsPlayerTurn())
                {
                    break;
                }

                PerformPlayerAttackInternal();
                yield return WaitForPresentationQueueToFinish();
                yield return WaitForFollowUpResolutionToFinish();
            }

            isAutoBattleRunning = false;
            autoBattleCoroutine = null;
            RefreshAllViews();
        }

        /// <summary>
        /// 撤退按钮：当前 Demo 直接返回主线，不扣资源。
        /// </summary>
        public void RetreatBattle()
        {
            // 战斗已经结束时不重复处理撤退，避免覆盖胜负结算状态。
            if (battleEnded)
            {
                return;
            }

            battleEnded = true;
            StopAutoBattleRoutine();
            StopFollowUpResolutionRoutine();
            ClearPresentationQueue();
            SetBattleMessage("\u5df2\u64a4\u9000\u672c\u573a\u6218\u6597\uff0c\u672a\u83b7\u5f97\u5956\u52b1\u3002");
            if (router != null)
            {
                router.ShowMainlineChapter();
            }
        }

        private void BindRuntimeReferences()
        {
            // 同一场战斗内不能重复创建单位视图：新视图会丢失 isRemoved 状态，
            // 让已经阵亡退场的角色重新出现在战场上。
            if (referencesBound)
            {
                return;
            }

            router = GetComponentInParent<HomePageRouter>();
            roundTipText = FindLabel("BattleRoundTip");
            actionPointText = FindLabel("ActionPointText");
            battleMessageText = FindLabel("BattleMessage");
            startBattleButton = FindButton("StartBattleButton");
            autoBattleButton = FindButton("AutoBattleButton");
            retreatButton = FindButton("RetreatButton");
            basicSkillButton = FindButton("SkillButton_1");
            poetryStrikeButton = FindButton("SkillButton_2");
            dreamAreaButton = FindButton("SkillButton_3");
            healSkillButton = FindButton("SkillButton_4");

            BindButton(startBattleButton, PressMainBattleButton);
            BindButton(autoBattleButton, PerformAutoAttacks);
            BindButton(retreatButton, RetreatBattle);
            BindButton(basicSkillButton, PerformPlayerAttack);
            BindButton(poetryStrikeButton, CastPoetryStrike);
            BindButton(dreamAreaButton, CastDreamAreaAttack);
            BindButton(healSkillButton, CastHealingVerse);

            RefreshBattleControls();

            for (int i = 0; i < UnitCount; i++)
            {
                allyViews[i] = BuildView("AllyBattleSlot_" + (i + 1));
                enemyViews[i] = BuildView("EnemyBattleSlot_" + (i + 1));

                int slotIndex = i;
                BindButton(allyViews[i].button, delegate { SelectAlly(slotIndex); });
                BindButton(enemyViews[i].button, delegate { SelectEnemy(slotIndex); });
            }

            referencesBound = true;
        }

        private void SelectAlly(int index)
        {
            if (index < 0 || index >= UnitCount || allyUnits[index] == null)
            {
                return;
            }

            BattleUnitState selectedUnit = allyUnits[index];
            if (selectedUnit.defeated)
            {
                SetBattleMessage(selectedUnit.unitName + " 已退场，不能作为当前行动单位。");
                RefreshAllViews();
                return;
            }

            selectedAllyIndex = index;
            string message = selectedUnit.unitName + "：生命 " + selectedUnit.currentHp + " / " + selectedUnit.maxHp;
            if (selectedUnit != currentActor)
            {
                message += "\n当前轮到 " + GetCurrentActorName() + " 行动；可为 " + selectedUnit.unitName + " 预选下次行动的大招，不会抢走当前行动。";
            }

            SetBattleMessage(message);
            RefreshAllViews();
        }

        private void SelectEnemy(int index)
        {
            if (index < 0 || index >= UnitCount || enemyUnits[index] == null)
            {
                return;
            }

            BattleUnitState selectedEnemy = enemyUnits[index];
            if (selectedEnemy.defeated)
            {
                SetBattleMessage(selectedEnemy.unitName + " 已退场，请选择其他目标。");
                RefreshAllViews();
                return;
            }

            selectedEnemyIndex = index;
            SetBattleMessage("已选中：" + enemyUnits[index].unitName + "。点击“开始战斗”攻击目标。");
            RefreshAllViews();
        }

        private BattleUnitState CreateAllyUnit(int index)
        {
            // 编队缓存决定“谁上阵”；battle demo-config 仅提供当前关卡的数值模板。
            // 这样保存编队后再进入战斗，角色身份不会被固定 Demo 队伍覆盖。
            string formationCharacterId = ShouyouBackendBootstrap.GetBattleFormationSlotId(index);
            BattleUnitDto dto = FindBackendUnitBySlot(battleConfig == null ? null : battleConfig.allies, index);
            if (string.IsNullOrEmpty(formationCharacterId))
            {
                return CreateEmptyAllyUnit(index);
            }

            return CreateAllyUnitFromFormation(formationCharacterId, index, dto);
        }

        /// <summary>
        /// 用已保存编队的角色身份创建我方单位。
        /// 当前仅复用关卡接口提供的 HP、攻击、行动值和头像模板；
        /// 角色专属成长与完整数值表会在养成系统接入后替换本方法的保守默认值。
        /// </summary>
        private BattleUnitState CreateAllyUnitFromFormation(string characterId, int index, BattleUnitDto template)
        {
            string unitName = ShouyouBackendBootstrap.GetCharacterNameById(characterId);
            if (string.IsNullOrEmpty(unitName) || unitName == "空位")
            {
                return CreateEmptyAllyUnit(index);
            }

            // 李清照是首位接入养成的角色：无论关卡接口是否给出默认模板，
            // 她的 HP 与攻击都应由养成快照决定，避免升级只停留在角色页展示。
            if (IsLiQingzhaoCharacterId(characterId))
            {
                return CreateLiQingzhaoUnitFromDevelopment(unitName, index, template);
            }

            if (template != null)
            {
                return CreateUnitFromDto(template, true, index, unitName);
            }

            if (characterId == "npc-qiu")
            {
                return new BattleUnitState(unitName, true, 1080, 170, "char_difang");
            }

            if (characterId == "npc-mo")
            {
                return new BattleUnitState(unitName, true, 1000, 155, "char_wanhe");
            }

            if (characterId == "npc-zheng")
            {
                return new BattleUnitState(unitName, true, 950, 145, "char_difang2");
            }

            if (characterId == "npc-yun")
            {
                return new BattleUnitState(unitName, true, 1050, 160, "char_wanhe");
            }

            // 婉禾和未来非李清照角色在数值表接入前使用独立保守模板，避免所有人都套用主角数据。
            return new BattleUnitState(unitName, true, 980, 145, "char_wanhe");
        }

        /// <summary>
        /// 从李清照的养成快照创建战斗单位。
        /// 仅替换 HP 与攻击；行动值、速度、头像和未来的终态字段仍沿用关卡模板，
        /// 因此不会把养成逻辑扩散到敌方或其他尚未开放成长的角色。
        /// </summary>
        private BattleUnitState CreateLiQingzhaoUnitFromDevelopment(string unitName, int index, BattleUnitDto template)
        {
            CharacterDevelopmentSnapshot snapshot = CharacterDevelopmentManager.Instance.GetSnapshot(CharacterDevelopmentManager.LiQingzhaoId);
            int defaultActionValue = 120 - index * 2;
            int actionValue = template != null && template.actionValue > 0 ? template.actionValue : defaultActionValue;
            int speed = template != null && template.speed > 0 ? template.speed : 100;
            string portraitIconKey = template != null && !string.IsNullOrEmpty(template.portraitIconKey)
                ? template.portraitIconKey
                : "char_liqingzhao";

            int health = snapshot != null ? snapshot.health : (template != null && template.hp > 0 ? template.hp : 1200);
            int attack = snapshot != null ? snapshot.attack : (template != null && template.attack > 0 ? template.attack : 220);
            float critRate = template == null ? 0f : Mathf.Clamp01(template.critRate);
            float critDamage = template != null && template.critDamage > 0f ? template.critDamage : 1.5f;
            float hitRate = template != null && template.hitRate > 0f ? template.hitRate : 1f;
            float dodgeRate = template == null ? 0f : Mathf.Clamp01(template.dodgeRate);
            int starLevel = template != null && template.starLevel > 0 ? template.starLevel : 1;
            int breakLevel = template == null ? 0 : Mathf.Max(0, template.breakLevel);
            string element = template == null ? null : template.element;
            string[] buffIds = template == null ? null : template.buffIds;

            return new BattleUnitState(
                unitName, true, health, attack, portraitIconKey, actionValue, speed,
                critRate, critDamage, hitRate, dodgeRate, element, starLevel, breakLevel, buffIds);
        }

        /// <summary>
        /// 兼容现有编队缓存的连字符 ID 与养成模块的下划线 ID。
        /// </summary>
        private static bool IsLiQingzhaoCharacterId(string characterId)
        {
            return characterId == "li-qingzhao" || characterId == CharacterDevelopmentManager.LiQingzhaoId;
        }

        private BattleUnitState CreateEnemyUnit(int index)
        {
            // 按关卡限制出场敌人数量：enemyCountPerStage 为空或关卡号越界时保持旧行为（全部敌人上场）。
            if (battleConfig != null && battleConfig.enemyCountPerStage != null && battleConfig.enemyCountPerStage.Length > 0)
            {
                int stageLimit = battleConfig.enemyCountPerStage[Mathf.Clamp(activeStageId - 1, 0, battleConfig.enemyCountPerStage.Length - 1)];
                if (index >= stageLimit)
                {
                    return CreateEmptyEnemyUnit(index);
                }
            }
            BattleUnitDto dto = FindBackendUnitBySlot(battleConfig == null ? null : battleConfig.enemies, index);
            if (dto != null)
            {
                return CreateUnitFromDto(dto, false, index);
            }

            string[] names = { "敌一", "敌二", "敌三", "敌四", "敌五", "敌六" };
            return new BattleUnitState(names[index], false, 520 + index * 70, 105 + index * 18, index < 2 ? "enemy_shadow" : string.Empty);
        }

        private IEnumerator LoadBackendBattleConfig()
        {
            if (backendBattleConfigLoading || backendBattleConfigLoaded)
            {
                yield break;
            }

            backendBattleConfigLoading = true;
            ShouyouApiClient client = new ShouyouApiClient(BattleApiBaseUrl, "demo-player");

            // ????????? demo-config????????????????????
            yield return client.GetBattleDemoConfig(delegate(BattleDemoConfigResponse response)
            {
                battleConfig = response;
                backendSkills = response == null ? null : response.skills;
                actionPointMax = response != null && response.maxActionPoint > 0 ? response.maxActionPoint : FallbackActionPointMax;
                backendBattleConfigLoaded = response != null;
            }, delegate(string error)
            {
                Debug.LogWarning("[BattleDemo] ????????????????????" + error);
            });

            BattleSkillAssetListResponse skillAssets = null;
            yield return client.GetBattleSkillAssets(delegate(BattleSkillAssetListResponse response)
            {
                skillAssets = response;
            }, delegate(string error)
            {
                Debug.LogWarning("[BattleDemo] ????????????????????" + error);
            });

            // 图标下载完成后再刷新按钮，避免按钮首次渲染时只显示文字、
            // 随后因页面重置或切换而错过一次图标刷新。
            if (skillAssets != null && skillAssets.icons != null)
            {
                yield return DownloadSkillIcons(skillAssets.icons);
            }

            backendBattleConfigLoading = false;

            // 异步配置只会在下一次进入战斗时由 ResetDemoBattle 使用。
            // 禁止在战斗过程中重置单位状态，否则阵亡单位会被无技能地重新创建。
        }

        private IEnumerator DownloadSkillIcons(BattleSkillAssetDto[] assets)
        {
            if (assets == null)
            {
                yield break;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                BattleSkillAssetDto asset = assets[i];
                if (asset == null || string.IsNullOrEmpty(asset.iconKey) || string.IsNullOrEmpty(asset.url) || asset._placeholder)
                {
                    continue;
                }

                string url = asset.url.StartsWith("http") ? asset.url : BattleApiBaseUrl + asset.url;
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning("[BattleDemo] ?????????" + asset.iconKey + " " + request.error);
                        continue;
                    }

                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (texture == null)
                    {
                        continue;
                    }

                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                    skillIconCache[asset.iconKey] = sprite;
                }
            }

            RefreshBattleControls();
        }

        private BattleUnitDto FindBackendUnitBySlot(BattleUnitDto[] units, int zeroBasedIndex)
        {
            if (units == null)
            {
                return null;
            }

            int slot = zeroBasedIndex + 1;
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] != null && units[i].slot == slot)
                {
                    return units[i];
                }
            }

            return null;
        }

        private BattleUnitState CreateUnitFromDto(BattleUnitDto dto, bool isAlly, int index, string displayName = null)
        {
            if (dto == null || string.IsNullOrEmpty(dto.name))
            {
                return isAlly ? CreateEmptyAllyUnit(index) : new BattleUnitState("敌" + (index + 1), false, 520 + index * 70, 105 + index * 18, string.Empty);
            }

            int hp = dto.hp > 0 ? dto.hp : (isAlly ? 900 : 520 + index * 70);
            int attack = dto.attack > 0 ? dto.attack : (isAlly ? 160 : 105 + index * 18);
            int defaultActionValue = isAlly ? 120 - index * 2 : 100 - index * 2;
            int actionValue = dto.actionValue > 0 ? dto.actionValue : defaultActionValue;
            // 新字段对旧接口保持安全默认值；除 speed 的行动排序外，本轮均只保存不参与计算。
            int speed = dto.speed > 0 ? dto.speed : 100;
            float critRate = Mathf.Clamp01(dto.critRate);
            float critDamage = dto.critDamage > 0f ? dto.critDamage : 1.5f;
            float hitRate = dto.hitRate > 0f ? dto.hitRate : 1f;
            float dodgeRate = Mathf.Clamp01(dto.dodgeRate);
            int starLevel = dto.starLevel > 0 ? dto.starLevel : 1;
            int breakLevel = Mathf.Max(0, dto.breakLevel);
            return new BattleUnitState(
                string.IsNullOrEmpty(displayName) ? dto.name : displayName,
                isAlly, hp, attack, dto.portraitIconKey, actionValue, speed,
                critRate, critDamage, hitRate, dodgeRate, dto.element, starLevel, breakLevel, dto.buffIds);
        }

        private BattleUnitState CreateEmptyAllyUnit(int index)
        {
            BattleUnitState emptyUnit = new BattleUnitState("空位 " + (index + 1), true, 1, 0, string.Empty);
            emptyUnit.currentHp = 0;
            emptyUnit.defeated = true;
            return emptyUnit;
        }

        private BattleUnitState CreateEmptyEnemyUnit(int index)
        {
            BattleUnitState emptyUnit = new BattleUnitState("空位 " + (index + 1), false, 1, 0, string.Empty);
            emptyUnit.currentHp = 0;
            emptyUnit.defeated = true;
            return emptyUnit;
        }

        private string GetFormationSummaryForBattle()
        {
            // 结算与战斗提示也必须与编队页使用同一份缓存，避免 UI 显示旧 Demo 队伍。
            return ShouyouBackendBootstrap.GetFormationSummary();
        }

        private BattleUnitState GetSelectedOrFirstAliveEnemy()
        {
            if (selectedEnemyIndex >= 0 && selectedEnemyIndex < UnitCount && enemyUnits[selectedEnemyIndex] != null && !enemyUnits[selectedEnemyIndex].defeated)
            {
                return enemyUnits[selectedEnemyIndex];
            }

            return FindFirstAlive(enemyUnits);
        }

        private BattleUnitState FindFirstAlive(BattleUnitState[] units)
        {
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] != null && !units[i].defeated)
                {
                    return units[i];
                }
            }

            return null;
        }

        private bool AllDefeated(BattleUnitState[] units)
        {
            if (units == null || units.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] != null && !units[i].defeated)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 校验当前是否轮到我方单位行动，同时校验技能消耗和冷却。
        /// 行动者由行动值队列决定，不能通过点击头像绕过回合顺序。
        /// </summary>
        private bool TryGetBattleActionContext(int actionCost, string skillId, out BattleUnitState attacker, out BattleUnitState target)
        {
            BindRuntimeReferences();
            attacker = null;
            target = null;

            if (battleEnded)
            {
                SetBattleMessage("\u672c\u573a\u6218\u6597\u5df2\u7ecf\u7ed3\u7b97\uff0c\u8bf7\u8fd4\u56de\u4e3b\u7ebf\u6216\u91cd\u65b0\u8fdb\u5165\u3002");
                return false;
            }

            if (!ValidateBattleStartup())
            {
                return false;
            }

            if (!IsPlayerTurn())
            {
                SetBattleMessage("\u5f53\u524d\u8f6e\u5230 " + GetCurrentActorName() + " \u884c\u52a8\uff0c\u8bf7\u7b49\u5f85\u654c\u65b9\u884c\u52a8\u7ed3\u675f\u3002");
                return false;
            }

            if (actionPoint < actionCost)
            {
                SetBattleMessage("\u884c\u52a8\u70b9\u4e0d\u8db3\uff0c\u8be5\u6280\u80fd\u9700\u8981 " + actionCost + " \u70b9\u884c\u52a8\u70b9\u3002");
                return false;
            }

            int cooldown = GetSkillCooldown(skillId);
            if (cooldown > 0)
            {
                SetBattleMessage("\u6280\u80fd\u8fd8\u5728\u51b7\u5374\uff0c\u5269\u4f59 " + cooldown + " \u56de\u5408\u3002");
                return false;
            }

            attacker = currentActor;
            target = GetSelectedOrFirstAliveEnemy();
            if (attacker == null || target == null)
            {
                Debug.LogError("[BattleDemo] \u6218\u6597\u72b6\u6001\u5f02\u5e38\uff1a\u627e\u4e0d\u5230\u53ef\u884c\u52a8\u5355\u4f4d\u6216\u53ef\u653b\u51fb\u76ee\u6807\u3002");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 战斗入口与行动入口共用的基础校验。
        /// 只判断队伍与敌人是否仍有可战斗单位，不改动伤害或行动值规则。
        /// </summary>
        private bool ValidateBattleStartup()
        {
            string reason = GetBattleUnavailableReason();
            if (string.IsNullOrEmpty(reason))
            {
                return true;
            }

            SetBattleMessage(reason);
            RefreshAllViews();
            return false;
        }

        private string GetBattleUnavailableReason()
        {
            if (AllDefeated(allyUnits))
            {
                return "当前编队没有可出战角色，请先返回编队页面配置队伍。";
            }

            if (AllDefeated(enemyUnits))
            {
                return "本关敌方已全部退场，请进入下一关或重新开始。";
            }

            return string.Empty;
        }

        /// <summary>
        /// 扣除行动点并登记技能冷却。普通攻击不消耗行动点，也不进入冷却。
        /// </summary>
        private void ConsumeSkill(string skillId, int actionCost)
        {
            actionPoint = Mathf.Max(0, actionPoint - actionCost);
            BattleSkillDto skill = FindSkill(skillId);
            int cooldown = skill == null ? 0 : Mathf.Max(0, skill.cooldown);
            if (cooldown > 0)
            {
                skillCooldowns[skillId] = cooldown;
            }
        }

        private int GetSkillCooldown(string skillId)
        {
            int cooldown;
            return !string.IsNullOrEmpty(skillId) && skillCooldowns.TryGetValue(skillId, out cooldown) ? cooldown : 0;
        }

        private bool CanUseSkill(string skillId, int actionCost)
        {
            return !IsBattleInputLocked() && !battleEnded && IsPlayerTurn() && actionPoint >= actionCost && GetSkillCooldown(skillId) <= 0;
        }

        /// <summary>
        /// 大招预选与即时普攻共用同一套 AP、冷却与行动者校验；
        /// 额外禁止同一角色重复预选，避免同一行动回合覆盖前一个指令。
        /// </summary>
        /// <summary>
        /// 返回玩家当前选中的技能归属角色。头像选择只决定“谁拥有这次技能指令”，
        /// 绝不改变行动值队列中的 currentActor。
        /// </summary>
        private BattleUnitState GetSelectedSkillOwner()
        {
            if (selectedAllyIndex >= 0 && selectedAllyIndex < allyUnits.Length)
            {
                BattleUnitState selected = allyUnits[selectedAllyIndex];
                if (selected != null && !selected.defeated)
                {
                    return selected;
                }
            }

            return IsPlayerTurn() ? currentActor : null;
        }

        /// <summary>
        /// 当前行动角色释放大招时，技能立即结算并结束本次行动。
        /// </summary>
        private bool CanExecuteSkillImmediately(BattleUnitState skillOwner, string skillId, int actionCost)
        {
            return skillOwner != null && skillOwner == currentActor && CanUseSkill(skillId, actionCost);
        }

        /// <summary>
        /// 非当前行动角色只允许登记下一次行动的大招。预选不推进 actionCursor，
        /// 因而不能用点头像的方式抢占行动权。
        /// </summary>
        private bool CanPreselectSkill(BattleUnitState skillOwner, string skillId, int actionCost)
        {
            return skillOwner != null &&
                   skillOwner != currentActor &&
                   IsPlayerTurn() &&
                   !battleEnded &&
                   actionPoint >= actionCost &&
                   GetSkillCooldown(skillId) <= 0 &&
                   !queuedSkills.ContainsKey(skillOwner);
        }

        private SkillInputState GetSkillInputState(BattleUnitState skillOwner, string skillId, int actionCost)
        {
            if (skillOwner == null || skillOwner.defeated || battleEnded)
            {
                return SkillInputState.Unavailable;
            }

            QueuedSkillState queuedSkill;
            if (queuedSkills.TryGetValue(skillOwner, out queuedSkill))
            {
                return queuedSkill.skillId == skillId ? SkillInputState.Queued : SkillInputState.ReservedOtherSkill;
            }

            if (CanExecuteSkillImmediately(skillOwner, skillId, actionCost))
            {
                return SkillInputState.Immediate;
            }

            return CanPreselectSkill(skillOwner, skillId, actionCost)
                ? SkillInputState.Preselect
                : SkillInputState.Unavailable;
        }

        /// <summary>
        /// 只登记本角色下一次行动要释放的大招。此处不触发伤害、飘字、冷却或行动点扣除。
        /// 单体大招保存目标槽位，若目标退场，结算时自动回退到其他存活敌人。
        /// </summary>
        private void UseOrQueueSkill(int actionCost, string skillId, bool needsEnemyTarget)
        {
            BattleUnitState skillOwner = GetSelectedSkillOwner();
            BattleUnitState target = needsEnemyTarget ? GetSelectedOrFirstAliveEnemy() : skillOwner;
            SkillInputState inputState = GetSkillInputState(skillOwner, skillId, actionCost);

            if (inputState == SkillInputState.Queued)
            {
                queuedSkills.Remove(skillOwner);
                SetBattleMessage(skillOwner.unitName + " 已取消预选“" + GetSkillDisplayName(skillId) + "”，本次行动顺序不变。");
                RefreshAllViews();
                return;
            }

            if (inputState == SkillInputState.Immediate)
            {
                ExecuteSkillNow(skillOwner, skillId, actionCost, target);
                return;
            }

            if (inputState == SkillInputState.Preselect)
            {
                QueueSkill(skillOwner, actionCost, skillId, target);
                return;
            }

            SetBattleMessage(BuildSkillUnavailableMessage(skillOwner, skillId, actionCost));
            RefreshAllViews();
        }

        /// <summary>
        /// 只登记未来行动的技能，不扣行动点、不进入冷却，也不结束当前行动。
        /// 当前行动角色仍必须用普攻或“立即”大招完成自己的回合。
        /// </summary>
        private void QueueSkill(BattleUnitState skillOwner, int actionCost, string skillId, BattleUnitState target)
        {
            if (!CanPreselectSkill(skillOwner, skillId, actionCost))
            {
                SetBattleMessage(BuildSkillUnavailableMessage(skillOwner, skillId, actionCost));
                RefreshAllViews();
                return;
            }

            int targetSlotIndex = target == null ? -1 : GetUnitSlotIndex(target);
            queuedSkills[skillOwner] = new QueuedSkillState(skillId, actionCost, targetSlotIndex);
            SetBattleMessage(skillOwner.unitName + " 已预选“" + GetSkillDisplayName(skillId) + "”，将在其下次行动时释放；当前仍轮到 " + GetCurrentActorName() + " 行动。");
            RefreshAllViews();
        }

        /// <summary>
        /// 立即施放复用既有的技能结算路径，确保它与预选施放使用完全相同的伤害、治疗、
        /// 行动点和冷却规则；临时登记会在同一调用内被取出并清除。
        /// </summary>
        private void ExecuteSkillNow(BattleUnitState skillOwner, string skillId, int actionCost, BattleUnitState target)
        {
            if (!CanExecuteSkillImmediately(skillOwner, skillId, actionCost))
            {
                SetBattleMessage(BuildSkillUnavailableMessage(skillOwner, skillId, actionCost));
                RefreshAllViews();
                return;
            }

            queuedSkills[skillOwner] = new QueuedSkillState(skillId, actionCost, target == null ? -1 : GetUnitSlotIndex(target));
            string actionMessage;
            if (!TryExecuteQueuedSkillForCurrentActor(out actionMessage))
            {
                SetBattleMessage("立即施放失败，请重新选择技能或目标。");
                RefreshAllViews();
                return;
            }

            // 既有结算方法的文案以“预选”为默认语境；立即分支只替换展示文字，数值规则不变。
            CompletePlayerAction(actionMessage.Replace("预选", "立即"));
        }

        private string BuildSkillUnavailableMessage(BattleUnitState skillOwner, string skillId, int actionCost)
        {
            if (skillOwner == null || skillOwner.defeated)
            {
                return "请先选择一名仍可行动的我方角色。";
            }

            if (GetSkillCooldown(skillId) > 0)
            {
                return GetSkillDisplayName(skillId) + "仍在 CD " + GetSkillCooldown(skillId) + "。";
            }

            if (actionPoint < actionCost)
            {
                return "行动点不足，" + GetSkillDisplayName(skillId) + "需要 " + actionCost + " 点行动点。";
            }

            QueuedSkillState queuedSkill;
            if (queuedSkills.TryGetValue(skillOwner, out queuedSkill))
            {
                return skillOwner.unitName + " 已预选“" + GetSkillDisplayName(queuedSkill.skillId) + "”，请先取消或等待其行动。";
            }

            return IsPlayerTurn()
                ? "当前轮到 " + GetCurrentActorName() + " 行动；请选择当前角色立即施放，或为其他角色预选下次行动。"
                : "正在处理敌方行动，请等待下一次我方行动。";
        }

        /// <summary>
        /// 角色重新轮到行动时执行已预选技能。执行时才扣 AP、登记冷却并产生实际战斗结果。
        /// </summary>
        private bool TryExecuteQueuedSkillForCurrentActor(out string actionMessage)
        {
            actionMessage = string.Empty;
            if (currentActor == null || currentActor.defeated)
            {
                return false;
            }

            QueuedSkillState queuedSkill;
            if (!queuedSkills.TryGetValue(currentActor, out queuedSkill))
            {
                return false;
            }

            queuedSkills.Remove(currentActor);
            if (actionPoint < queuedSkill.actionCost || GetSkillCooldown(queuedSkill.skillId) > 0)
            {
                actionMessage = currentActor.unitName + " 的预选“" + GetSkillDisplayName(queuedSkill.skillId) + "”条件不足，已取消。";
                return true;
            }

            if (queuedSkill.skillId == "poetry_strike")
            {
                BattleUnitState target = GetQueuedOrFirstAliveEnemy(queuedSkill.targetSlotIndex);
                if (target == null)
                {
                    actionMessage = currentActor.unitName + " 的预选词意连击没有可用目标。";
                    return true;
                }

                RequestPortraitAttackEffect(currentActor, target, "poetry_strike", false);
                int damage = CalculateSkillDamage(currentActor, target, "poetry_strike", 1.8f, 220);
                bool damageApplied;
                bool targetDefeated = ApplyDamage(target, damage, out damageApplied);
                if (damageApplied)
                {
                    ShowDamageText(target, damage, targetDefeated);
                    QueueDefeatPresentation(target, targetDefeated);
                }
                ConsumeSkill("poetry_strike", PoetryStrikeCost);
                actionMessage = currentActor.unitName + " 的预选词意连击生效，对 " + target.unitName + " 造成 " + damage + " 点伤害。" + (targetDefeated ? " " + target.unitName + " 已退场。" : string.Empty);
                return true;
            }

            if (queuedSkill.skillId == "dream_area")
            {
                int aliveTargets = 0;
                int defeatedTargets = 0;
                int damage = CalculateAreaSkillDamage(currentActor, "dream_area", 0.75f);
                RequestPortraitAttackEffect(currentActor, null, "dream_area", true);

                for (int i = 0; i < enemyUnits.Length; i++)
                {
                    BattleUnitState enemy = enemyUnits[i];
                    if (enemy == null || enemy.defeated)
                    {
                        continue;
                    }

                    aliveTargets++;
                    bool damageApplied;
                    bool targetDefeated = ApplyDamage(enemy, damage, out damageApplied);
                    if (targetDefeated)
                    {
                        defeatedTargets++;
                    }

                    if (damageApplied)
                    {
                        ShowDamageText(enemy, damage, targetDefeated);
                        QueueDefeatPresentation(enemy, targetDefeated);
                    }
                }

                ConsumeSkill("dream_area", DreamAreaCost);
                actionMessage = currentActor.unitName + " 的预选如梦令生效，命中 " + aliveTargets + " 个敌方目标，每人受到 " + damage + " 点伤害，退场 " + defeatedTargets + " 人。";
                return true;
            }

            if (queuedSkill.skillId == "healing_verse")
            {
                BattleUnitState target = FindLowestHpAlly();
                if (target == null)
                {
                    actionMessage = currentActor.unitName + " 的预选疗愈没有可治疗目标。";
                    return true;
                }

                int healAmount = CalculateHealAmount(currentActor, "healing_verse", 1.2f);
                int actualHeal = HealUnit(target, healAmount);
                ShowHealText(target, actualHeal);
                ConsumeSkill("healing_verse", HealingVerseCost);
                actionMessage = currentActor.unitName + " 的预选疗愈生效，为 " + target.unitName + " 回复 " + actualHeal + " 点生命。";
                return true;
            }

            actionMessage = currentActor.unitName + " 的预选技能无效，已取消。";
            return true;
        }

        private BattleUnitState GetQueuedOrFirstAliveEnemy(int targetSlotIndex)
        {
            if (targetSlotIndex >= 0 && targetSlotIndex < enemyUnits.Length)
            {
                BattleUnitState queuedTarget = enemyUnits[targetSlotIndex];
                if (queuedTarget != null && !queuedTarget.defeated)
                {
                    return queuedTarget;
                }
            }

            return FindFirstAlive(enemyUnits);
        }

        private string GetSkillDisplayName(string skillId)
        {
            BattleSkillDto skill = FindSkill(skillId);
            if (skill != null && !string.IsNullOrEmpty(skill.label))
            {
                return skill.label;
            }

            if (skillId == "poetry_strike")
            {
                return "词意连击";
            }

            if (skillId == "dream_area")
            {
                return "如梦令";
            }

            return skillId == "healing_verse" ? "疗愈" : "技能";
        }

        /// <summary>
        /// 本次玩家行动结束后，以协程逐段处理敌方行动与到点自动释放的预选技能。
        /// 每一段数值结算完成后都等待对应攻击表现播放完，再推进到下一名行动者。
        /// </summary>
        private void CompletePlayerAction(string playerMessage)
        {
            if (TryFinishBattle())
            {
                return;
            }

            if (followUpResolutionCoroutine != null)
            {
                return;
            }

            resolvingEnemyTurn = true;
            // 逻辑锁定后立即刷新按钮和提示，避免玩家看到仍可点击的旧状态。
            ShowResolvingActionLog(playerMessage);
            followUpResolutionCoroutine = StartCoroutine(ResolveFollowUpActionsRoutine(playerMessage));
        }

        /// <summary>
        /// 将玩家出手后的后续行动拆成多个表现批次。
        /// 本协程只改变“何时推进到下一名行动者”，继续复用已有伤害、技能与胜负结算逻辑。
        /// </summary>
        private IEnumerator ResolveFollowUpActionsRoutine(string playerMessage)
        {
            string actionLog = playerMessage;
            int safety = UnitCount * 3;

            // 先让玩家本次攻击的施法、受击、飘字与退场表现完整播完。
            yield return WaitForPresentationQueueToFinish();
            if (TryFinishBattle())
            {
                FinishFollowUpResolution();
                yield break;
            }

            MoveToNextAvailableActor(true);
            while (!battleEnded && currentActor != null && safety-- > 0)
            {
                if (currentActor.isAlly)
                {
                    string queuedMessage;
                    if (!TryExecuteQueuedSkillForCurrentActor(out queuedMessage))
                    {
                        break;
                    }

                    if (!string.IsNullOrEmpty(queuedMessage))
                    {
                        actionLog += "\n" + queuedMessage;
                        ShowResolvingActionLog(actionLog);
                    }
                }
                else
                {
                    string enemyMessage = ResolveEnemyAction(currentActor);
                    if (!string.IsNullOrEmpty(enemyMessage))
                    {
                        actionLog += "\n" + enemyMessage;
                        ShowResolvingActionLog(actionLog);
                    }
                }

                // 当前行动者的表现完成后，才能结算下一名行动者，避免多人看起来同时出手。
                yield return WaitForPresentationQueueToFinish();
                if (TryFinishBattle())
                {
                    FinishFollowUpResolution();
                    yield break;
                }

                MoveToNextAvailableActor(true);
            }

            actionLog += "\n" + BuildTurnPrompt();
            SetBattleMessage(actionLog);
            FinishFollowUpResolution();
            RefreshAllViews();
        }

        /// <summary>
        /// 在后续行动链进行中即时刷新文本与按钮状态。
        /// 该方法只显示已经结算完成的行动记录，不计算数值，也不改变战斗状态。
        /// </summary>
        private void ShowResolvingActionLog(string actionLog)
        {
            SetBattleMessage(actionLog + "\n行动表现中，等待下一位行动者。");
            RefreshAllViews();
        }

        /// <summary>
        /// 正常结束后续行动结算，恢复玩家输入。
        /// </summary>
        private void FinishFollowUpResolution()
        {
            resolvingEnemyTurn = false;
            followUpResolutionCoroutine = null;
        }

        private string ResolveEnemyAction(BattleUnitState enemyAttacker)
        {
            BattleUnitState allyTarget = FindLowestHpAlly();
            if (enemyAttacker == null || allyTarget == null)
            {
                return string.Empty;
            }

            RequestPortraitAttackEffect(enemyAttacker, allyTarget, "enemy_basic", false);
            int enemyDamage = CalculateDamage(enemyAttacker, allyTarget);
            bool damageApplied;
            bool enemyKilledTarget = ApplyDamage(allyTarget, enemyDamage, out damageApplied);
            if (damageApplied)
            {
                ShowDamageText(allyTarget, enemyDamage, enemyKilledTarget);
                QueueDefeatPresentation(allyTarget, enemyKilledTarget);
            }
            return BuildAttackMessage(enemyAttacker, allyTarget, enemyDamage, enemyKilledTarget);
        }

        /// <summary>
        /// 以行动值从高到低建立一回合的顺序；同值时我方优先，便于 Demo 首回合直接操作。
        /// </summary>
        private void BuildActionOrder()
        {
            actionOrder.Clear();
            AddAliveUnitsToActionOrder(allyUnits);
            AddAliveUnitsToActionOrder(enemyUnits);
            actionOrder.Sort(delegate(BattleUnitState left, BattleUnitState right)
            {
                int valueCompare = right.actionValue.CompareTo(left.actionValue);
                if (valueCompare != 0)
                {
                    return valueCompare;
                }

                // 行动值相同才按速度排序，速度不参与本轮伤害计算。
                int speedCompare = right.speed.CompareTo(left.speed);
                if (speedCompare != 0)
                {
                    return speedCompare;
                }

                if (left.isAlly == right.isAlly)
                {
                    return 0;
                }

                return left.isAlly ? -1 : 1;
            });
            actionCursor = 0;
            currentActor = actionOrder.Count > 0 ? actionOrder[0] : null;
        }

        private void AddAliveUnitsToActionOrder(BattleUnitState[] units)
        {
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] != null && !units[i].defeated)
                {
                    actionOrder.Add(units[i]);
                }
            }
        }

        /// <summary>
        /// 移动到下一个仍存活的行动者；队列走完时进入新回合并恢复行动点、减少冷却。
        /// </summary>
        private void MoveToNextAvailableActor(bool advanceCursor)
        {
            if (actionOrder.Count == 0)
            {
                BuildActionOrder();
                return;
            }

            if (advanceCursor)
            {
                actionCursor++;
            }

            int safety = UnitCount * 3;
            while (safety-- > 0)
            {
                if (actionCursor >= actionOrder.Count)
                {
                    StartNewRound();
                    return;
                }

                BattleUnitState candidate = actionOrder[actionCursor];
                if (candidate != null && !candidate.defeated)
                {
                    currentActor = candidate;
                    return;
                }

                actionCursor++;
            }

            currentActor = null;
        }

        private void StartNewRound()
        {
            roundIndex++;
            actionPoint = actionPointMax;
            DecreaseAllSkillCooldowns();
            BuildActionOrder();
        }

        private void DecreaseAllSkillCooldowns()
        {
            List<string> keys = new List<string>(skillCooldowns.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                int remaining = Mathf.Max(0, skillCooldowns[key] - 1);
                if (remaining == 0)
                {
                    skillCooldowns.Remove(key);
                }
                else
                {
                    skillCooldowns[key] = remaining;
                }
            }
        }

        private bool TryFinishBattle()
        {
            // 结算只能触发一次，后续敌方行动或按钮回调都直接短路。
            if (battleEnded)
            {
                return true;
            }

            if (AllDefeated(enemyUnits))
            {
                battleEnded = true;
                RefreshAllViews();
                if (router != null)
                {
                    router.ResolveBattleVictory();
                }
                return true;
            }

            if (AllDefeated(allyUnits))
            {
                battleEnded = true;
                RefreshAllViews();
                if (router != null)
                {
                    router.ResolveBattleDefeat();
                }
                return true;
            }

            return false;
        }

        private bool IsPlayerTurn()
        {
            return currentActor != null && currentActor.isAlly && !currentActor.defeated && !resolvingEnemyTurn;
        }

        private string GetCurrentActorName()
        {
            return currentActor == null ? "\u65e0\u4eba" : currentActor.unitName;
        }

        /// <summary>
        /// 组合顶部回合提示。行动者仍只由行动值队列决定，
        /// “目标”只表示玩家当前选择的敌方单位，不会抢占任何角色的行动权。
        /// </summary>
        private string BuildBattleRoundTip()
        {
            string targetName = GetSelectedEnemyName();
            string targetTip = string.IsNullOrEmpty(targetName) ? "目标：无" : "目标：" + targetName;
            return "第 " + roundIndex + " 回合    行动者：" + GetCurrentActorName() + "    " + targetTip +
                   "\n行动顺序：" + BuildActionOrderPreview();
        }

        /// <summary>
        /// 基于既有行动值队列给玩家展示当前回合接下来会行动的角色。
        /// 这里只读取 actionOrder/actionCursor；绝不推进游标、重排队列或改变当前行动者。
        /// </summary>
        private string BuildActionOrderPreview()
        {
            if (battleEnded || actionOrder.Count == 0)
            {
                return "本场已结束";
            }

            List<string> labels = new List<string>();
            int scanIndex = Mathf.Max(0, actionCursor);
            while (scanIndex < actionOrder.Count && labels.Count < ActionPreviewCount)
            {
                BattleUnitState unit = actionOrder[scanIndex];
                if (unit != null && !unit.defeated)
                {
                    labels.Add(GetActionPreviewUnitName(unit));
                }

                scanIndex++;
            }

            return labels.Count > 0 ? string.Join(" → ", labels.ToArray()) : "本回合结算中";
        }

        /// <summary>
        /// 用“我/敌”前缀降低同名或肖像相近时的识别成本。
        /// </summary>
        private string GetActionPreviewUnitName(BattleUnitState unit)
        {
            if (unit == null)
            {
                return string.Empty;
            }

            return (unit.isAlly ? "我·" : "敌·") + unit.unitName;
        }

        /// <summary>
        /// 获取当前选中的存活敌方名称。选中目标已经退场时返回空，
        /// 由既有 GetSelectedOrFirstAliveEnemy() 在真正结算时回退到其他存活目标。
        /// </summary>
        private string GetSelectedEnemyName()
        {
            if (selectedEnemyIndex < 0 || selectedEnemyIndex >= UnitCount)
            {
                return string.Empty;
            }

            BattleUnitState selectedEnemy = enemyUnits[selectedEnemyIndex];
            return selectedEnemy == null || selectedEnemy.defeated ? string.Empty : selectedEnemy.unitName;
        }

        private string BuildTurnPrompt()
        {
            return IsPlayerTurn()
                ? "\u8f6e\u5230 " + currentActor.unitName + " \u884c\u52a8\uff0c\u53ef\u9009\u62e9\u76ee\u6807\u540e\u65bd\u653e\u6280\u80fd\u3002"
                : "\u6b63\u5728\u5904\u7406\u654c\u65b9\u884c\u52a8\u3002";
        }

        private BattleUnitState FindLowestHpAlly()
        {
            BattleUnitState best = null;
            float bestRate = 2f;

            for (int i = 0; i < allyUnits.Length; i++)
            {
                BattleUnitState unit = allyUnits[i];
                if (unit == null || unit.defeated)
                {
                    continue;
                }

                float hpRate = unit.maxHp <= 0 ? 1f : (float)unit.currentHp / unit.maxHp;
                if (best == null || hpRate < bestRate)
                {
                    best = unit;
                    bestRate = hpRate;
                }
            }

            return best;
        }

        private int HealUnit(BattleUnitState target, int healAmount)
        {
            int oldHp = target.currentHp;
            target.currentHp = Mathf.Min(target.maxHp, target.currentHp + healAmount);
            target.defeated = target.currentHp <= 0;
            return target.currentHp - oldHp;
        }

        private int CalculateSkillDamage(BattleUnitState attacker, BattleUnitState target, string skillId, float fallbackMultiplier, int fallbackBonus)
        {
            BattleSkillDto skill = FindSkill(skillId);
            float multiplier = skill != null && skill.multiplier > 0f ? skill.multiplier : fallbackMultiplier;
            return Mathf.Max(90, Mathf.RoundToInt(CalculateDamage(attacker, target) * multiplier) + fallbackBonus);
        }

        private int CalculateAreaSkillDamage(BattleUnitState attacker, string skillId, float fallbackMultiplier)
        {
            BattleSkillDto skill = FindSkill(skillId);
            float multiplier = skill != null && skill.multiplier > 0f ? skill.multiplier : fallbackMultiplier;
            return Mathf.Max(70, Mathf.RoundToInt(attacker.attack * multiplier));
        }

        private int CalculateHealAmount(BattleUnitState healer, string skillId, float fallbackMultiplier)
        {
            BattleSkillDto skill = FindSkill(skillId);
            float multiplier = skill != null && skill.multiplier > 0f ? skill.multiplier : fallbackMultiplier;
            return Mathf.Max(160, Mathf.RoundToInt(healer.attack * multiplier));
        }

        private BattleSkillDto FindSkill(string skillId)
        {
            if (backendSkills == null || string.IsNullOrEmpty(skillId))
            {
                return null;
            }

            for (int i = 0; i < backendSkills.Length; i++)
            {
                if (backendSkills[i] != null && backendSkills[i].id == skillId)
                {
                    return backendSkills[i];
                }
            }

            return null;
        }

        private void SetSkillButton(Button button, string skillId, string fallbackLabel)
        {
            BattleSkillDto skill = FindSkill(skillId);
            string label = skill != null && !string.IsNullOrEmpty(skill.label) ? skill.label : fallbackLabel;
            int actionCost = GetSkillActionCost(skillId);
            BattleUnitState skillOwner = GetSelectedSkillOwner();
            SkillInputState inputState = skillId == "basic"
                ? (CanUseSkill("basic", BasicSkillCost) ? SkillInputState.Immediate : SkillInputState.Unavailable)
                : GetSkillInputState(skillOwner, skillId, actionCost);

            label += "\n" + GetSkillInputLabel(skillId, inputState, actionCost);
            SetButtonLabel(button, label);
            LayoutSkillButtonContent(button);
            SetSkillButtonVisualState(button, inputState);

            if (button == null || skill == null || string.IsNullOrEmpty(skill.iconKey))
            {
                return;
            }

            string iconKey = ResolveSkillIconKey(skillId, skill.iconKey);
            Sprite sprite;
            if (skillIconCache.TryGetValue(iconKey, out sprite) && sprite != null)
            {
                Image iconImage = GetOrCreateSkillIconImage(button);
                if (iconImage != null)
                {
                    iconImage.sprite = sprite;
                    iconImage.color = Color.white;
                    iconImage.type = Image.Type.Simple;
                    iconImage.preserveAspect = true;
                }
            }
        }

        private int GetSkillActionCost(string skillId)
        {
            if (skillId == "poetry_strike")
            {
                return PoetryStrikeCost;
            }

            if (skillId == "dream_area")
            {
                return DreamAreaCost;
            }

            return skillId == "healing_verse" ? HealingVerseCost : BasicSkillCost;
        }

        private string GetSkillInputLabel(string skillId, SkillInputState inputState, int actionCost)
        {
            if (GetSkillCooldown(skillId) > 0)
            {
                return "CD " + GetSkillCooldown(skillId);
            }

            if (actionPoint < actionCost)
            {
                return "行动点不足";
            }

            if (inputState == SkillInputState.Immediate)
            {
                return "立即";
            }

            if (inputState == SkillInputState.Preselect)
            {
                return "预选·下回合";
            }

            if (inputState == SkillInputState.Queued)
            {
                return "已预选·点击取消";
            }

            if (inputState == SkillInputState.ReservedOtherSkill)
            {
                return "已有预选";
            }

            return IsPlayerTurn() ? "等待行动" : "敌方行动中";
        }

        /// <summary>
        /// 技能按钮用颜色直接表达行为：金色立即、紫色预选、蓝紫色已预选、灰色不可用。
        /// 只改按钮底图，不遮挡后端加载的技能图标。
        /// </summary>
        private void SetSkillButtonVisualState(Button button, SkillInputState inputState)
        {
            if (button == null || button.targetGraphic == null)
            {
                return;
            }

            Color color = new Color32(220, 216, 210, 150);
            if (inputState == SkillInputState.Immediate)
            {
                color = new Color32(255, 224, 156, 255);
            }
            else if (inputState == SkillInputState.Preselect)
            {
                color = new Color32(214, 190, 255, 255);
            }
            else if (inputState == SkillInputState.Queued)
            {
                color = new Color32(168, 208, 255, 255);
            }

            button.targetGraphic.color = color;
        }

        /// <summary>
        /// 后端 demo-config 当前给的是 skill_bg_01~04，这类更像按钮底纹。
        /// 这里先按技能 ID 映射到真正的技能图标，避免把按钮背景误当技能图标显示。
        /// 后续如果后端直接返回 skill_basic_attack 等 iconKey，这个方法会自动尊重后端配置。
        /// </summary>
        private string ResolveSkillIconKey(string skillId, string backendIconKey)
        {
            if (backendIconKey == "skill_basic_attack" ||
                backendIconKey == "skill_poetry_attack" ||
                backendIconKey == "skill_group_damage" ||
                backendIconKey == "skill_heal")
            {
                return backendIconKey;
            }

            if (skillId == "basic")
            {
                return "skill_basic_attack";
            }

            if (skillId == "poetry_strike")
            {
                return "skill_poetry_attack";
            }

            if (skillId == "dream_area")
            {
                return "skill_group_damage";
            }

            if (skillId == "healing_verse")
            {
                return "skill_heal";
            }

            return backendIconKey;
        }

        /// <summary>
        /// 技能图标独立放在按钮子节点中，不覆盖按钮本身的通用底图。
        /// 这样后续换按钮皮肤时，技能图标和按钮框不会互相干扰。
        /// </summary>
        private Image GetOrCreateSkillIconImage(Button button)
        {
            if (button == null)
            {
                return null;
            }

            Transform iconTransform = button.transform.Find("SkillIcon");
            RectTransform rect;
            if (iconTransform == null)
            {
                GameObject iconObject = new GameObject("SkillIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(button.transform, false);
                rect = iconObject.GetComponent<RectTransform>();
            }
            else
            {
                rect = iconTransform as RectTransform;
                if (rect == null)
                {
                    rect = iconTransform.gameObject.AddComponent<RectTransform>();
                }
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 10f);
            rect.sizeDelta = new Vector2(46f, 46f);

            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.raycastTarget = false;
            image.enabled = true;
            rect.SetAsFirstSibling();
            return image;
        }

        /// <summary>
        /// 把图标和文字分开摆放：图标在上、技能名称在下。
        /// 这样既不会遮住通用按钮底图，也不会和文字重叠而看起来像“没有图标”。
        /// </summary>
        private void LayoutSkillButtonContent(Button button)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                return;
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, -18f);
            labelRect.sizeDelta = new Vector2(150f, 32f);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = Mathf.Min(label.fontSize, 18);
            label.raycastTarget = false;
            label.rectTransform.SetAsLastSibling();
        }

        private int CalculateDamage(BattleUnitState attacker, BattleUnitState target)
        {
            // 先用稳定的轻量公式：攻击力 - 少量防御修正。
            // 后续接角色技能时，这里会被技能倍率、属性克制、暴击等规则替换。
            int defenseOffset = target.isAlly ? 18 : 12;
            return Mathf.Max(60, attacker.attack - defenseOffset);
        }

        private bool ApplyDamage(BattleUnitState target, int damage, out bool damageApplied)
        {
            damageApplied = false;
            // 已退场的单位不能再次承受伤害，也不能生成新的受击表现。
            if (target == null || target.defeated)
            {
                return false;
            }

            bool wasAlive = !target.defeated;
            target.currentHp = Mathf.Max(0, target.currentHp - damage);
            target.defeated = target.currentHp <= 0;
            damageApplied = true;

            // 角色退场后不应保留其未来行动的预选大招。
            if (target.defeated)
            {
                queuedSkills.Remove(target);
            }

            if (target.defeated && target == enemyUnits[selectedEnemyIndex])
            {
                BattleUnitState nextTarget = FindFirstAlive(enemyUnits);
                selectedEnemyIndex = nextTarget == null ? selectedEnemyIndex : System.Array.IndexOf(enemyUnits, nextTarget);
            }

            return wasAlive && target.defeated;
        }

        /// <summary>
        /// 统一创建攻击表现请求并通知外部表现层。
        /// 这里故意不绑定具体动画组件：现阶段可零资源运行，后续替换 UI 或特效资源也不影响战斗逻辑。
        /// </summary>
        private void RequestPortraitAttackEffect(BattleUnitState attacker, BattleUnitState target, string skillId, bool hitsAllTargets)
        {
            if (attacker == null)
            {
                return;
            }

            int attackerSlotIndex = GetUnitSlotIndex(attacker);
            int targetSlotIndex = GetUnitSlotIndex(target);
            BattlePortraitEffectRequest request = new BattlePortraitEffectRequest(
                attackerSlotIndex,
                attacker.isAlly,
                attacker.unitName,
                skillId,
                targetSlotIndex,
                target != null && target.isAlly,
                target == null ? string.Empty : target.unitName,
                hitsAllTargets);

            // 外部头像特效也随此队列播放，避免未来接入 Animator 后多位角色同时施法。
            QueuePresentationEvent(BattlePresentationEvent.CreateAttack(attacker, target, request));
        }

        /// <summary>
        /// 将战斗单位映射回阵容槽位。找不到或没有目标时返回 -1，供未来全体攻击/无目标表现使用。
        /// </summary>
        private int GetUnitSlotIndex(BattleUnitState unit)
        {
            if (unit == null)
            {
                return -1;
            }

            return System.Array.IndexOf(unit.isAlly ? allyUnits : enemyUnits, unit);
        }

        private string BuildAttackMessage(BattleUnitState attacker, BattleUnitState target, int damage, bool targetDefeated)
        {
            string message = attacker.unitName + " \u5bf9 " + target.unitName + " \u9020\u6210 " + damage + " \u70b9\u4f24\u5bb3\u3002";
            if (targetDefeated)
            {
                message += " " + target.unitName + " \u5df2\u9000\u573a\u3002";
            }

            return message;
        }

        private void ShowDamageText(BattleUnitState target, int damage, bool canPlayWhenTargetIsDefeated)
        {
            QueuePresentationEvent(BattlePresentationEvent.CreateDamage(target, damage, target.isAlly ? new Color32(255, 92, 92, 255) : new Color32(255, 232, 128, 255), canPlayWhenTargetIsDefeated));
        }

        /// <summary>
        /// 将逻辑上的阵亡排在本次伤害飘字之后，避免先置灰/隐藏再出现伤害的错觉。
        /// </summary>
        private void QueueDefeatPresentation(BattleUnitState target, bool targetDefeated)
        {
            if (targetDefeated)
            {
                QueuePresentationEvent(BattlePresentationEvent.CreateDefeat(target));
            }
        }

        private void ShowHealText(BattleUnitState target, int healAmount)
        {
            QueuePresentationEvent(BattlePresentationEvent.CreateHeal(target, healAmount, new Color32(110, 255, 160, 255)));
        }

        /// <summary>
        /// 将一个已经得出结果的表现事件放进 FIFO 队列。这里不修改任何战斗数值，
        /// 因而可以在未来替换为 Animator、Timeline 或 Spine 而不影响结算。
        /// </summary>
        private void QueuePresentationEvent(BattlePresentationEvent presentationEvent)
        {
            if (presentationEvent == null)
            {
                return;
            }

            presentationEvents.Enqueue(presentationEvent);
            if (presentationCoroutine == null && isActiveAndEnabled)
            {
                presentationCoroutine = StartCoroutine(PlayPresentationQueue());
            }
        }

        /// <summary>
        /// 统一顺序播放攻击、受击/治疗和退场。战斗逻辑已经在调用方完成，
        /// 协程只负责让玩家按正确顺序看见这些结果。
        /// </summary>
        private IEnumerator PlayPresentationQueue()
        {
            isPlayingPresentation = true;
            RefreshBattleControls();

            // 让本帧的整段战斗解析先完成，再开始播放已收集的完整事件序列。
            yield return null;
            while (presentationEvents.Count > 0)
            {
                BattlePresentationEvent presentationEvent = presentationEvents.Dequeue();
                yield return PlayPresentationEvent(presentationEvent);
            }

            isPlayingPresentation = false;
            presentationCoroutine = null;
            RefreshAllViews();
        }

        private IEnumerator PlayPresentationEvent(BattlePresentationEvent presentationEvent)
        {
            if (presentationEvent == null)
            {
                yield break;
            }

            if (presentationEvent.type == PresentationEventType.Attack)
            {
                if (PortraitAttackEffectRequested != null && presentationEvent.attackEffectRequest != null)
                {
                    PortraitAttackEffectRequested.Invoke(presentationEvent.attackEffectRequest);
                }

                // 攻击者施法高亮：上抬 + 放大 + 冷色亮起，再回落还原。
                yield return PlayAttackerCast(GetViewForUnit(presentationEvent.source));
                yield break;
            }

            if (presentationEvent.type == PresentationEventType.Damage || presentationEvent.type == PresentationEventType.Heal)
            {
                // 最后一击允许先播放一次飘字；其他已经退场目标的历史事件直接跳过。
                if (presentationEvent.type == PresentationEventType.Damage &&
                    presentationEvent.target != null &&
                    presentationEvent.target.defeated &&
                    !presentationEvent.canPlayWhenTargetIsDefeated)
                {
                    yield break;
                }

                BattleUnitView targetView = GetViewForUnit(presentationEvent.target);
                ShowFloatingTextImmediately(targetView, presentationEvent.text, presentationEvent.textColor);
                Color32 impactColor = presentationEvent.type == PresentationEventType.Heal
                    ? new Color32(114, 255, 176, 225)
                    : new Color32(255, 164, 144, 235);

                // 受击三段表现：白闪（命中一帧）→ 颜色脉冲 → 飘字上浮淡出。
                yield return PlayImpactWhiteFlash(targetView, ImpactWhiteFlashSeconds);
                yield return PlayUnitPulse(targetView, impactColor, HitColorPulseSeconds);
                yield return PlayFloatingTextRise(targetView, FloatingTextRiseSeconds);
                HideFloatingText(targetView);
                yield break;
            }

            if (presentationEvent.type == PresentationEventType.Defeat)
            {
                BattleUnitView targetView = GetViewForUnit(presentationEvent.target);
                HideDefeatedUnitView(targetView);
            }
        }

        private IEnumerator PlayUnitPulse(BattleUnitView view, Color32 tint, float duration)
        {
            if (view == null)
            {
                yield break;
            }

            Image slotImage = view.slotImage;
            Image portrait = view.portrait;
            Color slotColor = slotImage == null ? Color.white : slotImage.color;
            Color portraitColor = portrait == null ? Color.white : portrait.color;
            Vector3 portraitScale = portrait == null ? Vector3.one : portrait.rectTransform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float rate = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(rate * Mathf.PI);
                if (slotImage != null)
                {
                    slotImage.color = Color.Lerp(slotColor, tint, pulse);
                }

                if (portrait != null)
                {
                    portrait.color = Color.Lerp(portraitColor, Color.white, pulse * 0.5f);
                    portrait.rectTransform.localScale = portraitScale * (1f + pulse * 0.08f);
                }

                yield return null;
            }

            if (slotImage != null)
            {
                slotImage.color = slotColor;
            }

            if (portrait != null)
            {
                portrait.color = portraitColor;
                portrait.rectTransform.localScale = portraitScale;
            }
        }

        /// <summary>
        /// 攻击者施法高亮：头像上抬 + 轻微放大 + 冷色亮起，再回落还原，模拟"起手施法"。
        /// 只做表现，不改变任何战斗数值；结束后还原位置、缩放与颜色。
        /// </summary>
        private IEnumerator PlayAttackerCast(BattleUnitView view)
        {
            if (view == null)
            {
                yield break;
            }

            Image slotImage = view.slotImage;
            Image portrait = view.portrait;
            Color slotColor = slotImage == null ? Color.white : slotImage.color;
            Color portraitColor = portrait == null ? Color.white : portrait.color;
            Vector3 portraitScale = portrait == null ? Vector3.one : portrait.rectTransform.localScale;
            Vector3 portraitPos = portrait == null ? Vector3.zero : portrait.rectTransform.localPosition;
            Color32 castTint = new Color32(112, 255, 214, 235);
            float elapsed = 0f;

            while (elapsed < AttackPresentationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float rate = AttackPresentationSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / AttackPresentationSeconds);
                // sin 波形保证前半程抬升、后半程回落，峰值出现在中点。
                float lift = Mathf.Sin(rate * Mathf.PI);
                if (portrait != null)
                {
                    portrait.rectTransform.localPosition = portraitPos + new Vector3(0f, lift * 8f, 0f);
                    portrait.rectTransform.localScale = portraitScale * (1f + lift * 0.10f);
                    portrait.color = Color.Lerp(portraitColor, castTint, lift * 0.6f);
                }

                if (slotImage != null)
                {
                    slotImage.color = Color.Lerp(slotColor, castTint, lift * 0.35f);
                }

                yield return null;
            }

            if (portrait != null)
            {
                portrait.rectTransform.localPosition = portraitPos;
                portrait.rectTransform.localScale = portraitScale;
                portrait.color = portraitColor;
            }

            if (slotImage != null)
            {
                slotImage.color = slotColor;
            }
        }

        /// <summary>
        /// 受击瞬间白闪：头像闪白 + 轻微放大，再快速还原，突出"命中"的一帧。
        /// </summary>
        private IEnumerator PlayImpactWhiteFlash(BattleUnitView view, float duration)
        {
            if (view == null)
            {
                yield break;
            }

            Image portrait = view.portrait;
            Color portraitColor = portrait == null ? Color.white : portrait.color;
            Vector3 portraitScale = portrait == null ? Vector3.one : portrait.rectTransform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float rate = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                // 从全白衰减回原色，前段保持放大制造"顿一下"的冲击感。
                float flash = 1f - rate;
                if (portrait != null)
                {
                    portrait.color = Color.Lerp(Color.white, portraitColor, rate);
                    portrait.rectTransform.localScale = portraitScale * (1f + flash * 0.14f);
                }

                yield return null;
            }

            if (portrait != null)
            {
                portrait.color = portraitColor;
                portrait.rectTransform.localScale = portraitScale;
            }
        }

        /// <summary>
        /// 飘字上浮 + 淡出。在受击脉冲之后调用，让飘字独立停留一段时间保证可读。
        /// 结束后还原位置与颜色，避免影响下一次飘字。
        /// </summary>
        private IEnumerator PlayFloatingTextRise(BattleUnitView view, float duration)
        {
            if (view == null || view.damageText == null)
            {
                yield break;
            }

            RectTransform textRect = view.damageText.rectTransform;
            Vector2 origin = textRect.anchoredPosition;
            Color originalColor = view.damageText.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float rate = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                textRect.anchoredPosition = origin + new Vector2(0f, rate * 22f);
                Color c = originalColor;
                c.a = Mathf.Lerp(originalColor.a, 0f, rate);
                view.damageText.color = c;
                yield return null;
            }

            textRect.anchoredPosition = origin;
            view.damageText.color = originalColor;
        }

        /// <summary>
        /// 最后一击飘字播放完毕后，立即移除阵亡单位的整个槽位。
        /// 不保留置灰、退场标签或淡出动画，避免让玩家误以为单位仍可被攻击。
        /// </summary>
        private void HideDefeatedUnitView(BattleUnitView view)
        {
            if (view == null || view.button == null)
            {
                return;
            }

            view.isRemoved = true;
            view.defeatLabelVisible = false;
            HideFloatingText(view);
            if (view.defeatedText != null)
            {
                view.defeatedText.gameObject.SetActive(false);
            }

            view.button.gameObject.SetActive(false);
        }

        /// <summary>
        /// 仅在明确开始一场新战斗时恢复所有槽位的可见状态。
        /// 战斗中的界面刷新不得修改 isRemoved；未来复活技能需要经过独立结算入口。
        /// </summary>
        private void ResetAllUnitViewRemovalState()
        {
            for (int i = 0; i < UnitCount; i++)
            {
                if (allyViews[i] != null)
                {
                    allyViews[i].isRemoved = false;
                }

                if (enemyViews[i] != null)
                {
                    enemyViews[i].isRemoved = false;
                }
            }
        }

        private void ShowFloatingTextImmediately(BattleUnitView view, string content, Color32 color)
        {
            if (view == null || view.damageText == null)
            {
                return;
            }

            view.damageText.text = content;
            view.damageText.color = color;
            view.damageText.gameObject.SetActive(true);
        }

        private void HideFloatingText(BattleUnitView view)
        {
            if (view != null && view.damageText != null)
            {
                view.damageText.text = string.Empty;
                view.damageText.gameObject.SetActive(false);
            }
        }

        private bool IsBattleInputLocked()
        {
            return resolvingEnemyTurn || isAutoBattleRunning || isPlayingPresentation || presentationEvents.Count > 0;
        }

        /// <summary>
        /// 等待当前攻击批次的所有表现事件播完。此处只控制节奏，
        /// 不计算伤害、不改变行动值，也不触碰技能冷却与奖励逻辑。
        /// </summary>
        private IEnumerator WaitForPresentationQueueToFinish()
        {
            while (isPlayingPresentation || presentationEvents.Count > 0)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 等待玩家本次出手后的敌我后续行动链结束。
        /// 自动战斗只负责等待，不参与行动值、伤害、冷却或胜负结算。
        /// </summary>
        private IEnumerator WaitForFollowUpResolutionToFinish()
        {
            while (followUpResolutionCoroutine != null)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 战斗重置、撤退或离开页面时停止自动战斗，
        /// 防止旧协程在新战斗或其他页面继续触发攻击。
        /// </summary>
        private void StopAutoBattleRoutine()
        {
            isAutoBattleRunning = false;
            if (autoBattleCoroutine != null)
            {
                StopCoroutine(autoBattleCoroutine);
                autoBattleCoroutine = null;
            }
        }

        /// <summary>
        /// 战斗重置、撤退或离开页面时停止后续行动结算，
        /// 防止旧战斗在新页面继续推进敌方或预选技能行动。
        /// </summary>
        private void StopFollowUpResolutionRoutine()
        {
            resolvingEnemyTurn = false;
            if (followUpResolutionCoroutine != null)
            {
                StopCoroutine(followUpResolutionCoroutine);
                followUpResolutionCoroutine = null;
            }
        }

        private void ClearPresentationQueue()
        {
            presentationEvents.Clear();
            isPlayingPresentation = false;
            if (presentationCoroutine != null)
            {
                StopCoroutine(presentationCoroutine);
                presentationCoroutine = null;
            }
        }

        private BattleUnitView GetViewForUnit(BattleUnitState unit)
        {
            if (unit == null)
            {
                return null;
            }

            BattleUnitState[] states = unit.isAlly ? allyUnits : enemyUnits;
            BattleUnitView[] views = unit.isAlly ? allyViews : enemyViews;
            int index = System.Array.IndexOf(states, unit);
            if (index < 0 || index >= views.Length)
            {
                return null;
            }

            return views[index];
        }

        private void RefreshBattleControls()
        {
            bool canContinueBattle = !IsBattleInputLocked() && !battleEnded && string.IsNullOrEmpty(GetBattleUnavailableReason());
            SetButtonLabel(startBattleButton, battleEnded ? "\u91cd\u65b0\u5f00\u59cb" : "\u5f00\u59cb\u6218\u6597");
            SetSkillButton(basicSkillButton, "basic", "\u666e\u653b");
            SetSkillButton(poetryStrikeButton, "poetry_strike", "\u8bcd\u610f\u8fde\u51fb");
            SetSkillButton(dreamAreaButton, "dream_area", "\u5982\u68a6\u4ee4");
            SetSkillButton(healSkillButton, "healing_verse", "\u7597\u6108");

            // 表现队列播放期间开始/重新开始按钮同样变灰，避免"可点但被输入锁静默忽略"。
            SetButtonInteractable(startBattleButton, !IsBattleInputLocked() && (battleEnded || (canContinueBattle && IsPlayerTurn())));
            SetButtonInteractable(autoBattleButton, canContinueBattle && IsPlayerTurn());
            SetButtonInteractable(basicSkillButton, canContinueBattle && CanUseSkill("basic", BasicSkillCost));
            BattleUnitState skillOwner = GetSelectedSkillOwner();
            SetButtonInteractable(poetryStrikeButton, canContinueBattle && CanClickSkill(skillOwner, "poetry_strike", PoetryStrikeCost));
            SetButtonInteractable(dreamAreaButton, canContinueBattle && CanClickSkill(skillOwner, "dream_area", DreamAreaCost));
            SetButtonInteractable(healSkillButton, canContinueBattle && CanClickSkill(skillOwner, "healing_verse", HealingVerseCost));
        }

        private bool CanClickSkill(BattleUnitState skillOwner, string skillId, int actionCost)
        {
            SkillInputState state = GetSkillInputState(skillOwner, skillId, actionCost);
            return state == SkillInputState.Immediate ||
                   state == SkillInputState.Preselect ||
                   state == SkillInputState.Queued;
        }

        private void RefreshAllViews()
        {
            SetText(roundTipText, BuildBattleRoundTip());
            SetText(actionPointText, "行动点 " + actionPoint + " / " + actionPointMax);

            for (int i = 0; i < UnitCount; i++)
            {
                RefreshView(allyViews[i], allyUnits[i], i == selectedAllyIndex, allyUnits[i] == currentActor, false);
                RefreshView(enemyViews[i], enemyUnits[i], i == selectedEnemyIndex, enemyUnits[i] == currentActor, true);
            }

            RefreshBattleControls();
        }

        private void RefreshView(BattleUnitView view, BattleUnitState unit, bool selected, bool acting, bool isEnemy)
        {
            if (view == null || unit == null)
            {
                return;
            }

            bool shouldShowSlot = !unit.defeated || !view.isRemoved;
            if (view.button != null && view.button.gameObject.activeSelf != shouldShowSlot)
            {
                view.button.gameObject.SetActive(shouldShowSlot);
            }

            // 最后一次飘字结束后已被移除的单位不再参与任何界面刷新。
            if (!shouldShowSlot)
            {
                return;
            }

            float hpRate = unit.maxHp <= 0 ? 0f : (float)unit.currentHp / unit.maxHp;
            if (view.hpBar != null)
            {
                Vector2 size = view.hpBar.sizeDelta;
                size.x = HpBarMaxWidth * hpRate;
                view.hpBar.sizeDelta = size;
            }

            SetText(view.nameText, GetUnitDisplayText(unit, view.isRemoved));

            if (view.slotImage != null)
            {
                view.slotImage.color = GetSlotBackgroundColor(unit, selected, acting, isEnemy);
            }

            if (view.selectedRing != null)
            {
                view.selectedRing.color = acting
                    ? new Color32(104, 255, 204, 220)
                    : (selected
                        ? (isEnemy ? new Color32(255, 160, 132, 235) : new Color32(255, 226, 145, 190))
                        : new Color32(255, 226, 145, 0));
            }

            Color portraitColor = view.isRemoved ? new Color(0.45f, 0.45f, 0.45f, 0.55f) : Color.white;
            if (view.portrait != null)
            {
                view.portrait.color = portraitColor;
            }

            if (view.defeatedText != null)
            {
                SetText(view.defeatedText, string.Empty);
                view.defeatedText.gameObject.SetActive(false);
            }
        }

        private string GetUnitDisplayText(BattleUnitState unit, bool isRemoved)
        {
            if (isRemoved)
            {
                return unit.unitName + "\n--";
            }

            return unit.unitName + "\n" + unit.currentHp + "/" + unit.maxHp;
        }

        private BattleUnitView BuildView(string slotName)
        {
            Transform slot = FindChildRecursive(transform, slotName);
            if (slot == null)
            {
                return null;
            }

            Button button = slot.GetComponent<Button>();
            Image slotImage = slot.GetComponent<Image>();
            if (button == null)
            {
                button = slot.gameObject.AddComponent<Button>();
            }

            if (slotImage != null)
            {
                slotImage.raycastTarget = true;
                button.targetGraphic = slotImage;
            }

            Transform hpBar = FindChildRecursive(slot, "HpBar");
            Transform selectedRing = FindChildRecursive(slot, "SelectedRing");
            Transform portrait = FindChildRecursive(slot, "Portrait");
            Text nameText = null;
            Transform nameLabel = FindChildRecursive(slot, "NameLabel");
            if (nameLabel != null)
            {
                nameText = nameLabel.GetComponentInChildren<Text>(true);
            }

            Text damageText = FindOrCreateSlotText(slot, "DamageText", new Vector2(0, 74), new Vector2(150, 36), 28, TextAnchor.MiddleCenter, new Color32(255, 232, 128, 255));
            Text defeatedText = FindOrCreateSlotText(slot, "DefeatedText", new Vector2(0, 22), new Vector2(108, 36), 24, TextAnchor.MiddleCenter, new Color32(255, 255, 255, 235));
            damageText.gameObject.SetActive(false);
            defeatedText.gameObject.SetActive(false);

            return new BattleUnitView
            {
                button = button,
                slotImage = slotImage,
                hpBar = hpBar == null ? null : hpBar.GetComponent<RectTransform>(),
                selectedRing = selectedRing == null ? null : selectedRing.GetComponent<Image>(),
                portrait = portrait == null ? null : portrait.GetComponent<Image>(),
                nameText = nameText,
                damageText = damageText,
                defeatedText = defeatedText
            };
        }

        private Text FindOrCreateSlotText(Transform parent, string objectName, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor, Color color)
        {
            Transform target = parent.Find(objectName);
            RectTransform rect;
            if (target == null)
            {
                GameObject textObject = new GameObject(objectName, typeof(RectTransform));
                textObject.transform.SetParent(parent, false);
                rect = textObject.GetComponent<RectTransform>();
            }
            else
            {
                rect = target as RectTransform;
                if (rect == null)
                {
                    rect = target.gameObject.AddComponent<RectTransform>();
                }
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = rect.GetComponent<Text>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<Text>();
            }

            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private Text FindLabel(string objectName)
        {
            Transform target = FindChildRecursive(transform, objectName);
            if (target == null)
            {
                return null;
            }

            return target.GetComponentInChildren<Text>(true);
        }

        private Button FindButton(string objectName)
        {
            Transform target = FindChildRecursive(transform, objectName);
            if (target == null)
            {
                return null;
            }

            return target.GetComponent<Button>();
        }

        private void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            // ?????????????????????????????
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }

        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private void SetBattleMessage(string message)
        {
            SetText(battleMessageText, message);
            SetText(roundTipText, BuildBattleRoundTip());
        }

        private void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
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

        private sealed class BattleUnitState
        {
            public readonly string unitName;
            public readonly bool isAlly;
            public readonly int maxHp;
            public readonly int attack;
            public readonly string portraitIconKey;
            public readonly int actionValue;
            // 速度只用于行动值相同后的行动顺序排序。
            public readonly int speed;
            // 以下字段为终态数据骨架：当前只存储，不参与伤害、命中、元素或 Buff 计算。
            public readonly float critRate;
            public readonly float critDamage;
            public readonly float hitRate;
            public readonly float dodgeRate;
            public readonly string element;
            public readonly int starLevel;
            public readonly int breakLevel;
            public readonly string[] buffIds;
            public int currentHp;
            public bool defeated;

            public BattleUnitState(string unitName, bool isAlly, int maxHp, int attack, string portraitIconKey)
                : this(unitName, isAlly, maxHp, attack, portraitIconKey, isAlly ? 120 : 100)
            {
            }

            public BattleUnitState(string unitName, bool isAlly, int maxHp, int attack, string portraitIconKey, int actionValue)
                : this(unitName, isAlly, maxHp, attack, portraitIconKey, actionValue, 100, 0f, 1.5f, 1f, 0f, null, 1, 0, null)
            {
            }

            public BattleUnitState(
                string unitName, bool isAlly, int maxHp, int attack, string portraitIconKey, int actionValue,
                int speed, float critRate, float critDamage, float hitRate, float dodgeRate,
                string element, int starLevel, int breakLevel, string[] buffIds)
            {
                this.unitName = unitName;
                this.isAlly = isAlly;
                this.maxHp = maxHp;
                this.attack = attack;
                this.portraitIconKey = portraitIconKey;
                this.actionValue = Mathf.Max(1, actionValue);
                this.speed = Mathf.Max(1, speed);
                this.critRate = Mathf.Clamp01(critRate);
                this.critDamage = Mathf.Max(1f, critDamage);
                this.hitRate = Mathf.Clamp01(hitRate);
                this.dodgeRate = Mathf.Clamp01(dodgeRate);
                this.element = string.IsNullOrEmpty(element) ? null : element;
                this.starLevel = Mathf.Max(1, starLevel);
                this.breakLevel = Mathf.Max(0, breakLevel);
                this.buffIds = buffIds == null ? new string[0] : (string[])buffIds.Clone();
                currentHp = maxHp;
            }
        }

        /// <summary>
        /// 用卡片底色补强选择反馈：青绿表示正在行动、珊瑚色表示当前攻击目标、
        /// 金色表示查看中的我方角色。它仅改变展示，不改变选择、行动值或伤害判定。
        /// </summary>
        private Color GetSlotBackgroundColor(BattleUnitState unit, bool selected, bool acting, bool isEnemy)
        {
            if (unit == null)
            {
                return new Color32(90, 82, 98, 82);
            }

            if (acting)
            {
                return new Color32(126, 243, 205, 190);
            }

            if (selected)
            {
                return isEnemy
                    ? new Color32(255, 178, 149, 190)
                    : new Color32(255, 231, 169, 170);
            }

            return new Color32(255, 248, 236, 118);
        }

        /// <summary>
        /// 预选指令只记录执行所需的最小数据，避免把 UI 状态写入战斗单位或后端存档。
        /// </summary>
        private enum SkillInputState
        {
            Unavailable,
            Immediate,
            Preselect,
            Queued,
            ReservedOtherSkill
        }

        /// <summary>
        /// 仅用于排列视觉播放顺序。枚举不会参与伤害、治疗、行动值或胜负结算。
        /// </summary>
        private enum PresentationEventType
        {
            Attack,
            Damage,
            Heal,
            Defeat
        }

        /// <summary>
        /// 已完成逻辑结算后的最小视觉事件数据。source 用于攻击者高亮，
        /// target 用于受击、治疗和退场表现。
        /// </summary>
        private sealed class BattlePresentationEvent
        {
            public readonly PresentationEventType type;
            public readonly BattleUnitState source;
            public readonly BattleUnitState target;
            public readonly string text;
            public readonly Color32 textColor;
            public readonly BattlePortraitEffectRequest attackEffectRequest;
            // 最后一击的伤害事件可在逻辑阵亡后播放一次；其他历史伤害事件则跳过。
            public readonly bool canPlayWhenTargetIsDefeated;

            private BattlePresentationEvent(
                PresentationEventType type,
                BattleUnitState source,
                BattleUnitState target,
                string text,
                Color32 textColor,
                BattlePortraitEffectRequest attackEffectRequest,
                bool canPlayWhenTargetIsDefeated)
            {
                this.type = type;
                this.source = source;
                this.target = target;
                this.text = text;
                this.textColor = textColor;
                this.attackEffectRequest = attackEffectRequest;
                this.canPlayWhenTargetIsDefeated = canPlayWhenTargetIsDefeated;
            }

            public static BattlePresentationEvent CreateAttack(BattleUnitState source, BattleUnitState target, BattlePortraitEffectRequest request)
            {
                return new BattlePresentationEvent(PresentationEventType.Attack, source, target, string.Empty, Color.white, request, false);
            }

            public static BattlePresentationEvent CreateDamage(BattleUnitState target, int damage, Color32 color, bool canPlayWhenTargetIsDefeated)
            {
                return new BattlePresentationEvent(PresentationEventType.Damage, null, target, "-" + damage, color, null, canPlayWhenTargetIsDefeated);
            }

            public static BattlePresentationEvent CreateHeal(BattleUnitState target, int healAmount, Color32 color)
            {
                return new BattlePresentationEvent(PresentationEventType.Heal, null, target, "+" + healAmount, color, null, false);
            }

            public static BattlePresentationEvent CreateDefeat(BattleUnitState target)
            {
                return new BattlePresentationEvent(PresentationEventType.Defeat, null, target, string.Empty, Color.white, null, false);
            }
        }

        private sealed class QueuedSkillState
        {
            public readonly string skillId;
            public readonly int actionCost;
            public readonly int targetSlotIndex;

            public QueuedSkillState(string skillId, int actionCost, int targetSlotIndex)
            {
                this.skillId = skillId;
                this.actionCost = actionCost;
                this.targetSlotIndex = targetSlotIndex;
            }
        }

        private sealed class BattleUnitView
        {
            public Button button;
            public Image slotImage;
            public RectTransform hpBar;
            public Image selectedRing;
            public Image portrait;
            public Text nameText;
            public Text damageText;
            public Text defeatedText;
            public bool defeatLabelVisible;
            public bool isRemoved;
        }
    }
}
