using System;
using System.IO;
using System.Runtime.CompilerServices;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Moonwater;

[DocumentAsJson]
public class QuartzBowlOutputStack : JsonItemStack, IConcreteCloneable<QuartzBowlOutputStack>, ICloneable
{
    [DocumentAsJson("Optional", "0", false)]
    public float Litres;

    public override void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
    {
        base.FromBytes(reader, instancer);
        this.Litres = reader.ReadSingle();
    }

    public override void ToBytes(BinaryWriter writer)
    {
        base.ToBytes(writer);
        writer.Write(this.Litres);
    }

    public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
    {
        if (!base.Resolve(world, sourceForErrorLogging))
            return false;
        this.ResolveLiquidProperties(world, sourceForErrorLogging);
        return true;
    }

    public QuartzBowlOutputStack Clone()
    {
        QuartzBowlOutputStack stack = new QuartzBowlOutputStack();
        this.CloneTo((object) stack);
        return stack;
    }

    protected override void CloneTo(object stack)
    {
        base.CloneTo(stack);
        if (!(stack is QuartzBowlOutputStack quartzBowlOutputStack))
            return;
        quartzBowlOutputStack.Litres = this.Litres;
    }

    protected virtual void ResolveLiquidProperties(IWorldAccessor world, string sourceForErrorLogging)
    {
        WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(this.ResolvedItemStack);
        if (containableProps == null)
            return;
        if ((double) this.Litres < 0.0)
        {
            if (this.Quantity > 0)
            {
                world.Logger.Warning($"({sourceForErrorLogging}) Quartz Bowl recipe output {this.Code} does not define a litres attribute but a stacksize, will assume stacksize=litres for backwards compatibility.");
                this.Litres = (float) this.Quantity;
            }
            else
                this.Litres = 1f;
        }
        this.Quantity = (int) ((double) containableProps.ItemsPerLitre * (double) this.Litres);
    }
}
