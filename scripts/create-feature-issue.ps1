# 创建功能需求 GitHub Issue 的脚本
# 用法: .\create-feature-issue.ps1 -Title "标题" -BodyFile "body.md"
# 或者: .\create-feature-issue.ps1 -Title "标题" -Body "内容"

param(
    [Parameter(Mandatory=$true)]
    [string]$Title,

    [Parameter(Mandatory=$false)]
    [string]$Body,

    [Parameter(Mandatory=$false)]
    [string]$BodyFile,

    [Parameter(Mandatory=$false)]
    [string[]]$Labels = @("enhancement"),

    [Parameter(Mandatory=$false)]
    [string]$Assignee,

    [Parameter(Mandatory=$false)]
    [string]$Milestone
)

# 切换到仓库目录
$repoPath = "C:\Users\frend\source\repos\VistaLauncher"
Set-Location $repoPath

# 确定 issue body
$issueBody = ""
if ($BodyFile) {
    if (Test-Path $BodyFile) {
        $issueBody = Get-Content $BodyFile -Raw
    } else {
        Write-Host "错误: 文件 '$BodyFile' 不存在" -ForegroundColor Red
        exit 1
    }
} elseif ($Body) {
    $issueBody = $Body
} else {
    Write-Host "错误: 必须提供 -Body 或 -BodyFile 参数" -ForegroundColor Red
    exit 1
}

# 构建 gh issue create 命令
$ghArgs = @(
    "issue", "create",
    "--title", $Title,
    "--body", $issueBody
)

# 添加标签
if ($Labels.Count -gt 0) {
    foreach ($label in $Labels) {
        $ghArgs += "--label"
        $ghArgs += $label
    }
}

# 添加负责人
if ($Assignee) {
    $ghArgs += "--assignee"
    $ghArgs += $Assignee
}

# 添加里程碑
if ($Milestone) {
    $ghArgs += "--milestone"
    $ghArgs += $Milestone
}

# 执行命令
Write-Host "正在创建 GitHub Issue..." -ForegroundColor Cyan
Write-Host "标题: $Title" -ForegroundColor Yellow
Write-Host ""

try {
    $issueUrl = & gh @ghArgs
    Write-Host "✓ Issue 创建成功!" -ForegroundColor Green
    Write-Host "URL: $issueUrl" -ForegroundColor Cyan

    # 复制 URL 到剪贴板
    $issueUrl | Set-Clipboard
    Write-Host "✓ URL 已复制到剪贴板" -ForegroundColor Green

    return $issueUrl
} catch {
    Write-Host "✗ 创建失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
