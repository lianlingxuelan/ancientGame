# ShouyouServer API 接口文档

> 本地测试服务器：`http://127.0.0.1:5188`
> 测试玩家 ID：`demo-player`
> 所有 Chapter ID：`chapter-1`
> 所有 Stage ID：`1-1` 到 `1-6`

---

## 1. 关卡进度

### GET /api/v1/stages/progress

获取玩家主线关卡进度。

```
GET /api/v1/stages/progress?playerId=demo-player
```

**响应示例：**
```json
{
  "playerId": "demo-player",
  "chapterId": "chapter-1",
  "highestClearedStageId": 1,
  "stages": [
    {
      "id": "1-1",
      "title": "明水入汴京",
      "cleared": true,
      "clearedAt": "2026-07-26T04:46:27.999Z",
      "unlocked": true
    },
    {
      "id": "1-2",
      "title": "雅集赴会",
      "cleared": false,
      "clearedAt": null,
      "unlocked": true
    }
  ]
}
```

**Unity 调用：**
```csharp
string url = $"{baseUrl}/api/v1/stages/progress?playerId={playerId}";
UnityWebRequest req = UnityWebRequest.Get(url);
yield return req.SendWebRequest();
var data = JsonUtility.FromJson<StageProgressResponse>(req.downloadHandler.text);
```

---

### PUT /api/v1/stages/complete

通关提交。返回 `progressAdvanced: true` 表示首次通关。

```
PUT /api/v1/stages/complete?playerId=demo-player
Content-Type: application/json

{"stageId": "1-1"}
```

**响应示例：**
```json
{
  "playerId": "demo-player",
  "chapterId": "chapter-1",
  "highestClearedStageId": 1,
  "stages": [...],
  "stageId": "1-1",
  "progressAdvanced": true
}
```

**Unity 调用：**
```csharp
string url = $"{baseUrl}/api/v1/stages/complete?playerId={playerId}";
string body = $"{{\"stageId\":\"{stageId}\"}}";
UnityWebRequest req = UnityWebRequest.Put(url, body);
req.SetRequestHeader("Content-Type", "application/json");
yield return req.SendWebRequest();
var data = JsonUtility.FromJson<CompleteStageResponse>(req.downloadHandler.text);
if (data.progressAdvanced) { /* 首次通关 */ }
```

---

## 2. 玩家资料

### GET /api/v1/player/profile

```
GET /api/v1/player/profile?playerId=demo-player
```

**响应：**
```json
{
  "id": "demo-player",
  "name": "玩家名",
  "level": 1,
  "coins": 9999,
  "jade": 120,
  "createdAt": "...",
  "updatedAt": "..."
}
```

---

## 3. 角色

### GET /api/v1/characters

```
GET /api/v1/characters?playerId=demo-player
```

**响应：**
```json
{
  "playerId": "demo-player",
  "characters": [
    {
      "id": "li-qingzhao",
      "name": "李清照",
      "rarity": "SSR",
      "role": "词意输出 / 群体辅助",
      "wordIntent": "如梦令",
      "description": "...",
      "level": 1,
      "bondLevel": 1,
      "unlocked": true
    },
    {
      "id": "wanhe",
      "name": "婉禾",
      "rarity": "SR",
      "role": "治疗 / 协奏辅助",
      "wordIntent": "灯下共稿",
      "description": "...",
      "level": 1,
      "bondLevel": 0,
      "unlocked": false
    }
  ]
}
```

---

## 4. 章节

### GET /api/v1/chapters

```
GET /api/v1/chapters
```

**响应：**
```json
{
  "chapters": [
    {
      "id": "chapter-1",
      "title": "卷一·汴京春深",
      "subtitle": "雅集初会",
      "sortOrder": 1,
      "stages": [
        {
          "id": "1-1",
          "title": "明水入汴京",
          "recommendedLevel": 1,
          "energyCost": 6,
          "defaultUnlocked": true,
          "sortOrder": 1
        }
      ]
    }
  ]
}
```

---

## 5. 编队

### GET /api/v1/formation

```
GET /api/v1/formation?playerId=demo-player
```

**响应：**
```json
{
  "playerId": "demo-player",
  "slots": [
    { "slotIndex": 1, "characterId": "li-qingzhao", "characterName": "李清照", "wordIntent": "如梦令" },
    { "slotIndex": 2, "characterId": null, "characterName": null, "wordIntent": null },
    { "slotIndex": 3, "characterId": null, "characterName": null, "wordIntent": null },
    { "slotIndex": 4, "characterId": null, "characterName": null, "wordIntent": null },
    { "slotIndex": 5, "characterId": null, "characterName": null, "wordIntent": null },
    { "slotIndex": 6, "characterId": null, "characterName": null, "wordIntent": null }
  ]
}
```

### PUT /api/v1/formation

保存编队。slots 必须正好 6 个，每个位置可以是角色 ID 或 null。同一角色不能重复上阵。

```
PUT /api/v1/formation?playerId=demo-player
Content-Type: application/json

{"slots": ["li-qingzhao", "wanhe", null, null, null, null]}
```

---

## 6. 存档

### GET /api/v1/save

```
GET /api/v1/save?playerId=demo-player
```

### PUT /api/v1/save

```
PUT /api/v1/save?playerId=demo-player
Content-Type: application/json

{
  "currentChapterId": "chapter-1",
  "currentStageId": "1-2",
  "completedStageIds": ["1-1"]
}
```

---

## 7. 健康检查

### GET /api/health

```
GET /api/health
```

**响应：**
```json
{ "ok": true, "service": "ShouyouServer", "version": "0.1.0", "time": "..." }
```

---

## Unity 客户端封装建议

```csharp
// ShouyouApiClient 扩展方法
public IEnumerator GetStageProgress(Action<StageProgressResponse> onSuccess)
{
    yield return Get("/api/v1/stages/progress?playerId=" + Escape(playerId), onSuccess, onError);
}

public IEnumerator CompleteStage(string stageId, Action<CompleteStageResponse> onSuccess)
{
    string body = "{\"stageId\":\"" + EscapeJson(stageId) + "\"}";
    yield return Put("/api/v1/stages/complete?playerId=" + Escape(playerId), body, onSuccess, onError);
}
```

**数据类：**
```csharp
[Serializable] public class StageProgressResponse {
    public string playerId;
    public string chapterId;
    public int highestClearedStageId;
    public StageDto[] stages;
}
[Serializable] public class StageDto {
    public string id;
    public string title;
    public bool cleared;
    public string clearedAt;
    public bool unlocked;
}
[Serializable] public class CompleteStageResponse : StageProgressResponse {
    public string stageId;
    public bool progressAdvanced;
}
```

---

*最后更新: 2026-07-26*
*服务端口: 5188*
