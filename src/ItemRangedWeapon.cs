using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RangedWeapons
{
    public class ItemRangedWeapon : Item
    {
        public static SimpleParticleProperties fireBlast;
        
        public static SimpleParticleProperties fireSmoke;
        
        static ItemRangedWeapon()
        {
            fireBlast = new SimpleParticleProperties(
                    10,
                    15,
                    ColorUtil.ToRgba(255, 250, 100, 50),
                    //ColorUtil.ColorFromRgba(175, 222, 255, 50),
                    new Vec3d(),
                    new Vec3d(),
                    new Vec3f(-4f, -4f, -4f),
                    new Vec3f(4f, 4f, 4f),
                    0.1f,
                    0.3f,
                    1.1f, 1.3f);
            // Evolution is scaled for the lifetime of the particle. I think.
            fireBlast.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f);
            fireBlast.GreenEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -100f);
            fireBlast.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -50f); // And by red we mean blue because BGR.
            fireBlast.VertexFlags = 128; // Glow

            fireSmoke = new SimpleParticleProperties(
                   9, 11,
                   ColorUtil.ToRgba(150, 80, 80, 80),
                   new Vec3d(-0.4f, -0.4f, -0.4f),
                   new Vec3d(0.4f, 0.4f, 0.4f),
                   new Vec3f(-1 / 8f, 0.01f, -1 / 8f),
                   new Vec3f(1 / 8f, 0.3f, 1 / 8f),
                   2f,
                   -0.025f / 4,
                   0.9f,
                   1.6f,
                   EnumParticleModel.Quad
               );

            fireSmoke.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
            fireSmoke.SelfPropelled = true;

            fireBlast = new SimpleParticleProperties(
                    10,
                    15,
                    ColorUtil.ToRgba(255, 250, 100, 50),
                    //ColorUtil.ColorFromRgba(175, 222, 255, 50),
                    new Vec3d(),
                    new Vec3d(),
                    new Vec3f(-4f, -4f, -4f),
                    new Vec3f(4f, 4f, 4f),
                    0.1f,
                    0.3f,
                    1.1f, 1.3f);
            // Evolution is scaled for the lifetime of the particle. I think.
            fireBlast.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f);
            fireBlast.GreenEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -100f);
            fireBlast.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -50f); // And by red we mean blue because BGR.
            fireBlast.VertexFlags = 128; // Glow
        }
            
        WorldInteraction[] interactions;
        long checkStillLoadedCallback;
        WeatherSystemBase wsys;

        public override void OnLoaded(ICoreAPI api)
        {
            // This figures out what item types are valid ammo and shows them as an interaction help.
            if (api.Side != EnumAppSide.Client) return;
            List<AssetLocation> locs = new List<AssetLocation>();
            string codeNames = "";
            if (Attributes.KeyExists("ammos"))
            {
                foreach (string s in Attributes["ammos"].AsArray<string>())
                {
                    locs.Add(AssetLocation.Create(s, Code.Domain));
                    codeNames += "_" + s;
                }
            }
            else
            {
                locs.Add(AssetLocation.Create(Attributes["ammo"].AsString("arrow-*"), Code.Domain));
                codeNames = Attributes["ammo"].AsString("arrow-*");
            }
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "ranged" + codeNames + "Interactions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();
                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    foreach (AssetLocation loc in locs)
                    {
                        if (WildcardUtil.Match(loc, obj.Code))
                        {
                            // api.Logger.Error("stack " + obj.Code);
                            stacks.Add(new ItemStack(obj));
                            break;
                        }
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = Attributes["heldhelp"].AsString("heldhelp-chargebow"),
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
        }



        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
        {
            return null;
        }

        ItemSlot GetNextAmmo(EntityAgent byEntity)
        {
            List<AssetLocation> locs = new List<AssetLocation>();
            if (Attributes.KeyExists("ammos"))
            {
                foreach (string s in Attributes["ammos"].AsArray<string>())
                {
                    locs.Add(AssetLocation.Create(s, Code.Domain));
                }
            }
            else
            {
                locs.Add(AssetLocation.Create(Attributes["ammo"].AsString("arrow-*"), Code.Domain));
            }
            ItemSlot slot = null;
            byEntity.WalkInventory((invslot) =>
            {
                if (invslot is ItemSlotCreative) return true;

                foreach (AssetLocation loc in locs)
                {
                    if (invslot.Itemstack != null && WildcardUtil.Match(loc, invslot.Itemstack.Collectible.Code))
                    {
                        // api.Logger.Error("ammoSlot " + invslot.Itemstack.Collectible.Code);
                        slot = invslot;
                        return false;
                    }
                }

                return true;
            });

            return slot;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent interactingEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            // api.Logger.Error("interactStart");
            checkStillLoaded(slot, interactingEntity);
            ItemSlot invslot = GetNextAmmo(interactingEntity);
            if (invslot == null) return;

            // Not ideal to code the aiming controls this way. Needs an elegant solution - maybe an event bus?
            interactingEntity.Attributes.SetInt("aimingCancel", 0);
            if (!Attributes["stayLoaded"].AsBool(false) || slot.Itemstack.Attributes.GetBool("loaded", false))
            {
                interactingEntity.Attributes.SetInt("aiming", 1);
            }

            if (slot.Itemstack.Attributes.GetBool("loaded", false))
            {
                interactingEntity.AnimManager.StartAnimation(Attributes["loadedAimAnimation"].AsString("bowaim"));
                if (Attributes["aimSound"].AsString("").Length > 0)
                {
                    interactingEntity.World.PlaySoundAt(AssetLocation.Create(Attributes["aimSound"].AsString(""), Code.Domain), interactingEntity, (interactingEntity as EntityPlayer)?.Player, false, 8);
                }
            }
            else
            {
                if (interactingEntity.World is IClientWorldAccessor)
                {
                    slot.Itemstack.TempAttributes.SetInt("renderVariant", 1);
                }

                slot.Itemstack.Attributes.SetInt("renderVariant", 1);

                interactingEntity.AnimManager.StartAnimation(Attributes["aimAnimation"].AsString("bowaim"));

                IPlayer player = null;
                if (interactingEntity is EntityPlayer) player = interactingEntity.World.PlayerByUid(((EntityPlayer)interactingEntity).PlayerUID);
                
                if (Attributes["stayLoaded"].AsBool(false))
                {
                    if (Attributes["loadSound"].AsString("").Length > 0)
                    {
                        interactingEntity.World.PlaySoundAt(AssetLocation.Create(Attributes["loadSound"].AsString(""), Code.Domain), interactingEntity, player, false, 8);
                    }
                }
                else
                {
                    if (Attributes["drawSound"].AsString("").Length > 0)
                    {
                        interactingEntity.World.PlaySoundAt(AssetLocation.Create(Attributes["drawSound"].AsString(""), Code.Domain), interactingEntity, player, false, 8);
                    }
                }
            }
            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent interactingEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            //// api.Logger.Error("interactStep");
            checkStillLoaded(slot, interactingEntity);
            if (slot.Itemstack.Attributes.GetBool("loaded", false)) return true;
//            if (byEntity.World is IClientWorldAccessor)
            {
                int renderVariant = GameMath.Clamp((int)Math.Ceiling(secondsUsed * Attributes["drawSpeed"].AsInt(4)), 1, Attributes["numDrawStages"].AsInt(3) + 1);
                int prevRenderVariant = slot.Itemstack.Attributes.GetInt("renderVariant", 0);

                slot.Itemstack.TempAttributes.SetInt("renderVariant", renderVariant);
                slot.Itemstack.Attributes.SetInt("renderVariant", renderVariant);

                if (prevRenderVariant != renderVariant)
                {
                    (interactingEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
                }
            }

            
            return true;
        }


        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent interactingEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            // api.Logger.Error("interactCancel: " + cancelReason);
            interactingEntity.Attributes.SetInt("aiming", 0);
            if (slot.Itemstack.Attributes.GetBool("loaded", false))
            {
                interactingEntity.AnimManager.StopAnimation(Attributes["loadedAimAnimation"].AsString("bowaim"));
            }
            else
            {
                interactingEntity.AnimManager.StopAnimation(Attributes["aimAnimation"].AsString("bowaim"));
            }

            if (interactingEntity.World is IClientWorldAccessor)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
            }

            slot.Itemstack.Attributes.SetInt("renderVariant", 0);
            (interactingEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();

            if (cancelReason != EnumItemUseCancelReason.ReleasedMouse)
            {
                interactingEntity.Attributes.SetInt("aimingCancel", 1);
            }

            return true;
        }

        public bool checkStillLoaded(ItemSlot slot, EntityAgent interactingEntity)
        {
            if (slot == null || slot.Itemstack == null || slot.Itemstack.Attributes == null) return false;
            if (!(interactingEntity is EntityPlayer)) return true;
            if (slot.Itemstack.Attributes.GetBool("loaded", false))
            {
                bool wetCheck = false;
                if (Attributes["waterUnloads"].AsBool(false))
                {
                    wetCheck = (interactingEntity as EntityPlayer).IsEyesSubmerged();
                    if (!wetCheck)
                    {
                        if (wsys == null) wsys = interactingEntity.Api.ModLoader.GetModSystem<WeatherSystemBase>();
                        double rainLevel = 0;
                        wetCheck =
                            interactingEntity.World.Rand.NextDouble() < 0.05
                            && interactingEntity.World.BlockAccessor.GetRainMapHeightAt(interactingEntity.Pos.AsBlockPos.X, interactingEntity.Pos.AsBlockPos.Z) <= interactingEntity.Pos.AsBlockPos.Y
                            && (rainLevel = wsys.GetPrecipitation(interactingEntity.Pos.XYZ)) > 0.04
                        ;
                        wetCheck = wetCheck && interactingEntity.Api.World.Rand.NextDouble() < rainLevel;
                    }
                }
                
                bool loadTimeoutCheck = Attributes["unloadAfterMilliseconds"].AsInt(0) != 0 && interactingEntity.World.ElapsedMilliseconds > slot.Itemstack.Attributes.GetLong("unloadTime", 0);
                bool switchedAwayCheck = interactingEntity.World.ElapsedMilliseconds > slot.Itemstack.Attributes.GetLong("switchAwayUnloadTime", 0);
                if (loadTimeoutCheck || switchedAwayCheck || wetCheck)
                {
                    // api.Logger.Error("unload: timeout " + loadTimeoutCheck + " switchedAway " + switchedAwayCheck + " wet " + wetCheck);
                    // Weapon timed out or player no longer holding weapon, becomes unloaded.
                    slot.Itemstack.Attributes.SetInt("renderVariant", 0);
                    if (interactingEntity.World is IClientWorldAccessor)
                    {
                        slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
                    }
                    (interactingEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
                    slot.Itemstack.Attributes.SetBool("loaded", false);
                    if (Attributes["fizzleSound"].AsString("").Length > 0)
                    {
                        interactingEntity.World.PlaySoundAt(AssetLocation.Create(Attributes["fizzleSound"].AsString(""), Code.Domain), interactingEntity, (interactingEntity as EntityPlayer)?.Player, false, 8);
                    }
                    // api.Logger.Error("weapon got unloaded");
                    return false;
                }
                else
                {
                    // Player continues to hold weapon.
                    slot.Itemstack.Attributes.SetLong("switchAwayUnloadTime", interactingEntity.World.ElapsedMilliseconds + 100);
                    return true;
                }
            }
            return false;
        }

        // We use this repeated callback so we can set the weapon status to unloaded while the player is not holding it.
        // If the callback fails due to reload, the weapon will still unload when the player selects it again thanks to OnHeldIdle.
        public void RegisterCheckStillLoadedCallback(ItemSlot slot, EntityAgent interactingEntity)
        {
            checkStillLoadedCallback = interactingEntity.World.RegisterCallback((dt) => {
                if (checkStillLoaded(slot, interactingEntity))
                {
                    RegisterCheckStillLoadedCallback(slot, interactingEntity);
                }
            }, 210);
        }

        public override void OnHeldIdle(ItemSlot slot, EntityAgent interactingEntity)
        {
            //// api.Logger.Error("onHeldIdle");
            checkStillLoaded(slot, interactingEntity);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent interactingEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // api.Logger.Error("interactStop");
            // api.Logger.Error("loaded? " + slot.Itemstack.Attributes.GetBool("loaded", false));
            // api.Logger.Error("aimingCancel? " + interactingEntity.Attributes.GetInt("aimingCancel"));
            interactingEntity.Attributes.SetInt("aiming", 0);
            if (interactingEntity.Attributes.GetInt("aimingCancel") == 1)
            {
                interactingEntity.Attributes.SetInt("aimingCancel", 0);
                return;
            }

            if (interactingEntity.World is IClientWorldAccessor)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
            }

            if (Attributes["stayLoaded"].AsBool(false))
            {
                interactingEntity.AnimManager.StopAnimation(Attributes["loadedAimAnimation"].AsString("bowaim"));
                if (slot.Itemstack.Attributes.GetBool("loaded", false))
                {   
                    int firingVariant = Attributes["firingVariant"].AsInt(-1);
                    if (firingVariant != -1)
                    {
                        slot.Itemstack.Attributes.SetInt("renderVariant", firingVariant);
                        interactingEntity.World.RegisterCallback((dt) => {
                            slot.Itemstack.Attributes.SetInt("renderVariant", 0);
                            (interactingEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
                        }, 300);
                    }
                    else
                    {
                        slot.Itemstack.Attributes.SetInt("renderVariant", 0);
                    }
                    (interactingEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
                    // api.Logger.Error("loaded fire");
                    fire(slot, interactingEntity);
                    slot.Itemstack.Attributes.SetBool("loaded", false);
                }
                else
                {
                    // api.Logger.Error("stayload");
                    if (secondsUsed < Attributes["drawTime"].AsFloat(0.35f)) return;
                    // api.Logger.Error("stayload!");
                    slot.Itemstack.Attributes.SetInt("renderVariant", Attributes["loadedVariant"].AsInt(0));
                    if (interactingEntity.World is IClientWorldAccessor)
                    {
                        slot.Itemstack.TempAttributes.SetInt("renderVariant", Attributes["loadedVariant"].AsInt(0));
                    }
                    (interactingEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
                    slot.Itemstack.Attributes.SetBool("loaded", true);
                    // api.Logger.Error("loading after " + secondsUsed);
                    slot.Itemstack.Attributes.SetLong("switchAwayUnloadTime", interactingEntity.World.ElapsedMilliseconds + 100);
                    RegisterCheckStillLoadedCallback(slot, interactingEntity);
                    int unloadAfterMilliseconds = Attributes["unloadAfterMilliseconds"].AsInt(0);
                    if (unloadAfterMilliseconds != 0)
                    {
                        slot.Itemstack.Attributes.SetLong("unloadTime", interactingEntity.World.ElapsedMilliseconds + unloadAfterMilliseconds);
                    }
                }
            }
            else
            {
                // api.Logger.Error("unstayload");
                if (secondsUsed < Attributes["drawTime"].AsFloat(0.35f)) return;
                // api.Logger.Error("unstayload!");
                interactingEntity.AnimManager.StopAnimation(Attributes["aimAnimation"].AsString("bowaim"));
                int firingVariant = Attributes["firingVariant"].AsInt(-1);
                if (firingVariant != -1)
                {
                    slot.Itemstack.Attributes.SetInt("renderVariant", firingVariant);
                    interactingEntity.World.RegisterCallback((dt) => {
                        slot.Itemstack.Attributes.SetInt("renderVariant", 0);
                        (interactingEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
                    }, 300);
                }
                else
                {
                    slot.Itemstack.Attributes.SetInt("renderVariant", 0);
                }
                (interactingEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
                fire(slot, interactingEntity);
            }
        }

        public void fire(ItemSlot slot, EntityAgent interactingEntity)
        {
            // api.Logger.Error("fire");
            ItemSlot ammoSlot = GetNextAmmo(interactingEntity);
            if (ammoSlot == null) return;
            // api.Logger.Error("fire!");

            string ammoMaterial = ammoSlot.Itemstack.Collectible.FirstCodePart(1);
            float damage = 0;
            int poison = 0;

            // Weapon damage
            if (slot.Itemstack.Collectible.Attributes != null)
            {
                damage += slot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
            }

            // Ammo damage
            if (ammoSlot.Itemstack.Collectible.Attributes != null)
            {
                damage += ammoSlot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
                poison += ammoSlot.Itemstack.Collectible.Attributes["poisonDamage"].AsInt(0);
            }

            ItemStack stack = ammoSlot.TakeOut(1);
            ammoSlot.MarkDirty();

            IPlayer player = null;
            if (interactingEntity is EntityPlayer) player = interactingEntity.World.PlayerByUid(((EntityPlayer)interactingEntity).PlayerUID);
            if (Attributes["releaseSound"].AsString("").Length > 0)
            {
                interactingEntity.World.PlaySoundAt(AssetLocation.Create(Attributes["releaseSound"].AsString(""), Code.Domain), interactingEntity, player, false, 8);
            }

            float breakChance = 0.5f;
            if (stack.ItemAttributes != null) breakChance = stack.ItemAttributes["breakChanceOnImpact"].AsFloat(0.5f);
            //System.Console.WriteLine("projectile " + AssetLocation.Create(Attributes["projectile"].AsString("arrow-*").Replace("*", ammoMaterial), Code.Domain));

            EntityProperties type = interactingEntity.World.GetEntityType(AssetLocation.Create(Attributes["projectile"].AsString("arrow-*").Replace("*", ammoMaterial), Code.Domain));
            Entity entity = interactingEntity.World.ClassRegistry.CreateEntity(type);
            if (entity is EntityProjectile)
            {
                ((EntityProjectile)entity).FiredBy = interactingEntity;
                ((EntityProjectile)entity).Damage = damage;
                ((EntityProjectile)entity).ProjectileStack = stack;
                ((EntityProjectile)entity).DropOnImpactChance = 1 - breakChance;
            }
            if (entity is EntityPoisonProjectile)
            {
                ((EntityPoisonProjectile)entity).FiredBy = interactingEntity;
                ((EntityPoisonProjectile)entity).Damage = damage;
                ((EntityPoisonProjectile)entity).ProjectileStack = stack;
                ((EntityPoisonProjectile)entity).DropOnImpactChance = 1 - breakChance;
                ((EntityPoisonProjectile)entity).PoisonDamage = poison;
            }
            

            float acc = Math.Max(0.001f, (1 - interactingEntity.Attributes.GetFloat("aimingAccuracy", 0)));
            //System.Console.WriteLine("acc: " + acc);
            double rndpitch = interactingEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1) * acc * 0.75;
            double rndyaw = interactingEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1) * acc * 0.75;
            
            Vec3d pos = interactingEntity.ServerPos.XYZ.Add(0, interactingEntity.LocalEyePos.Y, 0);
            Vec3d aheadPos = pos.AheadCopy(1, interactingEntity.SidedPos.Pitch + rndpitch, interactingEntity.SidedPos.Yaw + rndyaw);
            Vec3d velocity = (aheadPos - pos) * interactingEntity.Stats.GetBlended("bowDrawingStrength") * Attributes["projectileSpeed"].AsFloat(1.0f);

            entity.ServerPos.SetPos(interactingEntity.SidedPos.BehindCopy(0.21).XYZ.Add(0, interactingEntity.LocalEyePos.Y, 0));
            entity.ServerPos.Motion.Set(velocity);

            if (Attributes["releaseFire"].AsBool(false))
            {
                fireBlast.MinPos = interactingEntity.SidedPos.AheadCopy(2.3).XYZ.Add(0, interactingEntity.LocalEyePos.Y, 0);
                interactingEntity.World.SpawnParticles(fireBlast);
                fireSmoke.MinPos = interactingEntity.SidedPos.AheadCopy(2.3).XYZ.Add(0, interactingEntity.LocalEyePos.Y, 0);
                interactingEntity.World.SpawnParticles(fireSmoke);
            }

            entity.Pos.SetFrom(entity.ServerPos);
            entity.World = interactingEntity.World;
            if (entity is EntityProjectile)
            {
                ((EntityProjectile)entity).SetRotation();
            }
            if (entity is EntityPoisonProjectile)
            {
                ((EntityPoisonProjectile)entity).SetRotation();
            }

            interactingEntity.World.SpawnEntity(entity);

            slot.Itemstack.Collectible.DamageItem(interactingEntity.World, interactingEntity, slot);

            interactingEntity.AnimManager.StartAnimation(Attributes["hitAnimation"].AsString("bowhit"));
        }


        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withErrorInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withErrorInfo);

            if (inSlot.Itemstack.Collectible.Attributes == null) return;

            float dmg = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
            if (dmg != 0) dsc.AppendLine(dmg + Lang.Get("piercing-damage"));
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
