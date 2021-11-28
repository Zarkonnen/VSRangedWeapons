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

namespace RangedWeapons
{
    public class PoisonedBehavior : EntityBehavior
    {
        float accumulatedTime;
        public float poison;
        public float poisonInterval;

        public PoisonedBehavior(Entity entity) : base(entity)
        {

        }

        /// <summary>
        /// The event fired when a game ticks over.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void OnGameTick(float deltaTime) {
            accumulatedTime += deltaTime;
            if (accumulatedTime >= poisonInterval)
            {
                entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.Poison }, 1);
                accumulatedTime -= poisonInterval;
                poison--;
                if (poison <= 0) {
                    entity.RemoveBehavior(this);
                }
            }
        }

        public override string PropertyName()
        {
            return "entityispoisoned";
        }
    }
}
