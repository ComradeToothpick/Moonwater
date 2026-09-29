using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moonwater.blockentity;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Moonwater.blocks;

public class BlockQuartzBowl : BlockLiquidContainerBase
{
    public override bool AllowHeldLiquidTransfer => false;

    public override int GetContainerSlotId(BlockPos pos) => 1;

    public override int GetContainerSlotId(ItemStack containerStack) => 1;

    public override void OnBlockBroken(
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        float dropQuantityMultiplier = 1f)
    {
        bool flag = false;
        foreach (BlockBehavior blockBehavior in this.BlockBehaviors)
        {
            EnumHandling enumHandling = EnumHandling.PassThrough;
            IWorldAccessor world1 = world;
            BlockPos pos1 = pos;
            IPlayer byPlayer1 = byPlayer;
            double dropQuantityMultiplier1 = (double) dropQuantityMultiplier;
            ref EnumHandling local = ref enumHandling;
            blockBehavior.OnBlockBroken(world1, pos1, byPlayer1, (float) dropQuantityMultiplier1, ref local);
            if (enumHandling == EnumHandling.PreventDefault)
                flag = true;
            if (enumHandling == EnumHandling.PreventSubsequent)
                return;
        }
        if (flag)
            return;
        if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
        {
            ItemStack[] itemStackArray = new ItemStack[1]
            {
                new ItemStack((Block) this)
            };
            foreach (ItemStack itemstack in itemStackArray)
                world.SpawnItemEntity(itemstack, pos);
            world.PlaySoundAt(this.Sounds.GetBreakSound(byPlayer), pos, 0.0, byPlayer);
        }
        if (this.EntityClass != null)
            world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
        world.BlockAccessor.SetBlock(0, pos);
    }

    public override void OnHeldAttackStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        ref EnumHandHandling handling)
    {
    }

    public override int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
    {
        return base.TryPutLiquid(pos, liquidStack, desiredLitres);
    }

    public override int TryPutLiquid(
        ItemStack containerStack,
        ItemStack liquidStack,
        float desiredLitres)
    {
        return base.TryPutLiquid(containerStack, liquidStack, desiredLitres);
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
    {
        return new WorldInteraction[1]
        {
            new WorldInteraction()
            {
                ActionLangCode = "heldhelp-place",
                HotKeyCode = "shift",
                MouseButton = EnumMouseButton.Right,
                ShouldApply = (InteractionMatcherDelegate) ((wi, bs, es) => true)
            }
        };
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(
        IWorldAccessor world,
        BlockSelection blockSel,
        IPlayer forPlayer)
    {
        BEQuartzBowl blockEntityQuartzBowl = (BEQuartzBowl) null;
        if (blockSel.Position != (BlockPos) null)
            blockEntityQuartzBowl = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEQuartzBowl;
        return blockEntityQuartzBowl != null && blockEntityQuartzBowl.Sealed ? Array.Empty<WorldInteraction>() : base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
    }

    public override void OnHeldInteractStart(
        ItemSlot itemslot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handHandling)
    {
        base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
    }

    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel)
    {
        if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            return false;
        BEQuartzBowl blockEntityQuartzBowl = (BEQuartzBowl) null;
        if (blockSel.Position != (BlockPos) null)
            blockEntityQuartzBowl = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEQuartzBowl;
        if (blockEntityQuartzBowl != null && blockEntityQuartzBowl.Sealed)
            return true;
        ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (!activeHotbarSlot.Empty && activeHotbarSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorQuenchable>())
            return false;
        bool flag = base.OnBlockInteractStart(world, byPlayer, blockSel);
        if (flag || byPlayer.WorldData.EntityControls.ShiftKey || !(blockSel.Position != (BlockPos) null))
            return flag;
        blockEntityQuartzBowl?.OnPlayerRightClick(byPlayer);
        return true;
    }

    public override void GetHeldItemInfo(
        ItemSlot inSlot,
        StringBuilder dsc,
        IWorldAccessor world,
        bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        ItemStack[] contents = this.GetContents(world, inSlot.Itemstack);
        if (contents == null || contents.Length == 0)
            return;
        ItemStack itemStack = contents[0] == null ? contents[1] : contents[0];
        if (itemStack == null)
            return;
        dsc.Append(", " + Lang.Get("{0}x {1}", (object) itemStack.StackSize, (object) itemStack.GetName()));
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        string a = base.GetPlacedBlockInfo(world, pos, forPlayer);
        string str1 = "";
        int num = a.IndexOfOrdinal(Environment.NewLine + Environment.NewLine);
        if (num > 0)
        {
            str1 = a.Substring(num);
            a = a.Substring(0, num);
        }
        if ((double) this.GetCurrentLitres(pos) <= 0.0)
            a = "";
        if (world.BlockAccessor.GetBlockEntity(pos) is BEQuartzBowl blockEntity)
        {
            ItemSlot contentSlot = blockEntity.Inventory[0];
            if (!contentSlot.Empty)
                a = (a.Length <= 0 ? $"{a}{Lang.Get("Contents:")}\n " : a + " ") + Lang.Get("{0}x {1}", (object) contentSlot.Itemstack.StackSize, (object) contentSlot.Itemstack.GetName()) + BlockLiquidContainerBase.PerishableInfoCompact(this.api, contentSlot, 0.0f, false);
            if (blockEntity.Sealed && blockEntity.CurrentRecipe != null)
            {
                double d = world.Calendar.TotalHours - blockEntity.SealedSinceTotalHours;
                if (d < 3.0)
                    d = Math.Max(0.0, d + 0.2);
                string str2;
                if (d <= 24.0)
                    str2 = Lang.Get("{0} hours", (object) Math.Floor(d));
                else
                    str2 = Lang.Get("{0} days", (object) (Math.Floor(d / (double) this.api.World.Calendar.HoursPerDay * 10.0) / 10.0));
                string str3 = str2;
                string str4;
                if (blockEntity.CurrentRecipe.SealHours <= 24.0)
                    str4 = Lang.Get("{0} hours", (object) Math.Round(blockEntity.CurrentRecipe.SealHours));
                else
                    str4 = Lang.Get("{0} days", (object) Math.Round(blockEntity.CurrentRecipe.SealHours / (double) this.api.World.Calendar.HoursPerDay, 1));
                string str5 = str4;
                a = $"{a}\n{Lang.Get("Sealed for {0} / {1}", (object) str3, (object) str5)}";
            }
        }
        return a + str1;
    }

    public override void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos)
    {
    }
}