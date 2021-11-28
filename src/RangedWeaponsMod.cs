using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo( "RangedWeapons",
	Description = "Implements a more flexible ranged weapons system.",
	Website     = "https://github.com/Zarkonnen/VSRangedWeapons",
	Authors     = new []{ "Zarkonnen" } )]

namespace RangedWeapons
{
	public class RangedWeaponsMod : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			//api.RegisterBlockBehaviorClass(InstaTNTBehavior.NAME, typeof(InstaTNTBehavior));
			api.RegisterItemClass("ItemRangedWeapon", typeof(ItemRangedWeapon));
			api.RegisterEntity("EntityPoisonProjectile", typeof(EntityPoisonProjectile));
			api.RegisterEntityBehaviorClass("PoisonedBehavior", typeof(PoisonedBehavior));
		}
		
		public override void StartClientSide(ICoreClientAPI api)
		{
			
		}
		
		public override void StartServerSide(ICoreServerAPI api)
		{
			
		}
	}
}
