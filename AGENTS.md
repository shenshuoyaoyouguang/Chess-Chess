# 中国象棋项目上下文

## 一、项目概述

| 属性 | 说明 |
|------|------|
| **项目名称** | Chess-Chess 中国象棋软件 |
| **项目类型** | WPF 桌面应用程序 |
| **目标框架** | .NET 6.0 Windows (`net6.0-windows`) |
| **编程语言** | C# |
| **开发环境** | Visual Studio 2019/2022 |

### 主要功能

1. **人机对战** - 与象棋引擎对弈，测试棋艺水平
2. **电脑对战** - 观看电脑控制红黑双方攻杀
3. **自由打谱** - 练习各种变化，添加着法注释并保存
4. **残局破解** - 内置 30 个残局，测试残局能力
5. **残局设计** - 收集、扩展残局库
6. **古谱练习** - 练习经典象棋古谱

---

## 二、技术架构

### 技术栈

| 组件 | 版本/说明 |
|------|----------|
| **运行时** | .NET 6.0 Windows |
| **UI 框架** | WPF (Windows Presentation Foundation) |
| **数据库** | SQLite 3.0 (`system.data.sqlite` v1.0.115.5) |
| **JSON 库** | Newtonsoft.Json v13.0.1 |
| **引擎协议** | UCCI (Universal Chinese Chess Interface) |

### 架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                        MainWindow.xaml                          │
│                         主窗口入口                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        QiPanPage.xaml                           │
│                         棋盘页面                                │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┐     │
│  │ 人机对战    │ 电脑对战    │ 自由打谱    │ 残局破解    │     │
│  └─────────────┴─────────────┴─────────────┴─────────────┘     │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌────────────────┐   ┌─────────────────┐
│ GlobalValue   │    │  QiZi (棋子)   │   │  MoveCheck      │
│ 全局值管理类   │    │  棋子控件      │   │  走棋规则验证    │
└───────────────┘    └────────────────┘   └─────────────────┘
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐    ┌────────────────┐   ┌─────────────────┐
│  JiangJun     │    │   Qipu (棋谱)  │   │  XQEngine       │
│  将军/绝杀算法 │    │   棋谱记录     │   │  象棋引擎调用    │
└───────────────┘    └────────────────┘   └─────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      数据层 (SQLite)                            │
│              SqliteHelper + KaiJuKu.db (开局库)                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 三、目录结构

```
D:\AI\
├── Chess-Chess\                    # 主项目目录
│   ├── App.xaml(.cs)               # 应用程序入口
│   ├── MainWindow.xaml(.cs)        # 主窗口
│   ├── QiPanPage.xaml(.cs)         # 棋盘页面（核心）
│   ├── GlobalValue.cs              # 全局状态管理中心
│   ├── Chess.csproj                # 项目配置文件
│   │
│   ├── CustomClass\                # 自定义控件
│   │   ├── QiZi.xaml(.cs)          # 棋子控件（32 个实例）
│   │   ├── Qipu.cs                 # 棋谱数据结构
│   │   ├── MyArrows.xaml(.cs)      # 走棋箭头提示
│   │   ├── PathPoint.xaml(.cs)     # 可移动路径标记
│   │   ├── HuoHuan.xaml(.cs)       # 轮换指示动画
│   │   └── JueSha.xaml(.cs)        # 绝杀动画显示
│   │
│   ├── SuanFa\                     # 算法模块
│   │   ├── MoveCheck.cs            # 走棋规则验证（808 行）
│   │   └── JiangJun.cs             # 将军/绝杀判断
│   │
│   ├── Engine\                     # 引擎目录
│   │   ├── XQEngine.cs             # UCCI 协议实现
│   │   ├── ELEEYE.EXE              # 象棋引擎可执行文件
│   │   └── BOOK.DAT                # 开局库数据
│   │
│   ├── DataClass\                  # 数据模型
│   │   └── QiPuBook.cs             # 棋谱数据模型
│   │
│   ├── OpenSource\                 # 开源组件
│   │   └── SqliteHelper.cs         # SQLite 操作助手
│   │
│   ├── SubWindow\                  # 子窗口
│   │   ├── SystemSetting.xaml      # 系统设置窗口
│   │   ├── Window_QiPu.xaml        # 棋谱库窗口
│   │   ├── Window_JiPu.xaml        # 记谱窗口
│   │   ├── Save_Window.xaml        # 保存对话框
│   │   └── SpyWindow.xaml          # 棋盘数据监控
│   │
│   ├── Thems\                      # 主题样式（10 套）
│   │   ├── Dictionary_Orange.xaml  # 橙色主题
│   │   ├── Dictionary_Blue.xaml    # 蓝色主题
│   │   ├── Dictionary_Green.xaml   # 绿色主题
│   │   ├── Dictionary_ChinaRed.xaml# 中国红主题
│   │   ├── Dictionary_Wood.xaml    # 深木纹主题
│   │   └── ...                     # 其他主题
│   │
│   ├── picture\                    # 图片资源
│   │   ├── 红帅.png, 黑将.png...   # 棋子图片
│   │   ├── 棋盘（红上）.png        # 棋盘背景
│   │   ├── BackGround\             # 窗口背景图
│   │   └── Resource\               # UI 资源
│   │
│   ├── Sounds\                     # 音频资源
│   │   ├── go.mp3                  # 走棋音效
│   │   ├── eat.mp3                 # 吃子音效
│   │   ├── Male_*.mp3              # 男声语音
│   │   └── Female_*.mp3            # 女声语音
│   │
│   └── DB\                         # 数据库
│       └── KaiJuKu.db              # 开局库数据库
│
└── engine\                         # 外部引擎目录
    ├── pikafish\                   # 皮卡鱼引擎（多版本）
    │   ├── pikafish-avx2.exe
    │   ├── pikafish-avx512.exe
    │   ├── pikafish-bmi2.exe
    │   └── pikafish.nnue           # 神经网络权重
    └── sachess1.6\                 # 另一款引擎
        └── sachess_x86.exe
```

---

## 四、构建与运行

### 前置条件

1. 安装 [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
2. 安装 Visual Studio 2019/2022（可选，用于调试）

### 构建命令

```powershell
# 进入项目目录
cd D:\AI\Chess-Chess

# Debug 构建
dotnet build

# Release 构建
dotnet build -c Release

# 发布为独立应用（单文件）
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

### 运行方式

```powershell
# 直接运行
dotnet run

# 或运行生成的可执行文件
.\bin\Debug\net6.0-windows\Chess.exe
```

### NuGet 依赖恢复

项目会自动恢复以下 NuGet 包：
- `Newtonsoft.Json` v13.0.1
- `system.data.sqlite` v1.0.115.5

---

## 五、核心模块说明

### 5.1 GlobalValue（全局状态管理）

**文件**: `Chess-Chess\GlobalValue.cs`

**职责**: 游戏状态管理、棋盘数据、游戏逻辑入口

**关键属性**:
```csharp
// 棋盘常量
public const float GRID_WIDTH = 67.5f;      // 棋盘格大小
public const bool BLACKSIDE = false;        // 黑方标识
public const bool REDSIDE = true;           // 红方标识

// 游戏状态
public static bool SideTag { get; set; }    // 当前走棋方
public static bool IsGameOver { get; set; } // 游戏结束标志
public static int[,] QiPan = new int[9,10]; // 棋盘数据（-1=空, 0-31=棋子编号）

// 界面元素
public static QiZi[] qiZiArray = new QiZi[32];          // 32 个棋子实例
public static PathPoint[,] pathPointImage;              // 路径标记点
```

**核心方法**:
- `QiZiMoveTo()` - 棋子移动处理（带规则验证）
- `Reset()` - 重置棋盘到初始状态
- `HuiQi()` - 悔棋功能
- `AnimationMove()` - 走棋动画

### 5.2 MoveCheck（走棋规则验证）

**文件**: `Chess-Chess\SuanFa\MoveCheck.cs`

**职责**: 所有棋子的移动规则验证

**核心方法**:
```csharp
// 计算棋子可移动路径（返回 9x10 的布尔数组）
public static bool[,] GetPathPoints(int moveQiZi, in int[,] qiPan)

// 检查某点是否被攻击
public static bool IsKilledPoint(int jiangOrShuai, int col, int row, in int[,] qiPan)

// 移动后是否仍被将军
public static bool AfterMoveStillJiangJun(int qiZi, int x0, int y0, int x1, int y1, in int[,] qiPan)
```

**棋子编号规则**:
| 编号 | 棋子 | 黑方 | 红方 |
|------|------|------|------|
| 0/16 | 将/帅 | 将(0) | 帅(16) |
| 1-2/17-18 | 士/仕 | 士(1-2) | 仕(17-18) |
| 3-4/19-20 | 象/相 | 象(3-4) | 相(19-20) |
| 5-6/21-22 | 马 | 马(5-6) | 马(21-22) |
| 7-8/23-24 | 车 | 车(7-8) | 车(23-24) |
| 9-10/25-26 | 炮 | 炮(9-10) | 炮(25-26) |
| 11-15/27-31 | 卒/兵 | 卒(11-15) | 兵(27-31) |

### 5.3 JiangJun（将军/绝杀判断）

**文件**: `Chess-Chess\SuanFa\JiangJun.cs`

**职责**: 将军检测、绝杀判断、困毙检测

**核心方法**:
```csharp
// 判断是否绝杀
public static bool IsJueSha(int moveQiZi)

// 检查是否被将军（返回 int[3]: [0]=状态, [1][2]=将军棋子编号）
public static int[] IsJiangJun(int jiangOrShuai)

// 检查是否困毙（无子可走）
public static bool IsKunBi(bool side)
```

### 5.4 XQEngine（引擎通信）

**文件**: `Chess-Chess\Engine\XQEngine.cs`

**职责**: UCCI 协议实现、引擎进程管理、FEN 格式转换

**UCCI 协议命令**:
| 命令 | 说明 |
|------|------|
| `ucci` | 初始化协议 |
| `setoption <name> <value>` | 设置引擎选项 |
| `position fen <FEN串>` | 设置棋盘局面 |
| `go time <毫秒>` | 限时思考 |
| `go depth <深度>` | 限制搜索深度 |
| `quit` | 退出引擎 |

**FEN 格式示例**:
```
rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1
```

**FEN 棋子映射**:
- 小写字母 = 黑方 (k=将, a=士, b=象, n=马, r=车, c=炮, p=卒)
- 大写字母 = 红方 (K=帅, A=仕, B=相, N=马, R=车, C=炮, P=兵)

### 5.5 Qipu（棋谱记录）

**文件**: `Chess-Chess\CustomClass\Qipu.cs`

**职责**: 棋谱树形结构管理、变招记录

**数据结构**:
```csharp
// 完整棋谱树（支持无限变招）
public class QiPuRecord : QPBase
{
    public QiPuRecord ParentNode { get; set; }
    public ObservableCollection<QiPuRecord> ChildNode { get; set; }
    public QiPuRecord Cursor { get; set; }  // 当前节点指针
}

// 简易记录（数据库 JSON 存储）
public class QiPuSimpleRecord
{
    public List<QiPuSimpleRecord> Child { get; set; }
    public StepCode Data { get; set; }
}
```

---

## 六、用户设置

**文件**: `Chess-Chess\App.config`

| 设置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `mainBKImage` | String | 山水之间.jpeg | 窗口背景图片 |
| `stepArrow` | Bool | True | 显示走棋箭头 |
| `ArrowsMaxNum` | Int | 5 | 最大箭头数量 |
| `MoveDelayTime` | Int | 500 | 电脑走棋延迟(毫秒) |
| `EnableSound` | Bool | True | 启用音效 |
| `EnablePathPoint` | Bool | True | 显示可移动路径点 |
| `ThemsIndex` | Int | 2 | 主题索引 |

---

## 七、主题系统

项目内置 **10 套主题**，位于 `Thems\` 目录：

| 索引 | 主题文件 | 主题名称 |
|------|---------|---------|
| 0 | Dictionary_Null.xaml | 无主题 |
| 1 | Dictionary_Orange.xaml | 橙色主题 |
| 2 | Dictionary_Green.xaml | 绿色主题 |
| 3 | Dictionary_Blue.xaml | 蓝色主题 |
| 4 | Dictionary_Violet.xaml | 紫色主题 |
| 5 | Dictionary_ChinaRed.xaml | 中国红主题 |
| 6 | Dictionary_DarkGreen.xaml | 墨绿主题 |
| 7 | Dictionary_DarkViolet.xaml | 皇家紫主题 |
| 8 | Dictionary_Wood.xaml | 深木纹主题 |
| 9 | Dictionary_Wood_Light.xaml | 浅木纹主题 |

**主题切换实现**:
```csharp
// 动态切换主题
var dict = new ResourceDictionary { Source = new Uri("Thems/Dictionary_Blue.xaml", UriKind.Relative) };
Application.Current.Resources.MergedDictionaries.Clear();
Application.Current.Resources.MergedDictionaries.Add(dict);
```

---

## 八、数据库结构

**数据库文件**: `Chess-Chess\DB\KaiJuKu.db`

### 表结构

#### CanJuKu（残局库）
| 字段 | 类型 | 说明 |
|------|------|------|
| id | INTEGER | 主键 |
| name | TEXT | 残局名称 |
| fen | TEXT | FEN 字符串 |
| comment | TEXT | 注释 |

#### mybook（用户棋谱库）
| 字段 | 类型 | 说明 |
|------|------|------|
| id | INTEGER | 主键 |
| name | TEXT | 棋谱名称 |
| data | TEXT | JSON 格式棋谱数据 |

---

## 九、开发规范

### 命名约定

| 类型 | 命名风格 | 示例 |
|------|---------|------|
| 类名 | 帕斯卡命名法 | `MoveCheck`, `JiangJun` |
| 方法名 | 帕斯卡命名法 | `GetPathPoints()`, `IsJueSha()` |
| 属性 | 帕斯卡命名法 | `SideTag`, `IsGameOver` |
| 私有字段 | 驼峰命名法 | `qiZiArray`, `pathPointImage` |
| 常量 | 帕斯卡命名法 | `GRID_WIDTH`, `BLACSIDE` |

### 注释规范

- 使用简体中文 XML 文档注释
- 注释解释"为什么"而非"做什么"

### 代码风格

- 文件编码：UTF-8
- 缩进：4 空格
- 大括号：Allman 风格（独占一行）

---

## 十、已知问题与改进方向

### 当前架构问题

1. **紧耦合** - `GlobalValue` 类承担过多职责，违反单一职责原则
2. **静态依赖** - 大量使用静态类和方法，难以单元测试
3. **无 MVVM 模式** - 代码在 Code-Behind 中，未充分使用数据绑定
4. **同步阻塞** - 引擎调用为同步方式，可能影响 UI 响应

### 改进计划

1. **MVVM 重构** - 使用 Prism 框架重构，降低模块耦合
2. **依赖注入** - 引入 IoC 容器，提高可测试性
3. **异步引擎调用** - 改为异步方式，避免 UI 卡顿
4. **单元测试** - 为核心算法添加测试覆盖

---

## 十一、参考资料

### UCCI 协议

- [中国象棋电脑应用规范](https://blog.csdn.net/weixin_28681719/article/details/113090094)

### 象棋引擎

- **Eleeye** - 项目默认引擎
- **Pikafish** - 皮卡鱼引擎 (http://pikafish.org)

### 相关资源

- 项目仓库：Gitee (cygsd/Chess)
- 鲨鱼象棋官网：http://sharkchess.com
