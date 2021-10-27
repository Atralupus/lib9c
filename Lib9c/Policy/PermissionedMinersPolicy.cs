using System.Collections.Immutable;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class PermissionedMinersPolicy : VariableSubPolicy<ImmutableHashSet<Address>>
    {
        private PermissionedMinersPolicy(ImmutableHashSet<Address> defaultValue)
            : base(defaultValue)
        {
        }

        private PermissionedMinersPolicy(
            PermissionedMinersPolicy permissionedMinersPolicy,
            SpannedSubPolicy<ImmutableHashSet<Address>> spannedSubPolicy)
            : base(permissionedMinersPolicy, spannedSubPolicy)
        {
        }

        public static PermissionedMinersPolicy Default =>
            new PermissionedMinersPolicy(ImmutableHashSet<Address>.Empty);

        public static PermissionedMinersPolicy Mainnet =>
            Default
                .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                    startIndex: BlockPolicySource.PermissionedMiningStartIndex,
                    endIndex: null,
                    predicate: null,
                    value: BlockPolicySource.AuthorizedMiners));
    }
}
