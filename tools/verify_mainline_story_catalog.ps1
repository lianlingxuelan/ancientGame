param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$catalogPath = Join-Path $projectRoot 'ShouyouPrototype\ShouyouPrototype\Assets\_Project\Scripts\Data\MainlineStoryCatalog.cs'

function Assert-Contains {
    param([string]$Content, [string]$Expected, [string]$Message)

    if (-not $Content.Contains($Expected)) {
        throw ("{0}: {1}" -f $Message, $Expected)
    }
}

if (-not (Test-Path -LiteralPath $catalogPath)) {
    throw 'Mainline story catalog source file is missing.'
}

$catalogSource = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8

# 关卡详情、剧情回看和后续后端同步都需要安全查询，不能只依赖回退到第一关的旧接口。
Assert-Contains $catalogSource 'public int LineCount' 'Story sequence must expose a safe line count'
Assert-Contains $catalogSource 'public string GetLine(int index)' 'Story sequence must expose a bounds-safe line reader'
Assert-Contains $catalogSource 'public static bool TryGet(int stageId, out MainlineStorySequence sequence)' 'Story catalog must expose a safe stage lookup'
Assert-Contains $catalogSource 'public static int[] GetStageIds()' 'Story catalog must expose a stage-id list'

# 第一章目录必须仍使用本地序列配置，文案内容由人工评审确认，避免脚本环境的中文编码差异产生误报。
Assert-Contains $catalogSource 'private static readonly MainlineStorySequence[] Sequences' 'Chapter one local sequence storage is required'
Assert-Contains $catalogSource 'new MainlineStorySequence(' 'Chapter one must keep story sequence entries'

# 老调用方仍可拿到首关的非空回退，避免 Demo UI 因非法编号直接空引用。
Assert-Contains $catalogSource 'return Sequences[0];' 'Legacy Get fallback must remain available'

Write-Output 'Mainline story catalog static validation passed.'
