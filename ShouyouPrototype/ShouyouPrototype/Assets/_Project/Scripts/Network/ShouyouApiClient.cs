using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Shouyou.Network
{
    // Unity 和本地后台之间的 HTTP 通信入口。
    // 后续如果后台从本机迁到云服务器，只需要改 baseUrl，不需要重写 UI 逻辑。
    public sealed class ShouyouApiClient
    {
        private readonly string baseUrl;
        private readonly string playerId;

        public ShouyouApiClient(string baseUrl, string playerId)
        {
            this.baseUrl = TrimTrailingSlash(baseUrl);
            this.playerId = playerId;
        }

        public IEnumerator GetHealth(Action<HealthResponse> onSuccess, Action<string> onError)
        {
            yield return Get("/api/health", onSuccess, onError);
        }

        public IEnumerator GetPlayerProfile(Action<PlayerProfileResponse> onSuccess, Action<string> onError)
        {
            yield return Get("/api/v1/player/profile?playerId=" + Escape(playerId), onSuccess, onError);
        }

        public IEnumerator GetCharacters(Action<CharacterListResponse> onSuccess, Action<string> onError)
        {
            yield return Get("/api/v1/characters?playerId=" + Escape(playerId), onSuccess, onError);
        }

        public IEnumerator GetChapters(Action<ChapterListResponse> onSuccess, Action<string> onError)
        {
            yield return Get("/api/v1/chapters", onSuccess, onError);
        }

        public IEnumerator GetFormation(Action<FormationResponse> onSuccess, Action<string> onError)
        {
            yield return Get("/api/v1/formation?playerId=" + Escape(playerId), onSuccess, onError);
        }

        public IEnumerator GetSaveProgress(Action<SaveProgressResponse> onSuccess, Action<string> onError)
        {
            yield return Get("/api/v1/save?playerId=" + Escape(playerId), onSuccess, onError);
        }

        public IEnumerator GetStageProgress(Action<StageProgressResponse> onSuccess, Action<string> onError)
        {
            yield return Get("/api/v1/stages/progress?playerId=" + Escape(playerId), onSuccess, onError);
        }

        public IEnumerator SaveDemoFormation(Action<FormationResponse> onSuccess, Action<string> onError)
        {
            // 第一版先把李清照和婉禾放入 1、2 号位，后面接角色选择界面后再改成真实阵容。
            const string body = "{\"slots\":[\"li-qingzhao\",\"wanhe\",null,null,null,null]}";
            yield return Put("/api/v1/formation?playerId=" + Escape(playerId), body, onSuccess, onError);
        }

        public IEnumerator SaveFormation(string[] characterIds, Action<FormationResponse> onSuccess, Action<string> onError)
        {
            // 编队接口只关心 6 个槽位里的角色 id。
            // 空位用 null 传给后端，避免用“空位”这种展示文本污染真实数据。
            string[] normalizedSlots = new string[6];
            for (int i = 0; i < normalizedSlots.Length; i++)
            {
                normalizedSlots[i] = characterIds != null && i < characterIds.Length ? characterIds[i] : null;
            }

            string body =
                "{\"slots\":[" +
                ToJsonSlot(normalizedSlots[0]) + "," +
                ToJsonSlot(normalizedSlots[1]) + "," +
                ToJsonSlot(normalizedSlots[2]) + "," +
                ToJsonSlot(normalizedSlots[3]) + "," +
                ToJsonSlot(normalizedSlots[4]) + "," +
                ToJsonSlot(normalizedSlots[5]) +
                "]}";

            yield return Put("/api/v1/formation?playerId=" + Escape(playerId), body, onSuccess, onError);
        }

        public IEnumerator SaveStageProgress(string stageId, Action<SaveProgressResponse> onSuccess, Action<string> onError)
        {
            string body = "{\"currentChapterId\":\"chapter-1\",\"currentStageId\":\"" + EscapeJson(stageId) + "\",\"completedStageIds\":[\"1-1\"]}";
            yield return Put("/api/v1/save?playerId=" + Escape(playerId), body, onSuccess, onError);
        }

        public IEnumerator CompleteStage(string stageId, Action<StageCompleteResponse> onSuccess, Action<string> onError)
        {
            string body = "{\"stageId\":\"" + EscapeJson(stageId) + "\"}";
            yield return Put("/api/v1/stages/complete?playerId=" + Escape(playerId), body, onSuccess, onError);
        }

        private IEnumerator Get<T>(string path, Action<T> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + path))
            {
                yield return request.SendWebRequest();
                HandleResponse(request, onSuccess, onError);
            }
        }

        private IEnumerator Put<T>(string path, string jsonBody, Action<T> onSuccess, Action<string> onError)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(baseUrl + path, UnityWebRequest.kHttpVerbPUT))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

                yield return request.SendWebRequest();
                HandleResponse(request, onSuccess, onError);
            }
        }

        private static void HandleResponse<T>(UnityWebRequest request, Action<T> onSuccess, Action<string> onError)
        {
            // Unity 2020 起推荐使用 result 判断请求状态。
            // 旧字段 isNetworkError / isHttpError 会产生过时 API 警告，影响 Console 观察真实错误。
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
                return;
            }

            try
            {
                T data = JsonUtility.FromJson<T>(request.downloadHandler.text);
                onSuccess?.Invoke(data);
            }
            catch (Exception exception)
            {
                onError?.Invoke("JSON 解析失败：" + exception.Message + "\n" + request.downloadHandler.text);
            }
        }

        private static string TrimTrailingSlash(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.TrimEnd('/');
        }

        private static string Escape(string value)
        {
            return UnityWebRequest.EscapeURL(value ?? string.Empty);
        }

        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ToJsonSlot(string value)
        {
            return string.IsNullOrEmpty(value) ? "null" : "\"" + EscapeJson(value) + "\"";
        }
    }
}
