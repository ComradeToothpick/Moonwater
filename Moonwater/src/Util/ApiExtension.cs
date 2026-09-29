using Moonwater;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public static class ApiAdditions
{
    public static List<QuartzBowlRecipe> GetQuartzBowlRecipes(this ICoreAPI api)
    {
        return api.ModLoader.GetModSystem<MoonwaterModSystem>().QuartzBowlRecipes;
    }
}