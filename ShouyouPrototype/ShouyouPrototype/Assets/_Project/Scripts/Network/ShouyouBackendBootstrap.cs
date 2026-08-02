using System;
using System.Collections;
using UnityEngine;
using Shouyou.Data;

namespace Shouyou.Network
{
    // 后端联调启动器：游戏运行后自动拉取一次本地服务器数据。
    // 不需要手动拖到场景里，下面的 RuntimeInitializeOnLoadMethod 会自动创建它。
    public sealed class ShouyouBackendBootstrap : MonoBehaviour
    {
        private const string RuntimeObjectName = "ShouyouBackendRuntime";

        [SerializeField] private string baseUrl = "http://127.0.0.1:5188";
        [SerializeField] private string playerId = "demo-player";

        private ShouyouApiClient apiClient;
        private PlayerProfileResponse playerProfile;
        private CharacterListResponse characters;
        private ChapterListResponse chapters;
        private FormationResponse formation;
        private SaveProgressResponse saveProgress;
        private StageProgressResponse stageProgress;

        public static ShouyouBackendBootstrap Instance { get; private set; }

        public PlayerProfileResponse PlayerProfile => playerProfile;
        public CharacterListResponse Characters => characters;
        public ChapterListResponse Chapters => chapters;
        public FormationResponse Formation => formation;
        public SaveProgressResponse SaveProgress => saveProgress;
        public StageProgressResponse StageProgress => stageProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeObject()
        {
            EnsureRuntimeObject();
        }

        private static ShouyouBackendBootstrap EnsureRuntimeObject()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject existingObject = GameObject.Find(RuntimeObjectName);
            if (existingObject != null)
            {
                Instance = existingObject.GetComponent<ShouyouBackendBootstrap>();
                if (Instance != null)
                {
                    return Instance;
                }
            }

            // 兜底创建后端联调对象。
            // 有些按钮可能会早于 RuntimeInitializeOnLoadMethod 调用静态方法，
            // 所以这里不能只报 warning，必须主动补齐运行时对象。
            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);
            return runtimeObject.AddComponent<ShouyouBackendBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            apiClient = new ShouyouApiClient(baseUrl, playerId);
        }

        private void Start()
        {
            StartCoroutine(LoadInitialData());
        }

        public static void SaveCurrentDemoFormation()
        {
            ShouyouBackendBootstrap bootstrap = EnsureRuntimeObject();
            if (bootstrap == null)
            {
                Debug.LogWarning("后端联调对象还没有创建，暂时无法保存编队。");
                return;
            }

            bootstrap.StartCoroutine(bootstrap.SaveDemoFormation());
        }

        public static void SaveFormationSlots(string[] characterIds)
        {
            SaveFormationSlots(characterIds, null);
        }

        public static void SaveFormationSlots(string[] characterIds, Action<bool, string> onCompleted)
        {
            ShouyouBackendBootstrap bootstrap = EnsureRuntimeObject();
            if (bootstrap == null)
            {
                const string message = "后端联调对象还没有创建，暂时无法保存真实编队。";
                Debug.LogWarning(message);
                onCompleted?.Invoke(false, message);
                return;
            }

            bootstrap.StartCoroutine(bootstrap.SaveFormationSlotsRoutine(characterIds, onCompleted));
        }

        public static void CompleteMainlineStage(int stageId)
        {
            ShouyouBackendBootstrap bootstrap = EnsureRuntimeObject();
            if (bootstrap == null)
            {
                Debug.LogWarning("后端联调对象还没有创建，主线通关结果暂时只保存到本地。");
                return;
            }

            bootstrap.StartCoroutine(bootstrap.CompleteMainlineStageRoutine(stageId));
        }

        public static bool HasBattleReadyFormation()
        {
            FormationResponse currentFormation = Instance != null ? Instance.formation : null;
            if (currentFormation == null || currentFormation.slots == null)
            {
                // 后端还没返回时使用 Demo 默认队伍，避免本地服务器未启动时阻塞战斗测试。
                return true;
            }

            for (int i = 0; i < currentFormation.slots.Length; i++)
            {
                if (currentFormation.slots[i] != null && !string.IsNullOrEmpty(currentFormation.slots[i].characterId))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetFormationSummary()
        {
            FormationResponse currentFormation = Instance != null ? Instance.formation : null;
            if (currentFormation == null || currentFormation.slots == null)
            {
                return "李清照 / 空位 / 空位 / 空位 / 空位 / 空位（本地默认队伍）";
            }

            string[] labels = new string[6];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = "空位";
            }

            for (int i = 0; i < currentFormation.slots.Length && i < labels.Length; i++)
            {
                FormationSlotDto slot = currentFormation.slots[i];
                if (slot != null && !string.IsNullOrEmpty(slot.characterId))
                {
                    labels[i] = string.IsNullOrEmpty(slot.characterName) ? slot.characterId : slot.characterName;
                }
            }

            return string.Join(" / ", labels);
        }

        public static int GetFormationPower()
        {
            FormationResponse currentFormation = Instance != null ? Instance.formation : null;
            if (currentFormation == null || currentFormation.slots == null)
            {
                return 1200;
            }

            int power = 0;
            for (int i = 0; i < currentFormation.slots.Length; i++)
            {
                FormationSlotDto slot = currentFormation.slots[i];
                if (slot != null && !string.IsNullOrEmpty(slot.characterId))
                {
                    power += slot.characterId == "li-qingzhao" ? 1200 : 900;
                }
            }

            return power;
        }

        public static CharacterDto[] GetUnlockedCharacters()
        {
            CharacterListResponse currentCharacters = Instance != null ? Instance.characters : null;
            if (currentCharacters == null || currentCharacters.characters == null)
            {
                return new[]
                {
                    CreateFallbackCharacter("li-qingzhao", "李清照", "如梦令"),
                    CreateFallbackCharacter("wanhe", "婉禾", "协奏")
                };
            }

            int count = 0;
            for (int i = 0; i < currentCharacters.characters.Length; i++)
            {
                CharacterDto character = currentCharacters.characters[i];
                if (character != null && character.unlocked)
                {
                    count++;
                }
            }

            CharacterDto[] unlocked = new CharacterDto[count];
            int writeIndex = 0;
            for (int i = 0; i < currentCharacters.characters.Length; i++)
            {
                CharacterDto character = currentCharacters.characters[i];
                if (character != null && character.unlocked)
                {
                    unlocked[writeIndex] = character;
                    writeIndex++;
                }
            }

            return unlocked;
        }

        public static CharacterDto[] GetFormationCandidateCharacters()
        {
            CharacterListResponse currentCharacters = Instance != null ? Instance.characters : null;
            if (currentCharacters == null || currentCharacters.characters == null)
            {
                return new[]
                {
                    CreateFallbackCharacter("li-qingzhao", "李清照", "如梦令"),
                    CreateFallbackCharacter("wanhe", "婉禾", "协奏")
                };
            }

            // 编队 Demo 阶段允许使用所有已配置角色。
            // 原因：后端目前把婉禾标成 locked，用于测试角色解锁状态；
            // 但前端主流程需要她作为第二名试用角色参与编队和战斗。
            return currentCharacters.characters;
        }

        public static string[] GetFormationCharacterIds()
        {
            string[] ids = new string[6];
            FormationResponse currentFormation = Instance != null ? Instance.formation : null;
            if (currentFormation == null || currentFormation.slots == null)
            {
                ids[0] = "li-qingzhao";
                return ids;
            }

            for (int i = 0; i < currentFormation.slots.Length && i < ids.Length; i++)
            {
                FormationSlotDto slot = currentFormation.slots[i];
                ids[i] = slot == null ? null : slot.characterId;
            }

            return ids;
        }

        public static string GetCharacterNameById(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return "空位";
            }

            CharacterDto[] candidates = GetFormationCandidateCharacters();
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].id == characterId)
                {
                    return string.IsNullOrEmpty(candidates[i].name) ? characterId : candidates[i].name;
                }
            }

            return characterId;
        }

        public static string GetBattleFormationSlotName(int zeroBasedIndex)
        {
            string[] ids = GetFormationCharacterIds();
            if (zeroBasedIndex < 0 || zeroBasedIndex >= ids.Length)
            {
                return "空位";
            }

            return GetCharacterNameById(ids[zeroBasedIndex]);
        }

        public static string GetDebugSummary()
        {
            ShouyouBackendBootstrap bootstrap = EnsureRuntimeObject();
            if (bootstrap == null)
            {
                return "后端运行时对象：未创建\n连接状态：未连接\n说明：Unity 还没有创建 ShouyouBackendRuntime。";
            }

            string playerName = bootstrap.playerProfile != null ? bootstrap.playerProfile.name : "未读取";
            string currentStage = bootstrap.saveProgress != null ? bootstrap.saveProgress.currentStageId : "未读取";
            string highestCleared = bootstrap.stageProgress != null
                ? bootstrap.stageProgress.highestClearedStageId.ToString()
                : "未读取";
            int characterCount = bootstrap.characters != null && bootstrap.characters.characters != null
                ? bootstrap.characters.characters.Length
                : 0;
            int slotCount = bootstrap.formation != null && bootstrap.formation.slots != null
                ? bootstrap.formation.slots.Length
                : 0;

            return
                "后端运行时对象：已创建" +
                "\n接口地址：http://127.0.0.1:5188" +
                "\n玩家：" + playerName +
                "\n角色数量：" + characterCount +
                "\n当前后端关卡：" + currentStage +
                "\n后端最高通关：" + highestCleared +
                "\n编队槽位：" + slotCount +
                "\n当前编队：" + GetFormationSummary() +
                "\n队伍战力：" + GetFormationPower();
        }

        private IEnumerator LoadInitialData()
        {
            Debug.Log("开始连接本地后端：" + baseUrl);

            yield return apiClient.GetHealth(
                data => Debug.Log("后端健康检查成功：" + data.service),
                error => Debug.LogWarning("后端健康检查失败，请确认 ShouyouServer 已启动。\n" + error));

            yield return apiClient.GetPlayerProfile(
                data => playerProfile = data,
                error => Debug.LogWarning("玩家资料读取失败：\n" + error));

            yield return apiClient.GetCharacters(
                data => characters = data,
                error => Debug.LogWarning("角色列表读取失败：\n" + error));

            yield return apiClient.GetChapters(
                data => chapters = data,
                error => Debug.LogWarning("章节列表读取失败：\n" + error));

            yield return apiClient.GetFormation(
                data => formation = data,
                error => Debug.LogWarning("编队读取失败：\n" + error));

            yield return apiClient.GetSaveProgress(
                data => saveProgress = data,
                error => Debug.LogWarning("存档读取失败：\n" + error));

            yield return apiClient.GetStageProgress(
                data =>
                {
                    stageProgress = data;
                    LevelProgressManager.Instance.SyncHighestClearedStage(data.highestClearedStageId);
                },
                error => Debug.LogWarning("主线进度读取失败，将使用本地 Demo 进度：\n" + error));

            ApplyMainlineStageConfig();
            LogLoadedSummary();
        }

        // 将当前后端第一章的基础字段覆盖到前端目录。
        // 战力、剧情目标和奖励尚未由后台配置，因此继续沿用本地表，保证展示完整。
        private void ApplyMainlineStageConfig()
        {
            if (chapters == null || chapters.chapters == null || chapters.chapters.Length == 0)
            {
                Debug.LogWarning("后端未返回章节配置，主线继续使用本地兜底表。");
                return;
            }

            ChapterDto mainlineChapter = FindFirstChapter();
            if (mainlineChapter == null || mainlineChapter.stages == null || mainlineChapter.stages.Length == 0)
            {
                Debug.LogWarning("后端第一章没有可用关卡，主线继续使用本地兜底表。");
                return;
            }

            MainlineStageInfo[] remoteStages = new MainlineStageInfo[mainlineChapter.stages.Length];
            for (int i = 0; i < mainlineChapter.stages.Length; i++)
            {
                StageDto remoteStage = mainlineChapter.stages[i];
                int stageId = ParseStageNumber(remoteStage, i + 1);
                MainlineStageInfo fallback = MainlineStageCatalog.GetLocalFallback(stageId);

                bool unlocked = remoteStage.defaultUnlocked;
                StageProgressDto progress = FindStageProgress(remoteStage.id);
                if (progress != null)
                {
                    unlocked = progress.unlocked;
                }

                remoteStages[i] = new MainlineStageInfo(
                    stageId,
                    BuildStageDisplayTitle(remoteStage, fallback),
                    Mathf.Max(1, remoteStage.recommendedLevel),
                    fallback.recommendPower,
                    fallback.objective,
                    fallback.rewardPreview,
                    unlocked);
            }

            MainlineStageCatalog.ApplyRemoteStages(remoteStages);
            Debug.Log("主线关卡配置已套用后端章节数据：" + mainlineChapter.id + "，关卡数=" + remoteStages.Length);
        }

        // 当前 Demo 只读取排序最靠前的章节，后续多章节时可改成由主线入口传入章节 id。
        private ChapterDto FindFirstChapter()
        {
            ChapterDto firstChapter = null;
            for (int i = 0; i < chapters.chapters.Length; i++)
            {
                ChapterDto chapter = chapters.chapters[i];
                if (chapter == null)
                {
                    continue;
                }

                if (firstChapter == null || chapter.sortOrder < firstChapter.sortOrder)
                {
                    firstChapter = chapter;
                }
            }

            return firstChapter;
        }

        // 从“1-2”这类后端关卡 id 中读取关卡序号。
        // 解析失败时退回列表顺序，避免脏数据阻断主线页面。
        private static int ParseStageNumber(StageDto stage, int fallbackIndex)
        {
            if (stage != null && !string.IsNullOrEmpty(stage.id))
            {
                string[] parts = stage.id.Split('-');
                int parsed;
                if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out parsed) && parsed > 0)
                {
                    return parsed;
                }
            }

            return fallbackIndex;
        }

        // 后端 title 不带“1-1”前缀时，由前端统一补齐展示格式。
        private static string BuildStageDisplayTitle(StageDto stage, MainlineStageInfo fallback)
        {
            if (stage == null || string.IsNullOrEmpty(stage.title))
            {
                return fallback.title;
            }

            if (!string.IsNullOrEmpty(stage.id) && !stage.title.StartsWith(stage.id))
            {
                return stage.id + " " + stage.title;
            }

            return stage.title;
        }

        // 进度接口晚于章节接口返回时，用它修正默认解锁状态。
        private StageProgressDto FindStageProgress(string stageId)
        {
            if (stageProgress == null || stageProgress.stages == null || string.IsNullOrEmpty(stageId))
            {
                return null;
            }

            for (int i = 0; i < stageProgress.stages.Length; i++)
            {
                StageProgressDto progress = stageProgress.stages[i];
                if (progress != null && progress.id == stageId)
                {
                    return progress;
                }
            }

            return null;
        }

        private IEnumerator SaveDemoFormation()
        {
            yield return apiClient.SaveDemoFormation(
                data =>
                {
                    formation = data;
                    Debug.Log("编队已保存到后端：1号位李清照，2号位婉禾。");
                },
                error => Debug.LogWarning("编队保存失败，请确认本地后端仍在运行。\n" + error));
        }

        private IEnumerator SaveFormationSlotsRoutine(string[] characterIds, Action<bool, string> onCompleted)
        {
            yield return apiClient.SaveFormation(
                characterIds,
                data =>
                {
                    formation = data;
                    string message = "编队已保存到后端：" + GetFormationSummary();
                    Debug.Log(message);
                    onCompleted?.Invoke(true, message);
                },
                error =>
                {
                    string message = "真实编队保存失败，请确认本地后端仍在运行。\n" + error;
                    Debug.LogWarning(message);
                    onCompleted?.Invoke(false, message);
                });
        }

        private IEnumerator CompleteMainlineStageRoutine(int stageId)
        {
            string backendStageId = "1-" + Mathf.Clamp(stageId, 1, LevelProgressManager.MaxMainlineStageId);

            yield return apiClient.CompleteStage(
                backendStageId,
                data =>
                {
                    stageProgress = data;
                    LevelProgressManager.Instance.SyncHighestClearedStage(data.highestClearedStageId);
                    Debug.Log("主线通关已同步到后端：" + backendStageId);
                },
                error => Debug.LogWarning("主线通关同步后端失败，已保留本地进度：\n" + error));
        }

        private void LogLoadedSummary()
        {
            string playerName = playerProfile != null ? playerProfile.name : "未读取";
            int characterCount = characters != null && characters.characters != null ? characters.characters.Length : 0;
            int chapterCount = chapters != null && chapters.chapters != null ? chapters.chapters.Length : 0;
            int slotCount = formation != null && formation.slots != null ? formation.slots.Length : 0;
            string currentStage = saveProgress != null ? saveProgress.currentStageId : "未读取";
            int highestClearedStage = stageProgress != null ? stageProgress.highestClearedStageId : 0;

            Debug.Log(
                "后端数据读取完成：玩家=" + playerName +
                "，角色数=" + characterCount +
                "，章节数=" + chapterCount +
                "，编队槽位=" + slotCount +
                "，当前关卡=" + currentStage +
                "，最高通关=" + highestClearedStage +
                "，主线配置=" + (MainlineStageCatalog.IsUsingRemoteConfig ? "后端" : "本地兜底"));
        }

        private static CharacterDto CreateFallbackCharacter(string id, string name, string wordIntent)
        {
            return new CharacterDto
            {
                id = id,
                name = name,
                rarity = "SR",
                role = "Demo",
                wordIntent = wordIntent,
                description = "本地默认角色",
                level = 1,
                bondLevel = 1,
                unlocked = true
            };
        }
    }
}
