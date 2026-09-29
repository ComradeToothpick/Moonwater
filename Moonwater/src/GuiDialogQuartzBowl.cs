using Cairo;
using System;
using Moonwater.blockentity;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

#nullable disable

namespace Moonwater;

public class GuiDialogQuartzBowl : GuiDialogBlockEntity
{
  private GuiDialog.EnumPosFlag screenPos;
  private ElementBounds inputSlotBounds;

  protected override double FloatyDialogPosition => 0.6;

  protected override double FloatyDialogAlign => 0.8;

  public override double DrawOrder => 0.2;

  public GuiDialogQuartzBowl(
    string dialogTitle,
    InventoryBase inventory,
    BlockPos blockEntityPos,
    ICoreClientAPI capi)
    : base(dialogTitle, inventory, blockEntityPos, capi)
  {
    int num = this.IsDuplicate ? 1 : 0;
  }

  private void SetupDialog()
  {
    ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, 150.0, 200.0);
    ElementBounds bounds1 = ElementBounds.Fixed(170.0, 30.0, 150.0, 200.0);
    this.inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 30.0, 1, 1);
    this.inputSlotBounds.fixedHeight += 10.0;
    double fixedHeight = this.inputSlotBounds.fixedHeight;
    double fixedY = this.inputSlotBounds.fixedY;
    ElementBounds bounds2 = ElementBounds.Fixed(100.0, 30.0, 40.0, 200.0);
    ElementBounds bounds3 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
    bounds3.BothSizing = ElementSizing.FitToChildren;
    bounds3.WithChildren(elementBounds, bounds1);
    ElementBounds bounds4 = ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(this.IsRight(this.screenPos) ? -GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(this.IsRight(this.screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
    this.SingleComposer = this.capi.Gui.CreateCompo("blockentitybarrel" + this.BlockEntityPosition?.ToString(), bounds4).AddShadedDialogBG(bounds3).AddDialogTitleBar(this.DialogTitle, new Action(this.OnTitleBarClose)).BeginChildElements(bounds3).AddItemSlotGrid((IInventory) this.Inventory, new Action<object>(this.SendInvPacket), 1, new int[1], this.inputSlotBounds, "inputSlot").AddSmallButton(Lang.Get("barrel-seal"), new ActionConsumable(this.onSealClick), ElementBounds.Fixed(0.0, 100.0, 80.0, 25.0)).AddInset(bounds2.ForkBoundingParent(2.0, 2.0, 2.0, 2.0), 2).AddDynamicCustomDraw(bounds2, new DrawDelegateWithBounds(this.fullnessMeterDraw), "liquidBar").AddDynamicText(this.getContentsText(), CairoFont.WhiteDetailText(), bounds1, "contentText").EndChildElements().Compose();
  }

  private string getContentsText()
  {
    string contentsText = Lang.Get("Contents:");
    if (this.Inventory[0].Empty && this.Inventory[1].Empty)
    {
      contentsText = $"{contentsText}\n{Lang.Get("nobarrelcontents")}";
    }
    else
    {
      if (!this.Inventory[1].Empty)
      {
        ItemStack itemstack = this.Inventory[1].Itemstack;
        WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemstack);
        if (containableProps != null)
        {
          string str = Lang.Get($"{itemstack.Collectible.Code.Domain}:incontainer-{itemstack.Class.ToString().ToLowerInvariant()}-{itemstack.Collectible.Code.Path}");
          contentsText = $"{contentsText}\n{Lang.Get(containableProps.MaxStackSize > 0 ? "barrelcontents-items" : "barrelcontents-liquid", (object) (float) ((double) itemstack.StackSize / (double) containableProps.ItemsPerLitre), (object) str)}";
        }
        else
          contentsText = $"{contentsText}\n{Lang.Get("barrelcontents-items", (object) itemstack.StackSize, (object) itemstack.GetName())}";
      }
      if (!this.Inventory[0].Empty)
      {
        ItemStack itemstack = this.Inventory[0].Itemstack;
        contentsText = $"{contentsText}\n{Lang.Get("barrelcontents-items", (object) itemstack.StackSize, (object) itemstack.GetName())}";
      }
      BEQuartzBowl blockEntity = this.capi.World.BlockAccessor.GetBlockEntity(this.BlockEntityPosition) as BEQuartzBowl;
      if (blockEntity.CurrentRecipe != null)
      {
        ItemStack resolvedItemStack = blockEntity.CurrentRecipe.RecipeOutput.ResolvedItemStack;
        WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(resolvedItemStack);
        string str1;
        if (blockEntity.CurrentRecipe.SealHours <= 24.0)
          str1 = Lang.Get("{0} hours", (object) blockEntity.CurrentRecipe.SealHours);
        else
          str1 = Lang.Get("{0} days", (object) Math.Round(blockEntity.CurrentRecipe.SealHours / (double) this.capi.World.Calendar.HoursPerDay, 1));
        string str2 = str1;
        if (containableProps != null)
        {
          string str3 = Lang.Get($"{resolvedItemStack.Collectible.Code.Domain}:incontainer-{resolvedItemStack.Class.ToString().ToLowerInvariant()}-{resolvedItemStack.Collectible.Code.Path}");
          float num = (float) blockEntity.CurrentOutSize / containableProps.ItemsPerLitre;
          contentsText = $"{contentsText}\n\n{Lang.Get("Will turn into {0} litres of {1} after {2} of sealing.", (object) num, (object) str3, (object) str2)}";
        }
        else
          contentsText = $"{contentsText}\n\n{Lang.Get("Will turn into {0}x {1} after {2} of sealing.", (object) blockEntity.CurrentOutSize, (object) resolvedItemStack.GetName(), (object) str2)}";
      }
    }
    return contentsText;
  }

  public void UpdateContents()
  {
    this.SingleComposer.GetCustomDraw("liquidBar").Redraw();
    this.SingleComposer.GetDynamicText("contentText").SetNewText(this.getContentsText());
  }

  private void fullnessMeterDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
  {
    ItemSlot itemSlot = this.Inventory[1];
    if (itemSlot.Empty)
      return;
    BEQuartzBowl blockEntity = this.capi.World.BlockAccessor.GetBlockEntity(this.BlockEntityPosition) as BEQuartzBowl;
    float num = 1f;
    int val1 = blockEntity.CapacityLitres;
    WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemSlot.Itemstack);
    if (containableProps != null)
    {
      num = containableProps.ItemsPerLitre;
      val1 = Math.Max(val1, containableProps.MaxStackSize);
    }
    double y = (1.0 - (double) ((float) itemSlot.StackSize / num / (float) val1)) * currentBounds.InnerHeight;
    ctx.Rectangle(0.0, y, currentBounds.InnerWidth, currentBounds.InnerHeight - y);
    CompositeTexture compositeTexture = containableProps?.Texture ?? itemSlot.Itemstack.Collectible.Attributes?["inContainerTexture"].AsObject<CompositeTexture>((CompositeTexture) null, itemSlot.Itemstack.Collectible.Code.Domain);
    if (compositeTexture == null)
      return;
    ctx.Save();
    Matrix matrix = ctx.Matrix;
    matrix.Scale(GuiElement.scaled(3.0), GuiElement.scaled(3.0));
    ctx.Matrix = matrix;
    AssetLocation textureLoc = compositeTexture.Base.Clone().WithPathAppendixOnce(".png");
    GuiElement.fillWithPattern(this.capi, ctx, textureLoc, true, mulAlpha: compositeTexture.Alpha);
    ctx.Restore();
  }

  private bool onSealClick()//TODO: Come up with more permanent solution
  {
    /*if (!(this.capi.World.BlockAccessor.GetBlockEntity(this.BlockEntityPosition) is BEQuartzBowl blockEntity) || blockEntity.Sealed || !blockEntity.GetCanSeal((IPlayer) this.capi.World.Player))
      return true;
    blockEntity.SealBarrel();
    this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1337);
    this.capi.World.PlaySoundAt(new AssetLocation("sounds/player/seal"), this.BlockEntityPosition, 0.4);
    this.TryClose();*/
    return true;
  }

  private void SendInvPacket(object packet)
  {
    this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, packet);
  }

  private void OnTitleBarClose() => this.TryClose();

  public override void OnGuiOpened()
  {
    base.OnGuiOpened();
    this.screenPos = this.GetFreePos("smallblockgui");
    this.OccupyPos("smallblockgui", this.screenPos);
    this.SetupDialog();
  }

  public override void OnGuiClosed()
  {
    this.SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(this.capi);
    base.OnGuiClosed();
    this.FreePos("smallblockgui", this.screenPos);
  }
}
