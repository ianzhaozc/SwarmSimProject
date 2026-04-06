# SwarmSimProject 中文项目说明

## 1. 项目一句话介绍

这是一个“**Unity 负责仿真执行，Python 负责策略决策，CSV 负责结果分析**”的无人机集群攻防仿真项目。

如果你完全没接触过这个项目，可以先把它理解成下面这条主线：

1. Unity 生成攻击方和防守方无人机。
2. Unity 把每架无人机当前的观测信息发给 Python。
3. Python 根据策略计算下一步动作。
4. Unity 按动作让无人机飞行、转向、交战。
5. 一局结束后，Unity 把结果写入 CSV。
6. Python 再读取这些 CSV，做统计汇总和可视化分析。

它本质上不是传统的业务系统，而是一个“**可视化仿真环境 + 外部策略控制 + 结果分析**”的平台。

---

## 2. 这个项目在做什么

项目描述的是一个简化的空中对抗场景：

- 攻击方 UAV 的目标是穿过防线，进入目标区域。
- 防守方 UAV 的目标是在目标区周围巡逻，并拦截攻击方。
- 双方使用统一的飞行和攻击规则。
- 双方的决策不是直接写死在 Unity 里，而是交给 Python 来计算。

这样拆分有几个明显好处：

- Unity 擅长做场景、运动、可视化和实时仿真。
- Python 擅长做算法、策略、实验和数据处理。
- Unity 和 Python 通过 ML-Agents 通信，职责划分清晰。

---

## 3. 给小白的基础概念

### 3.1 Unity 是什么

Unity 是一个实时仿真和可视化引擎。  
在这个项目里，它负责：

- 场景和对象管理
- 无人机生成
- 运动执行
- 攻击与死亡判定
- 胜负判定
- 统计结果导出

### 3.2 Python 在这里做什么

Python 不负责画面，也不直接渲染场景。  
它主要负责：

- 根据观测计算动作
- 实现攻击方策略
- 实现防守方策略
- 读取结果并做统计分析

### 3.3 ML-Agents 是什么

ML-Agents 是 Unity 提供的一套“Unity 与外部算法通信”的机制。  
在这个项目中，它的作用就是：

- Unity 把 observation 发给 Python
- Python 把 action 回传给 Unity

你可以把它理解成 Unity 和 Python 之间的一条通信管道。

### 3.4 Episode 是什么

一个完整回合通常叫一个 episode，也就是“一局”。

比如：

- 生成 5 架攻击机和 5 架防守机
- 双方开始运动与交战
- 攻击方进入目标区，或者攻击方全灭，或者超时
- 这一局结束

这整个过程就是一个 episode。

### 3.5 Observation 和 Action 是什么

- Observation：观测，也就是无人机当前“看到”的信息
- Action：动作，也就是策略给无人机的下一步决策

在这个项目里：

- Unity 负责生成 observation
- Python 负责根据 observation 计算 action

---

## 4. 项目整体架构

这个项目不是 MVC。

更准确地说，它是一个：

- Unity 组件式仿真架构
- 外部 Python 策略控制架构
- 结果离线分析架构

如果画成一条链路，就是：

```text
Unity 场景与规则
  -> ML-Agents 通信
  -> Python 策略
  -> Unity 执行动作
  -> CSV 结果导出
  -> Python 分析结果
```

也可以分成 3 层：

### 4.1 仿真执行层

由 Unity 负责：

- 环境参数
- 场景和单位生成
- 路径规划和飞行
- 战斗与胜负规则
- 统计导出

### 4.2 决策控制层

由 Python 负责：

- 攻击方策略
- 防守方策略
- 与 Unity 的动作交互

### 4.3 分析评估层

由 Python 负责：

- 读取 CSV
- 统计胜率、生存率、平均时间
- 可选生成图表

---

## 5. 目录结构说明

项目当前的核心目录大致如下：

```text
SwarmSimProject/
├─ Assets/
│  ├─ Scenes/
│  │  └─ SampleScene.unity
│  ├─ Scripts/
│  │  ├─ EnvParams.cs
│  │  ├─ ScenarioManager.cs
│  │  ├─ GameRuleManager.cs
│  │  ├─ BattleManager.cs
│  │  ├─ BattleStatsManager.cs
│  │  ├─ UavParams.cs
│  │  ├─ UavPositionOptimizer.cs
│  │  ├─ UavAgent.cs
│  │  ├─ UavCombat.cs
│  │  ├─ DubinsController.cs
│  │  └─ DubinsCalculator.cs
│  ├─ Prefabs/
│  │  ├─ UavAttacker.prefab
│  │  └─ UavDefender.prefab
│  └─ Materials/
│     ├─ AttackerMat.mat
│     ├─ DefenderMat.mat
│     └─ GroundMat.mat
├─ Python/
│  ├─ run_battle.py
│  ├─ attacker_policy.py
│  ├─ defender_policy.py
│  ├─ common_utils.py
│  ├─ protocol_constants.py
│  ├─ runtime_policy_config.py
│  ├─ analyze_results.py
│  ├─ requirements.txt
│  ├─ README.md
│  └─ Results/
│     ├─ battle_stats_YYYYMMDD_HHMMSS_mmm.csv
│     └─ Summaries/
│        └─ YYYYMMDD_HHMMSS_micro/
│           ├─ summary_results.csv
│           └─ 图表文件
├─ Packages/
├─ ProjectSettings/
├─ README.md
└─ PROJECT_GUIDE_CN.md
```

### 5.1 `Assets/Scenes`

这里存放 Unity 场景。当前主场景是：

- `Assets/Scenes/SampleScene.unity`

它是整个项目的仿真入口。

### 5.2 `Assets/Scripts`

这里是 Unity 侧核心逻辑：

- 参数配置
- 场景与规则管理
- UAV 组件
- 路径规划与运动

### 5.3 `Assets/Prefabs`

这里是无人机预制体：

- `UavAttacker.prefab`
- `UavDefender.prefab`

可以把 prefab 理解成“无人机模板”。

### 5.4 `Python`

这里是 Python 侧逻辑，包括：

- 对战控制入口
- 攻击方策略
- 防守方策略
- 协议定义
- 结果分析

### 5.5 `Python/Results`

这里是结果输出目录。

当前输出分成两类：

- 原始对战结果：`battle_stats_时间戳.csv`
- 分析归档结果：`Summaries/时间戳/summary_results.csv` 及图表

这意味着现在每次运行都会保留自己的结果，不会轻易覆盖之前的数据。

---

## 6. Unity 侧核心模块

### 6.1 `EnvParams.cs`

文件：`Assets/Scripts/EnvParams.cs`

这是项目的**环境参数中心**。

它保存了几乎所有关键实验参数，包括：

- 战场大小 `areaSize`
- 目标区中心 `targetCenter`
- 目标区半径 `targetRadius`
- 全局探测开关 `enableGlobalDetection`
- 全局探测半径 `globalDetectRadius`
- 局部感知半径 `localSenseRadius`
- 攻击距离 `attackRadius`
- 攻击半角 `attackHalfAngleDeg`
- 攻击冷却 `attackCooldown`
- 最大生命值 `maxHp`
- 攻击伤害 `attackDamage`
- 飞行速度 `speed`
- 最小转弯半径 `minTurnRadius`
- 攻击方数量 `attackerCount`
- 防守方数量 `defenderCount`
- 防守巡逻半径 `defenderPatrolRadius`
- 攻击方出生线参数

当前场景里的 baseline 大致是：

- 战场大小：1000
- 目标区半径：75
- 全局探测：开启
- 全局探测半径：300
- 局部感知半径：150
- 攻击距离：60
- 攻击半角：45°
- 攻击冷却：0.5s
- 最大生命值：15
- 攻击伤害：1
- 飞行速度：15
- 最小转弯半径：35
- 攻击方数量：5
- 防守方数量：5
- 防守巡逻半径：150
- 攻击方出生 X：480
- 攻击方出生 Z：[-300, 300]

如果你要做实验，这通常是最先要改的地方。

### 6.2 `ScenarioManager.cs`

文件：`Assets/Scripts/ScenarioManager.cs`

这是项目的**单位生成与环境重置管理器**。

它负责：

- 清理上一局的单位
- 重新生成攻击方和防守方
- 初始化材质
- 给单位注入运行时依赖
- 将单位注册进 `BattleManager`

#### 防守方生成

防守方会围绕目标区按圆周均匀分布生成，初始朝向沿圆周切线方向。

#### 攻击方生成

攻击方会在一条出生线上展开，初始朝向会指向目标区中心。

#### 初始化注入

在 `ConfigureUnit()` 中，会给单位注入：

- `EnvParams`
- `BattleManager`
- 运行时 UAV ID
- 初始目标位姿

这一步保证了 UAV 各组件在启动时拿到一致的环境上下文。

### 6.3 `BattleManager.cs`

文件：`Assets/Scripts/BattleManager.cs`

这是项目的**单位注册与查询中心**。

它维护两张运行时列表：

- 攻击方列表
- 防守方列表

它的职责包括：

- 注册单位 `Register`
- 注销单位 `Unregister`
- 清空单位 `ClearAll`
- 查询某阵营存活单位
- 查询敌方或友方单位
- 查询全体存活单位
- 统计存活数量

很多模块都依赖它：

- `UavAgent` 通过它找敌人生成观测
- `UavCombat` 通过它找可攻击目标
- `GameRuleManager` 通过它判断攻击方是否全灭
- `BattleStatsManager` 通过它统计移动距离和存活情况

### 6.4 `GameRuleManager.cs`

文件：`Assets/Scripts/GameRuleManager.cs`

这是项目的**胜负判定和局流程管理器**。

它负责：

- 记录当前回合耗时
- 判断一局何时结束
- 输出 `[FINISH]`
- 调用 `BattleStatsManager` 写结果
- 控制是否自动连跑多局

#### 当前结束条件

1. 攻击方进入目标区：判 `Attacker Win (Entered Zone)`
2. 攻击方全灭：判 `Defender Win (All Attackers Dead)`
3. 超时：判 `Defender Win (Time Out)`

#### 多局自动运行

当前场景中，多局运行设置是：

- `autoRunEpisodes = true`
- `maxEpisodes = 10`
- `restartDelay = 1`

### 6.5 `BattleStatsManager.cs`

文件：`Assets/Scripts/BattleStatsManager.cs`

这是项目的**结果统计和 CSV 导出器**。

它负责：

- 跟踪单位累计移动距离
- 统计双方剩余数量
- 计算双方生存率
- 输出统计日志
- 写入 CSV

原始对战 CSV 的表头是：

```text
episode,winner,time,attacker_alive,defender_alive,attacker_survival_rate,defender_survival_rate,attacker_total_distance,defender_total_distance
```

当前不是固定写同一个 `battle_stats.csv`，而是每次 Unity 运行初始化一个独立文件，例如：

```text
Python/Results/battle_stats_20260406_193012_123.csv
```

这样做的好处是：

- 不同批次实验不会混在一个原始文件里
- 新运行不会轻易覆盖旧结果
- 更方便做批次对比

---

## 7. 每架 UAV 身上的组件

项目里的无人机不是单脚本对象，而是多个组件组合起来的。

### 7.1 `UavParams.cs`

文件：`Assets/Scripts/UavParams.cs`

负责身份信息：

- 阵营 `team`
- 单位 ID `uavId`

阵营包括：

- `Attacker`
- `Defender`

### 7.2 `UavPositionOptimizer.cs`

文件：`Assets/Scripts/UavPositionOptimizer.cs`

当前更像一个**动作缓存器**，主要保存：

- 下一目标位置 `nextBestPos`
- 下一目标朝向 `nextBestYaw`

它是决策层和运动层之间的中转站。

### 7.3 `UavAgent.cs`

文件：`Assets/Scripts/UavAgent.cs`

这是 Unity 与 ML-Agents 对接的核心组件。

它的主要职责有两个：

- 收集 observation
- 接收 action

当前统一约定：

- observation 维度：14
- action 维度：3

观测结构：

```text
[self_x, self_z, self_yaw_z_rad,
 target_x, target_z,
 enemy1_dx, enemy1_dz, enemy1_distNorm,
 enemy2_dx, enemy2_dz, enemy2_distNorm,
 enemy3_dx, enemy3_dz, enemy3_distNorm]
```

动作结构：

```text
[target_x, target_z, target_yaw_z_rad]
```

这不是底层舵量控制，而是高层目标点控制。

也就是说，Python 决定“想去哪里、想朝哪”，真正怎么走过去由 Unity 运动层完成。

#### 敌人是怎么被“看到”的

- 攻击方主要依赖局部探测半径
- 防守方可以依赖局部探测，且在开启全局探测时可通过目标区周边共享探测发现敌人

### 7.4 `UavCombat.cs`

文件：`Assets/Scripts/UavCombat.cs`

这是战斗组件，负责：

- 生命值维护
- 目标选择
- 攻击判定
- 受伤
- 死亡处理

攻击条件要求目标同时满足：

- 在攻击半径内
- 在攻击扇区内
- 攻击冷却已结束

默认规则是：

- 攻击距离：60
- 攻击半角：45°
- 冷却：0.5s
- 伤害：1
- 最大 HP：15

死亡后会：

- 标记为死亡
- 禁用 `DubinsController`
- 关闭碰撞体和渲染
- 关闭 `UavAgent`
- 关闭 `DecisionRequester`
- 从 `BattleManager` 注销

这意味着死亡单位不仅“不可见”，也真正退出了后续决策循环。

### 7.5 `DubinsController.cs`

文件：`Assets/Scripts/DubinsController.cs`

这是运动控制组件。

它负责：

- 从 `UavPositionOptimizer` 读取目标
- 判断是否需要重规划
- 调用 `DubinsCalculator` 生成路径
- 沿路径移动
- 平滑旋转

### 7.6 `DubinsCalculator.cs`

文件：`Assets/Scripts/DubinsCalculator.cs`

这是纯路径规划算法模块。

它计算 Dubins 路径，适合描述一种：

- 只能前进
- 转弯半径有限

的运动模型。

---

## 8. Python 侧核心模块

### 8.1 `run_battle.py`

文件：`Python/run_battle.py`

这是 Python 侧的主入口。

它会：

1. 读取 Unity 场景参数
2. 连接 Unity 环境
3. 获取行为名
4. 校验协议
5. 创建攻守策略
6. 进入主循环
7. 读取 observation
8. 计算 action
9. 把 action 发回 Unity

当前写法是：

```python
env = UnityEnvironment()
```

这表示它默认在等待 Unity Editor 回连，而不是自己主动启动一个打包好的 Unity 可执行程序。

所以当前推荐顺序是：

1. 先运行 `python Python/run_battle.py`
2. Python 进入等待 Unity Editor 连接状态
3. 再回 Unity 点 `Play`

#### 它现在还会自动同步 Unity 参数

`run_battle.py` 在连接 Unity 之前，会先读取已保存的 `Assets/Scenes/SampleScene.unity`，把 `EnvParams` 中和策略直接相关的参数同步到 Python。

当前已自动同步的参数包括：

- `Local Sense Radius`
- `Defender Patrol Radius`
- `Attack Radius`

也就是说：

- 你在 Inspector 中改了这些值
- 保存 `SampleScene`
- 再运行 `run_battle.py`

Python 就会自动使用新的环境参数，不用再手动改 Python 中的重复数字。

#### 为什么这里必须先保存场景

因为 Python 读取的是磁盘上的 `.unity` 文件，不是 Unity 编辑器内存里尚未保存的临时状态。

如果你改了 Inspector 但没保存，Python 读到的还是旧参数。

### 8.2 `protocol_constants.py`

文件：`Python/protocol_constants.py`

这是协议常量集中定义文件，统一保存：

- observation 维度
- action 维度
- 敌人槽位数
- 各字段索引

### 8.3 `runtime_policy_config.py`

文件：`Python/runtime_policy_config.py`

这是 Python 与 Unity 场景参数之间的桥接模块。

它负责：

- 读取 `SampleScene.unity`
- 找到 `EnvParams` 的关键字段
- 解析出策略需要的环境参数
- 找不到时回退到安全默认值

目前它负责给策略同步这些环境参数：

- `localSenseRadius`
- `defenderPatrolRadius`
- `attackRadius`

这样就把“环境参数”和“策略调参”区分开了：

- 环境参数来自 Unity
- 策略算法调参仍由 Python 维护

### 8.4 `common_utils.py`

文件：`Python/common_utils.py`

这是策略共享工具函数集合，主要包括：

- 平面距离计算
- 根据两点计算相对 Z 轴 yaw
- 从 observation 中解析敌人
- 找最近敌人

项目统一约定：

- yaw 相对于 Z 轴
- 单位是弧度

### 8.5 `attacker_policy.py`

文件：`Python/attacker_policy.py`

这是攻击方策略。

它不是简单“直冲目标”，而是：

- 想接近目标区
- 但尽量避开敌人威胁

当前实现是一个 PSO 风格的局部搜索：

- 在自己附近采样候选点
- 对候选点打分
- 越接近目标越好
- 越靠近敌人越差
- 最后选择较优目标点

### 8.6 `defender_policy.py`

文件：`Python/defender_policy.py`

这是防守方策略。

它更像一个状态机：

- `Patrol`
- `Engage`

Patrol 时沿目标区外围巡逻；Engage 时优先追最近敌人并执行局部拦截。

### 8.7 `analyze_results.py`

文件：`Python/analyze_results.py`

这是离线分析脚本。

它会：

- 自动扫描 `Python/Results/*.csv`
- 跳过自己生成的汇总文件
- 读取 battle stats 原始数据
- 输出单文件摘要
- 多文件时输出比较表
- 保存分析汇总
- 可选保存图表

为了避免后一次分析覆盖前一次结果，现在每次分析都会写到独立目录，例如：

```text
Python/Results/Summaries/20260406_193500_123456/
├─ summary_results.csv
├─ attacker_success_rate.png
├─ avg_time.png
└─ ...
```

---

## 9. 一局对战是怎么跑起来的

### 第一步：场景启动

打开 `SampleScene` 后，场景里有一个 `GameManager`，上面挂着：

- `EnvParams`
- `ScenarioManager`
- `GameRuleManager`
- `BattleManager`
- `BattleStatsManager`

### 第二步：生成单位

`ScenarioManager.Start()` 调用 `SpawnAll()`：

- 清理旧单位
- 生成防守方
- 生成攻击方
- 初始化 UAV 组件
- 注册到 `BattleManager`

### 第三步：Python 连接 Unity

你运行 `run_battle.py` 后：

- Python 会先读取已保存场景中的环境参数
- 然后连接 Unity
- 查找 `AttackerBehavior` 和 `DefenderBehavior`
- 校验 observation/action 协议

### 第四步：Unity 收集观测

`UavAgent.CollectObservations()` 会把：

- 自己位置
- 自己朝向
- 目标区中心
- 最多 3 个可探测敌人的信息

组成一个 14 维向量发给 Python。

### 第五步：Python 计算动作

- 攻击方使用 `AttackerPolicy`
- 防守方使用 `DefenderPolicy`

最后每架 UAV 都会得到：

- 目标点 x
- 目标点 z
- 目标朝向 yaw

### 第六步：Unity 接收动作

`UavAgent.OnActionReceived()` 会把动作写到：

- `UavPositionOptimizer`

### 第七步：运动执行

`DubinsController` 每帧会：

- 读取目标
- 判断是否需要重规划
- 调用 `DubinsCalculator`
- 沿路径移动
- 平滑旋转

### 第八步：战斗判定

`UavCombat` 每帧会：

- 查询敌方
- 判断是否满足攻击条件
- 执行伤害
- 处理死亡

### 第九步：结束判定

`GameRuleManager` 每帧会检查：

- 攻击方是否进入目标区
- 攻击方是否全灭
- 是否超时

一旦结束：

- 输出 `[FINISH]`
- 调用 `BattleStatsManager.OnBattleFinished()`
- 输出 `[STATS]`
- 写入 CSV
- 若开启自动连跑，则稍后重开下一局

---

## 10. 项目数据流

```text
EnvParams
  -> ScenarioManager 生成单位
  -> BattleManager 注册单位
  -> UavAgent 收集观测
  -> Python 策略计算动作
  -> UavPositionOptimizer 保存目标
  -> DubinsController 规划并执行路径
  -> UavCombat 处理交战
  -> GameRuleManager 判定结束
  -> BattleStatsManager 输出 CSV
  -> analyze_results.py 汇总分析
```

---

## 11. 当前场景与预制体状态

### 11.1 主场景 `SampleScene`

当前主场景已保存这些关键配置：

- `BattleStatsManager` 已挂在 `GameManager`
- `GameRuleManager` 已连上 `battleStatsManager`
- 自动多局运行开启
- `maxEpisodes = 10`
- `restartDelay = 1`
- CSV 导出开启
- Inspector 中的基础文件名仍是 `battle_stats.csv`
- 但运行时会自动生成带时间戳的实际输出文件名

### 11.2 攻击方预制体 `UavAttacker.prefab`

当前关键配置：

- `Team = Attacker`
- `Behavior Name = AttackerBehavior`
- `Vector Observation Size = 14`
- `Continuous Actions = 3`
- `Decision Period = 5`
- `Take Actions Between Decisions = true`

### 11.3 防守方预制体 `UavDefender.prefab`

当前关键配置：

- `Team = Defender`
- `Behavior Name = DefenderBehavior`
- `Vector Observation Size = 14`
- `Continuous Actions = 3`
- `Decision Period = 5`
- `Take Actions Between Decisions = true`

这说明当前 Unity 和 Python 协议是对齐的。

---

## 12. 为什么这样设计

### 为什么不把策略直接写在 Unity 里

因为把策略放 Python 更适合：

- 快速改算法
- 接优化方法或机器学习
- 记录实验
- 批量分析结果

### 为什么动作不是“前进/舵量/速度”

因为当前设计是“高层导航目标点 + Unity 运动执行”。

Python 负责说：

- 我想去哪
- 我想朝哪

Unity 负责决定如何物理地走过去。

### 为什么攻守双方共用统一协议

因为统一协议能降低维护成本。

攻守双方的主要差异不在通信格式，而在：

- 行为名
- 阵营
- Python 策略逻辑

---

## 13. 正确运行步骤

当前项目是在 Unity Editor 模式下联动 Python。

推荐顺序：

1. 打开 Unity 项目
2. 打开 `Assets/Scenes/SampleScene.unity`
3. 检查 `GameManager` 组件和引用是否完整
4. 如果你刚改过 `EnvParams`，先保存 `SampleScene`
5. 安装 Python 依赖

```powershell
pip install -r Python/requirements.txt
```

6. 先运行：

```powershell
python Python/run_battle.py
```

7. 等 Python 进入等待 Unity Editor 连接状态
8. 回 Unity 点 `Play`
9. 对局运行，并输出带时间戳的原始 battle CSV
10. 对局结束后运行：

```powershell
python Python/analyze_results.py
```

### 为什么要先跑 Python 再点 Play

因为当前使用的是：

```python
UnityEnvironment()
```

它默认在等待 Unity Editor 回连，而不是由 Python 直接启动 Unity 构建程序。

---

## 14. 如果我想改实验配置，应该改哪里

通常先看 `EnvParams`。

更推荐优先改这些：

- `Enable Global Detection`
- `Global Detect Radius`
- `Local Sense Radius`
- `Attacker Count`
- `Defender Count`
- `Attack Radius`

补充一点：

- 如果这些值会影响 Python 策略输入，例如 `Local Sense Radius`、`Defender Patrol Radius`、`Attack Radius`，改完后请先保存 `SampleScene`，再运行 `run_battle.py`。

---

## 15. 如果我想看结果，应该看哪里

### 15.1 Unity 视图

看无人机是否：

- 正常生成
- 正常巡逻
- 正常追击
- 正常进入目标区

### 15.2 Unity Console

看是否出现：

- `[FINISH]`
- `[STATS]`
- `[CSV] Saved to: ...`

### 15.3 `Python/Results/` 下的原始 battle CSV

当前原始结果不再固定写到单个 `battle_stats.csv` 里，而是每次 Unity 运行生成独立文件，例如：

```text
Python/Results/battle_stats_20260406_193012_123.csv
```

你需要重点确认：

- 新一次运行是否生成了新的时间戳文件
- 不同批次是否保留为独立原始数据

### 15.4 `python Python/analyze_results.py`

分析结果会写到：

```text
Python/Results/Summaries/<timestamp>/
```

每次分析都会保留自己的：

- `summary_results.csv`
- 各类图表

---

## 16. 这个项目当前的优点

从工程结构看，它已经具备一个小型仿真实验平台的基本骨架：

- Unity 与 Python 职责分离清晰
- 攻守双方策略独立
- 协议格式统一
- 支持多局自动运行
- 支持结果导出
- 支持离线分析
- 关键参数集中在 `EnvParams`
- 关键环境参数可自动同步到 Python

对于课程项目、论文实验、小规模策略对比，这个结构是很实用的。

---

## 17. 当前需要注意的点

### 17.1 当前主要运行在 Unity Editor 模式

这意味着：

- Python 不是直接启动 Unity 程序
- 而是等 Unity Editor 进入 Play 后连接

### 17.2 ML-Agents 版本要匹配

当前 Unity 侧依赖的 ML-Agents 包仍然和本机环境有关。  
如果换机器，要注意 Unity 与 Python 侧版本兼容。

### 17.3 结果输出现在分为两层

- 原始 battle 数据：`Python/Results/battle_stats_时间戳.csv`
- 分析归档结果：`Python/Results/Summaries/时间戳/`

### 17.4 这是“规则仿真 + 外部策略”系统

虽然项目用了 ML-Agents 的 Agent 和 Behavior Parameters，但当前核心用途是：

- 通信
- 决策接口

它并不是一套现成的端到端强化学习训练流水线。

---

## 18. 如果你只想用最短的话理解全项目

可以记住这句话：

> 这个项目在 Unity 里搭了一个无人机攻防战场。攻击方要进入目标区，防守方要巡逻和拦截。Unity 负责场景、运动、战斗和判胜负，Python 负责给双方算动作，每局结束后 Unity 输出 CSV，Python 再做统计分析。

---

## 19. 推荐阅读顺序

如果你是第一次接手，建议按这个顺序看：

1. 先看本文件，建立整体印象
2. 再看 [README.md](/d:/UnityProjects/Github/SwarmSimProject/README.md)
3. 再看 [Python/README.md](/d:/UnityProjects/Github/SwarmSimProject/Python/README.md)
4. 打开 Unity 的 `SampleScene`
5. 看 `EnvParams.cs`
6. 看 `ScenarioManager.cs` 和 `GameRuleManager.cs`
7. 看 `UavAgent.cs`
8. 看 `run_battle.py`
9. 再看 `attacker_policy.py` 和 `defender_policy.py`

---

## 20. 关键文件索引

根目录说明：

- [README.md](/d:/UnityProjects/Github/SwarmSimProject/README.md)

Python 说明：

- [Python/README.md](/d:/UnityProjects/Github/SwarmSimProject/Python/README.md)

Unity 环境参数：

- [EnvParams.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/EnvParams.cs)

场景与单位生成：

- [ScenarioManager.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/ScenarioManager.cs)

规则控制：

- [GameRuleManager.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/GameRuleManager.cs)

单位注册：

- [BattleManager.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/BattleManager.cs)

统计导出：

- [BattleStatsManager.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/BattleStatsManager.cs)

Unity Agent：

- [UavAgent.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/UavAgent.cs)

战斗逻辑：

- [UavCombat.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/UavCombat.cs)

运动控制：

- [DubinsController.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/DubinsController.cs)
- [DubinsCalculator.cs](/d:/UnityProjects/Github/SwarmSimProject/Assets/Scripts/DubinsCalculator.cs)

Python 运行入口：

- [run_battle.py](/d:/UnityProjects/Github/SwarmSimProject/Python/run_battle.py)

Python 参数桥接：

- [runtime_policy_config.py](/d:/UnityProjects/Github/SwarmSimProject/Python/runtime_policy_config.py)

攻击策略：

- [attacker_policy.py](/d:/UnityProjects/Github/SwarmSimProject/Python/attacker_policy.py)

防守策略：

- [defender_policy.py](/d:/UnityProjects/Github/SwarmSimProject/Python/defender_policy.py)

结果分析：

- [analyze_results.py](/d:/UnityProjects/Github/SwarmSimProject/Python/analyze_results.py)

---

## 21. 最后一句话

如果把它理解成一个“无人机攻防小游戏”，也不算错。  
但从工程角度看，它更准确的身份是：

> 一个用于无人机集群攻防实验的可视化仿真平台。
