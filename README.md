# ExamBilingualReader (四六级与雅思双语对照精读桌面端软件)

面向英语四六级与雅思备考的高效「双语对照翻译与长难句精读」桌面端软件。

## 技术栈
* **UI 框架**: WPF (.NET 8/10)
* **MVVM**: CommunityToolkit.Mvvm
* **依赖注入**: Microsoft.Extensions.DependencyInjection
* **本地词库**: SQLite + Dapper (ECDICT 考纲词条结构)
* **AI 大模型服务**: 标准 HttpClient (兼容 OpenAI BYOK 协议，支持 HTTP/SOCKS 代理)

## 项目结构
* [`src/ExamBilingualReader.Core`](file:///c:/Users/Renly/Desktop/rt/src/ExamBilingualReader.Core): 领域实体、考纲级别划分、分句工具算法 (`SentenceSplitter`)
* [`src/ExamBilingualReader.Infrastructure`](file:///c:/Users/Renly/Desktop/rt/src/ExamBilingualReader.Infrastructure): SQLite 词典数据库操作、OpenAI 协议客户端、配置持久化
* [`src/ExamBilingualReader.Wpf`](file:///c:/Users/Renly/Desktop/rt/src/ExamBilingualReader.Wpf): MVVM 视图层、考纲色彩流式高亮渲染、Tooltip 词卡、长难句学术语法树展示侧边栏
* [`tests/ExamBilingualReader.Tests`](file:///c:/Users/Renly/Desktop/rt/tests/ExamBilingualReader.Tests): 单元测试用例

## 快速运行
使用 Visual Studio 2022 / Rider 打开 [`ExamBilingualReader.sln`](file:///c:/Users/Renly/Desktop/rt/ExamBilingualReader.sln) 并将 `ExamBilingualReader.Wpf` 设为启动项目即可启动。
