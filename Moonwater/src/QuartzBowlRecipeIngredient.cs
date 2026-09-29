using System.IO;
using System.Runtime.CompilerServices;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Moonwater;

public class QuartzBowlRecipeIngredient : CraftingRecipeIngredient
{
    [DocumentAsJson("Optional", "Consume All", false)]
  public int? ConsumeQuantity;
  [DocumentAsJson("Optional", "None", false)]
  public float Litres = -1f;
  [DocumentAsJson("Optional", "Consume All", false)]
  public float? ConsumeLitres;

  [PreserveBaseOverrides]
  public override QuartzBowlRecipeIngredient Clone()
  {
    QuartzBowlRecipeIngredient cloneTo = new QuartzBowlRecipeIngredient();
    this.CloneTo((object) cloneTo);
    return cloneTo;
  }

  public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
  {
    base.FromBytes(reader, resolver);
    this.ConsumeQuantity = !reader.ReadBoolean() ? new int?() : new int?(reader.ReadInt32());
    this.ConsumeLitres = !reader.ReadBoolean() ? new float?() : new float?(reader.ReadSingle());
    this.Litres = reader.ReadSingle();
  }

  public override void ToBytes(BinaryWriter writer)
  {
    base.ToBytes(writer);
    if (this.ConsumeQuantity.HasValue)
    {
      writer.Write(true);
      writer.Write(this.ConsumeQuantity.Value);
    }
    else
      writer.Write(false);
    if (this.ConsumeLitres.HasValue)
    {
      writer.Write(true);
      writer.Write(this.ConsumeLitres.Value);
    }
    else
      writer.Write(false);
    writer.Write(this.Litres);
  }

  public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
  {
    this.MatchingType = IRecipeIngredient.GetMatchType(this.Code?.ToString(), this.Name != null);
    if (this.ReturnedStack != null)
      this.ReturnedStack.Resolve(world, $"{sourceForErrorLogging} recipe with output {this.Code}");
    if (this.MatchingType != EnumRecipeMatchType.Exact)
      return true;
    if (this.MatchingType == EnumRecipeMatchType.Exact && (this.Code == (AssetLocation) null || this.Code == (AssetLocation) "*.*"))
    {
      world.Logger.Warning("Failed resolving crafting recipe ingredient with unspecified code in " + sourceForErrorLogging);
      return false;
    }
    ItemStack itemStack;
    if (this.Type == EnumItemClass.Block)
    {
      Block block = world.GetBlock(this.Code);
      if (block == null || block.IsMissing)
      {
        world.Logger.Warning($"Failed resolving crafting recipe block ingredient with code {this.Code} in {sourceForErrorLogging}");
        return false;
      }
      itemStack = new ItemStack(block, this.Quantity);
    }
    else
    {
      Item obj = world.GetItem(this.Code);
      if (obj == null || obj.IsMissing)
      {
        world.Logger.Warning($"Failed resolving crafting recipe item ingredient with code {this.Code} in {sourceForErrorLogging}");
        return false;
      }
      itemStack = new ItemStack(obj, this.Quantity);
    }
    if (this.Attributes != null && this.Attributes.ToAttribute() is ITreeAttribute attribute)
    {
      this.ResolvedAttributes = attribute;
      itemStack.Attributes = attribute;
    }
    this.ResolvedItemStack = itemStack;
    this.ResolveLiquidProperties(world, sourceForErrorLogging);
    return true;
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
        world.Logger.Warning($"({sourceForErrorLogging}) Quartz Bowl recipe ingredient '{this.Code}' does not define a litres attribute but a quantity, will assume quantity=litres for backwards compatibility.");
        this.Litres = (float) this.Quantity;
        int? consumeQuantity = this.ConsumeQuantity;
        this.ConsumeLitres = consumeQuantity.HasValue ? new float?((float) consumeQuantity.GetValueOrDefault()) : new float?();
      }
      else
        this.Litres = 1f;
    }
    this.Quantity = (int) ((double) containableProps.ItemsPerLitre * (double) this.Litres);
    if (!this.ConsumeLitres.HasValue)
      return;
    float itemsPerLitre = containableProps.ItemsPerLitre;
    float? consumeLitres = this.ConsumeLitres;
    this.ConsumeQuantity = new int?((int) (consumeLitres.HasValue ? new float?(itemsPerLitre * consumeLitres.GetValueOrDefault()) : new float?()).Value);
  }

  protected override void CloneTo(object cloneTo)
  {
    base.CloneTo(cloneTo);
    if (!(cloneTo is QuartzBowlRecipeIngredient recipeIngredient))
      return;
    recipeIngredient.ConsumeQuantity = this.ConsumeQuantity;
    recipeIngredient.Litres = this.Litres;
    recipeIngredient.ConsumeLitres = this.ConsumeLitres;
  }
}