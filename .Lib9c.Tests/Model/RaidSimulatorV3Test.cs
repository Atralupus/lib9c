namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class RaidSimulatorV3Test
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public RaidSimulatorV3Test()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();

            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            _avatarState.level = 250;
        }

        [Fact]
        public void Simulate()
        {
            var bossId = _tableSheets.WorldBossListSheet.First().Value.BossId;
            var simulator = new RaidSimulator(
                bossId,
                _random,
                _avatarState,
                new List<Guid>(),
                null,
                _tableSheets.GetRaidSimulatorSheets(),
                _tableSheets.CostumeStatSheet,
                new List<StatModifier>
                {
                    new (StatType.DEF, StatModifier.OperationType.Percentage, 100),
                });
            Assert.Equal(_random, simulator.Random);
            Assert.Equal(simulator.Player.Stats.BaseStats.DEF * 2, simulator.Player.Stats.DEF);
            Assert.Equal(simulator.Player.Stats.BaseStats.DEF, simulator.Player.Stats.CollectionStats.DEF);

            var log = simulator.Simulate();

            var turn = log.OfType<WaveTurnEnd>().Count();
            Assert.Equal(simulator.TurnNumber, turn);

            var expectedWaveCount = _tableSheets.WorldBossCharacterSheet[bossId].WaveStats.Count;
            Assert.Equal(expectedWaveCount, log.waveCount);

            var deadEvents = log.OfType<Dead>();
            foreach (var dead in deadEvents)
            {
                Assert.True(dead.Character.IsDead);
            }
        }
    }
}
