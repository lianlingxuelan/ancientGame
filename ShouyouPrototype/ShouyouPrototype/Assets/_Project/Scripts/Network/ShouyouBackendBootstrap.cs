using System.Collections;
using UnityEngine;

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

        public static ShouyouBackendBootstrap Instance { get; private set; }

        public PlayerProfileResponse PlayerProfile => playerProfile;
        public CharacterListResponse Characters => characters;
        public ChapterListResponse Chapters => chapters;
        public FormationResponse Formation => formation;
        public SaveProgressResponse SaveProgress => saveProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeObject()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<ShouyouBackendBootstrap>();
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
            if (Instance == null)
            {
                Debug.LogWarning("后端联调对象还没有创建，暂时无法保存编队。");
                return;
            }

            Instance.StartCoroutine(Instance.SaveDemoFormation());
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

            LogLoadedSummary();
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

        private void LogLoadedSummary()
        {
            string playerName = playerProfile != null ? playerProfile.name : "未读取";
            int characterCount = characters != null && characters.characters != null ? characters.characters.Length : 0;
            int chapterCount = chapters != null && chapters.chapters != null ? chapters.chapters.Length : 0;
            int slotCount = formation != null && formation.slots != null ? formation.slots.Length : 0;
            string currentStage = saveProgress != null ? saveProgress.currentStageId : "未读取";

            Debug.Log(
                "后端数据读取完成：玩家=" + playerName +
                "，角色数=" + characterCount +
                "，章节数=" + chapterCount +
                "，编队槽位=" + slotCount +
                "，当前关卡=" + currentStage);
        }
    }
}
