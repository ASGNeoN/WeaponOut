﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WeaponOut.Items.Weapons.Fists
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class KnucklesDemon : ModItem
    {
        public override bool Autoload(ref string name) { return ModConf.enableFists; }
        public static int altEffect = 0;
        public static int buffID = 0;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Demon Hand");
            Tooltip.SetDefault(
                "<right> consumes combo to slash through enemies and steal life\n" +
                "Combo grants increased melee damage and damage recieved\n" + 
                "'Might makes right'");
            altEffect = ModPlayerFists.RegisterComboEffectID(ComboEffects);
            buffID = mod.BuffType<Buffs.DemonFrenzy>();
        }
        public override void SetDefaults()
        {
            item.melee = true;
            item.damage = 45;
            item.useAnimation = 18; // 30%-50% reduction
            item.knockBack = 3.5f;
            item.tileBoost = 8; // Combo Power

            item.value = Item.sellPrice(0, 0, 15, 0);
            item.rare = 3;

            item.UseSound = SoundID.Item19;
            item.useStyle = ModPlayerFists.useStyle;
            item.autoReuse = true;
            item.noUseGraphic = true;
            item.width = 20;
            item.height = 20;
        }
        const int fistHitboxSize = 30;
        const float fistDashSpeed = 8f;
        const float fistDashThresh = 12f;
        const float fistJumpVelo = 11.7f; // http://rextester.com/OIY60171
        public bool AltStats(Player p) { return p.GetModPlayer<ModPlayerFists>().ComboEffectAbs == altEffect; }
        const int altHitboxSize = 64    ;
        const float altDashSpeed = 16f;
        const float altDashThresh = 12f;
        const float altJumpVelo = 16.1f;
        public override void AddRecipes()
        {
            for (int i = 0; i < 2; i++)
            {
                ModRecipe recipe = new ModRecipe(mod);
                recipe.AddIngredient(mod.ItemType<FistsMolten>(), 1);
                recipe.AddIngredient(mod.ItemType<KnucklesDungeon>(), 1);
                recipe.AddIngredient(mod.ItemType<FistsJungleClaws>(), 1);
                if (i == 0)
                { recipe.AddIngredient(mod.ItemType<GlovesCaestus>(), 1); }
                else
                { recipe.AddIngredient(mod.ItemType<GlovesCaestusCrimson>(), 1); }
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this);
                recipe.AddRecipe();
            }
        }
        
        //Combo
        public override void HoldItem(Player player)
        {
            ModPlayerFists mpf = player.GetModPlayer<ModPlayerFists>();
            if (mpf.IsComboActive)
            {
                player.AddBuff(buffID, 2);
            }
        }
        /// <summary> The method called during a combo. Use for ongoing dust and gore effects. </summary>
        public static void ComboEffects(Player player, bool initial)
        {
            if (initial)
            {
                player.itemAnimation = player.itemAnimationMax + 10;
                Main.PlaySound(SoundID.Item73);
                player.GetModPlayer<ModPlayerFists>().jumpAgainUppercut = true;

                for (int i = 0; i < 64; i++)
                {
                    double angle = Main.time + i / 10.0;
                    Dust d = Dust.NewDustPerfect(player.Center, 21,
                        new Vector2((float)(5.0 * Math.Sin(angle)), (float)(5.0 * Math.Cos(angle))));
                }
            }

            Rectangle r = ModPlayerFists.UseItemGraphicbox(player, 8, altHitboxSize);
            if (player.itemAnimation > player.itemAnimationMax)
            {
                // Charging
            }
            else if (player.itemAnimation == player.itemAnimationMax)
            {
                Main.PlaySound(SoundID.Item71, player.position);
                // Force dash
                player.GetModPlayer<ModPlayerFists>().
                SetDash(altDashSpeed, altDashThresh, 0.992f, 0.96f, true, 0);
            }
            else
            {
                player.yoraiz0rEye = Math.Max(2, player.yoraiz0rEye);
                if(player.attackCD > 2) player.attackCD -= 2; // Attack more things
                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(r.TopLeft(), r.Width, r.Height, 27, player.velocity.X * -0.5f, player.velocity.Y * -0.5f, 180);
                    Main.dust[d].noGravity = true;
                }
            }
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (AltStats(player) && !target.immortal)
            {
                int heal = Math.Min(target.lifeMax, damage) / 4;
                player.HealEffect(heal, true);
                player.statLife += heal;
                player.statLife = Math.Min(player.statLife, player.statLifeMax2);
                if (Main.netMode == 1 && Main.myPlayer == player.whoAmI) NetMessage.SendData(MessageID.PlayerHealth, -1, -1, null, player.whoAmI);
            }
        }

        // Melee Effect
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            Rectangle r = ModPlayerFists.UseItemGraphicbox(player, 2, 22);
            Vector2 velocity = ModPlayerFists.GetFistVelocity(player);
            Vector2 perpendicular = velocity.RotatedBy(Math.PI / 2);
            Vector2 pVelo = (player.position - player.oldPosition);
            // Claw like effect
            for (int y = -1; y < 2; y++)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust d = Main.dust[Dust.NewDust(r.TopLeft() + perpendicular * y * 7, r.Width, r.Height, 27,
                        0, 0, 0, default(Color), 0.7f)];
                    d.velocity /= 4;
                    d.velocity += new Vector2(velocity.X * -2, velocity.Y * -2);
                    d.position -= d.velocity * 8;
                    d.velocity += pVelo;
                    d.noGravity = true;
                }
            }
        }



        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { ModPlayerFists.ModifyTooltips(tooltips, item); }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 0)
            {   // Short dash brings up to max default speed.
                player.GetModPlayer<ModPlayerFists>().
                SetDashOnMovement(fistDashSpeed, fistDashThresh, 0.992f, 0.96f, true, 0);
            }
            return true;
        }
        public override bool AltFunctionUse(Player player)
        {
            return player.GetModPlayer<ModPlayerFists>().
                AltFunctionCombo(player, altEffect);
        }
        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            if (!AltStats(player))
            {
                ModPlayerFists.UseItemHitbox(player, ref hitbox, fistHitboxSize, fistJumpVelo, 3f, 12f);
            }
            else
            {
                ModPlayerFists.UseItemHitbox(player, ref hitbox, altHitboxSize, altJumpVelo, 3f, 12f);
            }
        }
    }
}
