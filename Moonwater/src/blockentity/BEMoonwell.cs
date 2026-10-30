using Vintagestory.GameContent;
using System;
using System.Collections.Generic;
using System.Text;
using Moonwater.blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Collectible.Block;

namespace Moonwater.blockentity;

public class BEMoonwell : BlockEntityLiquidContainer, ICoolingMedium
{
    private GuiDialogQuartzBowl invDialog; //Need to make custom GUI for this
    private long moonPhaseListenerId;
    private MeshData currentMesh;
    private BlockQuartzBowl ownBlock;
    public double TotalHoursMoonlightReceived;
    public bool receivesMoonlight;
    public QuartzBowlRecipe CurrentRecipe;
    public int CurrentOutSize;

    protected static SoundAttributes barrelOpen =
        new SoundAttributes(AssetLocation.Create("sounds/block/vesselopen"), true);

    protected static SoundAttributes barrelClose =
        new SoundAttributes(AssetLocation.Create("sounds/block/vesselclose"), true);

    private bool ignoreChange;

    private int checkedMoonLoS = 0;
    private bool MoonLoS = false;
    //private MoonwaterModSystem modSystem;

    public int CapacityLitres { get; set; } = 25;

    public override string InventoryClassName => "moonwell";

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

    public BEMoonwell()
    {
        this.inventory = new InventoryGeneric(1, (string)null, (ICoreAPI)null,
            (NewSlotDelegate)((id, self) =>
                (ItemSlot)new ItemSlotLiquidOnly((InventoryBase)self, (float)this.CapacityLitres)));
        this.inventory.BaseWeight = 1f;
        this.inventory.OnGetSuitability = new GetSuitabilityDelegate(this.GetSuitability);
        this.inventory.SlotModified += new Action<int>(this.Inventory_SlotModified);
        this.inventory.OnAcquireTransitionSpeed +=
            new CustomGetTransitionSpeedMulDelegate(this.Inventory_OnAcquireTransitionSpeed1);
    }

    protected float Inventory_OnAcquireTransitionSpeed1(
        EnumTransitionType transType,
        ItemStack stack,
        float mul)
    {
        int num;

        num = 1;
        label_4:
        return (float)num;
    }

    protected float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
    {
        if (targetSlot == this.inventory[0])
        {
            ItemStack itemstack1 = this.inventory[0].Itemstack;
            ItemStack itemstack2 = sourceSlot.Itemstack;
            if (itemstack1.Collectible.Equals(itemstack1, itemstack2, GlobalConstants.IgnoredStackAttributes))
                return -1f;
        }

        return (isMerge ? this.inventory.BaseWeight + 3f : this.inventory.BaseWeight + 1f) +
               (float)(sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
    }

    protected override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
    {
        return atBlockFace == BlockFacing.UP ? this.inventory[0] : (ItemSlot)null;
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        this.ownBlock = this.Block as BlockQuartzBowl;
        JsonObject attribute = this.ownBlock?.Attributes?["capacityLitres"];
        if (attribute != null && attribute.Exists)
        {
            this.CapacityLitres = attribute.AsInt(1);
            (this.inventory[1] as ItemSlotLiquidOnly).CapacityLitres = (float)this.CapacityLitres;
        }

        if (api.Side == EnumAppSide.Server)
            this.RegisterGameTickListener(new Action<float>(this.OnEvery3Second), 3000);
        this.FindMatchingRecipe();
        base.Initialize(api);
        this.structure = ((CollectibleObject)this.Block).Attributes["multiblockStructure"]
            .AsObject<MultiblockStructure>((MultiblockStructure)null);
        /*if (this.Orientation == null)
            return;*/
        this.Init();
    }

    protected void Inventory_SlotModified(int slotId)
    {
        if (this.ignoreChange || slotId != 0 && slotId != 1)
            return;
        this.invDialog?.UpdateContents();
        ICoreAPI api = this.Api;
        if ((api != null ? (api.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
            this.currentMesh = (MeshData)null;
        this.MarkDirty(true);
        this.FindMatchingRecipe();
    }

    protected void FindMatchingRecipe() => this.FindMatchingRecipe((IPlayer)null);

    protected void FindMatchingRecipe(IPlayer byPlayer)
    {
        if (!CraftingRequirements()) return; //Should only work during a full moon
        ItemSlot[] inputSlots = new ItemSlot[1]
        {
            this.inventory[0]
        };
        this.CurrentRecipe = (QuartzBowlRecipe)null;
        //if (this.modSystem is null) this.modSystem = (MoonwaterModSystem) this.Api.ModLoader.GetModSystem<MoonwaterModSystem>();
        foreach (QuartzBowlRecipe quartzBowlRecipe in Api.GetQuartzBowlRecipes())
        {
            int outputStackSize;
            if (byPlayer == null
                    ? quartzBowlRecipe.Matches(inputSlots, out outputStackSize)
                    : quartzBowlRecipe.Matches(byPlayer, inputSlots, out outputStackSize))
            {
                //Api.Logger.Event("a");
                this.ignoreChange = true;

                Api.Logger.Event("c");
                ICoreAPI api = this.Api;
                if ((api != null ? (api.Side == EnumAppSide.Server ? 1 : 0) : 0) != 0)
                {
                    //Api.Logger.Event("d");
                    quartzBowlRecipe.TryCraftNow(this.Api, inputSlots);
                    this.MarkDirty(true);
                    this.Api.World.BlockAccessor.MarkBlockEntityDirty(this.Pos);
                }

                //Api.Logger.Event("e");
                this.invDialog?.UpdateContents();
                ICoreAPI api1 = this.Api;
                if ((api1 != null ? (api1.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
                {
                    this.currentMesh = (MeshData)null;
                    this.MarkDirty(true);
                }

                this.ignoreChange = false;
                break;
            }
        }
    }

    protected void OnEvery3Second(float dt)
    {
        if (!CraftingRequirements(true))
        {
            return;
        }

        if (!(this.inventory[0].Empty && this.inventory[1].Empty) && this.CurrentRecipe == null)
            this.FindMatchingRecipe();
        if (this.CurrentRecipe != null)
        {
            //Api.Logger.Event("Recipe found!");
            if (!this.CurrentRecipe.TryCraftNow(this.Api, new ItemSlot[1]
                {
                    this.inventory[0]
                }))
                return;
            /*this.Inventory.TryFlipItems(1, this.inventory[0]);*/
            this.MarkDirty(true);
            this.Api.World.BlockAccessor.MarkBlockEntityDirty(this.Pos);
        }
        else
        {
            //Api.Logger.Event("No recipe found for quartz bowl");
            this.MarkDirty(true);
        }
    }

    protected bool CraftingRequirements(bool ignoreMoonLoS = false)
    {
        if (!ignoreMoonLoS) checkedMoonLoS--;
        if (!IsIsolated() || !IsFullMoon() || !IsNightTime() || !MoonIsUp() || !MinimumHeight())
        {
            return false;
        }

        if (!ignoreMoonLoS)
        {
            //Api.Logger.Event("checkedMoonLoS: " + checkedMoonLoS);
            if (checkedMoonLoS > 0)
            {
                if (MoonLoS) return true;
                else return false;
            }

            checkedMoonLoS = 10; //Only check MoonLoS every 30 seconds to minimize lag
            MoonLoS = MoonLineOfSight();
            return MoonLoS;
        }

        return true;
    }

    protected bool IsIsolated()
    {
        int searchRadius = 128;
        int count = 0;
        Api.World.BlockAccessor.SearchBlocks(
            new BlockPos(Pos.X - searchRadius, Pos.Y - searchRadius, Pos.Z - searchRadius),
            new BlockPos(Pos.X + searchRadius, Pos.Y + searchRadius, Pos.Z + searchRadius),
            (blockPos, block) =>
            {
                if (block is BlockQuartzBowl)
                {
                    count++;
                    if (count > 1)
                    {
                        return false;
                    }
                }

                return true;
            });
        if (count <= 1)
        {
            //Api.Logger.Event("Moonwell is isolated");
            return true;
        }

        return false;
    }

    protected bool IsNightTime()
    {
        //Checking if the sun is below the horizon
        if (Api.World.Calendar.GetSunPosition(Pos.ToVec3d(), Api.World.Calendar.TotalDays).Y > 0)
        {
            return false;
        }

        //Api.Logger.Event("Sun is below horizon");
        return true;
    }

    protected bool MoonIsUp()
    {
        if (Api.World.Calendar.GetMoonPosition(Pos.ToVec3d(), Api.World.Calendar.ElapsedDays).Y >=
            0.05) //0.05 is chosen arbitrarily
        {
            //Api.Logger.Error("Moon is below horizon, y = " + Api.World.Calendar.GetMoonPosition(Pos.ToVec3d(), Api.World.Calendar.ElapsedDays).Y);
            return false; //0.05 is chosen arbitrarily
        }

        //Api.Logger.Event("Moon is above horizon");
        return true;
    }

    protected bool IsFullMoon()
    {
        if (Api.World.Calendar.MoonPhase != EnumMoonPhase.Full)
        {
            return false;
        }

        //Api.Logger.Event("Full Moon");
        return true;
    }

    protected bool MinimumHeight()
    {
        float minHeightMult = 0.8f;
        if (this.Pos.Y < Math.Floor(Api.World.BlockAccessor.MapSizeY * minHeightMult)) return false;
        //Api.Logger.Event("Moonwell is High enough");
        return true;
    }

    protected bool MoonLineOfSight()
    {
        //Api.Logger.Event("Checking Moon Line of Sight");
        Vec3d moonVec = Api.World.Calendar.GetMoonPosition(Pos.ToVec3d(), Api.World.Calendar.ElapsedDays).ToVec3d()
            .Normalize();
        moonVec *= -128; //Correcting for the moon being oriented strangely
        //Api.Logger.Event("Moon vector: " + moonVec);
        Vec3d origin = Pos.ToVec3d().Add(0.5, 1, 0.5);

        Ray ray = Ray.FromPositions(origin, origin + moonVec);
        BlockSelection? blockSel = null;
        EntitySelection? entitySel = null;
        //Api.Logger.Event("Beginning ray trace");
        Api.World.RayTraceForSelection(ray, ref blockSel, ref entitySel, (pos, block) =>
        {
            //if (blockSel != null) Api.Logger.Event("Block selection found: " + blockSel.Block.Code);
            //Api.Logger.Event("Checking if block is obstructing LoS to moon: " + block.BlockId);
            if (block.AllSidesOpaque || block.LightAbsorption >= 16)
            {
                //Api.Logger.Event("Obstruction found!");
                return true; //return true to say this block is obstructing LoS to the moon
            }

            //Api.Logger.Event("No obstruction found!");
            return false;
        });
        if (blockSel == null)
        {
            //Api.Logger.Event("Block obstructing LoS to moon!");
            return true;
        }

        //Api.Logger.Event("No Obstruction found!");
        return false;
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        ItemSlot itemSlot1 = this.Inventory[0];
        if (itemSlot1.Empty || BlockLiquidContainerBase.GetContainableProps(itemSlot1.Itemstack) == null)
            return;
    }

    public override void OnBlockBroken(IPlayer byPlayer = null)
    {
        this.invDialog?.TryClose();
        this.invDialog = (GuiDialogQuartzBowl)null;
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
        this.FindMatchingRecipe(byPlayer);
        if (this.Api.Side != EnumAppSide.Client)
            return;
        this.toggleInventoryDialogClient(byPlayer);
    }

    protected void toggleInventoryDialogClient(IPlayer byPlayer) //TODO:Redo GUI for Quartz Bowl
    {
        if (this.invDialog == null)
        {
            ICoreClientAPI capi = this.Api as ICoreClientAPI;
            this.invDialog = new GuiDialogQuartzBowl(Lang.Get("Quartz Bowl"), this.Inventory, this.Pos,
                this.Api as ICoreClientAPI);
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
            if (!new BlockEntity.CachedAccessPerms(this.Api.World, this.Pos, player).IsInteractingPlayerAllowedTo(
                    EnumBlockAccessFlags.Use, true, "moonwell"))
            {
                this.Inventory.InvNetworkUtil.SendInventoryRollback((IServerPlayer)player, packetid,
                    (ReadOnlySpan<byte>)data);
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
                player.InventoryManager?.CloseInventory((IInventory)this.Inventory);
            if (packetid == 1000)
            {
                if (!new BlockEntity.CachedAccessPerms(this.Api.World, this.Pos, player).IsInteractingPlayerAllowedTo(
                        EnumBlockAccessFlags.Use, true, "moonwell"))
                    return;
                player.InventoryManager?.OpenInventory((IInventory)this.Inventory);
            }

            if (packetid != 1337)
                return;
            if (!new BlockEntity.CachedAccessPerms(this.Api.World, this.Pos, player).IsInteractingPlayerAllowedTo(
                    EnumBlockAccessFlags.Use, true, "moonwell"))
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)player, this.Pos, 1338);
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
                (this.Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(
                    (IInventory)this.Inventory);
                this.invDialog?.TryClose();
                this.invDialog?.Dispose();
                this.invDialog = (GuiDialogQuartzBowl)null;
                break;
            case 1338:
                this.currentMesh = (MeshData)null;
                this.MarkDirty(true);
                break;
        }
    }

    /*public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        ICoreAPI api = this.Api;
        if ((api != null ? (api.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
        {
            this.currentMesh = (MeshData)null;
            this.MarkDirty(true);
            this.invDialog?.UpdateContents();
        }

        if (this.Api == null)
            return;
        this.FindMatchingRecipe();
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        this.invDialog?.Dispose();
    }*/

    public void
        CoolNow(ItemSlot slot, Vec3d pos, float dt,
            bool playSizzle = true) //Moonwater should have interesting quenching properties
    {
        ItemSlot itemSlot = this.Inventory[0];
        if (itemSlot.Empty)
            return;
        itemSlot.Itemstack.Collectible.GetCollectibleInterface<ICoolingMedium>()?.CoolNow(slot, pos, dt, playSizzle);
    }

    public bool CanCool(ItemSlot slot, Vec3d pos)
    {
        ItemSlot itemSlot = this.Inventory[0];
        if (itemSlot.Empty)
            return false;
        ICoolingMedium collectibleInterface = itemSlot.Itemstack.Collectible.GetCollectibleInterface<ICoolingMedium>();
        return collectibleInterface != null && collectibleInterface.CanCool(slot, pos);
    }

    //public BlockFacing Orientation;
    private MultiblockStructure structure;
    private MultiblockStructure highlightedStructure;
    private BlockPos CenterPos;
    public double TotalHoursLastUpdate;
    public bool StructureComplete;
    private int tickCounter;
    private bool wasNotProcessing;
    private BlockPos[] particlePositions;

    public override void OnPlacementBySchematic(
        ICoreServerAPI api,
        IBlockAccessor blockAccessor,
        BlockPos pos,
        Dictionary<int, Dictionary<int, int>> replaceBlocks,
        int centerrockblockid,
        Vintagestory.API.Common.Block layerBlock,
        bool resolveImports)
    {
        base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock,
            resolveImports);
        if (!CenterPos.Equals((BlockPos)null))
            return;
        Init();
    }

    public void Init()
    {
        if (Api.Side == EnumAppSide.Client)
            RegisterGameTickListener(new Action<float>(this.OnClientTick50ms), 50, 0);
        else
            RegisterGameTickListener(new Action<float>(this.OnServerTick1s), 1000, 0);
        int num = 0;

        this.structure.InitForUse((float)num);
        this.particlePositions = new BlockPos[10];
        BlockPos blockPos = this.CenterPos.Down(1);
        
        //this.beBehaviorDoor = this.GetBehavior<BEBehaviorDoor>();
    }

    private void OnClientTick50ms(float dt)
    {
        /*this.receivesHeatSmooth = GameMath.Clamp(this.receivesHeatSmooth + (this.receivesHeat ? dt / 10f : (float) (-(double) dt / 3.0)), 0.0f, 1f);
        if ((double) this.receivesHeatSmooth == 0.0)
          return;
        Random rand = this.Api.World.Rand;
        for (int index1 = 0; index1 < Entity.FireParticleProps.Length; ++index1)
        {
          int index2 = Math.Min(Entity.FireParticleProps.Length - 1, this.Api.World.Rand.Next(Entity.FireParticleProps.Length + 1));
          for (int index3 = 0; index3 < this.particlePositions.Length; ++index3)
          {
            AdvancedParticleProperties particleProperties = Entity.FireParticleProps[index2];
            BlockPos particlePosition = this.particlePositions[index3];
            if (index3 == 9)
            {
              particleProperties = BlockEntityBeeHiveKiln.smokeParticles;
              particleProperties.Quantity.avg = 0.2f;
              particleProperties.basePos.Set((double) particlePosition.X + 0.5, (double) particlePosition.InternalY + 0.75, (double) particlePosition.Z + 0.5);
              particleProperties.Velocity[1].avg = (float) (0.3 + 0.3 * rand.NextDouble()) * 2f;
              particleProperties.PosOffset[1].var = 0.2f;
              particleProperties.Velocity[0].avg = (float) (rand.NextDouble() - 0.5) / 4f;
              particleProperties.Velocity[2].avg = (float) (rand.NextDouble() - 0.5) / 4f;
            }
            else
            {
              NatFloat quantity = particleProperties.Quantity;
              double num1;
              switch (index2)
              {
                case 0:
                  num1 = 0.5;
                  break;
                case 1:
                  num1 = 5.0;
                  break;
                default:
                  num1 = 0.6000000238418579;
                  break;
              }
              double num2 = (double) GameMath.Sqrt((float) (0.5 * num1)) / 4.0;
              quantity.avg = (float) num2;
              particleProperties.basePos.Set((double) particlePosition.X + 0.5, (double) particlePosition.InternalY + 0.5, (double) particlePosition.Z + 0.5);
              particleProperties.Velocity[1].avg = (float) (0.5 + 0.5 * rand.NextDouble()) * 2f;
              particleProperties.PosOffset[1].var = 1f;
              particleProperties.Velocity[0].avg = (float) (rand.NextDouble() - 0.5);
              particleProperties.Velocity[2].avg = (float) (rand.NextDouble() - 0.5);
            }
            particleProperties.PosOffset[0].var = 0.49f;
            particleProperties.PosOffset[2].var = 0.49f;
            this.Api.World.SpawnParticles((IParticlePropertiesProvider) particleProperties, (IPlayer) null);
          }
        }*/
    }

    private void OnServerTick1s(float dt)
    {
        /*if (this.receivesHeat)
        {
            Vec3d vec3d = this.CenterPos.ToVec3d().Add(0.5, 0.0, 0.5);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: method pointer
            foreach (Entity entity in this.Api.World.GetEntitiesAround(vec3d, 1.75f, 3f,
                         BlockEntityBeeHiveKiln.\u003C\u003Ec.\u003C\u003E9__23_0 ??
                         (BlockEntityBeeHiveKiln.\u003C\u003Ec.\u003C\u003E9__23_0 =
                             new ActionConsumable<Entity>((object)BlockEntityBeeHiveKiln.\u003C\u003Ec.\u003C\u003E9,
                                 __methodptr(\u003COnServerTick1s\u003Eb__23_0)))))
                entity.ReceiveDamage(new DamageSource()
                {
                    DamageTier = 1,
                    SourcePos = vec3d,
                    SourceBlock = this.Block,
                    Type = (EnumDamageType)1
                }, 4f);
        }*/

        if (++this.tickCounter % 3 != 0)
            return;
        this.OnServerTick3s();
    }

    private void OnServerTick3s()
    {
        double totalHours = this.Api.World.Calendar.TotalHours;
        float val1 = float.MaxValue;
        BlockPos blockPos = new BlockPos(this.Pos.dimension);
        for (int index = 0; index < 9; ++index)
        {
            blockPos.Set(this.particlePositions[index].X, this.particlePositions[index].Y - 1,
                this.particlePositions[index].Z);
            BlockEntity blockEntity = this.Api.World.BlockAccessor.GetBlockEntity(blockPos);
            if (blockEntity == null && this.Api.World.BlockAccessor.GetChunkAtBlockPos(blockPos) == null)
                return;
            float val2 = 0.0f;
            if (blockEntity is IExternalTickable iexternalTickable)
                iexternalTickable.SetExternallyTicked();
            if (blockEntity is BlockEntityCoalPile blockEntityCoalPile && blockEntityCoalPile.IsBurning)
                val2 = blockEntityCoalPile.GetHoursLeft(totalHours);
            else if (blockEntity is BlockEntityGroundStorage entityGroundStorage && entityGroundStorage.IsBurning)
                val2 = entityGroundStorage.GetHoursLeft(totalHours);
            val1 = Math.Min(val1, val2);
        }

        bool markDirty = false;
        int num1 = this.receivesMoonlight ? 1 : 0;
        bool structureComplete = this.StructureComplete;
        if (!this.receivesMoonlight)
            this.TotalHoursLastUpdate = totalHours;
        this.receivesMoonlight = (double)val1 > 0.0;
        this.StructureComplete =
            this.structure.InCompleteBlockCount(this.Api.World, this.Pos, (PositionMismatchDelegate)null) == 0;
        int num2 = this.receivesMoonlight ? 1 : 0;
        if (num1 != num2 || structureComplete != this.StructureComplete)
            markDirty = true;
        if (this.receivesMoonlight)
        {
            if (!this.StructureComplete)
            {
                this.wasNotProcessing = true;
                this.TotalHoursLastUpdate = totalHours;
                this.MarkDirty(false, (IPlayer)null);
                return;
            }

            if (this.wasNotProcessing)
            {
                this.wasNotProcessing = false;
                this.TotalHoursLastUpdate = totalHours;
            }

            float hoursMoonlightReceived =
                Math.Max(0.0f, GameMath.Min((float)(totalHours - this.TotalHoursLastUpdate), val1));
            this.TotalHoursMoonlightReceived += (double)hoursMoonlightReceived;
            this.UpdateGroundStorage(hoursMoonlightReceived);
            this.TotalHoursLastUpdate = totalHours;
            markDirty = true;
        }

        /*if (this.TotalHoursMoonlightReceived >= (double)BlockEntityBeeHiveKiln.KilnBreakAfterHours)//Moonwell should require maintenance
        {
            this.TotalHoursMoonlightReceived = 0.0;
            this.structure.WalkMatchingBlocks(this.Api.World, this.Pos,
                (Action<Vintagestory.API.Common.Block, BlockPos>)((block, pos) =>
                {
                    JsonObject attributes = ((CollectibleObject)block).Attributes;
                    float num3 = attributes != null ? attributes["heatResistance"].AsFloat(1f) : 1f;
                    if (this.Api.World.Rand.NextDouble() <= (double)num3)
                        return;
                    this.Api.World.BlockAccessor.SetBlock(
                        ((CollectibleObject)this.Api.World.GetBlock(
                            ((RegistryObject)block).CodeWithVariant("state", "damaged"))).Id, pos);
                    this.StructureComplete = false;
                    markDirty = true;
                }));
        }*/

        if (markDirty)
            this.MarkDirty(false, (IPlayer)null);
    }

    private void UpdateGroundStorage(float hoursMoonlightReceived)
    {
        for (int i = 0; i < 9; ++i)
        {
            for (int j = 1; j < 4; ++j)
            {
                BlockEntityGroundStorage blockEntity =
                    this.Api.World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(this.particlePositions[i]
                        .UpCopy(j));
                if (blockEntity != null)
                {
                    for (int k = 0; k < blockEntity.Inventory.Count; ++k)
                    {
                        ItemSlot itemSlot = blockEntity.Inventory[k];
                        if (!itemSlot.Empty)
                        {
                            float moonTime = 0.0f;
                            CollectibleObject collectible = itemSlot.Itemstack.Collectible;
                            CombustibleProperties combustibleProperties =
                                collectible.GetCombustibleProperties(this.Api.World, itemSlot.Itemstack,
                                    (BlockPos)null);

                            float num2 = itemSlot.Itemstack.Collectible.GetTemperature(this.Api.World,
                                itemSlot.Itemstack, (double)hoursMoonlightReceived);
                            float num3 = hoursMoonlightReceived * (float)BlockEntityBeeHiveKiln.ItemTemperatureGainPerHour;
                            float num4 = ((float)BlockEntityBeeHiveKiln.ItemBurnTemperature - num2) /
                                         (float)BlockEntityBeeHiveKiln.ItemTemperatureGainPerHour;
                            if ((double)num4 < 0.0)
                                num4 = 0.0f;
                            if ((double)num2 < (double)BlockEntityBeeHiveKiln.ItemMaxTemperature)
                            {
                                num2 = GameMath.Min((float)BlockEntityBeeHiveKiln.ItemMaxTemperature, num2 + num3);
                                collectible.SetTemperature(this.Api.World, itemSlot.Itemstack, num2, true);
                            }

                            float num5 = hoursMoonlightReceived - num4;
                            if ((double)num2 >= (double)BlockEntityBeeHiveKiln.ItemBurnTemperature &&
                                (double)num5 > 0.0)
                            {
                                moonTime = itemSlot.Itemstack.Attributes.GetFloat(nameof(hoursMoonlightReceived), 0.0f) + num5;
                                itemSlot.Itemstack.Attributes.SetFloat(nameof(hoursMoonlightReceived), moonTime);
                            }

                            itemSlot.MarkDirty();
                        }
                    }

                    blockEntity.MarkDirty(false, (IPlayer)null);
                }
            }
        }
    }

    /*public void Interact(IPlayer byPlayer)
    {
        // ISSUE: object of a compiler-generated type is created
        // ISSUE: variable of a compiler-generated type
        BlockEntityBeeHiveKiln.\u003C\u003Ec__DisplayClass29_0 cDisplayClass290 =
            new BlockEntityBeeHiveKiln.\u003C\u003Ec__DisplayClass29_0();
        if (!(this.Api is ICoreClientAPI api))
            return;
        int num1 = byPlayer.WorldData.EntityControls.ShiftKey ? 1 : 0;
        // ISSUE: reference to a compiler-generated field
        cDisplayClass290.damagedTiles = 0;
        // ISSUE: reference to a compiler-generated field
        cDisplayClass290.wrongTiles = 0;
        int num2 = 0;
        BlockPos pos = this.Pos;
        if (num1 == 0)
        {
            // ISSUE: method pointer
            num2 = this.structure.InCompleteBlockCount(this.Api.World, this.Pos,
                new PositionMismatchDelegate((object)cDisplayClass290, __methodptr(\u003CInteract\u003Eb__0)));
            if (num2 > 0)
                this.highlightedStructure = this.structure;
        }

        if (num1 == 0 && num2 > 0)
        {
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            if (cDisplayClass290.wrongTiles > 0 && cDisplayClass290.damagedTiles > 0)
            {
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                api.TriggerIngameError((object)this, "incomplete", Lang.Get(
                    "Structure is not complete, {0} blocks are missing or wrong, {1} tiles are damaged!", new object[2]
                    {
                        (object)cDisplayClass290.wrongTiles,
                        (object)cDisplayClass290.damagedTiles
                    }));
            }
            else
            {
                // ISSUE: reference to a compiler-generated field
                if (cDisplayClass290.wrongTiles > 0)
                {
                    // ISSUE: reference to a compiler-generated field
                    api.TriggerIngameError((object)this, "incomplete", Lang.Get(
                        "Structure is not complete, {0} blocks are missing or wrong!", new object[1]
                        {
                            (object)cDisplayClass290.wrongTiles
                        }));
                }
                else
                {
                    // ISSUE: reference to a compiler-generated field
                    if (cDisplayClass290.damagedTiles == 1)
                    {
                        // ISSUE: reference to a compiler-generated field
                        api.TriggerIngameError((object)this, "incomplete", Lang.Get(
                            "Structure is not complete, {0} tile is damaged!", new object[1]
                            {
                                (object)cDisplayClass290.damagedTiles
                            }));
                    }
                    else
                    {
                        // ISSUE: reference to a compiler-generated field
                        api.TriggerIngameError((object)this, "incomplete", Lang.Get(
                            "Structure is not complete, {0} tiles are damaged!", new object[1]
                            {
                                (object)cDisplayClass290.damagedTiles
                            }));
                    }
                }
            }

            this.highlightedStructure.HighlightIncompleteParts(this.Api.World, byPlayer, pos);
        }
        else
            this.highlightedStructure?.ClearHighlights(this.Api.World, byPlayer);
    }*/
    
    public void Interact(IPlayer byPlayer)
    {
        if (!(this.Api is ICoreClientAPI api))
            return;
        int shiftDown = byPlayer.WorldData.EntityControls.ShiftKey ? 1 : 0;
        int damagedTiles = 0;
        int wrongTiles = 0;
        int badTiles = 0;
        BlockPos pos = this.Pos;
        if (shiftDown == 0)
        {
            badTiles = this.structure.InCompleteBlockCount(this.Api.World, this.Pos, (PositionMismatchDelegate) ((haveBlock, wantLoc) =>
            {
                /*string str = haveBlock.FirstCodePart();
                if ((str == "quartzpillar" || str == "quartzslab" || str == "quartzstairs") && haveBlock.Variant["state"] == "damaged")
                    ++damagedTiles;
                else*/
                    ++wrongTiles;
            }));
            if (badTiles > 0)
                this.highlightedStructure = this.structure;
        }
        if (shiftDown == 0 && badTiles > 0)
        {
            if (wrongTiles > 0 && damagedTiles > 0)
                api.TriggerIngameError((object) this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong, {1} tiles are damaged!", (object) wrongTiles, (object) damagedTiles));
            else if (wrongTiles > 0)
                api.TriggerIngameError((object) this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong!", (object) wrongTiles));
            else if (damagedTiles == 1)
                api.TriggerIngameError((object) this, "incomplete", Lang.Get("Structure is not complete, {0} tile is damaged!", (object) damagedTiles));
            else
                api.TriggerIngameError((object) this, "incomplete", Lang.Get("Structure is not complete, {0} tiles are damaged!", (object) damagedTiles));
            this.highlightedStructure.HighlightIncompleteParts(this.Api.World, byPlayer, pos);
        }
        else
            this.highlightedStructure?.ClearHighlights(this.Api.World, byPlayer);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        this.receivesMoonlight = tree.GetBool("receivesMoonlight", false);
        this.TotalHoursLastUpdate = tree.GetDouble("totalHoursLastUpdate", 0.0);
        this.StructureComplete = tree.GetBool("structureComplete", false);
        this.TotalHoursMoonlightReceived = tree.GetDouble("totalHoursMoonlightReceived", 0.0);
        if (!(this.Api is ICoreClientAPI) || this.structure.TransformedOffsets != null)
            return;
        this.Init();
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetBool("receivesMoonlight", this.receivesMoonlight);
        tree.SetDouble("totalHoursLastUpdate", this.TotalHoursLastUpdate);
        tree.SetBool("structureComplete", this.StructureComplete);
        tree.SetDouble("totalHoursMoonlightReceived", this.TotalHoursMoonlightReceived);
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        if (Api is ICoreClientAPI api)
            highlightedStructure?.ClearHighlights(Api.World, api.World.Player);
        if (!(Api is ICoreServerAPI))
            return;
        for (int index = 0; index < 9; ++index)
        {
            if (Api.World.BlockAccessor.GetBlockEntity(particlePositions[index].DownCopy()) is
                IExternalTickable blockEntity)
                blockEntity.SetInternallyTicked();
        }
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        if (!(this.Api is ICoreClientAPI api))
            return;
        this.highlightedStructure?.ClearHighlights(this.Api.World, (IPlayer)api.World.Player);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        if (!this.StructureComplete)
        {
            dsc.AppendLine(Lang.Get("Structure incomplete! Can't contain fluids, paused.", Array.Empty<object>()));
        }
        else
        {
            if (this.receivesMoonlight)
                dsc.AppendLine(Lang.Get("Okay! Receives moonlight!", Array.Empty<object>()));
            else
                dsc.AppendLine(Lang.Get(
                    "Ready to be fired. Ignite 3x3 piles of coal below. (progress will proceed once the moonwell has line of sight to a full moon)",
                    Array.Empty<object>()));
            if (this.TotalHoursMoonlightReceived <= 0.0)
                return;
            dsc.AppendLine(Lang.Get("Basking: for {0:0.##} hours", new object[1]
            {
                (object)this.TotalHoursMoonlightReceived
            }));
        }
    }

    /*public void OnTransformed(
      IWorldAccessor worldAccessor,
      ITreeAttribute tree,
      int degreeRotation,
      Dictionary<int, AssetLocation> oldBlockIdMapping,
      Dictionary<int, AssetLocation> oldItemIdMapping,
      EnumAxis? flipAxis)
    {
      BlockFacing horizontalRotated = BlockFacing.FromFirstLetter(tree.GetString("orientation", (string) null)).GetHorizontalRotated(-degreeRotation - 180);
      tree.SetString("orientation", horizontalRotated.Code);
      float num = (float) (((double) tree.GetFloat("rotateYRad", 0.0f) - (double) degreeRotation * (Math.PI / 180.0)) % 6.2831854820251465);
      tree.SetFloat("rotateYRad", num);
    }*/
}