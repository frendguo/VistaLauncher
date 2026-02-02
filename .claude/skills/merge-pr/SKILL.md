---
name: merge-pr
description: |
  VistaLauncher 项目专用的 PR 合并流程。用于：
  - 当用户说"合并 PR"、"merge PR"、"合并这个分支"时触发
  - 当 CI 通过后需要合并 PR 时触发
  - 当用户想要完成一个 feature 分支的合并流程时触发

  此 skill 仅适用于 VistaLauncher 项目 (frendguo/VistaLauncher)。
---

# VistaLauncher PR 合并流程

## 前置条件检查

1. 确认当前在 worktree 目录或正确的分支上
2. 确认所有更改已提交并推送

## 合并流程

### 1. 检查 PR 状态

```bash
# 查看当前分支的 PR
gh pr view --repo frendguo/VistaLauncher

# 或列出所有 PR
gh pr list --repo frendguo/VistaLauncher
```

### 2. 检查 CI 状态

```bash
# 查看最近的 CI 运行
gh run list --repo frendguo/VistaLauncher --limit 5

# 查看特定运行的状态
gh run view <run-id> --repo frendguo/VistaLauncher --json status,conclusion
```

如果 CI 失败：
```bash
# 查看失败日志
gh run view <run-id> --repo frendguo/VistaLauncher --log-failed 2>&1 | head -100
```

### 3. CI 通过后合并 PR

```bash
# 使用 squash 合并（推荐，保持历史整洁）
gh pr merge <pr-number> --repo frendguo/VistaLauncher --squash --delete-branch

# 或使用普通合并
gh pr merge <pr-number> --repo frendguo/VistaLauncher --merge --delete-branch
```

### 4. 清理 worktree（如果使用了 worktree）

```bash
# 切换回主仓库
cd C:\Users\frend\source\repos\VistaLauncher

# 删除 worktree
git worktree remove worktrees/<worktree-name>

# 更新主分支
git checkout main
git pull origin main
```

## CI 常见问题及解决方案

### MSIX 打包失败

1. **manifest 命名空间问题**: `windows.startupTask` 需要 `uap5` 命名空间
2. **路径问题**: 确保 `AppxPackageDir` 和 artifact 上传路径匹配

### 测试失败

```bash
# 本地运行测试
cd src/VistaLauncher
dotnet test VistaLauncher.Tests/VistaLauncher.Tests.csproj -p:Platform=x64 --verbosity normal
```

## 注意事项

- 合并前确保 CI 全部通过（x64, x86, ARM64 三个平台）
- 使用 `--delete-branch` 自动删除远程分支
- 合并后记得清理本地 worktree
