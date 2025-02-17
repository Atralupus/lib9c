namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Xunit;

    public class BattleLogTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;

        public BattleLogTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();
        }

        [Fact]
        public void IsClearBeforeSimulate()
        {
            var agentState = new AgentState(default(Address));
            var avatarState = new AvatarState(
                default,
                agentState.address,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            var simulator = new StageSimulator(
                _random,
                avatarState,
                new List<Guid>(),
                1,
                1,
                _tableSheets.GetStageSimulatorSheets(),
                2,
                1);
            Assert.False(simulator.Log.IsClear);
        }

        [Theory]
        [InlineData(true, 3)]
        [InlineData(false, 1)]
        public void IsClear(bool expected, int wave)
        {
            var log = new BattleLog()
            {
                result = BattleLog.Result.Win,
                clearedWaveNumber = wave,
                waveCount = 3,
            };
            Assert.Equal(expected, log.IsClear);
        }
    }
}
