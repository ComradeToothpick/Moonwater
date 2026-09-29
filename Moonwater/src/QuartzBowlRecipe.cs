using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Moonwater;

public class QuartzBowlRecipe : 
    RecipeBase,
    IByteSerializable,
    IConcreteCloneable<QuartzBowlRecipe>,
    ICloneable
{
    [DocumentAsJson("Required", "", false)]
    public QuartzBowlOutputStack? Output { get; set; }

    /*[DocumentAsJson("Required", "", false)]
    public double SealHours = 0; { get; set; }*/
    public double SealHours = 0;

    [DocumentAsJson("Required", "", false)]
    public string? Code { get; set; }

    [DocumentAsJson("Required", "", false)]
    public QuartzBowlRecipeIngredient[]? Ingredients { get; set; }

    public override IEnumerable<IRecipeIngredient> RecipeIngredients
    {
        get
        {
            return (IEnumerable<IRecipeIngredient>) (this.Ingredients ?? throw new InvalidOperationException($"Quartz Bowl recipe '{this.Name}' does not have ingredients specified"));
        }
    }

    public override IRecipeOutput RecipeOutput
    {
        get
        {
            return (IRecipeOutput) (this.Output ?? throw new InvalidOperationException($"Quartz Bowl recipe '{this.Name}' does not have output specified"));
        }
    }

    public override void OnParsed(IWorldAccessor world)
    {
        if (this.Ingredients == null)
            return;
        int num1 = 1;
        foreach (QuartzBowlRecipeIngredient ingredient in this.Ingredients)
        {
            if (ingredient.Id == null)
            {
                QuartzBowlRecipeIngredient recipeIngredient = ingredient;
                int num2 = num1++;
                string str1;
                string str2 = str1 = num2.ToString();
                recipeIngredient.Id = str1;
            }
        }
    }

    public bool Matches(ItemSlot[] inputSlots, out int outputStackSize)
    {
        outputStackSize = 0;
        List<(ItemSlot, QuartzBowlRecipeIngredient)> matched = this.PairInput(inputSlots);
        if (matched.Count == 0)
            return false;
        outputStackSize = this.GetOutputSize(matched);
        return outputStackSize >= 0;
    }

    public bool Matches(IPlayer forPlayer, ItemSlot[] inputSlots, out int outputStackSize)
    {
        outputStackSize = 0;
        return forPlayer.Entity.Api.Event.TriggerMatchesRecipe(forPlayer, (IRecipeBase) this, inputSlots) && this.Matches(inputSlots, out outputStackSize);
    }

    public bool TryCraftNow(ICoreAPI api, double nowSealedHours, ItemSlot[] inputSlots)
    {
        if (this.SealHours > 0.0 && nowSealedHours < this.SealHours)
        {
            return false;
        }
            
        List<(ItemSlot slot, QuartzBowlRecipeIngredient ingredient)> matched = this.PairInput(inputSlots);
        int outputSize = this.GetOutputSize(matched);
        if (outputSize < 0 || this.Output?.ResolvedItemStack == null)
            return false;
        ItemStack itemStack1 = this.Output.ResolvedItemStack.Clone();
        itemStack1.StackSize = outputSize;
        this.CarryOverFreshness(api, itemStack1, inputSlots);
        ItemStack itemStack2 = (ItemStack) null;
        foreach ((ItemSlot slot, QuartzBowlRecipeIngredient ingredient) in matched)
        {
            if (ingredient.ConsumeQuantity.HasValue && slot.Itemstack != null)
            {
                itemStack2 = slot.Itemstack;
                itemStack2.StackSize -= ingredient.ConsumeQuantity.Value * (itemStack1.StackSize / this.Output.StackSize);
                if (itemStack2.StackSize <= 0)
                {
                    itemStack2 = (ItemStack) null;
                    break;
                }
                break;
            }
        }
        ItemSlot inputSlot1 = inputSlots[0];
        ItemSlot inputSlot2 = inputSlots[1];
        if (this.ShouldBeInLiquidSlot(itemStack1))
        {
            inputSlot1.Itemstack = itemStack2;
            inputSlot2.Itemstack = itemStack1;
        }
        else
        {
            inputSlot2.Itemstack = itemStack2;
            inputSlot1.Itemstack = itemStack1;
        }
        inputSlot1.MarkDirty();
        inputSlot2.MarkDirty();
        return true;
    }

    public override void ToBytes(BinaryWriter writer)
    {
        base.ToBytes(writer);
        if (this.Code == null || this.Ingredients == null || this.Output == null)
            throw new InvalidOperationException("Cannot serialize quartz bowl recipes: some of the properties are null");
        writer.Write(this.Code);
        writer.Write(this.Ingredients.Length);
        for (int index = 0; index < this.Ingredients.Length; ++index)
            this.Ingredients[index].ToBytes(writer);
        this.Output.ToBytes(writer);
        /*writer.Write(this.SealHours);*/
    }

    public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        base.FromBytes(reader, resolver);
        this.Code = reader.ReadString();
        QuartzBowlRecipeIngredient[] recipeIngredientArray = new QuartzBowlRecipeIngredient[reader.ReadInt32()];
        this.Ingredients = recipeIngredientArray;
        for (int index = 0; index < recipeIngredientArray.Length; ++index)
        {
            QuartzBowlRecipeIngredient recipeIngredient = new QuartzBowlRecipeIngredient();
            recipeIngredient.FromBytes(reader, resolver);
            recipeIngredient.Resolve(resolver, "Quartz Bowl Recipe (FromBytes)", (IRecipeBase) this);
            recipeIngredientArray[index] = recipeIngredient;
        }
        this.Output = new QuartzBowlOutputStack();
        this.Output.FromBytes(reader, resolver.ClassRegistry);
        this.Output.Resolve(resolver, "Quartz Bowl Recipe (FromBytes)");
        /*this.SealHours = reader.ReadDouble();*/
    }

    public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
    {
        bool flag = true;
        if (this.Ingredients == null || this.Output == null)
        {
            world.Logger.Error($"Cannot resolve quartz bowl recipe '{this.Name}', either Ingredients or Output are not specified");
            return false;
        }
        foreach (QuartzBowlRecipeIngredient ingredient in this.Ingredients)
            flag &= ingredient.Resolve(world, sourceForErrorLogging/*, (IRecipeBase) this*/);
        return flag & this.Output.Resolve(world, sourceForErrorLogging);
    }
    
    
    public override QuartzBowlRecipe Clone()
    {
        QuartzBowlRecipe recipe = new QuartzBowlRecipe();
        this.CloneTo((object) recipe);
        return recipe;
    }

    protected override void CloneTo(object recipe)
    {
        base.CloneTo(recipe);
        if (!(recipe is QuartzBowlRecipe quartzBowlRecipe))
            throw new ArgumentException("CloneTo should take object of same class or it subclass");
        quartzBowlRecipe.Output = this.Output?.Clone();
        quartzBowlRecipe.SealHours = this.SealHours;
        quartzBowlRecipe.Code = this.Code;
        if (this.Ingredients == null)
            return;
        quartzBowlRecipe.Ingredients = new QuartzBowlRecipeIngredient[this.Ingredients.Length];
        for (int index = 0; index < this.Ingredients.Length; ++index)
            quartzBowlRecipe.Ingredients[index] = Ingredients[index].Clone();
    }

    protected virtual bool ShouldBeInLiquidSlot(ItemStack? stack)
    {
        return stack != null && stack.ItemAttributes["waterTightContainerProps"].AsBool();
    }

    protected virtual List<(ItemSlot slot, QuartzBowlRecipeIngredient ingredient)> PairInput(
        ItemSlot[] inputSlots)
    {
        int num = 0;
        foreach (ItemSlot inputSlot in inputSlots)
        {
            if (!inputSlot.Empty)
                ++num;
        }
        if (this.Ingredients == null || num != this.Ingredients.Length)
            return new List<(ItemSlot, QuartzBowlRecipeIngredient)>();
        List<(ItemSlot, QuartzBowlRecipeIngredient)> valueTupleList = new List<(ItemSlot, QuartzBowlRecipeIngredient)>();
        List<QuartzBowlRecipeIngredient> list = Ingredients.ToList();
        foreach (ItemSlot inputSlot1 in inputSlots)
        {
            ItemSlot inputSlot = inputSlot1;
            if (inputSlot.Itemstack != null)
            {
                QuartzBowlRecipeIngredient recipeIngredient = list.Find((Predicate<QuartzBowlRecipeIngredient>) (ingredient => this.MatchStackToIngredient(inputSlot.Itemstack, (IRecipeIngredient) ingredient)));
                if (recipeIngredient == null)
                    return new List<(ItemSlot, QuartzBowlRecipeIngredient)>();
                valueTupleList.Add((inputSlot, recipeIngredient));
                list.Remove(recipeIngredient);
            }
        }
        return valueTupleList.Count < this.Ingredients.Length ? new List<(ItemSlot, QuartzBowlRecipeIngredient)>() : valueTupleList;
    }

    protected virtual int GetOutputSize(
        List<(ItemSlot slot, QuartzBowlRecipeIngredient ingredient)> matched)
    {
        int num = -1;
        foreach ((ItemSlot slot, QuartzBowlRecipeIngredient ingredient) in matched)
        {
            if (!ingredient.ConsumeQuantity.HasValue)
                num = slot.StackSize / ingredient.Quantity;
        }
        if (num == -1)
            return -1;
        foreach ((ItemSlot slot, QuartzBowlRecipeIngredient ingredient) in matched)
        {
            if (!ingredient.ConsumeQuantity.HasValue)
            {
                if (slot.StackSize % ingredient.Quantity != 0 || num != slot.StackSize / ingredient.Quantity)
                    return -1;
            }
            else if (slot.StackSize < ingredient.Quantity * num)
                return -1;
        }
        return this.Output.StackSize * num;
    }

    protected virtual void CarryOverFreshness(
        ICoreAPI api,
        ItemStack mixedStack,
        ItemSlot[] inputSlots)
    {
        TransitionableProperties[] transitionableProperties = mixedStack.Collectible.GetTransitionableProperties(api.World, mixedStack, (Entity) null);
        TransitionableProperties perishProps = transitionableProperties != null ? ((IEnumerable<TransitionableProperties>) transitionableProperties).FirstOrDefault<TransitionableProperties>((System.Func<TransitionableProperties, bool>) (p => p.Type == EnumTransitionType.Perish)) : (TransitionableProperties) null;
        if (perishProps == null)
            return;
        CollectibleObject.CarryOverFreshness(api, inputSlots, new ItemStack[1]
        {
            mixedStack
        }, perishProps);
    }
}