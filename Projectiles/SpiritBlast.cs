﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace WeaponOut.Projectiles
{
    public class SpiritBlast : ModProjectile
    {
        public override bool Autoload(ref string name) { return true; }//TESTING4BREAK

        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            projectile.width = 24;
            projectile.height = 24;
            
            projectile.penetrate = 1;

            projectile.friendly = true;
            projectile.melee = true;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (projectile.ai[0] < 10)
            {
                if (projectile.ai[0] == 0)
                {
                    Main.PlaySound(SoundID.Item20, projectile.Center);
                    projectile.velocity /= 2; // Half speed
                }
                else
                {
                    projectile.velocity *= 1.1f; // Speed up to original (roughly)
                }
                projectile.alpha = Math.Max(0, 200 - (int)projectile.ai[0] * 20);
            }

            Dust d = Main.dust[Dust.NewDust(projectile.Center - new Vector2(projectile.width / 4 + 2, projectile.height / 4 + 2), 
                projectile.width / 2, projectile.height / 2,
                 DustID.t_Martian, 0, 0, 100, default(Color), Main.rand.NextFloat() + 0.5f)];
            d.noGravity = true;
            d.noLight = true;
            d.velocity = projectile.velocity / 2;

            // Cosine-wave movement
            if (projectile.ai[1] != 0f)
            {
                Vector2 parallelDirection = projectile.velocity.RotatedBy(MathHelper.PiOver2);

                projectile.localAI[0] = parallelDirection.X * (float)Math.Cos(projectile.ai[0] * 0.2f)
                    * projectile.ai[1] * 0.25f;
                projectile.localAI[1] = parallelDirection.Y * (float)Math.Cos(projectile.ai[0] * 0.2f)
                    * projectile.ai[1] * 0.25f;

                projectile.position += new Vector2(projectile.localAI[0], projectile.localAI[1]);
            }

            projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y + projectile.localAI[1], (double)projectile.velocity.X + projectile.localAI[0]) + 1.57f;

            projectile.ai[0]++;
            projectile.frameCounter++;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (projectile.penetrate == projectile.maxPenetrate)
            {
                if (Main.player[projectile.owner].active)
                {
                    Main.player[projectile.owner].GetModPlayer<ModPlayerFists>().ModifyComboCounter(1);
                }
            }
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust d = Main.dust[Dust.NewDust(projectile.position, projectile.width, projectile.height,
                     DustID.t_Martian, 0, 0, 100, default(Color), Main.rand.NextFloat() + 1f)];
                d.noGravity = true;
                d.velocity *= 2;
            }
            Main.PlaySound(SoundID.Item118, projectile.Center);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if(projectile.frameCounter > 6)
            {
                projectile.frameCounter = 0;
                projectile.frame++;
                if (projectile.frame >= Main.projFrames[projectile.type]) projectile.frame = 0;
            }
            
            #region lighting free multi-frame alpha render
            Texture2D t = Main.projectileTexture[projectile.type];
            int height = t.Height;
            Rectangle? source = null;
            if (Main.projFrames[projectile.type] > 1)
            {
                height = t.Height / Main.projFrames[projectile.type]; ;
                source = new Rectangle(0, height * projectile.frame, t.Width, height);
            }
            Vector2 p = projectile.position - Main.screenPosition;
            Vector2 c = new Vector2(projectile.width / 2, projectile.height / 2);

            // Draw with trail
            for (int i = 2; i >= 0; i--)
            {
                spriteBatch.Draw(t,
                    p - (projectile.velocity * i) + c,
                    source, new Color(
                        255 - (projectile.alpha + 80 * i), 255 - (projectile.alpha + 80 * i),
                        255 - (projectile.alpha + 80 * i), 255 - (projectile.alpha + 80 * i)),
                    projectile.rotation,
                    new Vector2(t.Width / 2, height / 2),
                    6f * projectile.scale / (i + 6),
                    SpriteEffects.None,
                    0f);
            }
            #endregion
            return false;
        }
    }
}
