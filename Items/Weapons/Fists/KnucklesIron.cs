﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WeaponOut.Items.Weapons.Fists
{
    [AutoloadEquip(EquipType.HandsOn, EquipType.HandsOff)]
    public class KnucklesIron : ModItem
    {
        public override bool Autoload(ref string name)
        {
            return ModConf.enableFists;
        }
        public static int comboEffect = 0;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Iron Knuckleduster");
            Tooltip.SetDefault(
                "<right> to consume combo for a special attack\n" +
                "Combo grants 2 bonus damage");
            comboEffect = ModPlayerFists.RegisterComboEffectID(ComboEffects);
        }
        public override void SetDefaults()
        {
            item.useStyle = ModPlayerFists.useStyle;
            item.autoReuse = true;
            item.useAnimation = 18; // Combos can increase speed by 30-50% since it halves remaining attack time

            item.width = 20;
            item.height = 20;
            item.damage = 11;
            item.knockBack = 3f;
            item.UseSound = SoundID.Item7;

            item.tileBoost = 8; // For fists, we read this as the combo power

            item.value = Item.sellPrice(0, 0, 90, 0);
            item.noUseGraphic = true;
            item.melee = true;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronBar, 2);
            recipe.AddTile(TileID.WorkBenches);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { ModPlayerFists.ModifyTooltips(tooltips, item); }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 0)
            {   // Short dash brings up to max default speed.
                player.GetModPlayer<ModPlayerFists>().
                SetDashOnMovement(3f, 12f, 0.992f, 0.96f, true, 0);
            }
            return true;
        }
        public override bool AltFunctionUse(Player player)
        {
            return player.GetModPlayer<ModPlayerFists>().
                AltFunctionCombo(player, comboEffect); ;
        }
        /// <summary> The method called during a combo. Use for ongoing dust and gore effects. </summary>
        public static void ComboEffects(Player player, bool initial)
        {
            if (initial)
            {
                player.itemAnimation = player.itemAnimationMax + 20;
                Main.PlaySound(SoundID.DD2_SkyDragonsFurySwing, player.position);
            }
            // Charging
            Rectangle r = ModPlayerFists.UseItemGraphicbox(player, 16);
            if (player.itemAnimation > player.itemAnimationMax)
            {
                // Charge effect
                Dust d = Main.dust[Dust.NewDust(r.TopLeft(), 16, 16, 31, 0, 0, 100, default(Color), 1.2f)];
                d.position -= d.velocity * 10f;
                d.velocity /= 2;
            }
            // Initial throw
            else if (player.itemAnimation == player.itemAnimationMax)
            {
                // Higher pitch
                Main.PlaySound(42, (int)player.position.X, (int)player.position.Y, 184, 1f, 0.5f);
                // Allow dash
                player.GetModPlayer<ModPlayerFists>().
                SetDashOnMovement(3f, 12f, 0.992f, 0.96f, true, 0);
            }
            else
            {
                // Punch effect
                Dust d = Main.dust[Dust.NewDust(r.TopLeft(), 16, 16, 31, 2, 2, 100, default(Color), 1f)];
                d.velocity *= ModPlayerFists.GetFistVelocity(player);
            }
        }

        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            // jump exactly 6 blocks high!
            ModPlayerFists.UseItemHitbox(player, ref hitbox, 20, 9f, 8f, 8f);
        }

        //Combo
        public override void ModifyHitPvp(Player player, Player target, ref int damage, ref bool crit)
        { float knockBack = 5f; ModifyHit(player, ref damage, ref knockBack, ref crit); }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        { ModifyHit(player, ref damage, ref knockBack, ref crit); }
        private void ModifyHit(Player player, ref int damage, ref float knockBack, ref bool crit)
        {
            ModPlayerFists mpf = player.GetModPlayer<ModPlayerFists>();
            if (mpf.IsComboActiveItemOnHit)
            {
                damage += 2;
            }
            if(mpf.ComboEffectAbs == comboEffect)
            {
                damage *= 2;
                knockBack *= 2;
                crit = true;
            }
        }
    }
}