namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class CombinationEquipment4Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;
        private IAccountStateDelta _initialState;

        public CombinationEquipment4Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _agentAddress = default;
            _avatarAddress = _agentAddress.Derive("avatar");
            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            var sheets = TableSheetsImporter.ImportSheets();
            _random = new TestRandom();
            _tableSheets = new TableSheets(sheets);
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var gameConfigState = new GameConfigState();
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );
            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(
                    slotAddress,
                    new CombinationSlotState(
                        slotAddress,
                        GameConfig.RequireClearedStageLevel.CombinationEquipmentAction
                    ).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(_agentAddress, gold.Currency * 300);

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var row = _tableSheets.EquipmentItemRecipeSheet[109];
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            foreach (var materialInfo in _tableSheets.EquipmentItemSubRecipeSheet[255].Materials)
            {
                var subMaterial = ItemFactory.CreateItem(_tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                _avatarState.inventory.AddItem2(subMaterial, count: materialInfo.Count);
            }

            const int requiredStage = 21;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            var equipmentRow = _tableSheets.EquipmentItemSheet[row.ResultEquipmentId];
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, default, 0);

            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                subRecipeId = 255,
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                _avatarState.Update2(mail);
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment4()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SlotIndex = 0,
                SubRecipeId = 255,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);

            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);

            Assert.Equal(30, nextAvatarState.mailBox.Count);
            Assert.Equal(2, slotState.Result.itemUsable.GetOptionCount());

            var goldCurrencyState = nextState.GetGoldCurrency();
            var blackSmithGold = nextState.GetBalance(Addresses.Blacksmith, goldCurrencyState);
            var currency = new Currency("NCG", 2, minter: null);
            Assert.Equal(300 * currency, blackSmithGold);
            var agentGold = nextState.GetBalance(_agentAddress, goldCurrencyState);
            Assert.Equal(currency * 0, agentGold);
        }

        [Fact]
        public void ExecuteThrowInsufficientBalanceException()
        {
            var row = _tableSheets.EquipmentItemRecipeSheet[2];
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            foreach (var materialInfo in _tableSheets.EquipmentItemSubRecipeSheet[3].Materials)
            {
                var subMaterial = ItemFactory.CreateItem(_tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                _avatarState.inventory.AddItem2(subMaterial, count: materialInfo.Count);
            }

            const int requiredStage = 21;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment4()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SlotIndex = 0,
                SubRecipeId = 3,
            };

            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void SelectOption()
        {
            var options = new Dictionary<int, int>();
            var subRecipe = _tableSheets.EquipmentItemSubRecipeSheet[255];
            var equipment =
                (Necklace)ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet[10411000], default, 0);
            var i = 0;
            while (i < 10000)
            {
                var ids = CombinationEquipment4.SelectOption(
                    _tableSheets.EquipmentItemOptionSheet,
                    _tableSheets.SkillSheet,
                    subRecipe,
                    _random,
                    equipment
                );

                foreach (var id in ids)
                {
                    if (options.ContainsKey(id))
                    {
                        options[id] += 1;
                    }
                    else
                    {
                        options[id] = 1;
                    }
                }

                i++;
            }

            var optionIds = options
                .OrderByDescending(r => r.Value)
                .Select(r => r.Key)
                .ToArray();
            Assert.Equal(new[] { 932, 933, 934, 935 }, optionIds);
        }
    }
}
