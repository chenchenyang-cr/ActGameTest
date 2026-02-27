using System.Collections;
using System.Collections.Generic;
using UnityEngine;
	
 namespace CombatEditor
{	
	public class HitBox : MonoBehaviour
	{
        // 所有者控制器
        public CombatController Owner;
        
        // Transform绑定功能
        [Header("Transform Binding")]
        [Tooltip("要绑定的Transform名称，留空则使用自身Transform")]
        public string bindTransformName = "";
        [Tooltip("绑定的Transform对象，可通过名称自动搜索或手动指定")]
        public Transform bindTransform;
        [Tooltip("是否在初始化时自动搜索bindTransformName")]
        public bool autoSearchBindTransform = true;
  
        // 基础属性
        [Header("HitBox Properties")]
        public Vector3 hitBoxSize = Vector3.one; // 碰撞盒大小
        public Vector3 hitBoxOffset = Vector3.zero; // 碰撞盒偏移
        
        // 持续时间控制
        public float duration = 0.3f; // 持续时间（秒）
        private float currentLifetime = 0f;
        
        // 形状类型
        public enum HitBoxShape
        {
            Box,
            Sphere,
            Capsule
        }
        public HitBoxShape shape = HitBoxShape.Box;
        
        // 当形状为Sphere或Capsule时使用
        public float radius = 0.5f;
        public float height = 1f; // 仅用于Capsule
        
        // 判定标签，用于目标过滤
        public string[] hitTags = new string[] { "Player", "Enemy" };
        
        // 是否在命中后自动销毁
        public bool destroyOnHit = false;
        
        // 最大命中次数（0表示无限制）
        public int maxHits = 0;
        private int currentHits = 0;
        
        // 显示相关颜色（用于Gizmos渲染和运行时设置）
        public Color hitBoxColor = new Color(1f, 0f, 0f, 0.3f);
        
        // 已命中的目标缓存，防止重复命中
        private List<GameObject> hitTargets = new List<GameObject>();
        
        // 最后一次检测的时间戳
        private float lastDetectionTime = 0f;
        private float detectionInterval = 0.05f; // 检测间隔，秒
        
        // 初始化函数
        public void Init(CombatController controller)
        {
            Owner = controller;
            currentLifetime = 0f;
            hitTargets.Clear();
            lastDetectionTime = 0f;
            currentHits = 0;
            
            // 初始化Transform绑定
            InitializeBindTransform();
        }
        
        // 初始化Transform绑定
        private void InitializeBindTransform()
        {
            // 如果没有手动指定bindTransform且设置了自动搜索
            if (bindTransform == null && autoSearchBindTransform && !string.IsNullOrEmpty(bindTransformName))
            {
                SearchAndBindTransform(bindTransformName);
            }
            
            // 如果还是没有绑定的Transform，使用自身的Transform
            if (bindTransform == null)
            {
                bindTransform = this.transform;
                Debug.LogWarning($"HitBox {name}: 没有找到绑定的Transform，使用自身Transform");
            }
        }
        
        // 搜索并绑定Transform
        public void SearchAndBindTransform(string transformName)
        {
            if (string.IsNullOrEmpty(transformName))
            {
                Debug.LogWarning($"HitBox {name}: Transform名称为空");
                return;
            }
            
            // 首先在Owner的子对象中搜索
            if (Owner != null)
            {
                Transform found = FindTransformInChildren(Owner.transform, transformName);
                if (found != null)
                {
                    bindTransform = found;
                    Debug.Log($"HitBox {name}: 在Owner中找到并绑定Transform: {transformName}");
                    return;
                }
            }
            
            // 如果在Owner中没找到，在整个场景中搜索
            GameObject foundObj = GameObject.Find(transformName);
            if (foundObj != null)
            {
                bindTransform = foundObj.transform;
                Debug.Log($"HitBox {name}: 在场景中找到并绑定Transform: {transformName}");
            }
            else
            {
                Debug.LogWarning($"HitBox {name}: 未找到名为 '{transformName}' 的Transform");
            }
        }
        
        // 在子对象中递归搜索Transform
        private Transform FindTransformInChildren(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;
                
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindTransformInChildren(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }
            
            return null;
        }
        
        // 手动设置绑定的Transform
        public void SetBindTransform(Transform target)
        {
            bindTransform = target;
            if (target != null)
            {
                Debug.Log($"HitBox {name}: 手动绑定Transform: {target.name}");
            }
        }
        
        // 获取当前使用的Transform（用于位置和旋转计算）
        private Transform GetActiveTransform()
        {
            return bindTransform != null ? bindTransform : this.transform;
        }
        
        private void Update()
        {
            // 生命周期管理
            currentLifetime += Time.deltaTime;
            if (currentLifetime >= duration)
            {
                // 到达生命周期，销毁自身
                Destroy(gameObject);
                return;
            }
            
            // 间隔检测，优化性能
            if (Time.time - lastDetectionTime >= detectionInterval)
            {
                DetectHits();
                lastDetectionTime = Time.time;
            }
        }
        
        // 检测是否命中目标
        private void DetectHits()
        {
            // 如果已达到最大命中次数（且不为0），则不继续检测
            if (maxHits > 0 && currentHits >= maxHits)
            {
                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
                return;
            }
            
            // 获取所有可能的目标
            List<GameObject> possibleTargets = new List<GameObject>();
            
            // 对每个hitTag进行查找并合并结果
            foreach (string tag in hitTags)
            {
                try
                {
                    GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
                    possibleTargets.AddRange(taggedObjects);
                }
                catch (System.Exception)
                {
                    // 如果标签不存在，跳过
                    Debug.LogWarning($"Tag: {tag} is not defined. Please add it in Edit > Project Settings > Tags and Layers.");
                    continue;
                }
            }
            
            foreach (GameObject target in possibleTargets)
            {
                // 忽略自身和已命中的目标
                if (target == Owner.gameObject || hitTargets.Contains(target))
                    continue;
                
                // 获取目标的战斗控制器或者直接使用碰撞体
                CombatController targetController = target.GetComponent<CombatController>();
                
                // 如果没有CombatController，我们尝试检查碰撞体并创建一个临时控制器
                if (targetController == null)
                {
                    // 检查是否有任何碰撞体组件
                    Collider targetCollider = target.GetComponent<Collider>();
                    Collider2D targetCollider2D = target.GetComponent<Collider2D>();
                    
                    if (targetCollider == null && targetCollider2D == null)
                        continue; // 如果没有碰撞体，跳过
                        
                    // 创建一个临时的CombatController用于处理碰撞
                    GameObject tempControllerObj = new GameObject("TempController");
                    targetController = tempControllerObj.AddComponent<CombatController>();
                    tempControllerObj.transform.position = target.transform.position;
                    tempControllerObj.transform.rotation = target.transform.rotation;
                }
                
                // 标签过滤已经在查找时完成，不需要再次检查
                
                // 根据不同形状进行命中检测
                bool isHit = false;
                
                switch (shape)
                {
                    case HitBoxShape.Box:
                        isHit = CheckBoxHit(target);
                        break;
                    case HitBoxShape.Sphere:
                        isHit = CheckSphereHit(target);
                        break;
                    case HitBoxShape.Capsule:
                        isHit = CheckCapsuleHit(target);
                        break;
                }
                
                if (isHit)
                {
                    // 命中目标，执行命中逻辑
                    OnHitTarget(targetController);
                    hitTargets.Add(target);
                    currentHits++;
                    
                    // 如果设置了命中后销毁且已达到最大命中次数，则销毁
                    if (destroyOnHit && (maxHits <= 0 || currentHits >= maxHits))
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
                
                // 如果是临时创建的控制器，销毁它
                if (targetController != null && targetController.gameObject.name == "TempController")
                {
                    Destroy(targetController.gameObject);
                }
            }
        }
        
        // 盒形命中检测
        private bool CheckBoxHit(GameObject target)
        {
            // 使用绑定的Transform计算碰撞盒的世界坐标位置（考虑旋转）
            Transform activeTransform = GetActiveTransform();
            Vector3 boxCenter = activeTransform.position + activeTransform.rotation * hitBoxOffset;
            
            // 获取目标的碰撞体
            Collider targetCollider = target.GetComponent<Collider>();
            Collider2D targetCollider2D = target.GetComponent<Collider2D>();
            
            Vector3 targetPos;
            Bounds targetBounds;
            
            // 确定目标的位置和边界
            if (targetCollider != null)
            {
                targetPos = targetCollider.bounds.center;
                targetBounds = targetCollider.bounds;
            }
            else if (targetCollider2D != null)
            {
                targetPos = targetCollider2D.bounds.center;
                targetBounds = new Bounds(
                    targetCollider2D.bounds.center,
                    new Vector3(targetCollider2D.bounds.size.x, targetCollider2D.bounds.size.y, 1f)
                );
            }
            else
            {
                targetPos = target.transform.position;
                targetBounds = new Bounds(targetPos, Vector3.one * 0.1f); // 小的默认边界
            }
            
            // 使用OBB (Oriented Bounding Box) 碰撞检测
            return IsPointInOrientedBox(targetPos, boxCenter, hitBoxSize, activeTransform.rotation) ||
                   IsBoxIntersectingOrientedBox(targetBounds, boxCenter, hitBoxSize, activeTransform.rotation);
        }
        
        // 检查点是否在旋转的盒子内
        private bool IsPointInOrientedBox(Vector3 point, Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation)
        {
            // 将点转换到盒子的本地坐标系
            Vector3 localPoint = Quaternion.Inverse(boxRotation) * (point - boxCenter);
            
            // 检查点是否在本地坐标系的边界内
            Vector3 halfSize = boxSize * 0.5f;
            return Mathf.Abs(localPoint.x) <= halfSize.x &&
                   Mathf.Abs(localPoint.y) <= halfSize.y &&
                   Mathf.Abs(localPoint.z) <= halfSize.z;
        }
        
        // 检查轴对齐边界框是否与旋转的盒子相交
        private bool IsBoxIntersectingOrientedBox(Bounds aabb, Vector3 obbCenter, Vector3 obbSize, Quaternion obbRotation)
        {
            // 简化版本：检查AABB的8个顶点是否有任何一个在OBB内
            Vector3[] aabbCorners = new Vector3[8];
            Vector3 min = aabb.min;
            Vector3 max = aabb.max;
            
            aabbCorners[0] = new Vector3(min.x, min.y, min.z);
            aabbCorners[1] = new Vector3(max.x, min.y, min.z);
            aabbCorners[2] = new Vector3(min.x, max.y, min.z);
            aabbCorners[3] = new Vector3(max.x, max.y, min.z);
            aabbCorners[4] = new Vector3(min.x, min.y, max.z);
            aabbCorners[5] = new Vector3(max.x, min.y, max.z);
            aabbCorners[6] = new Vector3(min.x, max.y, max.z);
            aabbCorners[7] = new Vector3(max.x, max.y, max.z);
            
            // 检查是否有任何顶点在OBB内
            for (int i = 0; i < 8; i++)
            {
                if (IsPointInOrientedBox(aabbCorners[i], obbCenter, obbSize, obbRotation))
                {
                    return true;
                }
            }
            
            // 反向检查：OBB的顶点是否在AABB内
            Vector3[] obbCorners = GetOrientedBoxCorners(obbCenter, obbSize, obbRotation);
            for (int i = 0; i < 8; i++)
            {
                if (aabb.Contains(obbCorners[i]))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        // 获取旋转盒子的8个顶点
        private Vector3[] GetOrientedBoxCorners(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Vector3[] corners = new Vector3[8];
            Vector3 halfSize = size * 0.5f;
            
            corners[0] = center + rotation * new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            corners[1] = center + rotation * new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + rotation * new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[3] = center + rotation * new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[4] = center + rotation * new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[5] = center + rotation * new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[6] = center + rotation * new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            corners[7] = center + rotation * new Vector3(halfSize.x, halfSize.y, halfSize.z);
            
            return corners;
        }
        
        // 球形命中检测
        private bool CheckSphereHit(GameObject target)
        {
            Transform activeTransform = GetActiveTransform();
            Vector3 sphereCenter = activeTransform.position + activeTransform.rotation * hitBoxOffset;
            
            // 获取目标的碰撞体
            Collider targetCollider = target.GetComponent<Collider>();
            Collider2D targetCollider2D = target.GetComponent<Collider2D>();
            
            // 如果目标有3D碰撞体
            if (targetCollider != null)
            {
                // 使用球体与碰撞体边界的检测
                Vector3 closestPoint = targetCollider.bounds.ClosestPoint(sphereCenter);
                float sqrDist = (closestPoint - sphereCenter).sqrMagnitude;
                return sqrDist <= radius * radius;
            }
            // 如果目标有2D碰撞体
            else if (targetCollider2D != null)
            {
                // 将2D碰撞体转换为3D检测
                Vector2 closestPoint2D = targetCollider2D.bounds.ClosestPoint((Vector2)sphereCenter);
                Vector3 closestPoint = new Vector3(closestPoint2D.x, closestPoint2D.y, sphereCenter.z);
                float sqrDist = (closestPoint - sphereCenter).sqrMagnitude;
                return sqrDist <= radius * radius;
            }
            else
            {
                // 简单的球体-点距离检测
                Vector3 targetPos = target.transform.position;
                float sqrDist = (targetPos - sphereCenter).sqrMagnitude;
                return sqrDist <= radius * radius;
            }
        }
        
        // 胶囊体命中检测
        private bool CheckCapsuleHit(GameObject target)
        {
            Transform activeTransform = GetActiveTransform();
            Vector3 capsuleCenter = activeTransform.position + activeTransform.rotation * hitBoxOffset;
            
            // 计算胶囊体的方向（应用旋转）
            Vector3 capsuleUp = activeTransform.rotation * Vector3.up;
            
            // 计算胶囊体两端的位置（考虑旋转）
            Vector3 point1 = capsuleCenter + capsuleUp * (height * 0.5f - radius);
            Vector3 point2 = capsuleCenter - capsuleUp * (height * 0.5f - radius);
            
            // 获取目标的碰撞体
            Collider targetCollider = target.GetComponent<Collider>();
            Collider2D targetCollider2D = target.GetComponent<Collider2D>();
            
            Vector3 targetPos;
            if (targetCollider != null)
            {
                targetPos = targetCollider.bounds.center;
            }
            else if (targetCollider2D != null)
            {
                targetPos = targetCollider2D.bounds.center;
            }
            else
            {
                targetPos = target.transform.position;
            }
            
            // 计算点到胶囊体轴线的最短距离
            Vector3 lineVec = point2 - point1;
            Vector3 pointVec = targetPos - point1;
            
            float t = Vector3.Dot(pointVec, lineVec) / Vector3.Dot(lineVec, lineVec);
            t = Mathf.Clamp01(t);
            
            Vector3 nearestPoint = point1 + t * lineVec;
            float sqrDist = (targetPos - nearestPoint).sqrMagnitude;
            
            return sqrDist <= radius * radius;
        }
        
        // 命中目标的回调
        private void OnHitTarget(CombatController targetController)
        {
            // 通知所有者控制器，已命中目标
            if (Owner != null)
            {
                // 先重置条件，确保每次命中都能触发事件
                Owner.SetHitTargetCondition(false, null);
                Owner.SetHitCheckedCondition(false);
                targetController.SetBeenHitCondition(false, null);
                
                // 设置自己的击中目标条件为true，传递目标控制器
                Owner.SetHitTargetCondition(true, targetController);
                
                // 通知目标控制器，已被击中，传递攻击者控制器
                targetController.SetBeenHitCondition(true, Owner);
                
                // 设置命中判定条件
                Owner.SetHitCheckedCondition(true);
                
                // 使用协程在短时间后重置条件，确保条件判定完成后重置
                StartCoroutine(ResetConditionsAfterDelay(Owner, targetController));
            }
            
            // 这里可以添加其他命中效果，如伤害计算等
            Debug.Log("HitBox命中目标: " + targetController.name);
        }
        
        // 延迟重置条件的协程
        private System.Collections.IEnumerator ResetConditionsAfterDelay(CombatController owner, CombatController target)
        {
            // 等待一小段时间，确保事件系统有时间处理条件
            yield return new WaitForSeconds(0.05f);
            
            // 重置所有者的条件
            if (owner != null)
            {
                owner.SetHitTargetCondition(false, null);
                owner.SetHitCheckedCondition(false);
            }
            
            // 不再在这里直接重置目标的被击中条件
            // 而是通知目标控制器延迟重置自己的被击中条件
            if (target != null)
            {
                target.DelayedResetBeenHitCondition(0.1f);  // 使用延迟重置机制
            }
        }
        
#if UNITY_EDITOR
        // 在编辑器中可视化HitBox
        private void OnDrawGizmos()
        {
            // 只在播放模式时显示实际HitBox的Gizmos，避免与Preview重复
            if (!UnityEditor.EditorApplication.isPlaying)
                return;
                
            // 使用绑定的Transform进行绘制
            Transform activeTransform = GetActiveTransform();
            if (activeTransform == null) return;
            
            // 只在编辑器内显示Gizmos
            Gizmos.color = hitBoxColor;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(activeTransform.position, activeTransform.rotation, activeTransform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            
            // 只显示红色hitbox (不再显示绿色部分)
            switch (shape)
            {
                case HitBoxShape.Box:
                    Gizmos.DrawCube(hitBoxOffset, hitBoxSize);
                    break;
                case HitBoxShape.Sphere:
                    Gizmos.DrawSphere(hitBoxOffset, radius);
                    break;
                case HitBoxShape.Capsule:
                    // 绘制胶囊体的可视化
                    Vector3 top = hitBoxOffset + Vector3.up * (height * 0.5f - radius);
                    Vector3 bottom = hitBoxOffset - Vector3.up * (height * 0.5f - radius);
                    
                    Gizmos.DrawSphere(top, radius);
                    Gizmos.DrawSphere(bottom, radius);
                    
                    // 绘制圆柱体部分（使用线段近似）
                    const int segments = 12;
                    float angleStep = 360f / segments;
                    
                    for (int i = 0; i < segments; i++)
                    {
                        float angle1 = i * angleStep * Mathf.Deg2Rad;
                        float angle2 = ((i + 1) % segments) * angleStep * Mathf.Deg2Rad;
                        
                        Vector3 point1Top = top + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
                        Vector3 point2Top = top + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;
                        
                        Vector3 point1Bottom = bottom + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
                        Vector3 point2Bottom = bottom + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;
                        
                        Gizmos.DrawLine(point1Top, point2Top);
                        Gizmos.DrawLine(point1Bottom, point2Bottom);
                        Gizmos.DrawLine(point1Top, point1Bottom);
                    }
                    break;
            }
        }
#endif
    }
}
