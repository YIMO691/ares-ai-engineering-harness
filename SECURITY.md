# 安全说明

## 支持范围

安全维护针对当前主线。`v0.2.0` 是不可变历史基线；使用前应阅读下方依赖说明，不将旧 tag 当作当前安全基线。本项目是本地单用户工具，不提供多租户网络服务。

## 报告漏洞

通过与仓库维护者已有的私密联系渠道报告最小复现、受影响 commit 和安全影响。不要在 Issue 中粘贴凭证、商业源码、原始数据库或运行证据；不假设 GitHub 私密漏洞报告功能已启用。项目没有承诺响应时限。

## 执行与访问边界

当前 Direct Primary 由原生 Codex 宿主控制，Ares 不接管其工具循环。Harness 的 verify 会运行配置的真实构建/测试，并按风险等级调用独立 Reviewer；这些命令可能产生副作用，必须遵守目标项目的实际授权。

Web 默认 ObserverOnly，仅绑定 loopback，接受 GET/HEAD，不启动执行队列。旧执行模式只有明确配置后才启用，其防伪和本地请求限制不能替代身份认证。不要把本服务开放为公共网络服务。

Codex 管理原生沙箱和会话。Reviewer 使用只读执行；allowed_paths 和指纹检查是业务校验，不是逐文件 OS ACL。不要扩大权限来掩盖失败。

## 数据边界

认证、CODEX_HOME、tokens、keys、本地配置、SQLite、artifacts/evidence、浏览器配置、商业项目源码和受限原始资料不得提交。输出使用仓库外的授权目录。发布 Harness 代码不意味着获准发布目标项目或运行记录。

## 历史依赖说明

历史 v0.2.0 通过 Microsoft.Data.Sqlite 解析 SQLitePCLRaw.lib.e_sqlite3 2.1.11；此前干净 CI 还原识别了 [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)。当前项目选择 2.1.13 bundle，并测试加载的 SQLite 至少为 3.50.2，NuGet audit 保持启用。历史 tag 不重写；此条记录已实施的依赖修复，不代表未来不存在新的安全问题。
