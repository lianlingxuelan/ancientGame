# 踩坑记录

> 记录项目中遇到的所有坑，避免重复犯错。

---

## 一、AI 贾维斯 — 源码编译

### 1. vendored 上游源码不完整
**现象**：`llama-model.cpp` 报 `Cannot open include file: "models/models.h"`  
**根因**：`third_party/runtime/vendor/src/models/` 整个目录缺失（131 个文件），上游压缩包包含但 vendor 快照遗漏  
**修复**：从 `F:/AI-project/llama.cpp-omni`（用户 clone 的上游）完整复制  
**教训**：vendor 快照不一定是完整的，遇到缺文件优先去上游仓库查

### 2. mtmd 编译阻断 omni
**现象**：cmake 配不过，报 `Cannot find source file: models/models.h`  
**根因**：`tools/mtmd/CMakeLists.txt` 在 `tools/omni/CMakeLists.txt` 之前执行，mtmd 失败后 cmake 停止，omni 从未被处理  
**修复**：在 `tools/CMakeLists.txt` 中注释掉 `add_subdirectory(mtmd)`  
**教训**：cmake 子目录按顺序执行，前置错误会阻断后续，不一定是最后的模块出了问题

### 3. MSBuild `.tlog` 文件锁
**现象**：编译中途报 `link.exe failed: file used by another process`  
**根因**：MSBuild 的 Tracked Output 机制写 `.tlog` 文件，并行编译时互相锁死  
**修复**：`/p:TrackFileAccess=false` 禁用跟踪文件  
**教训**：Windows 上用 MSBuild 编译器，这个开关几乎是必加的

### 4. CUDA 13.1 VS 集成 bug
**现象**：cmake 编译器测试阶段 CUDA 测试失败 `MSB4023: metadataName cannot be zero`  
**根因**：`CUDA 13.1.targets` 与 MSBuild 17.14 的 item metadata 存在兼容问题  
**修复**：加 `-DCMAKE_CUDA_COMPILER_FORCED=ON -DCMAKE_CUDA_FLAGS=-allow-unsupported-compiler`  
**教训**：新版本 CUDA Toolkit 的 VS 集成可能有 bug，遇到编译器测试失败直接跳过

### 5. sed 修改 CMakeLists 破坏结构
**现象**：`Cannot find source file: add_library`  
**根因**：`sed '/add_library(omni/ a\xxx'` 把新行插到了 `add_library(omni` 和它的源文件列表**之间**，导致 `add_library(omni` 收到 0 个源文件  
**修复**：用 Python 精确找到 `add_library` 的结束时括号位置再插入  
**教训**：永远不要用 sed 改 CMake 文件，用 Python 精确定位行号再插入

### 6. bash 吃 Windows 路径
**现象**：`powershell.exe -Command "& 'F:\...\build.cmd'"` 输出乱码或 `command not found`  
**根因**：bash 把反斜杠 `\` 转义、空格截断、`"` 配对错乱  
**修复**：Windows 命令行操作写成 `.bat` 文件，用户双击运行  
**教训**：涉及 VS DevCmd、MSBuild 的命令，不要让 bash 中转

### 7. VS DevCmd 环境不在管道中生效
**现象**：通过 bash 管道跑的 cmake 找不到 `cl.exe`  
**根因**：`VsDevCmd.bat` 通过 `call` 设置环境变量，管道子进程拿不到  
**修复**：写成 `.cmd` 文件双击运行  
**教训**：VS 开发者命令只能在新 cmd 窗口中运行，不能通过管道传递

### 8. 360 杀软删除编译中间文件
**现象**：编译突然报 0 obj，二进制失踪  
**根因**：360 把 `.obj`/`.exe` 当成木马隔离  
**修复**：退出 360 或把项目目录加信任区 + 恢复区恢复文件  
**教训**：Windows 上做 C++ 编译必关杀毒，或用 WSL

### 9. omni_text_runtime alias 在 master 分支不存在
**现象**：cmake 配不过 `Pinned patch contract requires target omni_text_runtime`  
**根因**：vendor 源码打了补丁加了 `add_library(omni_text_runtime ALIAS omni)`，但用户 clone 的 master 分支没有这行  
**修复**：在 `tools/omni/CMakeLists.txt` 的 `add_library(omni ...)` 闭合括号后精确插入  
**教训**：不同分支的 patch 状态不一样，不能假设 master 已打补丁

### 10. GitHub API 分页截断
**现象**：下载了 130 个文件，缺 `qwen35.cpp`  
**根因**：GitHub API 默认每页 100 条，131 个文件跨两页，第一页没包含 q 开头的文件  
**修复**：用用户的完整 clone 替换  
**教训**：用 API 批量下载必须处理分页

---

## 二、ancientGame — Unity 前端 + Node 后端

### 11. 按钮无点击响应
**现象**：SkillButton_1~4 在 Unity Play Mode 中无法点击  
**根因**：`HomeUILayoutBuilder` 用 `AddCommonButtonImage` 创建技能按钮，该方法只加 `Image` 组件，不加 `Button` 组件；Controller 的 `FindButton` 找不到 Button 就静默跳过  
**修复**：在 Builder 的技能按钮创建循环中 `AddComponent<Button>()`  
**教训**：UI 按钮 = Image + Button 组件，缺一不可

### 12. 后端端口被旧进程占用
**现象**：修改 server.mjs 后测试仍然是旧行为  
**根因**：旧 node 进程未杀掉，Unity 可能自动重启了服务器  
**修复**：`netstat -ano | grep :5188` 找到 PID → `taskkill /F /PID xxx`  
**教训**：先确认旧服务真死了再测试新代码

### 13. UTF-8 文件在 GBK 终端显示乱码
**现象**：JSON 文件里中文显示为 `ÀîÇåÕÕ`  
**根因**：文件是 UTF-8，但读取环境默认 GBK（Windows 中文代码页 936）  
**修复**：文件保持 UTF-8，Node.js 读取正常；Unity 前端用本地中文映射作为兜底  
**教训**：UTF-8 ≠ GBK，跨工具链时编码永远不会一帆风顺

### 14. 多人协作时日志被覆盖
**现象**：Claude 写的 task log 和 Codex 写的冲突  
**根因**：两人同时 append 到 `AI_TASK_LOG.md`，后提交的覆盖先提交的  
**修复**：各自先 `git pull` 再写日志  
**教训**：共享 append-only 日志 = 需要 lock-step 提交节奏

### 15. demo-config.json 出现重复字段
**现象**：JSON 解析正常但 `enemies` 有 2 段  
**根因**：Edit 工具替换 skills 块时，新内容带了另一个 enemies 数组，原 enemies 未自动删除  
**修复**：重写整个文件，确保每个 key 只出现一次  
**教训**：用 Edit 工具替换 JSON 局部时，注意别引入重复 key

---

## 三、ComfyUI — AI 视频生成

### 16. Wan2.1 14B 在 12GB 显卡上跑不动
**现象**：GPU 100%、CPU 100%，无输出  
**根因**：Wan2.1 I2V 14B FP8 模型+VAE+CLIP 约 10-11GB，33 帧 832×480 解码时爆显存  
**修复**：换 AnimateDiff（SD1.5 based），12GB 随便跑  
**教训**：视频模型对显存要求比文本模型高一个数量级

### 17. AnimateDiff 运动模块全网下不到
**现象**：HuggingFace 直连超时、hf-mirror 404、ModelScope 404、Gitee 404、GitHub Releases 404  
**根因**：文件被作者移动/重命名，所有镜像都找不到  
**修复**：用 ComfyUI Manager（秋叶版自带）的模型管理页面下载  
**教训**：模型文件首选工具自带的下载器，手动下载是最后手段

### 18. ComfyUI WanVideoWrapper 节点名不标准
**现象**：写的标准 Wan2.1 工作流 JSON 加载后连线全断，颜色不匹配  
**根因**：秋叶版 ComfyUI 用的是深度魔改的 `WanVideoWrapper` 分支，节点名、输入输出类型都与官方不同  
**修复**：逐节点检查 `INPUT_TYPES` / `RETURN_TYPES` 再写工作流  
**教训**：不同 ComfyUI 发行版（秋叶/官方）节点接口不通用

---

## 四、通用教训

| # | 教训 |
|:--:|------|
| 1 | Windows C++ 编译 = 关杀毒 + TrackFileAccess=false + CUDA 编译器测试跳过 |
| 2 | bash 中转 Windows 命令 = 灾难，写成 .bat/.cmd 双击跑 |
| 3 | JSON/YAML 配置 = 重写全文件优于局部 Edit |
| 4 | 端口冲突 = 先 `netstat` 再 `taskkill`，再测 |
| 5 | 中文 = UTF-8 是文件格式，GBK 是终端格式，两者打架是常态 |
| 6 | 开源项目 vendor = 不要假设完整，去上游仓库查缺补漏 |
| 7 | 多人协作 = 先 pull 再写，先读再改 |

---

*最后更新：2026-07-29*
