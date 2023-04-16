using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;

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
			api.RegisterItemClass("ItemPoisonArrow", typeof(ItemPoisonArrow));
			api.RegisterEntity("EntityPoisonProjectile", typeof(EntityPoisonProjectile));
			api.RegisterEntityBehaviorClass("poisonable", typeof(Poisonable));
			//api.Event.OnEntitySpawn += AppendEntityBehaviors;
		}

		// Would be a cool way to do it but doesn't work on already existing animals.
		/*
		public void AppendEntityBehaviors(Entity entity) {
			if (entity.Code.ToString().ToLower().Contains("drifter")) return;
			if (entity.Code.ToString().ToLower().Equals("bell")) return;
			if (entity.Code.ToString().ToLower().Contains("locust")) return;
			if (entity.Code.ToString().ToLower().Contains("dummy")) return;
			if (entity.Code.ToString().ToLower().Contains("dead")) return;
			entity.AddBehavior(new Poisonable(entity));
		}
		*/
		
		public override void StartClientSide(ICoreClientAPI api)
		{
			
		}
		
		public override void StartServerSide(ICoreServerAPI api)
		{
			
		}
	}
}
