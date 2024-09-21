using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    public sealed class ReleaseValidatorUnbondings : ActionBase
    {
        public ReleaseValidatorUnbondings() { }

        public ReleaseValidatorUnbondings(Address validatorDelegatee)
        {
        }

        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(0L);

            var world = context.PreviousState;
            var repository = new ValidatorRepository(world, context);
            var unbondings = repository.GetUnbondingSet().UnbondingsToRelease(context.BlockIndex);

            foreach (var unbonding in unbondings)
            {
                unbonding.Release(context.BlockIndex);
            }

            return repository.World;
        }
    }
}
