using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
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
        private const int UnitCount = 6;
        private const int FallbackActionPointMax = 3;
        private const string BattleApiBaseUrl = "http://127.0.0.1:5188";
        private const float HpBarMaxWidth = 86f;
        private const float DamageTextVisibleSeconds = 0.8f;

        private readonly BattleUnitState[] allyUnits = new BattleUnitState[UnitCount];
        private readonly BattleUnitState[] enemyUnits = new BattleUnitState[UnitCount];
        private readonly BattleUnitView[] allyViews = new BattleUnitView[UnitCount];
        private readonly BattleUnitView[] enemyViews = new BattleUnitView[UnitCount];
        private readonly Dictionary<string, Sprite> skillIconCache = new Dictionary<string, Sprite>();

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
        private int roundIndex = 1;
        private int actionPoint = FallbackActionPointMax;
        private bool battleEnded;
        private bool referencesBound;

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

        /// <summary>
        /// 每次进入战斗页时重置 Demo 战斗。
        /// 当前没有接正式战报，所以先保证每次进入都从完整血量开始。
        /// </summary>
        public void ResetDemoBattle()
        {
            selectedEnemyIndex = 0;
            roundIndex = 1;
            actionPoint = actionPointMax;
            battleEnded = false;

            for (int i = 0; i < UnitCount; i++)
            {
                allyUnits[i] = CreateAllyUnit(i);
                enemyUnits[i] = CreateEnemyUnit(i);
            }

            SetBattleMessage(
                "第一回合：我方行动。选择敌方头像，或直接点击“开始战斗”。" +
                "\n当前阵容：" + ShouyouBackendBootstrap.GetFormationSummary()
            );
            RefreshAllViews();
        }

        /// <summary>
        /// ???????????????????????????
        /// </summary>
        public void PressMainBattleButton()
        {
            BindRuntimeReferences();

            if (battleEnded)
            {
                ResetDemoBattle();
                return;
            }

            PerformPlayerAttack();
        }

        public void PerformPlayerAttack()
        {
            BattleUnitState attacker;
            BattleUnitState target;
            if (!TryGetBattleActionContext(out attacker, out target))
            {
                return;
            }

            int damage = CalculateDamage(attacker, target);
            bool targetDefeated = ApplyDamage(target, damage);
            ShowDamageText(target, damage);
            CompletePlayerAction(BuildAttackMessage(attacker, target, damage, targetDefeated));
        }

        /// <summary>
        /// ?????????????????????????
        /// ??????????????????????????
        /// </summary>
        public void CastPoetryStrike()
        {
            BattleUnitState attacker;
            BattleUnitState target;
            if (!TryGetBattleActionContext(out attacker, out target))
            {
                return;
            }

            int damage = CalculateSkillDamage(attacker, target, "poetry_strike", 1.8f, 220);
            bool targetDefeated = ApplyDamage(target, damage);
            ShowDamageText(target, damage);
            CompletePlayerAction(attacker.unitName + " \u65bd\u653e\u8bcd\u610f\u8fde\u51fb\uff0c\u5bf9 " + target.unitName + " \u9020\u6210 " + damage + " \u70b9\u4f24\u5bb3\u3002" + (targetDefeated ? " " + target.unitName + " \u5df2\u9000\u573a\u3002" : string.Empty));
        }

        /// <summary>
        /// ?????????????????????? Demo ???????????????
        /// </summary>
        public void CastDreamAreaAttack()
        {
            BattleUnitState attacker;
            BattleUnitState ignoredTarget;
            if (!TryGetBattleActionContext(out attacker, out ignoredTarget))
            {
                return;
            }

            int aliveTargets = 0;
            int defeatedTargets = 0;
            int damage = CalculateAreaSkillDamage(attacker, "dream_area", 0.75f);

            for (int i = 0; i < enemyUnits.Length; i++)
            {
                BattleUnitState enemy = enemyUnits[i];
                if (enemy == null || enemy.defeated)
                {
                    continue;
                }

                aliveTargets++;
                if (ApplyDamage(enemy, damage))
                {
                    defeatedTargets++;
                }

                ShowDamageText(enemy, damage);
            }

            CompletePlayerAction(attacker.unitName + " \u65bd\u653e\u5982\u68a6\u4ee4\uff0c\u547d\u4e2d " + aliveTargets + " \u4e2a\u654c\u65b9\u76ee\u6807\uff0c\u6bcf\u4eba\u53d7\u5230 " + damage + " \u70b9\u4f24\u5bb3\u3002\u9000\u573a " + defeatedTargets + " \u4eba\u3002");
        }

        /// <summary>
        /// ?????????????????????
        /// ?????????????????????????????
        /// </summary>
        public void CastHealingVerse()
        {
            BindRuntimeReferences();

            if (battleEnded)
            {
                SetBattleMessage("\u672c\u573a\u6218\u6597\u5df2\u7ecf\u7ed3\u7b97\uff0c\u8bf7\u8fd4\u56de\u4e3b\u7ebf\u6216\u91cd\u65b0\u8fdb\u5165\u3002");
                return;
            }

            BattleUnitState healer = FindFirstAlive(allyUnits);
            BattleUnitState target = FindLowestHpAlly();
            if (healer == null || target == null)
            {
                Debug.LogError("[BattleDemo] \u6218\u6597\u72b6\u6001\u5f02\u5e38\uff1a\u627e\u4e0d\u5230\u53ef\u6cbb\u7597\u7684\u53cb\u65b9\u5355\u4f4d\u3002");
                return;
            }

            int healAmount = CalculateHealAmount(healer, "healing_verse", 1.2f);
            int actualHeal = HealUnit(target, healAmount);
            ShowHealText(target, actualHeal);
            CompletePlayerAction(healer.unitName + " \u65bd\u653e\u7597\u6108\uff0c\u4e3a " + target.unitName + " \u56de\u590d " + actualHeal + " \u70b9\u751f\u547d\u3002");
        }

        /// <summary>
        /// 临时自动战斗：连续执行三次普通攻击。
        /// 这里不是开关状态，而是立即执行一组自动攻击，所以方法名必须和行为一致。
        /// </summary>
        public void PerformAutoAttacks()
        {
            for (int i = 0; i < 3 && !battleEnded; i++)
            {
                PerformPlayerAttack();
            }
        }

        /// <summary>
        /// 撤退按钮：当前 Demo 直接返回主线，不扣资源。
        /// </summary>
        public void RetreatBattle()
        {
            if (router != null)
            {
                router.ShowMainlineChapter();
            }
        }

        private void BindRuntimeReferences()
        {
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

            SetBattleMessage(allyUnits[index].unitName + "：生命 " + allyUnits[index].currentHp + " / " + allyUnits[index].maxHp);
        }

        private void SelectEnemy(int index)
        {
            if (index < 0 || index >= UnitCount || enemyUnits[index] == null || enemyUnits[index].defeated)
            {
                return;
            }

            selectedEnemyIndex = index;
            SetBattleMessage("已选中：" + enemyUnits[index].unitName + "。点击“开始战斗”攻击目标。");
            RefreshAllViews();
        }

        private BattleUnitState CreateAllyUnit(int index)
        {
            BattleUnitDto dto = FindBackendUnitBySlot(battleConfig == null ? null : battleConfig.allies, index);
            if (dto != null)
            {
                return CreateUnitFromDto(dto, true, index);
            }

            string unitName = ShouyouBackendBootstrap.GetBattleFormationSlotName(index);
            if (string.IsNullOrEmpty(unitName) || unitName == "空位")
            {
                return CreateEmptyAllyUnit(index);
            }

            if (unitName == "李清照")
            {
                return new BattleUnitState(unitName, true, 1200, 220, "char_liqingzhao");
            }

            if (unitName == "婉禾")
            {
                return new BattleUnitState(unitName, true, 1100, 160, "char_wanhe");
            }

            return new BattleUnitState(unitName, true, 900, 165, string.Empty);
        }

        private BattleUnitState CreateEnemyUnit(int index)
        {
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

            yield return client.GetBattleSkillAssets(delegate(BattleSkillAssetListResponse response)
            {
                if (response != null && response.icons != null)
                {
                    StartCoroutine(DownloadSkillIcons(response.icons));
                }
            }, delegate(string error)
            {
                Debug.LogWarning("[BattleDemo] ????????????????????" + error);
            });

            backendBattleConfigLoading = false;

            if (backendBattleConfigLoaded)
            {
                // ?????????????????? DB ???? demo-config ?????
                ResetDemoBattle();
            }
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

        private BattleUnitState CreateUnitFromDto(BattleUnitDto dto, bool isAlly, int index)
        {
            if (dto == null || string.IsNullOrEmpty(dto.name))
            {
                return isAlly ? CreateEmptyAllyUnit(index) : new BattleUnitState("敌" + (index + 1), false, 520 + index * 70, 105 + index * 18, string.Empty);
            }

            int hp = dto.hp > 0 ? dto.hp : (isAlly ? 900 : 520 + index * 70);
            int attack = dto.attack > 0 ? dto.attack : (isAlly ? 160 : 105 + index * 18);
            return new BattleUnitState(dto.name, isAlly, hp, attack, dto.portraitIconKey);
        }

        private BattleUnitState CreateEmptyAllyUnit(int index)
        {
            BattleUnitState emptyUnit = new BattleUnitState("空位 " + (index + 1), true, 1, 0, string.Empty);
            emptyUnit.currentHp = 0;
            emptyUnit.defeated = true;
            return emptyUnit;
        }

        private string GetFormationSummaryForBattle()
        {
            if (battleConfig != null && battleConfig.allies != null)
            {
                string summary = string.Empty;
                for (int i = 0; i < UnitCount; i++)
                {
                    BattleUnitDto dto = FindBackendUnitBySlot(battleConfig.allies, i);
                    summary += (i == 0 ? string.Empty : " / ") + (dto == null || string.IsNullOrEmpty(dto.name) ? "空位" : dto.name);
                }

                return summary;
            }

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
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] != null && !units[i].defeated)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryGetBattleActionContext(out BattleUnitState attacker, out BattleUnitState target)
        {
            BindRuntimeReferences();

            attacker = null;
            target = null;

            if (battleEnded)
            {
                SetBattleMessage("\u672c\u573a\u6218\u6597\u5df2\u7ecf\u7ed3\u7b97\uff0c\u8bf7\u8fd4\u56de\u4e3b\u7ebf\u6216\u91cd\u65b0\u8fdb\u5165\u3002");
                return false;
            }

            attacker = FindFirstAlive(allyUnits);
            target = GetSelectedOrFirstAliveEnemy();
            if (attacker == null || target == null)
            {
                Debug.LogError("[BattleDemo] \u6218\u6597\u72b6\u6001\u5f02\u5e38\uff1a\u627e\u4e0d\u5230\u53ef\u884c\u52a8\u5355\u4f4d\u6216\u53ef\u653b\u51fb\u76ee\u6807\u3002");
                return false;
            }

            return true;
        }

        private void CompletePlayerAction(string playerMessage)
        {
            SetBattleMessage(playerMessage);

            if (AllDefeated(enemyUnits))
            {
                battleEnded = true;
                RefreshAllViews();
                if (router != null)
                {
                    router.ResolveBattleVictory();
                }
                return;
            }

            string enemyMessage = ResolveEnemyCounterAttack();
            if (!string.IsNullOrEmpty(enemyMessage))
            {
                SetBattleMessage(playerMessage + "\n" + enemyMessage);
            }

            if (AllDefeated(allyUnits))
            {
                battleEnded = true;
                RefreshAllViews();
                if (router != null)
                {
                    router.ResolveBattleDefeat();
                }
                return;
            }

            AdvanceRoundClock();
            RefreshAllViews();
        }

        private string ResolveEnemyCounterAttack()
        {
            BattleUnitState enemyAttacker = FindFirstAlive(enemyUnits);
            BattleUnitState allyTarget = FindFirstAlive(allyUnits);
            if (enemyAttacker == null || allyTarget == null)
            {
                return string.Empty;
            }

            int enemyDamage = CalculateDamage(enemyAttacker, allyTarget);
            bool enemyKilledTarget = ApplyDamage(allyTarget, enemyDamage);
            ShowDamageText(allyTarget, enemyDamage);
            return BuildAttackMessage(enemyAttacker, allyTarget, enemyDamage, enemyKilledTarget);
        }

        private void AdvanceRoundClock()
        {
            roundIndex++;
            actionPoint = Mathf.Max(0, actionPoint - 1);
            if (actionPoint == 0)
            {
                actionPoint = actionPointMax;
            }
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

        private int CalculateDamage(BattleUnitState attacker, BattleUnitState target)
        {
            // 先用稳定的轻量公式：攻击力 - 少量防御修正。
            // 后续接角色技能时，这里会被技能倍率、属性克制、暴击等规则替换。
            int defenseOffset = target.isAlly ? 18 : 12;
            return Mathf.Max(60, attacker.attack - defenseOffset);
        }

        private bool ApplyDamage(BattleUnitState target, int damage)
        {
            bool wasAlive = !target.defeated;
            target.currentHp = Mathf.Max(0, target.currentHp - damage);
            target.defeated = target.currentHp <= 0;

            if (target.defeated && target == enemyUnits[selectedEnemyIndex])
            {
                BattleUnitState nextTarget = FindFirstAlive(enemyUnits);
                selectedEnemyIndex = nextTarget == null ? selectedEnemyIndex : System.Array.IndexOf(enemyUnits, nextTarget);
            }

            // ?? true ????????????????????????
            return wasAlive && target.defeated;
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

        private void ShowDamageText(BattleUnitState target, int damage)
        {
            ShowFloatingText(target, "-" + damage, target.isAlly ? new Color32(255, 92, 92, 255) : new Color32(255, 232, 128, 255));
        }

        private void ShowHealText(BattleUnitState target, int healAmount)
        {
            ShowFloatingText(target, "+" + healAmount, new Color32(110, 255, 160, 255));
        }

        private void ShowFloatingText(BattleUnitState target, string content, Color32 color)
        {
            BattleUnitView view = GetViewForUnit(target);
            if (view == null || view.damageText == null)
            {
                return;
            }

            view.damageSerial++;
            int serial = view.damageSerial;
            view.damageText.text = content;
            view.damageText.color = color;
            view.damageText.gameObject.SetActive(true);
            StartCoroutine(HideDamageTextLater(view, serial));
        }

        private IEnumerator HideDamageTextLater(BattleUnitView view, int serial)
        {
            yield return new WaitForSeconds(DamageTextVisibleSeconds);

            if (view != null && view.damageText != null && view.damageSerial == serial)
            {
                view.damageText.text = string.Empty;
                view.damageText.gameObject.SetActive(false);
            }
        }

        private BattleUnitView GetViewForUnit(BattleUnitState unit)
        {
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
            SetButtonLabel(startBattleButton, battleEnded ? "\u91cd\u65b0\u5f00\u59cb" : "\u5f00\u59cb\u6218\u6597");
            SetSkillButton(basicSkillButton, "basic", "\u666e\u653b");
            SetSkillButton(poetryStrikeButton, "poetry_strike", "\u8bcd\u610f\u8fde\u51fb");
            SetSkillButton(dreamAreaButton, "dream_area", "\u5982\u68a6\u4ee4");
            SetSkillButton(healSkillButton, "healing_verse", "\u7597\u6108");

            SetButtonInteractable(startBattleButton, true);
            SetButtonInteractable(autoBattleButton, !battleEnded);
            SetButtonInteractable(basicSkillButton, !battleEnded);
            SetButtonInteractable(poetryStrikeButton, !battleEnded);
            SetButtonInteractable(dreamAreaButton, !battleEnded);
            SetButtonInteractable(healSkillButton, !battleEnded);
        }

        private void RefreshAllViews()
        {
            SetText(roundTipText, "第 " + roundIndex + " 回合    我方行动    回合 PVE Demo");
            SetText(actionPointText, "行动点 " + actionPoint + " / " + actionPointMax);

            for (int i = 0; i < UnitCount; i++)
            {
                RefreshView(allyViews[i], allyUnits[i], false);
                RefreshView(enemyViews[i], enemyUnits[i], i == selectedEnemyIndex);
            }
        }

        private void RefreshView(BattleUnitView view, BattleUnitState unit, bool selected)
        {
            if (view == null || unit == null)
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

            SetText(view.nameText, unit.unitName + "\n" + unit.currentHp + "/" + unit.maxHp);

            if (view.selectedRing != null)
            {
                view.selectedRing.color = selected ? new Color32(255, 226, 145, 180) : new Color32(255, 226, 145, 0);
            }

            Color portraitColor = unit.defeated ? new Color(0.45f, 0.45f, 0.45f, 0.55f) : Color.white;
            if (view.portrait != null)
            {
                view.portrait.color = portraitColor;
            }
        }

        private string GetUnitDisplayText(BattleUnitState unit)
        {
            if (unit.defeated)
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
            SetText(roundTipText, "\u7b2c " + roundIndex + " \u56de\u5408    \u6211\u65b9\u884c\u52a8    \u56de\u5408 PVE Demo");
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
            public int currentHp;
            public bool defeated;

            public BattleUnitState(string unitName, bool isAlly, int maxHp, int attack, string portraitIconKey)
            {
                this.unitName = unitName;
                this.isAlly = isAlly;
                this.maxHp = maxHp;
                this.attack = attack;
                this.portraitIconKey = portraitIconKey;
                currentHp = maxHp;
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
            public int damageSerial;
        }
    }
}
