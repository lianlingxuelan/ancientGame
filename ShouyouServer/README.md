# 手游本地服务器

这个服务器负责保存玩家资料、角色、章节、六人编队和游戏进度。

当前使用 Node.js 自带的 HTTP 和 SQLite，不需要安装 Docker、MySQL 或 PostgreSQL。

## 启动方法

双击：

`启动本地服务器.cmd`

看到下面文字就表示启动成功：

```text
手游本地服务器已启动：http://127.0.0.1:5188
```

然后用浏览器打开：

<http://127.0.0.1:5188>

关闭服务器时，在服务器窗口按 `Ctrl + C`。

## 默认测试玩家

```text
playerId: demo-player
玩家名: 玩家名
等级: 1
铜钱: 9999
玉: 120
```

## 接口

| 方法 | 地址 | 用途 |
|---|---|---|
| GET | `/api/health` | 检查服务器是否运行 |
| GET | `/api/v1/player/profile?playerId=demo-player` | 获取玩家资料 |
| PUT | `/api/v1/player/profile?playerId=demo-player` | 修改玩家名 |
| GET | `/api/v1/characters?playerId=demo-player` | 获取玩家角色 |
| GET | `/api/v1/chapters` | 获取章节和关卡 |
| GET | `/api/v1/formation?playerId=demo-player` | 获取六人编队 |
| PUT | `/api/v1/formation?playerId=demo-player` | 保存六人编队 |
| GET | `/api/v1/save?playerId=demo-player` | 获取存档 |
| PUT | `/api/v1/save?playerId=demo-player` | 保存进度 |

## 文件说明

```text
ShouyouServer/
├── data/
│   └── shouyou.db          # 第一次启动后自动生成
├── src/
│   ├── database.mjs        # 数据库、表结构和初始数据
│   └── server.mjs          # HTTP 接口
├── package.json
└── 启动本地服务器.cmd
```

`data/shouyou.db` 是本地数据。以后迁移 PostgreSQL 时，Unity 端的接口地址和数据模型可以保持不变。
