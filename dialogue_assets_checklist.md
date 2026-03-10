# 对话系统首版素材清单

这份清单是给“开始游戏后进入第一段剧情对话场景”的首版架构准备的。

目标不是一次把所有美术都补齐，而是先把**可复用的对话系统 MVP**跑起来：

- 点击“开始游戏”后进入剧情场景
- 根据 JSON 播放背景、人物出场、对话文本
- 根据人物 `id` 自动找到立绘
- 支持基础表情切换、左右站位、淡入淡出
- 后续再扩展角色移动、镜头、选择支、战斗切换

---

## 一、首版必须素材

如果你想让我先把**第一版完整做出来**，下面这些是最少必须有的。

### 1. 场景背景图

每个对话地点至少 1 张。

建议规格：

- 格式：`png`
- 分辨率：优先 `1920x1080`，最低不要低于 `1280x720`
- 内容：纯背景，不要 UI，不要人物
- 风格：尽量和游戏整体像素/复古风统一

建议命名：

- `Assets/dialogue/backgrounds/xuzijiang_study_day.png`
- `Assets/dialogue/backgrounds/xuzijiang_study_night.png`

首个剧情建议至少给我 1 张：

- `许子将相面/论世` 相关场景背景
  - 可选方向：书房、庭院、厅堂、东汉风室内

### 2. 人物立绘 PNG

这是首版最核心的素材。系统会根据人物 `id + expression` 自动取图。

建议规格：

- 格式：`png`
- 背景：**透明**
- 内容：单人物
- 方向：默认正面或略微侧身都可以
- 高度建议：`1400px ~ 2200px`
- 宽度建议：按角色构图自然来，不必强行统一

建议每个角色首版至少提供这些表情：

- `normal`：平静/默认
- `serious`：严肃
- `thinking`：思索
- `surprised`：惊讶
- `smile`：轻笑或意味深长

建议目录结构：

- `Assets/dialogue/portraits/caocao/normal.png`
- `Assets/dialogue/portraits/caocao/serious.png`
- `Assets/dialogue/portraits/caocao/thinking.png`
- `Assets/dialogue/portraits/xuzijiang/normal.png`
- `Assets/dialogue/portraits/xuzijiang/smile.png`

首幕最少需要这 2 个角色：

- `caocao`
- `xuzijiang`

### 3. 对话框 UI 素材

如果你希望首版就有自己的风格化 UI，而不是我先用程序画一个临时框，请准备：

- 对话框底板：`png`
- 姓名牌底板：`png`
- “继续”提示小图标：`png`

建议规格：

- 对话框底板：透明背景，建议宽屏底部横向构图
- 姓名牌：透明背景，尽量可复用
- 提示图标：比如小箭头/花纹/烛火点

建议命名：

- `Assets/dialogue/ui/dialog_box.png`
- `Assets/dialogue/ui/nameplate.png`
- `Assets/dialogue/ui/next_indicator.png`

### 4. 首幕用 BGM / 环境音

不是绝对必须，但强烈建议首版就给。

建议规格：

- 格式优先：`ogg`
- 也可：`wav`（但务必是 PCM 或 IEEE float，避免 Godot 导入报错）
- 长度：30 秒到 2 分钟都可以，最好可循环

首版建议至少：

- 1 条剧情 BGM
- 可选 1 条室内环境音

建议命名：

- `Assets/dialogue/audio/bgm/intro_counsel.ogg`
- `Assets/dialogue/audio/ambience/room_night.ogg`

---

## 二、建议补齐的增强素材

这些不是首版必须，但如果你提前准备好，我后面可以直接往上叠功能。

### 1. 人物入场/呼吸用轻微动效素材

如果你希望立绘不是完全静止，可以二选一：

#### 方案 A：分层立绘

给我拆层 PNG：

- 身体主体
- 前发
- 后发
- 眼睛
- 嘴
- 饰品

这样我可以做：

- 轻微呼吸
- 头发摆动
- 眨眼
- 简单说话动效

#### 方案 B：整张立绘多状态图

比如：

- `normal_1.png`
- `normal_2.png`
- `normal_3.png`

这样可以做轻量帧切换呼吸感。

### 2. 场景前景遮罩 / 氛围层

用于提升镜头层次感。

比如：

- 窗棂前景
- 烛火光晕
- 雾气
- 花瓣 / 灰尘 / 雪点

建议命名：

- `Assets/dialogue/overlays/study_window_fg.png`
- `Assets/dialogue/overlays/candle_glow.png`

### 3. 角色出场特效素材

如果你想让人物登场更有仪式感，可以准备：

- 光点粒子贴图
- 墨迹扩散贴图
- 纸屑 / 火星 / 花瓣贴图

建议命名：

- `Assets/dialogue/vfx/sparkle_particle.png`
- `Assets/dialogue/vfx/ink_smoke.png`

### 4. 选择支按钮 UI

如果后面剧情要出现“是 / 否 / 追问 / 离开”这种选项，建议提前准备：

- 默认态
- 悬浮态
- 选中态

建议命名：

- `Assets/dialogue/ui/choice_button_idle.png`
- `Assets/dialogue/ui/choice_button_hover.png`
- `Assets/dialogue/ui/choice_button_selected.png`

---

## 三、如果你要做“人物在场景里站着说话”

你刚才提到“人物在场景里 idle 的帧动画 png”，这里我帮你区分一下：

### A. 立绘对话模式

特点：

- 人物以大立绘形式站在左右两侧
- 更接近视觉小说 / 曹操传剧情演出界面
- 首版最适合先做这个

需要素材：

- 立绘 PNG
- 表情 PNG
- 背景图

### B. 场景内角色行走/站立模式

特点：

- 人物真的站在地图场景里
- 可以朝左/朝右/走动/停下
- 更像 RPG 场景演出

如果你要这一套，我需要额外素材：

#### 1. 角色像素角色图 / 帧动画

每个角色建议提供：

- `idle_down`
- `idle_up`
- `idle_left`
- `idle_right`
- `walk_down`
- `walk_up`
- `walk_left`
- `walk_right`

建议规格：

- 单帧尺寸统一，比如：`64x64`、`96x96` 或 `128x128`
- 每组动画建议 `4~8` 帧
- 透明背景 PNG

建议目录结构：

- `Assets/dialogue/actors/caocao/idle_down_strip.png`
- `Assets/dialogue/actors/caocao/walk_right_strip.png`
- `Assets/dialogue/actors/xuzijiang/idle_left_strip.png`

#### 2. 场景碰撞 / 前景遮挡信息

如果角色要在地图里移动，还最好有：

- 地图背景图
- 前景遮挡层
- 简单可行走区域说明

这一套我建议放到**第二阶段**做，不要和首版立绘对话系统混在一起，否则实现成本会明显上升。

---

## 四、首幕《曹操 vs 许子将》建议素材表

如果我们就做“点击开始游戏后进入第一幕”，那我建议你先交这一包：

### 必交

- 1 张背景图
  - `许子将住所 / 书房 / 厅堂` 任意一种
- 曹操立绘 3~5 张表情
  - `normal`
  - `serious`
  - `surprised`
  - `smile` 或 `thinking`
- 许子将立绘 3~5 张表情
  - `normal`
  - `smile`
  - `serious`
  - `thinking` 或 `closed_eye`
- 1 条剧情 BGM

### 强烈建议一起给

- 对话框底板
- 姓名牌底板
- “继续”指示图标
- 1 条环境音
- 1~2 张前景氛围叠层

---

## 五、我这边希望你按这个命名规范整理

为了后面 JSON 自动读取，我建议你尽量按下面放：

```text
Assets/
  dialogue/
    backgrounds/
      xuzijiang_study_day.png
    portraits/
      caocao/
        normal.png
        serious.png
        surprised.png
        smile.png
      xuzijiang/
        normal.png
        serious.png
        thinking.png
        smile.png
    ui/
      dialog_box.png
      nameplate.png
      next_indicator.png
    audio/
      bgm/
        intro_counsel.ogg
      ambience/
        room_night.ogg
    actors/
      caocao/
        idle_down_strip.png
        walk_right_strip.png
```

---

## 六、图片交付要求

为了避免我接入时反复返工，请你尽量满足这些：

### PNG 要求

- 透明背景是真的透明，不是棋盘底烘焙进图片里
- 不要带白边/黑边/半透明残边
- 不要把阴影烘得太死，尽量留后期空间

### 音频要求

- `wav` 一定是 PCM 或 IEEE float
- 如果不确定，优先给 `ogg`
- 文件名尽量英文，后面脚本引用更稳定

### 分辨率要求

- 同一角色不同表情，画布尺寸尽量一致
- 同一组 UI，尺寸风格尽量统一
- 同一批背景图，尽量统一 16:9

---

## 七、你给我素材后，我下一步会做什么

你把素材给齐后，我会按这个顺序继续：

1. 搭 `DialoguePlayer` 可复用场景
2. 建 `characters.json` 与首幕脚本 JSON
3. 接开始游戏按钮 -> 第一幕剧情
4. 做人物出场、切表情、逐字机、点击推进
5. 留好后续扩展口：移动、选择支、战斗切换

---

## 八、你现在最优先给我的素材

如果你想最快看到效果，先只给我下面这一组就够：

- `1` 张剧情背景图
- `曹操` 立绘 `3~5` 张表情
- `许子将` 立绘 `3~5` 张表情
- `1` 张对话框底板
- `1` 条 BGM

这 5 类素材到位，我就可以开始把**开始游戏后的第一段剧情场景**正式做出来。
