using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemiesFeature;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Configs/Gameplay/Enemies/New EnemiesListConfig", fileName = "EnemiesListConfig")]
    public class EnemiesListConfig : ScriptableObject
    {
        [SerializeField] private List<EnemyConfig> _enemies;

        public IReadOnlyList<EnemyConfig> Enemies => _enemies;

        public EnemyConfig GetConfigFor(EnemyTypes type) => _enemies.Single(config => config.Type == type);
    }
}
