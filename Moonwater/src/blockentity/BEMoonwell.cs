using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Moonwater.blockentity;

public class BEMoonwell : BlockEntity//BlockEntityLiquidContainer, ICoolingMedium
{
    /*private MultiblockStructure structure;
    public bool StructureComplete { get; private set; }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        structure = Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
        structure.InitForUse(0);
    }

    public override string InventoryClassName { get; }

    private void UpdateStructureState()
    {
        bool complete = structure.InCompleteBlockCount(Api.World, Pos) == 0;
        if (complete != StructureComplete) { StructureComplete = complete; MarkDirty(true); }
    }

    public bool CanCool(ItemSlot slot, Vec3d pos)
    {
        throw new System.NotImplementedException();
    }

    public void CoolNow(ItemSlot slot, Vec3d pos, float dt, bool playSizzle = true)
    {
        throw new System.NotImplementedException();
    }*/
}