using System.Collections.Generic;
using Moonwater.blockentity;
using Moonwater.blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Newtonsoft.Json.Linq;
using System;

namespace Moonwater;

public class MoonwaterModSystem : ModSystem
{
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public static bool canRegister = true;
    public List<QuartzBowlRecipe> QuartzBowlRecipes = new List<QuartzBowlRecipe>();
    private ICoreAPI api;
    
    public override void Start(ICoreAPI api)
    {
        Mod.Logger.Notification("Hello from template mod: " + api.Side);
        api.RegisterBlockClass("BlockQuartzBowl", typeof(BlockQuartzBowl));
        api.RegisterBlockEntityClass("BEQuartzBowl", typeof(BEQuartzBowl));
        this.QuartzBowlRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<QuartzBowlRecipe>>("quartzbowlrecipes").Recipes;
    }
    
    public override void StartPre(ICoreAPI api)
    {
        this.api = api;
        MoonwaterModSystem.canRegister = true;
    }

    public List<QuartzBowlRecipe> GetQuartzBowlRecipes()
    {
        return QuartzBowlRecipes;
    }
    
    public override double ExecuteOrder()
    {
        return 1.0;
    }
    
    public override void AssetsLoaded(ICoreAPI api)
    {
        if (!(api is ICoreServerAPI sapi))
            return;
        Dictionary<AssetLocation, JToken> many = sapi.Assets.GetMany<JToken>(sapi.Server.Logger, "recipes/quartzbowl", "moonwater");
        foreach (KeyValuePair<AssetLocation, JToken> keyValuePair in many)
        {
            if (keyValuePair.Value is JObject)
                this.loadRecipe(sapi, keyValuePair.Key, keyValuePair.Value);
            if (keyValuePair.Value is JArray)
            {
                foreach (JToken jrec in keyValuePair.Value as JArray)
                    this.loadRecipe(sapi, keyValuePair.Key, jrec);
            }
        }
        sapi.World.Logger.Event("{0} quartz bowl recipes loaded", many.Count);
        sapi.World.Logger.StoryEvent(Lang.Get("A Moonlit Night..."));
    }
    
    private void loadRecipe(ICoreServerAPI sapi, AssetLocation loc, JToken jrec)
    {
        QuartzBowlRecipe recipe = jrec.ToObject<QuartzBowlRecipe>(loc.Domain);
        if (!recipe.Enabled)
            return;
        recipe.Resolve(sapi.World, "quartz bowl recipe " + (string) loc);
        this.RegisterQuartzBowlRecipe(recipe);
    }
    
    public void RegisterQuartzBowlRecipe(QuartzBowlRecipe recipe)
    {
        if (!MoonwaterModSystem.canRegister)
            throw new InvalidOperationException("Coding error: Can no long register quartz bowl recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
        if (recipe.Code == null)
            throw new ArgumentException("Quartz Bowl recipes must have a non-null code! (choose freely)");
        foreach (QuartzBowlRecipeIngredient ingredient in recipe.Ingredients)
        {
            if (ingredient.ConsumeQuantity.HasValue)
            {
                int? consumeQuantity = ingredient.ConsumeQuantity;
                int quantity = ingredient.Quantity;
                if (consumeQuantity.GetValueOrDefault() > quantity & consumeQuantity.HasValue)
                    throw new ArgumentException("Quartz Bowl recipe with code {0} has an ingredient with ConsumeQuantity > Quantity. Not a valid recipe!");
            }
        }
        this.QuartzBowlRecipes.Add(recipe);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        //Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("moonwater:hello"));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        //Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("moonwater:hello"));
    }
}