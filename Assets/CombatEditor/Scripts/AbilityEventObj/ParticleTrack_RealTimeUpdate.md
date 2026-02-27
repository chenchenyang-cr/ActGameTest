# 粒子轨道实时更新功能说明

## 🔄 问题解决

### 原始问题
用户报告：在编辑器模式下调整特效的transform时，场景中的特效没有立即反映变化，需要再次播放才能看到改变后的效果。

### 解决方案
我们实现了一套完整的实时更新系统，确保在编辑器模式下对粒子轨道配置的任何修改都能立即在场景中反映出来。

## 🛠️ 技术实现

### 1. 配置变化检测 (`CheckAndUpdateConfiguration`)

在每帧的预览更新中，系统会检查以下配置是否发生变化：

```csharp
// 节点绑定变化检测
Transform newTargetTransform = GetCurrentTargetTransform();
if (newTargetTransform != targetTransform)
{
    targetTransform = newTargetTransform;
    UpdateNodeBinding(); // 重新绑定节点
}

// NodeFollower配置更新
if (nodeFollower != null)
{
    UpdateNodeFollowerConfiguration();
}

// 粒子系统配置更新
UpdateParticleSystemConfiguration();
```

### 2. 实时Transform更新

#### NodeFollower模式
- 当启用`followNodeMovement`时，使用NodeFollower组件
- 实时重新初始化NodeFollower以应用最新的偏移和旋转设置
- 支持位置偏移和旋转偏移的实时更新

#### 静态Transform模式
- 当禁用`followNodeMovement`时，使用静态位置计算
- 每帧重新计算世界空间偏移：`worldOffset = targetRotation * localOffset`
- 实时应用位置和旋转变换

### 3. 编辑器集成

#### GUI变化响应
```csharp
if (GUI.changed && !Application.isPlaying)
{
    UpdatePreview();
    Repaint(); // 立即重绘Inspector
}
```

#### 强制预览刷新
```csharp
var editor = CombatEditorUtility.GetCurrentEditor();
if (editor != null && editor._previewer != null)
{
    editor.HardResetPreviewToCurrentFrame(); // 强制重置当前帧预览
}
```

#### 编辑器Update监听
- 通过`EditorApplication.update`监听编辑器更新
- 限制更新频率为0.1秒，避免过度更新
- 只在相关轨道被选中时进行场景重绘

### 4. Scene视图交互 (`OnSceneGUI`)

#### 可视化辅助
- 绘制从绑定节点到粒子位置的连接线
- 显示当前的位置偏移值
- 提供实时的视觉反馈

#### 交互式位置调整
```csharp
// 在Scene视图中提供位置手柄
Vector3 newOffsetPosition = Handles.PositionHandle(offsetPosition, targetRotation);

if (EditorGUI.EndChangeCheck())
{
    // 计算新的本地偏移
    Vector3 newLocalOffset = CalculateLocalOffset(newOffsetPosition);
    
    // 记录撤销操作
    Undo.RecordObject(particleTrack, "修改粒子位置偏移");
    
    // 应用变化并立即更新预览
    particleTrack.particleData.positionOffset = newLocalOffset;
    UpdatePreview();
}
```

## 🎯 实时更新的内容

### 位置和旋转
- ✅ **位置偏移** (`positionOffset`) - 立即应用到粒子位置
- ✅ **旋转偏移** (`rotationOffset`) - 立即应用到粒子旋转
- ✅ **节点绑定** (`targetNode`) - 立即切换绑定目标
- ✅ **跟随设置** (`followNodeMovement`, `followNodeRotation`) - 立即切换跟随模式

### 粒子系统配置
- ✅ **播放速度** (`playbackSpeed`) - 立即更新simulation speed
- ✅ **循环设置** (`loopParticles`) - 立即更新loop模式
- ✅ **强度曲线** (`useIntensityCurve`, `intensityCurve`) - 立即应用发射率变化
- ✅ **强度倍率** (`maxIntensityMultiplier`) - 立即调整发射强度

### 高级设置
- ✅ **自定义目标** (`useCustomTarget`, `customTargetName`) - 立即切换绑定目标
- ✅ **自定义父级** (`customParent`) - 立即更新父子关系

## 🔧 性能优化

### 更新频率控制
```csharp
// 限制编辑器更新频率
if (EditorApplication.timeSinceStartup - lastUpdateTime < 0.1f) return;

// 只在轨道被选中时更新
if (isCurrentEventSelected)
{
    SceneView.RepaintAll();
}
```

### 条件性更新
- 只在配置实际发生变化时才进行更新
- 只在编辑器模式下启用实时更新
- 只在当前轨道被选中时进行频繁的Scene视图重绘

### 避免重复计算
- 缓存计算结果，避免每帧重复计算相同的值
- 使用条件检查，只在必要时执行昂贵的操作

## 📱 用户体验改进

### 立即反馈
- 修改任何配置后，场景中的粒子效果立即更新
- 无需重新播放或重新加载预览
- 支持撤销/重做操作

### 可视化辅助
- Scene视图中显示粒子偏移的连接线和位置手柄
- 实时显示当前的偏移数值
- 直观的拖拽调整功能

### 智能更新
- 只在真正需要时进行更新，避免不必要的性能开销
- 保持编辑器的响应性
- 与CombatEditor的现有预览系统完美集成

## 🧪 测试场景

### 基本功能测试
1. **位置偏移调整**：在Inspector中修改positionOffset，观察粒子位置立即变化
2. **旋转偏移调整**：修改rotationOffset，观察粒子朝向立即变化
3. **节点切换**：切换targetNode，观察粒子立即移动到新节点
4. **跟随模式切换**：开关followNodeMovement，观察粒子行为立即改变

### 高级功能测试
1. **强度曲线调整**：修改intensityCurve，观察粒子发射率立即变化
2. **播放速度调整**：修改playbackSpeed，观察粒子播放速度立即变化
3. **Scene视图交互**：在Scene视图中拖拽位置手柄，观察实时更新

### 性能测试
1. **频繁修改测试**：连续快速修改配置，确保编辑器保持响应
2. **多轨道测试**：同时存在多个粒子轨道时的更新性能
3. **复杂粒子系统测试**：使用包含多个ParticleSystem的复杂预制体

## 💡 使用技巧

### 高效调试
1. **启用调试日志**：通过`enableDebugLog`查看详细的更新信息
2. **使用Scene视图手柄**：直接在Scene视图中拖拽调整位置
3. **组合快捷键**：使用Ctrl+Z撤销不满意的调整

### 最佳实践
1. **适度使用实时更新**：虽然系统优化了性能，但避免在复杂场景中频繁调整
2. **利用可视化辅助**：使用连接线和手柄来精确调整位置
3. **分步调整**：先调整大致位置，再进行精细调整

---

💡 **总结**：新的实时更新系统完全解决了配置修改后需要重新播放才能看到效果的问题，为粒子特效的制作提供了流畅、直观的编辑体验。 