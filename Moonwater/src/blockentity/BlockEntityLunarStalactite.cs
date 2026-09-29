using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Moonwater.blockentity;

public class BlockEntityLunarStalactite : BlockEntity
{
    private long dripListenerId;
    private double lastCheck;
    
    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        dripListenerId = RegisterGameTickListener(new Action<float>(dropMoonwaterDroplets), 3000, 0);
    }
    
    public override void OnBlockRemoved()
    {
        Api.World.UnregisterGameTickListener(dripListenerId);
        base.OnBlockRemoved();
    }
    
    private void dropMoonwaterDroplets(float obj)
    {
      if (!(Api.World.Calendar.MoonPhase == EnumMoonPhase.Full)) return;//Only check for drops if the moon is full
      //Particles are a client side only thing, so check if we are on the client side
      if (Api.Side != EnumAppSide.Client) return;
      
      SimpleParticleProperties particleProperties = new SimpleParticleProperties(1f, 5f, ColorUtil.ToRgba(byte.MaxValue, 153, 255, 255), new Vec3d(), new Vec3d(), new Vec3f(-0.4f, 0.2f, -0.45f), new Vec3f(0.44f, -0.5f, 0.41f), 3f, 0.9f, 0.2f, 0.4f, (EnumParticleModel) 1);
      particleProperties.LifeLength = 3.5f;
      particleProperties.MinSize = 0.3f;
      particleProperties.MaxSize = 0.6f;
      particleProperties.MinPos = Pos.ToVec3d().AddCopy(0.5, 0.3, 0.5);
      
      Api.World.SpawnParticles(particleProperties);
  }
}