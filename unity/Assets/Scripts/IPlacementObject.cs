using UnityEngine;

namespace RunGame
{
    public enum PlacementObjectType
    {
        None,
        SafeObject,     // 安全なオブジェクト（アイテムなど）
        DamageObject,   // ダメージを与えるオブジェクト（敵、障害物）
    }

    /// <summary>
    /// 配置物のインターフェース
    /// BDD仕様: spec/ref/IPlacementObject.cs
    /// </summary>
    public interface IPlacementObject
    {
        PlacementObjectType ObjType { get; }
        
        /// <summary>
        /// オブジェクトの位置を取得
        /// </summary>
        Vector3 GetPosition();
        
        /// <summary>
        /// 接触時のアクション
        /// </summary>
        void Action();
    }
}