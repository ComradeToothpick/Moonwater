using Vintagestory.GameContent;
using System;
using Moonwater.blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Moonwater.blockentity;

public class BEQuartzBowl : BlockEntityLiquidContainer, ICoolingMedium
{
    private GuiDialogQuartzBowl invDialog;//Need to make custom GUI for this
    private long moonPhaseListenerId;
    private MeshData currentMesh;
    private BlockQuartzBowl ownBlock;
    public bool Sealed;//Probably don't need to seal
    public double SealedSinceTotalHours;
    public QuartzBowlRecipe CurrentRecipe;
    public int CurrentOutSize;
    protected static SoundAttributes barrelOpen = new SoundAttributes(AssetLocation.Create("sounds/block/vesselopen"), true);
    protected static SoundAttributes barrelClose = new SoundAttributes(AssetLocation.Create("sounds/block/vesselclose"), true);
    private bool ignoreChange;
    //private MoonwaterModSystem modSystem;

    public int CapacityLitres { get; set; } = 50;

    public override string InventoryClassName => "barrel";//TODO:Investigate if this needs to be changed

    /*[Obsolete("Use player aware 'GetCanSeal' instead")]
    public bool CanSeal
    {
        get
        {
            this.FindMatchingRecipe();
            return this.CurrentRecipe != null && this.CurrentRecipe.SealHours > 0.0;
        }
    }

    public bool GetCanSeal(IPlayer byPlayer)
    {
        this.FindMatchingRecipe(byPlayer);
        return this.CurrentRecipe != null && this.CurrentRecipe.SealHours > 0.0;
    }*/

    public BEQuartzBowl()
    {
        this.inventory = new InventoryGeneric(2, (string) null, (ICoreAPI) null, (NewSlotDelegate) ((id, self) => id == 0 ? (ItemSlot) new ItemSlotBarrelInput((InventoryBase) self) : (ItemSlot) new ItemSlotLiquidOnly((InventoryBase) self, 50f)));
        this.inventory.BaseWeight = 1f;
        this.inventory.OnGetSuitability = new GetSuitabilityDelegate(this.GetSuitability);
        this.inventory.SlotModified += new Action<int>(this.Inventory_SlotModified);
        this.inventory.OnAcquireTransitionSpeed += new CustomGetTransitionSpeedMulDelegate(this.Inventory_OnAcquireTransitionSpeed1);
    }

    public void ConvertContents(float obj)
    {
        if (!(this.Api.World.Calendar.MoonPhase == EnumMoonPhase.Full)) return; //Do nothing if it is not a full moon
        
    }

    protected float Inventory_OnAcquireTransitionSpeed1(
        EnumTransitionType transType,
        ItemStack stack,
        float mul)
    {
        int num;
        if (this.Sealed)
        {
            QuartzBowlRecipe currentRecipe = this.CurrentRecipe;
            if ((currentRecipe != null ? (currentRecipe.SealHours > 0.0 ? 1 : 0) : 0) != 0)
            {
                num = 0;
                goto label_4;
            }
        }
        num = 1;
        label_4:
        return (float) num;
    }

    protected float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
    {
        if (targetSlot == this.inventory[1] && this.inventory[0].StackSize > 0)
        {
            ItemStack itemstack1 = this.inventory[0].Itemstack;
            ItemStack itemstack2 = sourceSlot.Itemstack;
            if (itemstack1.Collectible.Equals(itemstack1, itemstack2, GlobalConstants.IgnoredStackAttributes))
                return -1f;
        }
        return (isMerge ? this.inventory.BaseWeight + 3f : this.inventory.BaseWeight + 1f) + (float) (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
    }

    protected override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
    {
        return atBlockFace == BlockFacing.UP ? this.inventory[0] : (ItemSlot) null;
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        this.ownBlock = this.Block as BlockQuartzBowl;
        JsonObject attribute = this.ownBlock?.Attributes?["capacityLitres"];
        if (attribute != null && attribute.Exists)
        {
            this.CapacityLitres = attribute.AsInt(50);
            (this.inventory[1] as ItemSlotLiquidOnly).CapacityLitres = (float) this.CapacityLitres;
        }
        if (api.Side == EnumAppSide.Server)
            this.RegisterGameTickListener(new Action<float>(this.OnEvery3Second), 3000);
        this.FindMatchingRecipe();
    }

    protected void Inventory_SlotModified(int slotId)
    {
        if (this.ignoreChange || slotId != 0 && slotId != 1)
            return;
        this.invDialog?.UpdateContents();
        ICoreAPI api = this.Api;
        if ((api != null ? (api.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
            this.currentMesh = (MeshData) null;
        this.MarkDirty(true);
        this.FindMatchingRecipe();
    }

    protected void FindMatchingRecipe() => this.FindMatchingRecipe((IPlayer) null);

    protected void FindMatchingRecipe(IPlayer byPlayer)
    {
        if (Api.World.Calendar.MoonPhase != EnumMoonPhase.Full) return;//Should only work during a full moon
        ItemSlot[] inputSlots = new ItemSlot[2]
        {
            this.inventory[0],
            this.inventory[1]
        };
        this.CurrentRecipe = (QuartzBowlRecipe) null;
        //if (this.modSystem is null) this.modSystem = (MoonwaterModSystem) this.Api.ModLoader.GetModSystem<MoonwaterModSystem>();
        foreach (QuartzBowlRecipe quartzBowlRecipe in Api.GetQuartzBowlRecipes())
        {
            int outputStackSize;
            if (byPlayer == null ? quartzBowlRecipe.Matches(inputSlots, out outputStackSize) : quartzBowlRecipe.Matches(byPlayer, inputSlots, out outputStackSize))
            {
                Api.Logger.Event("a");
                this.ignoreChange = true;
                if (quartzBowlRecipe.SealHours > 0.0)
                {
                    Api.Logger.Event("b");
                    this.CurrentRecipe = quartzBowlRecipe;
                    this.CurrentOutSize = outputStackSize;
                }
                else
                {
                    Api.Logger.Event("c");
                    ICoreAPI api = this.Api;
                    if ((api != null ? (api.Side == EnumAppSide.Server ? 1 : 0) : 0) != 0)
                    {
                        Api.Logger.Event("d");
                        quartzBowlRecipe.TryCraftNow(this.Api, 0.0, inputSlots);
                        this.MarkDirty(true);
                        this.Api.World.BlockAccessor.MarkBlockEntityDirty(this.Pos);
                    }
                }
                Api.Logger.Event("e");
                this.invDialog?.UpdateContents();
                ICoreAPI api1 = this.Api;
                if ((api1 != null ? (api1.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
                {
                    this.currentMesh = (MeshData) null;
                    this.MarkDirty(true);
                }
                this.ignoreChange = false;
                break;
            }
            else
            {
                Api.Logger.Event("False");
            }
        }
    }

    protected void OnEvery3Second(float dt)
    {
        if (Api.World.Calendar.MoonPhase != EnumMoonPhase.Full)
        {
            return;
        }
        if (!(this.inventory[0].Empty && this.inventory[1].Empty) && this.CurrentRecipe == null)
            this.FindMatchingRecipe();
        if (this.CurrentRecipe != null)
        {
            Api.Logger.Event("Recipe found!");
            if (!this.CurrentRecipe.TryCraftNow(this.Api, this.Api.World.Calendar.TotalHours - this.SealedSinceTotalHours, new ItemSlot[2]
                {
                    this.inventory[0],
                    this.inventory[1]
                }))
                return;
            this.MarkDirty(true);
            this.Api.World.BlockAccessor.MarkBlockEntityDirty(this.Pos);
            this.Sealed = false;
        }
        else
        {
            Api.Logger.Event("No recipe found for quartz bowl");
            if (!this.Sealed)
                return;
            this.Sealed = false;
            this.MarkDirty(true);
        }
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        ItemSlot itemSlot1 = this.Inventory[0];
        ItemSlot itemSlot2 = this.Inventory[1];
        if (itemSlot1.Empty || !itemSlot2.Empty || BlockLiquidContainerBase.GetContainableProps(itemSlot1.Itemstack) == null)
            return;
        this.Inventory.TryFlipItems(1, itemSlot1);
    }

    public override void OnBlockBroken(IPlayer byPlayer = null)
    {
        if (!this.Sealed)
            base.OnBlockBroken(byPlayer);
        this.invDialog?.TryClose();
        this.invDialog = (GuiDialogQuartzBowl) null;
    }

    /*public void SealBarrel()
    {
        if (this.Sealed)
            return;
        this.Sealed = true;
        this.SealedSinceTotalHours = this.Api.World.Calendar.TotalHours;
        this.MarkDirty(true);
    }*/

    public void OnPlayerRightClick(IPlayer byPlayer)
    {
        if (this.Sealed)
            return;
        this.FindMatchingRecipe(byPlayer);
        if (this.Api.Side != EnumAppSide.Client)
            return;
        this.toggleInventoryDialogClient(byPlayer);
    }

    protected void toggleInventoryDialogClient(IPlayer byPlayer)//TODO:Redo GUI for Quartz Bowl
    {
        if (this.invDialog == null)
        {
            ICoreClientAPI capi = this.Api as ICoreClientAPI;
            this.invDialog = new GuiDialogQuartzBowl(Lang.Get("Barrel"), this.Inventory, this.Pos, this.Api as ICoreClientAPI);
            this.invDialog.OnClosed += (Action) (() =>
            {
                this.invDialog = (GuiDialogQuartzBowl) null;
                capi.Network.SendBlockEntityPacket(this.Pos, 1001);
                capi.Network.SendPacketClient(this.Inventory.Close(byPlayer));
            });
            GuiDialogQuartzBowl invDialog1 = this.invDialog;
            SoundAttributes? nullable = (SoundAttributes?) this.Block.Attributes?["openSound"]?.AsObject<SoundAttributes?>(new SoundAttributes?(), this.Block.Code.Domain, true);
            SoundAttributes soundAttributes1 = nullable ?? BEQuartzBowl.barrelOpen;
            invDialog1.OpenSound = soundAttributes1;
            GuiDialogQuartzBowl invDialog2 = this.invDialog;
            nullable = (SoundAttributes?) this.Block.Attributes?["closeSound"]?.AsObject<SoundAttributes?>(new SoundAttributes?(), this.Block.Code.Domain, true);
            SoundAttributes soundAttributes2 = nullable ?? BEQuartzBowl.barrelClose;
            invDialog2.CloseSound = soundAttributes2;
            this.invDialog.TryOpen();
            capi.Network.SendPacketClient(this.Inventory.Open(byPlayer));
            capi.Network.SendBlockEntityPacket(this.Pos, 1000);
        }
        else
            this.invDialog.TryClose();
    }

    public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
    {
        base.OnReceivedClientPacket(player, packetid, data);
        if (packetid < 1000)
        {
            if (!new BlockEntity.CachedAccessPerms(this.Api.World, this.Pos, player).IsInteractingPlayerAllowedTo(EnumBlockAccessFlags.Use, true, "barrel"))
            {
                this.Inventory.InvNetworkUtil.SendInventoryRollback((IServerPlayer) player, packetid, (ReadOnlySpan<byte>) data);
            }
            else
            {
                this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
            }
        }
        else
        {
            if (packetid == 1001)
                player.InventoryManager?.CloseInventory((IInventory) this.Inventory);
            if (packetid == 1000)
            {
                if (!new BlockEntity.CachedAccessPerms(this.Api.World, this.Pos, player).IsInteractingPlayerAllowedTo(EnumBlockAccessFlags.Use, true, "quartzbowl"))
                    return;
                player.InventoryManager?.OpenInventory((IInventory) this.Inventory);
            }
            if (packetid != 1337)
                return;
            if (!new BlockEntity.CachedAccessPerms(this.Api.World, this.Pos, player).IsInteractingPlayerAllowedTo(EnumBlockAccessFlags.Use, true, "quartzbowl"))
                ((ICoreServerAPI) this.Api).Network.SendBlockEntityPacket((IServerPlayer) player, this.Pos, 1338);
            //else
                //this.SealBarrel();
        }
    }

    public override void OnReceivedServerPacket(int packetid, byte[] data)
    {
        base.OnReceivedServerPacket(packetid, data);
        switch (packetid)
        {
            case 1001:
                (this.Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory((IInventory) this.Inventory);
                this.invDialog?.TryClose();
                this.invDialog?.Dispose();
                this.invDialog = (GuiDialogQuartzBowl) null;
                break;
            case 1338:
                this.Sealed = false;
                this.SealedSinceTotalHours = 0.0;
                this.currentMesh = (MeshData) null;
                this.MarkDirty(true);
                break;
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        this.Sealed = tree.GetBool("sealed");
        ICoreAPI api = this.Api;
        if ((api != null ? (api.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
        {
            this.currentMesh = (MeshData) null;
            this.MarkDirty(true);
            this.invDialog?.UpdateContents();
        }
        this.SealedSinceTotalHours = tree.GetDouble("sealedSinceTotalHours");
        if (this.Api == null)
            return;
        this.FindMatchingRecipe();
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetBool("sealed", this.Sealed);
        tree.SetDouble("sealedSinceTotalHours", this.SealedSinceTotalHours);
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        this.invDialog?.Dispose();
    }

    public void CoolNow(ItemSlot slot, Vec3d pos, float dt, bool playSizzle = true)
    {
        ItemSlot itemSlot = this.Inventory[1];
        if (itemSlot.Empty)
            return;
        itemSlot.Itemstack.Collectible.GetCollectibleInterface<ICoolingMedium>()?.CoolNow(slot, pos, dt, playSizzle);
    }

    public bool CanCool(ItemSlot slot, Vec3d pos)
    {
        ItemSlot itemSlot = this.Inventory[1];
        if (itemSlot.Empty)
            return false;
        ICoolingMedium collectibleInterface = itemSlot.Itemstack.Collectible.GetCollectibleInterface<ICoolingMedium>();
        return collectibleInterface != null && collectibleInterface.CanCool(slot, pos);
    }
}