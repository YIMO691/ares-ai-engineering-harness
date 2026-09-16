# ANGE 试点记录检查与描述性汇总

把登记的实验条件、任务、运行结果和证据引用对应起来，暴露漏报、缺失和版本漂移。来源与实施次序由[研究来源](../../docs/research/REFERENCES.md#ai-native-game-engineeringange研究)和[目标总纲](../../docs/TARGET_HARNESS.md#113-ange-研究落地)维护。

这是独立、可选的只读 Python 3.11+ 工具，仅依赖标准库。它不启动 AI、不调用 Direct CLI、不改变任务状态，不执行输入中的命令、不读取证据引用的目标，也不写文件。结果输出到 stdout，错误输出到 stderr；需要保存时由调用者按项目存储边界重定向到外部 EvidenceRoot。

## 运行合成示例

从仓库根目录执行：

```powershell
python -B tools/ange-eval/ange_eval.py protocol --protocol tools/ange-eval/examples/protocol.json
$pilotRuns = @(Get-ChildItem -LiteralPath tools/ange-eval/examples/runs -Filter '*.json' | Sort-Object Name | ForEach-Object FullName)
python -B tools/ange-eval/ange_eval.py report --protocol tools/ange-eval/examples/protocol.json --runs $pilotRuns
```

[协议](examples/protocol.json)及 9 条运行记录全部为 **synthetic**。三个任务为局部修复、有状态伙伴治疗、治疗反馈体验，三个条件为 P0_BASELINE / P0_LIGHT / P0_FULL；低风险例子中 Full 成本更高，状态例子保留一个失败，体验例子展示发现数量与成本。数值全部虚构，未运行模型、未进行真人试玩，不能报告收益。

## 协议 v1

JSON 字段严格检查；未知字段、重复键、重复 ID 和无效类型会报错，演进需新 schema_version。完整格式见合成协议。

| 字段 | 约定 |
|---|---|
| `schema_version` / `study_id` / `kind` | 版本为 1；研究 ID；synthetic 或 empirical |
| `hypothesis` | 本实验的问题与适用边界 |
| `model.id` / `model.configuration` | 实际精确模型标识及配置版本；不填写“最新”。工具只比对字符串，不能向供应商验证身份 |
| `environment` | 环境快照标识，包含需要控制的工具、平台和资源配置 |
| `comparison_controls` | 匹配任务、顺序、经验/熟悉度、预算、缓存、权限及其他控制；是文本记录，不是自动随机化 |
| `evaluator` | 实际评价者/评分规则和盲法，注明业务与体验判据 |
| `missing_data_policy` | 预设缺失、超时、中止及偏离的处理方法；工具始终展示所有已登记 assignment |
| `conditions[]` | 稳定 `id`、`description`、`harness_revision`、`policy_ref`；至少两组 |
| `tasks[]` | 稳定 `id`、`family`、`repo_revision`、`success_criteria`；原始任务与隐藏 oracle 分开保管 |
| `metrics[]` | `id`、`unit`、`direction`（lower/higher/descriptive）、`primary`、`minimum`、`maximum`；界限可为 null，至少一个 primary |
| `assignments[]` | `run_id`、`condition_id`、`task_id`、匿名 `participant_id`、正整数 `repeat`；执行前列出计划运行，失败也必须提交记录 |

同一 condition/task/participant/repeat 只能登记一次，重跑需使用新的 repeat 编号。条件、任务、指标、证据及 assignment 中的 ID 使用 ASCII 字母、数字、点、连字符或下划线，以字母或数字开头。条件 ID 只在当前协议内解释，不复用不同研究阶段的 B0/B1 含义。每个条件和任务都要有 assignment；不是所有任务都必须出现在每个条件中，空分组会显式显示 0 次，不制造可比性。

`protocol` 操作计算 JSON 对象键排序、UTF-8、紧凑表示的 SHA-256；空白与对象键顺序不影响哈希，数组顺序和内容变化会影响。把哈希复制到本协议的每条运行记录中。**哈希只检测内容变化，不证明预登记发生在运行之前。** 正式实验另保留有时间依据的冻结记录，任何修改建立新版本，不给旧结果重写哈希后声称仍是原实验。

## 运行记录 v1

| 字段 | 约定 |
|---|---|
| `schema_version` / `kind` / `protocol_sha256` / `run_id` | 匹配协议及已登记 assignment；不得混合合成和真实类型 |
| `status` | success / failed / timeout / aborted / tool_failure；非 success 的 notes 必须解释原因 |
| `started_at` / `ended_at` | 带时区的 ISO 时间，结束不得早于开始；不是活跃人工时间的自动估计 |
| `observed` | 实际 `model_id`、`model_configuration`、`environment`、`harness_revision`、`repo_revision`；与协议逐字段一致 |
| `metrics` | 必须含每个注册指标；每项为 `{value, missing_reason, evidence_ids}`，缺失填 null 并解释，不填 0；有数值必须引用证据，数值在协议界限内 |
| `evidence[]` | 每项含 `id`、`kind`、`ref`、`claim`、`limitations`；kind 为 functional/engineering/experience/telemetry/recovery/human_cost/process |
| `notes` / `protocol_deviations` | 字符串数组；保留失败、人工干预和协议偏离，不用重命名状态隐藏它们 |

证据引用可以指向授权的外部运行记录、工具报告或真人观察。工具只校验引用存在于该记录，**不证明文件真实存在、证据充分或主观评价正确**。每条记录至少保留一项证据引用；中止时也可引用实际操作记录。Direct 任务可以引用其真实 Run/文档版本，但当前没有自动导入或计时功能。

正式数据不得直接将示例改为 empirical；示例不包含真实观察。先登记真实任务、模型/环境、评价者、预算和参与者安排，执行后如实记录。配置漂移需要独立协议分析；原计划中断/失败的记录仍应保留，不从 assignment 中删去。工具只接受同一冻结配置的数据；漂移现场的原始记录在工具之外保留并说明，不伪造匹配值。

## 结果与退出码

- **0**：结构有效，且所有 assignment 已提交、指标无缺失、无登记的协议偏离；这不表示实验成功或方法有效。失败/中止任务有完整记录时也可返回 0。
- **2**：输入无效、哈希/配置不一致、重复运行、非法指标或读文件失败。
- **3**：输出仍是有效描述性报告，但有缺失运行、缺失指标或协议偏离，需要后续处理。

`record_set_complete` 只表示上述记录齐备条件；`analysis` 始终是 `descriptive_only`。报告按任务 × 条件分组，保留所有状态、数值、样本数、中位数及不同参与者数量。重复运行不被报告为更多参与者，也不跨异质任务计算总体收益。失败运行中实际观测到的值仍参与该分组的描述性汇总；缺失值不插补，未注册于该组的任务没有模拟结果。

工具不计算显著性、置信区间、自动胜者或综合 ANGE 分数。真实推论仍需任务/参与者分层、顺序与学习效应分析、适当样本及预设的效果边界。发现更多体验问题不自动意味着体验更好，status success 也不代表 Owner 验收、合并或发布。

## 验证

```powershell
python -B -m unittest discover -s tools/ange-eval/tests -v
```

已接入根 `scripts/Test-Unified.ps1`；测试覆盖失败漏报、缺失值、配置/协议漂移、重复运行、字段与数值异常、CLI 返回码和合成数据边界。示例及单元测试只验证工具行为。
