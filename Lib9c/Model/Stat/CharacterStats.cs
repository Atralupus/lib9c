using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Stat
{
    /// <summary>
    /// 캐릭터의 스탯을 관리한다.
    /// 스탯은 레벨에 의한 _levelStats를 기본으로 하고
    /// > 장비에 의한 _equipmentStats
    /// > 소모품에 의한 _consumableStats
    /// > 버프에 의한 _buffStats
    /// > 옵션에 의한 _optionalStats
    /// 마지막으로 모든 스탯을 합한 CharacterStats 순서로 계산한다.
    /// </summary>
    [Serializable]
    public class CharacterStats : Stats, IBaseAndAdditionalStats, ICloneable
    {
        private readonly CharacterSheet.Row _row;

        private readonly Stats _levelStats = new Stats();
        private readonly Stats _equipmentStats = new Stats();
        private readonly Stats _consumableStats = new Stats();
        private readonly Stats _buffStats = new Stats();
        private readonly Stats _optionalStats = new Stats();

        private readonly List<StatModifier> _equipmentStatModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _consumableStatModifiers = new List<StatModifier>();
        private readonly Dictionary<int, StatModifier> _buffStatModifiers = new Dictionary<int, StatModifier>();
        private readonly List<StatModifier> _optionalStatModifiers = new List<StatModifier>();

        public int Level { get; private set; }

        public IStats LevelStats => _levelStats;
        public IStats EquipmentStats => _equipmentStats;
        public IStats ConsumableStats => _consumableStats;
        public IStats BuffStats => _buffStats;
        public IStats OptionalStats => _optionalStats;


        public int BaseHP => LevelStats.HP;
        public int BaseATK => LevelStats.ATK;
        public int BaseDEF => LevelStats.DEF;
        public int BaseCRI => LevelStats.CRI;
        public int BaseHIT => LevelStats.HIT;
        public int BaseSPD => LevelStats.SPD;

        public bool HasBaseHP => LevelStats.HasHP;
        public bool HasBaseATK => LevelStats.HasATK;
        public bool HasBaseDEF => LevelStats.HasDEF;
        public bool HasBaseCRI => LevelStats.HasCRI;
        public bool HasBaseHIT => LevelStats.HasHIT;
        public bool HasBaseSPD => LevelStats.HasSPD;

        public int AdditionalHP => HP - _levelStats.HP;
        public int AdditionalATK => ATK - _levelStats.ATK;
        public int AdditionalDEF => DEF - _levelStats.DEF;
        public int AdditionalCRI => CRI - _levelStats.CRI;
        public int AdditionalHIT => HIT - _levelStats.HIT;
        public int AdditionalSPD => SPD - _levelStats.SPD;

        public bool HasAdditionalHP => AdditionalHP > 0;
        public bool HasAdditionalATK => AdditionalATK > 0;
        public bool HasAdditionalDEF => AdditionalDEF > 0;
        public bool HasAdditionalCRI => AdditionalCRI > 0;
        public bool HasAdditionalHIT => AdditionalHIT > 0;
        public bool HasAdditionalSPD => AdditionalSPD > 0;

        public bool HasAdditionalStats => HasAdditionalHP || HasAdditionalATK || HasAdditionalDEF || HasAdditionalCRI ||
                                          HasAdditionalHIT || HasAdditionalSPD;

        public CharacterStats(
            CharacterSheet.Row row,
            int level
        )
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
            SetLevel(level);
            EqualizeCurrentHPWithHP();
        }

        public CharacterStats(CharacterStats value) : base(value)
        {
            _row = value._row;

            _levelStats = new Stats(value._levelStats);
            _equipmentStats = new Stats(value._equipmentStats);
            _consumableStats = new Stats(value._consumableStats);
            _buffStats = new Stats(value._buffStats);
            _optionalStats = new Stats(value._optionalStats);

            _equipmentStatModifiers = value._equipmentStatModifiers;
            _consumableStatModifiers = value._consumableStatModifiers;
            _buffStatModifiers = value._buffStatModifiers;
            _optionalStatModifiers = value._optionalStatModifiers;

            Level = value.Level;
        }

        public CharacterStats SetAll(
            int level,
            IEnumerable<Equipment> equipments,
            IEnumerable<Costume> costumes,
            IEnumerable<Consumable> consumables,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet)
        {
            SetLevel(level, false);
            SetEquipments(equipments, equipmentItemSetEffectSheet, false);
            SetCostumes(costumes, costumeStatSheet);
            SetConsumables(consumables, false);
            UpdateLevelStats();
            EqualizeCurrentHPWithHP();

            return this;
        }

        /// <summary>
        /// 레벨을 설정하고, 생성자에서 받은 캐릭터 정보와 레벨을 바탕으로 모든 스탯을 재설정한다.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetLevel(int level, bool updateImmediate = true)
        {
            if (level == Level)
                return this;

            Level = level;

            if (updateImmediate)
            {
                UpdateLevelStats();
            }

            return this;
        }

        /// <summary>
        /// 장비들을 바탕으로 장비 스탯을 재설정한다. 또한 소모품 스탯과 버프 스탯을 다시 계산한다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetEquipments(
            IEnumerable<Equipment> value,
            EquipmentItemSetEffectSheet sheet,
            bool updateImmediate = true
        )
        {
            _equipmentStatModifiers.Clear();
            if (!(value is null))
            {
                foreach (var equipment in value)
                {
                    var statMap = equipment.StatsMap;
                    if (statMap.HasHP)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Add,
                            statMap.HP));
                    }

                    if (statMap.HasATK)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Add,
                            statMap.ATK));
                    }

                    if (statMap.HasDEF)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.DEF, StatModifier.OperationType.Add,
                            statMap.DEF));
                    }

                    if (statMap.HasCRI)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.CRI, StatModifier.OperationType.Add,
                            statMap.CRI));
                    }

                    if (statMap.HasHIT)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.HIT, StatModifier.OperationType.Add,
                            statMap.HIT));
                    }

                    if (statMap.HasSPD)
                    {
                        _equipmentStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Add,
                            statMap.SPD));
                    }
                }

                // set effects.
                var setEffectRows = sheet.GetSetEffectRows(value);
                foreach (var statModifier in setEffectRows.SelectMany(row => row.StatModifiers.Values))
                {
                    _equipmentStatModifiers.Add(statModifier);
                }
            }

            if (updateImmediate)
            {
                UpdateEquipmentStats();
            }

            return this;
        }

        /// <summary>
        /// 소모품들을 바탕으로 소모품 스탯을 재설정한다. 또한 버프 스탯을 다시 계산한다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetConsumables(IEnumerable<Consumable> value, bool updateImmediate = true)
        {
            _consumableStatModifiers.Clear();
            if (!(value is null))
            {
                foreach (var consumable in value)
                {
                    var statMap = consumable.StatsMap;
                    if (statMap.HasHP)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Add,
                            statMap.HP));
                    }

                    if (statMap.HasATK)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Add,
                            statMap.ATK));
                    }

                    if (statMap.HasDEF)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.DEF, StatModifier.OperationType.Add,
                            statMap.DEF));
                    }

                    if (statMap.HasCRI)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.CRI, StatModifier.OperationType.Add,
                            statMap.CRI));
                    }

                    if (statMap.HasHIT)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.HIT, StatModifier.OperationType.Add,
                            statMap.HIT));
                    }

                    if (statMap.HasSPD)
                    {
                        _consumableStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Add,
                            statMap.SPD));
                    }
                }
            }

            if (updateImmediate)
            {
                UpdateConsumableStats();
            }

            return this;
        }

        /// <summary>
        /// 버프들을 바탕으로 버프 스탯을 재설정한다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetBuffs(IEnumerable<Buff.Buff> value, bool updateImmediate = true)
        {
            _buffStatModifiers.Clear();
            if (!(value is null))
            {
                foreach (var buff in value)
                {
                    AddBuff(buff, false);
                }
            }

            if (updateImmediate)
            {
                UpdateBuffStats();
            }

            return this;
        }

        public void AddBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            _buffStatModifiers[buff.RowData.GroupId] = buff.RowData.StatModifier;

            if (updateImmediate)
            {
                UpdateBuffStats();
            }
        }

        public void RemoveBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            if (!_buffStatModifiers.ContainsKey(buff.RowData.GroupId))
                return;

            _buffStatModifiers.Remove(buff.RowData.GroupId);

            if (updateImmediate)
            {
                UpdateBuffStats();
            }
        }

        public void AddOption(IEnumerable<StatModifier> statModifiers)
        {
            _optionalStatModifiers.AddRange(statModifiers);
            UpdateOptionalStats();
        }

        private void SetCostumes(IEnumerable<Costume> costumes, CostumeStatSheet costumeStatSheet)
        {
            var statModifiers = new List<StatModifier>();
            foreach (var costume in costumes)
            {
                var stat = costumeStatSheet.OrderedList
                    .Where(r => r.CostumeId == costume.Id)
                    .Select(row => new StatModifier(row.StatType, StatModifier.OperationType.Add,
                        (int)row.Stat));
                statModifiers.AddRange(stat);
            }
            SetOption(statModifiers);
        }

        public void SetOption(IEnumerable<StatModifier> statModifiers)
        {
            _optionalStatModifiers.Clear();
            AddOption(statModifiers);
        }

        private void UpdateLevelStats()
        {
            var statsData = _row.ToStats(Level);
            _levelStats.Set(statsData);
            UpdateEquipmentStats();
        }

        private void UpdateEquipmentStats()
        {
            _equipmentStats.Set(_equipmentStatModifiers, _levelStats);
            UpdateConsumableStats();
        }

        private void UpdateConsumableStats()
        {
            _consumableStats.Set(_consumableStatModifiers, _levelStats, _equipmentStats);
            UpdateBuffStats();
        }

        private void UpdateBuffStats()
        {
            _buffStats.Set(_buffStatModifiers.Values, _levelStats, _equipmentStats, _consumableStats);
            UpdateOptionalStats();
        }

        private void UpdateOptionalStats()
        {
            _optionalStats.Set(_optionalStatModifiers, _levelStats, _equipmentStats, _consumableStats, _buffStats);
            UpdateTotalStats();
        }

        private void UpdateTotalStats()
        {
            Set(_levelStats, _equipmentStats, _consumableStats, _buffStats, _optionalStats);
            // 최소값 보정
            hp.SetValue(Math.Max(0, hp.Value));
            atk.SetValue(Math.Max(0, atk.Value));
            def.SetValue(Math.Max(0, def.Value));
            cri.SetValue(Math.Max(0, cri.Value));
            hit.SetValue(Math.Max(0, hit.Value));
            spd.SetValue(Math.Max(0, spd.Value));
        }

        public override object Clone()
        {
            return new CharacterStats(this);
        }

        public IEnumerable<(StatType statType, int baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            if (ignoreZero)
            {
                if (HasBaseHP)
                    yield return (StatType.HP, BaseHP);
                if (HasBaseATK)
                    yield return (StatType.ATK, BaseATK);
                if (HasBaseDEF)
                    yield return (StatType.DEF, BaseDEF);
                if (HasBaseCRI)
                    yield return (StatType.CRI, BaseCRI);
                if (HasBaseHIT)
                    yield return (StatType.HIT, BaseHIT);
                if (HasBaseSPD)
                    yield return (StatType.SPD, BaseSPD);
            }
            else
            {
                yield return (StatType.HP, BaseHP);
                yield return (StatType.ATK, BaseATK);
                yield return (StatType.DEF, BaseDEF);
                yield return (StatType.CRI, BaseCRI);
                yield return (StatType.HIT, BaseHIT);
                yield return (StatType.SPD, BaseSPD);
            }
        }

        public IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            if (ignoreZero)
            {
                if (HasAdditionalHP)
                    yield return (StatType.HP, AdditionalHP);
                if (HasAdditionalATK)
                    yield return (StatType.ATK, AdditionalATK);
                if (HasAdditionalDEF)
                    yield return (StatType.DEF, AdditionalDEF);
                if (HasAdditionalCRI)
                    yield return (StatType.CRI, AdditionalCRI);
                if (HasAdditionalHIT)
                    yield return (StatType.HIT, AdditionalHIT);
                if (HasAdditionalSPD)
                    yield return (StatType.SPD, AdditionalSPD);
            }
            else
            {
                yield return (StatType.HP, AdditionalHP);
                yield return (StatType.ATK, AdditionalATK);
                yield return (StatType.DEF, AdditionalDEF);
                yield return (StatType.CRI, AdditionalCRI);
                yield return (StatType.HIT, AdditionalHIT);
                yield return (StatType.SPD, AdditionalSPD);
            }
        }

        public IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            if (ignoreZero)
            {
                if (HasBaseHP || HasAdditionalHP)
                    yield return (StatType.HP, BaseHP, AdditionalHP);
                if (HasBaseATK || HasAdditionalATK)
                    yield return (StatType.ATK, BaseATK, AdditionalATK);
                if (HasBaseDEF || HasAdditionalDEF)
                    yield return (StatType.DEF, BaseDEF, AdditionalDEF);
                if (HasBaseCRI || HasAdditionalCRI)
                    yield return (StatType.CRI, BaseCRI, AdditionalCRI);
                if (HasBaseHIT || HasAdditionalHIT)
                    yield return (StatType.HIT, BaseHIT, AdditionalHIT);
                if (HasBaseSPD || HasAdditionalSPD)
                    yield return (StatType.SPD, BaseSPD, AdditionalSPD);
            }
            else
            {
                yield return (StatType.HP, BaseHP, AdditionalHP);
                yield return (StatType.ATK, BaseATK, AdditionalATK);
                yield return (StatType.DEF, BaseDEF, AdditionalDEF);
                yield return (StatType.CRI, BaseCRI, AdditionalCRI);
                yield return (StatType.HIT, BaseHIT, AdditionalHIT);
                yield return (StatType.SPD, BaseSPD, AdditionalSPD);
            }
        }
    }
}
