using UnityEngine;
using Assets._Project.Develop.Runtime.Meta.Progress;

namespace Assets._Project.Develop.Runtime.Meta.Configs
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Configs/Economy")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [SerializeField] private int _initialGold = 100;
        [SerializeField] private int _winReward = 10;
        [SerializeField] private int _lossPenalty = 5;
        [SerializeField] private int _statisticsResetCost = 25;

        public EconomyRules Rules => new EconomyRules(
            _initialGold,
            _winReward,
            _lossPenalty,
            _statisticsResetCost);
    }
}
