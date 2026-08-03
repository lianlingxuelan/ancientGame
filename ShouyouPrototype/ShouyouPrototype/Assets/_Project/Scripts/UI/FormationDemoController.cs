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

        // 当前正在编辑的槽位。-1 表示还没有选中槽位，候选角色按钮此时只给出引导。
        private int selectedSlotIndex = -1;
        private Text qiYunText;
        private Text hintText;
        private Text candidateOneLabel;
        private Text candidateTwoLabel;
        private Button candidateOneButton;
        private Button candidateTwoButton;
        private Button clearSlotButton;
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

            // 编队缓存是唯一的阵容来源：不在前端自动补位，避免玩家清空槽位后又被旧 Demo 逻辑写回角色。
            RefreshView("已读取当前编队。先点击槽位，再选择角色；保存后写入本地后端。");
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
                    if (success)
                    {
                        // 保存接口返回的数据已写入启动器缓存；重新读取一次，避免本地草稿和后端权威状态分叉。
                        LoadFormationFromBackendCache();
                        RefreshView("保存成功：" + GetLocalFormationSummary());
                        return;
                    }

                    // 保存失败时保留当前草稿，方便玩家检查本地后端后再次保存。
                    RefreshView("保存失败：" + message);
                });
        }

        public void SelectSlotOne() { SelectSlot(0); }
        public void SelectSlotTwo() { SelectSlot(1); }
        public void SelectSlotThree() { SelectSlot(2); }
        public void SelectSlotFour() { SelectSlot(3); }
        public void SelectSlotFive() { SelectSlot(4); }
        public void SelectSlotSix() { SelectSlot(5); }

        /// <summary>
        /// 选择候选列表第一个角色。后续替换为滚动角色列表后，此入口仍可作为列表项回调模式。
        /// </summary>
        public void SelectCandidateOne()
        {
            SelectCandidateAt(0);
        }

        /// <summary>
        /// 选择候选列表第二个角色。
        /// </summary>
        public void SelectCandidateTwo()
        {
            SelectCandidateAt(1);
        }

        /// <summary>
        /// 清空当前选中的槽位。空位会在保存时以 null 提交给后端。
        /// </summary>
        public void ClearSelectedSlot()
        {
            if (!HasSelectedSlot())
            {
                RefreshView("请先点击一个编队位置，再执行清空。");
                return;
            }

            selectedCharacterIds[selectedSlotIndex] = null;
            RefreshView(GetPositionLabel(selectedSlotIndex) + " 已清空。点击保存编队后才会写入后端。");
        }

        /// <summary>
        /// “编辑阵容”按钮的入口。编辑不再弹出旧说明页，而是直接提示玩家按槽位与角色的顺序操作。
        /// </summary>
        public void BeginFormationEditing()
        {
            if (HasSelectedSlot())
            {
                RefreshView("正在编辑 " + GetPositionLabel(selectedSlotIndex) + "。请点击左侧角色，或清空当前槽位。");
                return;
            }

            RefreshView("编辑阵容：请先点击一个前排或后排槽位，再从左侧选择角色。");
        }

        private void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
            {
                return;
            }

            selectedSlotIndex = slotIndex;
            RefreshView("已选择 " + GetPositionLabel(slotIndex) + "。请在左侧选择角色，或清空当前槽位。");
        }

        private void SelectCandidateAt(int candidateIndex)
        {
            CharacterDto[] candidates = ShouyouBackendBootstrap.GetFormationCandidateCharacters();
            if (candidates == null || candidateIndex < 0 || candidateIndex >= candidates.Length || candidates[candidateIndex] == null)
            {
                RefreshView("该候选角色暂不可用，请等待角色数据加载完成。");
                return;
            }

            AssignCandidateToSelectedSlot(candidates[candidateIndex].id);
        }

        private void AssignCandidateToSelectedSlot(string characterId)
        {
            if (!HasSelectedSlot())
            {
                RefreshView("请先点击一个编队位置，再选择角色。");
                return;
            }

            if (string.IsNullOrEmpty(characterId))
            {
                RefreshView("该角色数据缺少编号，暂时无法上阵。");
                return;
            }

            // 同一角色不能同时出现在两个槽位。若角色已在其它位置，自动与当前槽位交换，
            // 这样玩家能直接调整前后排，而不会产生重复数据或额外的“先下阵”步骤。
            if (TryMoveExistingCharacter(characterId, selectedSlotIndex))
            {
                RefreshView("已调整 " + GetSlotDisplayName(selectedSlotIndex) + " 至 " + GetPositionLabel(selectedSlotIndex) + "。点击保存编队后生效。");
                return;
            }

            selectedCharacterIds[selectedSlotIndex] = characterId;
            RefreshView("已将 " + GetSlotDisplayName(selectedSlotIndex) + " 放入 " + GetPositionLabel(selectedSlotIndex) + "。点击保存编队后生效。");
        }

        private bool TryMoveExistingCharacter(string characterId, int targetSlotIndex)
        {
            for (int i = 0; i < selectedCharacterIds.Length; i++)
            {
                if (i == targetSlotIndex || selectedCharacterIds[i] != characterId)
                {
                    continue;
                }

                string replacedCharacterId = selectedCharacterIds[targetSlotIndex];
                selectedCharacterIds[i] = replacedCharacterId;
                selectedCharacterIds[targetSlotIndex] = characterId;
                return true;
            }

            return false;
        }

        private bool HasSelectedSlot()
        {
            return selectedSlotIndex >= 0 && selectedSlotIndex < SlotCount;
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
                BindButton(slotButtons[i], delegate { SelectSlot(index); });
            }

            qiYunText = FindLabel("CurrentQiYun");
            hintText = FindLabel("BondHint");
            candidateOneButton = FindButton("FormationCandidate_1");
            candidateTwoButton = FindButton("FormationCandidate_2");
            clearSlotButton = FindButton("ClearFormationSlotButton");
            candidateOneLabel = FindLabel(candidateOneButton == null ? null : candidateOneButton.transform);
            candidateTwoLabel = FindLabel(candidateTwoButton == null ? null : candidateTwoButton.transform);
            BindButton(candidateOneButton, SelectCandidateOne);
            BindButton(candidateTwoButton, SelectCandidateTwo);
            BindButton(clearSlotButton, ClearSelectedSlot);
            referencesBound = true;
        }

        private void RefreshView(string message)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                string selectedPrefix = i == selectedSlotIndex ? "【已选】" : string.Empty;
                SetText(slotLabels[i], selectedPrefix + GetPositionLabel(i) + "\n" + GetSlotDisplayName(i));
            }

            RefreshCandidateLabels();

            int filledCount = CountFilledSlots();
            SetText(
                qiYunText,
                "当前气韵\n\n" + CalculateLocalFormationPower() +
                "\n\n已上阵：" + filledCount + " / 6"
            );

            SetText(
                hintText,
                "编队提示\n\n" +
                "先点槽位，再点左侧角色\n" +
                "重复角色会自动交换位置\n" +
                "保存编队：写入本地后端\n\n" +
                message
            );
        }

        private void RefreshCandidateLabels()
        {
            CharacterDto[] candidates = ShouyouBackendBootstrap.GetFormationCandidateCharacters();
            SetText(candidateOneLabel, BuildCandidateLabel(candidates, 0));
            SetText(candidateTwoLabel, BuildCandidateLabel(candidates, 1));

            if (candidateOneButton != null) candidateOneButton.interactable = candidates != null && candidates.Length > 0 && candidates[0] != null;
            if (candidateTwoButton != null) candidateTwoButton.interactable = candidates != null && candidates.Length > 1 && candidates[1] != null;
            if (clearSlotButton != null) clearSlotButton.interactable = HasSelectedSlot();
        }

        private static string BuildCandidateLabel(CharacterDto[] candidates, int index)
        {
            if (candidates == null || index < 0 || index >= candidates.Length || candidates[index] == null)
            {
                return "角色位\n待解锁";
            }

            CharacterDto candidate = candidates[index];
            string status = candidate.unlocked ? "可上阵" : "试用";
            return candidate.name + "\n" + candidate.wordIntent + " · " + status;
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
