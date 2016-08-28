﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WeaponOut.Items.Weapons
{
    public class StaffOfExplosion : ModItem
    {
        public override void SetDefaults()
        {
            item.name = "Staff of Explosion";
            item.toolTip = "Create an explosion at a location\nChannel to create larger explosions\nOnly channels when standing still";
            item.width = 52;
            item.height = 14;
            item.scale = 1f;

            item.magic = true;
            item.channel = true;
            item.mana = 10;
            item.damage = 40; //damage * (charge ^ 2) *1(0) - *25(8) - *160(11) - *1000(15)
            item.knockBack = 3; //up to x2.18
            item.autoReuse = true;

            item.noMelee = true;
            Item.staff[item.type] = true; //rotate weapon, as it is a staff
            item.shoot = mod.ProjectileType("Explosion");
            item.shootSpeed = 1;

            item.useStyle = 5;
            item.useSound = 8;
            item.useTime = 60;
            item.useAnimation = 60;

            item.rare = 8;
            item.value = Item.sellPrice(0, 10, 0, 0);
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.WandofSparking, 1);
            recipe.AddIngredient(ItemID.RubyStaff, 1);
            recipe.AddIngredient(ItemID.MeteorStaff, 1);
            recipe.AddIngredient(ItemID.InfernoFork, 1);
            recipe.anyWood = true;
            recipe.AddTile(TileID.AdamantiteForge);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override void UseStyle(Player player)
        {
            PlayerFX.modifyPlayerItemLocation(player, -4, -5);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            position = Main.MouseWorld;
            return true;
        }
    }
}