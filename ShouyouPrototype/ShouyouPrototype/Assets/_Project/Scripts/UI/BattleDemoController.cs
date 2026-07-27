using UnityEngine;
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
        private const int ActionPointMax = 3;
        private const float HpBarMaxWidth = 86f;

        private readonly BattleUnitState[] allyUnits = new BattleUnitState[UnitCount];
        private readonly BattleUnitState[] enemyUnits = new BattleUnitState[UnitCount];
        private readonly BattleUnitView[] allyViews = new BattleUnitView[UnitCount];
        private readonly BattleUnitView[] enemyViews = new BattleUnitView[UnitCount];

        private HomePageRouter router;
        private Text roundTipText;
        private Text actionPointText;
        private Text battleMessageText;
        private Button startBattleButton;
        private Button autoBattleButton;
        private Button retreatButton;

        private int selectedEnemyIndex;
        private int roundIndex = 1;
        private int actionPoint = ActionPointMax;
        private bool battleEnded;
        private bool referencesBound;

        private void Awake()
        {
            BindRuntimeReferences();
            ResetDemoBattle();
        }

        private void OnEnable()
        {
            BindRuntimeReferences();
            ResetDemoBattle();
        }

        /// <summary>
        /// 每次进入战斗页时重置 Demo 战斗。
        /// 当前没有接正式战报，所以先保证每次进入都从完整血量开始。
        /// </summary>
        public void ResetDemoBattle()
        {
            selectedEnemyIndex = 0;
            roundIndex = 1;
            actionPoint = ActionPointMax;
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
        /// 主按钮：执行一次我方攻击，然后敌方自动反击一次。
        /// </summary>
        public void PerformPlayerAttack()
        {
            BindRuntimeReferences();

            if (battleEnded)
            {
                SetBattleMessage("本场战斗已经结算，请返回主线或重新进入。");
                return;
            }

            BattleUnitState attacker = FindFirstAlive(allyUnits);
            BattleUnitState target = GetSelectedOrFirstAliveEnemy();
            if (attacker == null || target == null)
            {
                Debug.LogError("[BattleDemo] 战斗状态异常：找不到可行动单位或可攻击目标。");
                return;
            }

            int playerDamage = CalculateDamage(attacker, target);
            ApplyDamage(target, playerDamage);
            SetBattleMessage(attacker.unitName + " 对 " + target.unitName + " 造成 " + playerDamage + " 点伤害。");

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

            BattleUnitState enemyAttacker = FindFirstAlive(enemyUnits);
            BattleUnitState allyTarget = FindFirstAlive(allyUnits);
            if (enemyAttacker != null && allyTarget != null)
            {
                int enemyDamage = CalculateDamage(enemyAttacker, allyTarget);
                ApplyDamage(allyTarget, enemyDamage);
                SetBattleMessage(
                    attacker.unitName + " 造成 " + playerDamage + " 点伤害；" +
                    enemyAttacker.unitName + " 反击 " + allyTarget.unitName + "，造成 " + enemyDamage + " 点伤害。"
                );
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

            roundIndex++;
            actionPoint = Mathf.Max(0, actionPoint - 1);
            if (actionPoint == 0)
            {
                actionPoint = ActionPointMax;
            }

            RefreshAllViews();
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

            BindButton(startBattleButton, PerformPlayerAttack);
            BindButton(autoBattleButton, PerformAutoAttacks);
            BindButton(retreatButton, RetreatBattle);

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
            string unitName = ShouyouBackendBootstrap.GetBattleFormationSlotName(index);
            if (string.IsNullOrEmpty(unitName) || unitName == "空位")
            {
                // 空槽位在战斗里显示为灰色，不参与攻击和承伤。
                BattleUnitState emptyUnit = new BattleUnitState("空位 " + (index + 1), true, 1, 0);
                emptyUnit.currentHp = 0;
                emptyUnit.defeated = true;
                return emptyUnit;
            }

            if (unitName == "李清照")
            {
                return new BattleUnitState(unitName, true, 1200, 220);
            }

            return new BattleUnitState(unitName, true, 900, 165);
        }

        private BattleUnitState CreateEnemyUnit(int index)
        {
            string[] names = { "敌一", "敌二", "敌三", "敌四", "敌五", "敌六" };
            return new BattleUnitState(names[index], false, 520 + index * 70, 105 + index * 18);
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

        private int CalculateDamage(BattleUnitState attacker, BattleUnitState target)
        {
            // 先用稳定的轻量公式：攻击力 - 少量防御修正。
            // 后续接角色技能时，这里会被技能倍率、属性克制、暴击等规则替换。
            int defenseOffset = target.isAlly ? 18 : 12;
            return Mathf.Max(60, attacker.attack - defenseOffset);
        }

        private void ApplyDamage(BattleUnitState target, int damage)
        {
            target.currentHp = Mathf.Max(0, target.currentHp - damage);
            target.defeated = target.currentHp <= 0;

            if (target.defeated && target == enemyUnits[selectedEnemyIndex])
            {
                BattleUnitState nextTarget = FindFirstAlive(enemyUnits);
                selectedEnemyIndex = nextTarget == null ? selectedEnemyIndex : System.Array.IndexOf(enemyUnits, nextTarget);
            }
        }

        private void RefreshAllViews()
        {
            SetText(roundTipText, "第 " + roundIndex + " 回合    我方行动    回合 PVE Demo");
            SetText(actionPointText, "行动点 " + actionPoint + " / " + ActionPointMax);

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

            return new BattleUnitView
            {
                button = button,
                hpBar = hpBar == null ? null : hpBar.GetComponent<RectTransform>(),
                selectedRing = selectedRing == null ? null : selectedRing.GetComponent<Image>(),
                portrait = portrait == null ? null : portrait.GetComponent<Image>(),
                nameText = nameText
            };
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

            // 运行时直接替换点击事件。
            // 原因：当前场景里可能残留编辑器持久化绑定，例如“开始战斗”旧逻辑会直接弹胜利。
            // 这里清掉后再绑定，可以避免一次点击同时触发旧逻辑和新战斗逻辑。
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }

        private void SetBattleMessage(string message)
        {
            SetText(battleMessageText, message);
            SetText(roundTipText, "第 " + roundIndex + " 回合    我方行动    " + message);
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
            public int currentHp;
            public bool defeated;

            public BattleUnitState(string unitName, bool isAlly, int maxHp, int attack)
            {
                this.unitName = unitName;
                this.isAlly = isAlly;
                this.maxHp = maxHp;
                this.attack = attack;
                currentHp = maxHp;
            }
        }

        private sealed class BattleUnitView
        {
            public Button button;
            public RectTransform hpBar;
            public Image selectedRing;
            public Image portrait;
            public Text nameText;
        }
    }
}
