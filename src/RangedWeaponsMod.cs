using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo( "RangedWeapons",
	Description = "Implements a more flexible ranged weapons system.",
	Website     = "https://github.com/copygirl/howto-example-mod",
	Authors     = new []{ "Zarkonnen" } )]

namespace RangedWeapons
{
	public class RangedWeaponsMod : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			//api.RegisterBlockBehaviorClass(InstaTNTBehavior.NAME, typeof(InstaTNTBehavior));
			api.RegisterItemClass("ItemRangedWeapon", typeof(ItemRangedWeapon));
		}
		
		public override void StartClientSide(ICoreClientAPI api)
		{
			
		}
		
		public override void StartServerSide(ICoreServerAPI api)
		{
			
		}
	}
}