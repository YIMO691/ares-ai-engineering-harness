# 本地集成改动

原始固定版本与文件哈希见 UPSTREAM_IMPORTS.json，源项目提交历史仍可通过原仓库固定 commit 追溯。

SOP 将自动入口引用改为 UPSTREAM-AGENTS.md 来源文档，统一执行入口在仓库根 AGENTS.md。Change Lens 允许显式指定外部构建 Worker，测试复用该 Worker，保持原分析契约与许可。

已改动的导入文件：
- workflow/AI-PLAYBOOK.md
- workflow/README.md
- workflow/START-HERE.md
- workflow/pilots/AI-OPERATION-LAYER/SPEC.md
- workflow/prompts/CONTINUE-TASK.md
- workflow/prompts/NEW-TASK.md
- workflow/scripts/validate-workflow.ps1
- tools/change-lens/src/aeh_change_lens/languages/csharp/revision_analysis.py
- tools/change-lens/tests/analyzer/test_worker.py
- tools/change-lens/tests/context/test_build_provenance.py

新增集成文件：workflow/ARES_PROFILE.md、docs/UNIFIED_WORKFLOW.md、现有 Harness 的文档/Align/Lens 适配与统一验证入口。
