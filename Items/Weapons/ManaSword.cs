﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WeaponOut.Items.Weapons
{
    public class ManaSword : ModItem
    {
        HelperDual dual;
        public override void SetDefaults()
        {
            item.name = "Mana Sword";
            item.toolTip = "";
            item.width = 58;
            item.height = 28;
            item.scale = 0.9f;

            item.autoReuse = true;

            item.useSound = 28;
            item.useStyle = 1; //swing
            item.useAnimation = 16;
            item.useTime = 15;

            item.magic = true;
            item.damage = 10;
            item.knockBack = 5f;

            item.mana = 10;
            item.shoot = mod.ProjectileType("ManaBlast");
            item.shootSpeed = 11f;

            Item.staff[item.type] = true; //rotate weapon, as it is a staff

            item.rare = 4;
            item.value = 10;

            dual = new HelperDual(item, true); //prioritise magic defaults
            dual.UseSound = 60;
            dual.UseStyle = 5;
            dual.UseAnimation = 40;
            dual.UseTime = 40;

            dual.NoMelee = true;
            dual.Damage = 30;
            dual.KnockBack = 10f;

            dual.Mana = 10;
            dual.Shoot = ProjectileID.ChargedBlasterOrb; //staff one is magic, sword one is melee
            dual.ShootSpeed = 14f;

            dual.setValues(false, true);
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.EnchantedSword, 1);
            recipe.AddIngredient(mod.GetItem("ManaBlast").item.type, 5);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override bool AltFunctionUse(Player player) { return true; }
        public override void UseStyle(Player player)
        {
            dual.UseStyleMultiplayer(player);
            if (player.altFunctionUse > 0) PlayerFX.modifyPlayerItemLocation(player, -6, 0);
        }
        public override bool CanUseItem(Player player)
        {
            dual.CanUseItem(player);
            return base.CanUseItem(player);
        }
        public override void HoldStyle(Player player)
        {
            dual.HoldStyle(player);
            base.HoldStyle(player);
        }

        public override void MeleeEffects(Player player, Microsoft.Xna.Framework.Rectangle hitbox)
        {
            if (player.HasBuff(WeaponOut.BuffIDManaReduction) != -1)
            {
                int d = Dust.NewDust(hitbox.TopLeft(), hitbox.Width, hitbox.Height, 15, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, Color.White, 1.3f);
                Main.dust[d].noGravity = true;
            }
            else
            {
                int d = Dust.NewDust(hitbox.TopLeft(), hitbox.Width, hitbox.Height, 15, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, Color.White, 0.6f);
                Main.dust[d].noGravity = true;
            }
        }

    }
}