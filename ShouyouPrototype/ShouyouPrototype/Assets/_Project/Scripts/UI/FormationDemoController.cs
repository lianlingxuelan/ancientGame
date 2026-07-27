using Shouyou.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Shouyou.UI
{
    /// <summary>
    /// 第一版编队 Demo 控制器。
    /// 目标不是做最终编队系统，而是先打通“读后端阵容 → 点击槽位换人 → 保存阵容 → 战斗读取阵容”的主流程。
    /// </summary>
    public sealed class FormationDemoController : MonoBehaviour
    {
        private const int SlotCount = 6;

        private readonly Button[] slotButtons = new Button[SlotCount];
        private readonly Text[] slotLabels = new Text[SlotCount];
        private readonly string[] selectedCharacterIds = new string[SlotCount];

        private Text qiYunText;
        private Text hintText;
        private bool referencesBound;

        private void Awake()
        {
            BindRuntimeReferences();
            LoadFormationFromBackendCache();
        }

        private void OnEnable()
        {
            BindRuntimeReferences();
            LoadFormationFromBackendCache();
        }

        /// <summary>
        /// 从 ShouyouBackendBootstrap 的缓存里读取当前阵容。
        /// 后端数据由启动器统一拉取，这里只负责展示和本页编辑。
        /// </summary>
        public void LoadFormationFromBackendCache()
        {
            string[] ids = ShouyouBackendBootstrap.GetFormationCharacterIds();
            for (int i = 0; i < SlotCount; i++)
            {
                selectedCharacterIds[i] = ids != null && i < ids.Length ? ids[i] : null;
            }

            EnsureDemoCompanionSlot();
            RefreshView("已读取当前编队。点击槽位可切换角色，保存后写入本地后端。");
        }

        /// <summary>
        /// 保存当前 6 个槽位。
        /// 空位会以 null 发送给后端，后端负责校验 6 槽位和重复角色。
        /// </summary>
        public void SaveCurrentFormation()
        {
            RefreshView("正在保存编队，请稍候。");
            ShouyouBackendBootstrap.SaveFormationSlots(
                selectedCharacterIds,
                delegate(bool success, string message)
                {
                    RefreshView(success ? "保存成功：" + GetLocalFormationSummary() : "保存失败：" + message);
                });
        }

        public void SelectSlotOne() { CycleSlot(0); }
        public void SelectSlotTwo() { CycleSlot(1); }
        public void SelectSlotThree() { CycleSlot(2); }
        public void SelectSlotFour() { CycleSlot(3); }
        public void SelectSlotFive() { CycleSlot(4); }
        public void SelectSlotSix() { CycleSlot(5); }

        private void CycleSlot(int slotIndex)
        {
            CharacterDto[] candidates = ShouyouBackendBootstrap.GetFormationCandidateCharacters();
            if (candidates == null || candidates.Length == 0)
            {
                selectedCharacterIds[slotIndex] = null;
                RefreshView("当前没有已解锁角色，无法上阵。");
                return;
            }

            string currentId = selectedCharacterIds[slotIndex];
            int currentCandidateIndex = FindCandidateIndex(candidates, currentId);

            // 从“当前角色的下一个”开始找，跳过其它槽位已经使用的角色。
            // 如果一圈都找不到可用角色，就切回空位。
            for (int step = 1; step <= candidates.Length; step++)
            {
                int nextIndex = (currentCandidateIndex + step) % candidates.Length;
                string nextId = candidates[nextIndex].id;
                if (!IsUsedByOtherSlot(nextId, slotIndex))
                {
                    selectedCharacterIds[slotIndex] = nextId;
                    RefreshView("已将 " + GetSlotDisplayName(slotIndex) + " 放入 " + GetPositionLabel(slotIndex) + "。当前需要点击“保存编队”才会写入后端。");
                    return;
                }
            }

            selectedCharacterIds[slotIndex] = null;
            RefreshView(GetPositionLabel(slotIndex) + " 已切换为空位。当前需要点击“保存编队”才会写入后端。");
        }

        private void BindRuntimeReferences()
        {
            if (referencesBound)
            {
                return;
            }

            for (int i = 0; i < SlotCount; i++)
            {
                int index = i;
                slotButtons[i] = FindButton("FormationSlot_" + (i + 1));
                slotLabels[i] = FindLabel(slotButtons[i] == null ? null : slotButtons[i].transform);
                BindButton(slotButtons[i], delegate { CycleSlot(index); });
            }

            qiYunText = FindLabel("CurrentQiYun");
            hintText = FindLabel("BondHint");
            referencesBound = true;
        }

        private void RefreshView(string message)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                SetText(slotLabels[i], GetPositionLabel(i) + "\n" + GetSlotDisplayName(i));
            }

            int filledCount = CountFilledSlots();
            SetText(
                qiYunText,
                "当前气韵\n\n" + CalculateLocalFormationPower() +
                "\n\n已上阵：" + filledCount + " / 6"
            );

            SetText(
                hintText,
                "编队提示\n\n" +
                "点击槽位：切换已解锁角色\n" +
                "保存编队：写入本地后端\n\n" +
                message
            );
        }

        private string GetSlotDisplayName(int slotIndex)
        {
            string characterId = selectedCharacterIds[slotIndex];
            string displayName = ShouyouBackendBootstrap.GetCharacterNameById(characterId);
            CharacterDto character = FindCandidate(characterId);
            if (character != null && !character.unlocked)
            {
                return displayName + "（试用）";
            }

            return displayName;
        }

        public string GetLocalFormationSummary()
        {
            string[] labels = new string[SlotCount];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = GetSlotDisplayName(i);
            }

            return string.Join(" / ", labels);
        }

        private int CountFilledSlots()
        {
            int count = 0;
            for (int i = 0; i < selectedCharacterIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(selectedCharacterIds[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private int CalculateLocalFormationPower()
        {
            int power = 0;
            for (int i = 0; i < selectedCharacterIds.Length; i++)
            {
                if (string.IsNullOrEmpty(selectedCharacterIds[i]))
                {
                    continue;
                }

                power += selectedCharacterIds[i] == "li-qingzhao" ? 1200 : 900;
            }

            return power;
        }

        private void EnsureDemoCompanionSlot()
        {
            // 当前 Demo 需要至少两个角色参与编队测试。
            // 后端为了测试“角色锁定”把婉禾标成 locked，但这里先把她作为试用角色补到 2 号位。
            if (!string.IsNullOrEmpty(selectedCharacterIds[1]) || IsUsedByOtherSlot("wanhe", 1))
            {
                return;
            }

            CharacterDto[] candidates = ShouyouBackendBootstrap.GetFormationCandidateCharacters();
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].id == "wanhe")
                {
                    selectedCharacterIds[1] = "wanhe";
                    return;
                }
            }
        }

        private bool IsUsedByOtherSlot(string characterId, int currentSlotIndex)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return false;
            }

            for (int i = 0; i < selectedCharacterIds.Length; i++)
            {
                if (i != currentSlotIndex && selectedCharacterIds[i] == characterId)
                {
                    return true;
                }
            }

            return false;
        }

        private CharacterDto FindCandidate(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return null;
            }

            CharacterDto[] candidates = ShouyouBackendBootstrap.GetFormationCandidateCharacters();
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].id == characterId)
                {
                    return candidates[i];
                }
            }

            return null;
        }

        private static int FindCandidateIndex(CharacterDto[] candidates, string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return -1;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].id == characterId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetPositionLabel(int slotIndex)
        {
            return slotIndex < 3 ? "前排 " + (slotIndex + 1) : "后排 " + (slotIndex - 2);
        }

        private Button FindButton(string objectName)
        {
            Transform target = FindChildRecursive(transform, objectName);
            return target == null ? null : target.GetComponent<Button>();
        }

        private Text FindLabel(string objectName)
        {
            Transform target = FindChildRecursive(transform, objectName);
            return FindLabel(target);
        }

        private static Text FindLabel(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            Transform label = root.Find("Label");
            return label == null ? root.GetComponentInChildren<Text>(true) : label.GetComponent<Text>();
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            // 运行时覆盖旧绑定，避免 Clean 之前残留的 ShowFormationSlotX 弹窗逻辑继续触发。
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static Transform FindChildRecursive(Transform root, string childName)
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
    }
}
