using Assets._Project.Develop.Runtime.Gameplay.Features.EnemiesFeature;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Configs/Gameplay/Enemies/New EnemyConfig", fileName = "EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        [SerializeField] private EnemyTypes _type;
        [SerializeField, Min(0)] private float _moveSpeed = 4;
        [SerializeField, Min(0)] private float _rotationSpeed = 360;
        [SerializeField] private string _prefabPath;

        public EnemyTypes Type => _type;
        public float MoveSpeed => _moveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public string PrefabPath => _prefabPath;
    }
}
