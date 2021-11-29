using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace RangedWeapons
{
    public class Poisonable : EntityBehavior
    {
        public float accumulatedTime;

        public Poisonable(Entity entity) : base(entity)
        {

        }

        /// <summary>
        /// The event fired when a game ticks over.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void OnGameTick(float deltaTime) {
            // Actual poison damaging mechanics are only needed server-side.
            if (entity.World is IClientWorldAccessor) { return; }
            int poison = entity.WatchedAttributes.GetInt("poisonedAmount", 0);
            if (poison > 0)
            {
                accumulatedTime += deltaTime;
                if (accumulatedTime >= 15)
                {
                    entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.Poison }, 1);
                    accumulatedTime -= 15;
                    poison--;
                    entity.WatchedAttributes.SetInt("poisonedAmount", poison);
                }
            }
        }

        public override string PropertyName()
        {
            return "poisonable";
        }

        public override void GetInfoText(StringBuilder infotext)
        {
            if (!entity.Alive) return;
            int poison = entity.WatchedAttributes.GetInt("poisonedAmount", 0);
            if (poison > 0)
            {
                infotext.AppendLine(Lang.Get("rangedweapons:Poisoned"));
            }
        }
    }
}
