﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Microsoft.Xna.Framework;

namespace WeaponOut.Items.Armour
{
    [AutoloadEquip(EquipType.Legs)]
    public class LunarFistLegs : ModItem
    {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Starlight Leggings");

            Tooltip.SetDefault(
                "15% increased minion damage, 12% increased movement and melee speed\n" +
                "Increases your max number of minions by 1\n" +
                "100% increased divekick damage and knockback");
        }
        public override void SetDefaults() {
            item.defense = 12;
            item.value = 0;
            item.rare = 10;

            item.width = 18;
            item.height = 18;
        }
        public override void AddRecipes() {
            if (!ModConf.EnableFists) return;
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.FragmentStardust, 6);
            recipe.AddIngredient(ItemID.FragmentSolar, 6);
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
        
        public override void UpdateEquip(Player player) {
            player.minionDamage += 0.15f;
            player.moveSpeed += 0.12f;
            player.meleeSpeed += 0.12f;
            player.maxMinions += 1;
            ModPlayerFists.Get(player).divekickDamage += 1f;
            ModPlayerFists.Get(player).divekickKnockback += 1f;

            Lighting.AddLight(player.Center, new Vector3(0.9f, 0.9f, 0.95f));
        }

        public override void DrawArmorColor(Player drawPlayer, float shadow, ref Color color, ref int glowMask, ref Color glowMaskColor)
        {
            color = Color.White
                * (1f - shadow)
                * ((byte.MaxValue - drawPlayer.immuneAlpha) / (float)byte.MaxValue);
        }
    }
}
