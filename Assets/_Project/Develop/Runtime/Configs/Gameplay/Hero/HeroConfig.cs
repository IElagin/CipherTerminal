using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Hero
{
    [CreateAssetMenu(menuName = "Configs/Gameplay/Hero/New HeroConfig", fileName = "HeroConfig")]
    public class HeroConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private float _moveSpeed = 4;
        [SerializeField, Min(0)] private float _rotationSpeed = 360;
        [SerializeField] private string _prefabPath;

        public float MoveSpeed => _moveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public string PrefabPath => _prefabPath;
    }
}
