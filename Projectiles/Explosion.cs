﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace WeaponOut.Projectiles
{
    /// <summary>
    /// ai[0] = charge
    /// ai[1] = explosion state (charging, released, exploding
    /// localai[0] = charging
    /// localai[1] = basedmg
    /// damage * (charge ^ 2) x35-870-3500
    /// log (charge * 10) x1-1.84-2.17
    /// 
    /// Possibly increase size by projectile velocity
    /// </summary>
    public class Explosion : ModProjectile
    {
        public static Texture2D textureTargetS;
        public static Texture2D textureTargetM;
        public static Texture2D textureTargetL;
        public static Texture2D textureLaser;

        public float[] textureSizes = new float[maxCharge];
        public float[] textureAlphas = new float[maxCharge];
        public float[] textureCastAlphas = new float[2];
        public float centreY;
        public Vector2 GraphicCentre
        {
            get
            {
                return new Vector2(projectile.Center.X, centreY);
            }
        }

        public const int chargeTicksIdle = 4; //bonus ticks for standing still
        public const int chargeTicksMax = 48 * (1 + chargeTicksIdle); //5 ticks per update
        public const float chargeTickGameMax = (chargeTicksMax / chargeTicksIdle);
        public const int castTicksTime = 5; //time per charge spent casting
        public const int fireTicksTime = 3; //time per charge spent exploding (penetrating projs gives npcs 10tick immune)
        public const float manaIncrease = 0.5f;//additive mana multiplier per increase
        public const float explosionScale = 1.15f; //width * this^15
        //10, 15, 20, 25 etc.

        private int ChargeLevel { get { return (int)projectile.ai[0]; } set { projectile.ai[0] = value; } }
        private int ExplosionState { get { return (int)projectile.ai[1]; } set { projectile.ai[1] = value; } }
        private int ChargeTick { get { return (int)projectile.localAI[0]; } set { projectile.localAI[0] = value; } }
        private int Damage { get { return (int)projectile.localAI[1]; } set { projectile.localAI[1] = value; } }
        private int manaCost { get { return (int)(Main.player[projectile.owner].inventory[Main.player[projectile.owner].selectedItem].mana * (1f + (ChargeLevel + 1) / 2f)); } }

        public const int maxCharge = 15;
        //Titanium/Hallowed Headgear, Diamond Robe, 
        //Crystal Ball Buff, Arcane Band of Starpower, Arcane Mana Regeneration Band, 
        //Arcane Magic Cuffs, three Arcane Accessories (one extra with Demon Heart)
        //For 600 mana, with max reduced mana mana cost from 10 -> 8 via Masterful modifier
        //sum 8 * (1+k/2), k=0 to n
        //chg 1, 2   3   4   5   6    7    8    9    10   11   12   13   14   15   16
        //    8, 20, 36, 56, 80, 108, 140, 176, 216, 260, 308, 360, 416, 476, 540, 608
        //NOTE: it seems 400 is maximum mana allowed, Must consume a mana pot at tier 6 to cast full power
        //

        private Vector2 zoomDiff = new Vector2();

        public override void SetDefaults()
        {
            projectile.name = "Explosion"; //great explosion, mega explosion, extreme explosion, ultra explosion, grand explosion
            projectile.width = 100;
            projectile.height = 100;

            projectile.timeLeft = 3600;
            projectile.penetrate = -1;

            projectile.friendly = true;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player player = Main.player[projectile.owner];
            player.heldProj = projectile.whoAmI; //set to this

            projectile.velocity = Vector2.Zero;
            centreY = (centreY * chargeTickGameMax + projectile.Center.Y) / (chargeTickGameMax + 1);
            if (ExplosionState == 0f)
            {
                chargeState(player);
            }
            else if (ExplosionState == 1f)
            {
                castState(player);
            }
            else if (ExplosionState == 2f)
            {
                fireState(player);
            }
        }

        #region Behaviours

        private static bool canChannel(Player player)
        {
            return !player.dead && !player.frozen && !player.stoned && !player.webbed && !player.tongued;
        }
        /// <summary>
        /// Does all the "is holding and facing this projectile" things
        /// </summary>
        /// <param name="player"></param>
        private void playerFaceProjectileChannel(Player player)
        {
            if (projectile.damage != 0) //first tiome
            {
                Damage = projectile.damage;
                projectile.damage = 0;
                centreY = projectile.Center.Y;

                Main.NewText("Damage: " + Damage);
            }

            Vector2 vectorDiff = player.Center - projectile.Center;
            if (vectorDiff.X < 0f)
            {
                player.ChangeDir(1);
                projectile.direction = 1;
            }
            else
            {
                player.ChangeDir(-1);
                projectile.direction = -1;
            }

            //stop generating new projectiles
            player.itemAnimation = player.itemAnimationMax;
            player.itemTime = player.itemAnimationMax;

            //point at projectile
            player.itemRotation = (vectorDiff * -1f * (float)projectile.direction).ToRotation();
            projectile.spriteDirection = ((vectorDiff.X > 0f) ? -1 : 1);
        }



        private void chargeState(Player player)
        {
            Vector2 staffTip = player.MountedCenter + new Vector2(
                player.inventory[player.selectedItem].width * 1.1f * (float)Math.Cos(player.itemRotation),
                player.inventory[player.selectedItem].width * 1.1f * (float)Math.Sin(player.itemRotation)
            ) * player.direction;
            int d = Dust.NewDust(staffTip - new Vector2(3 - player.direction * 2 + ChargeLevel * 0.05f, 3 + ChargeLevel * 0.05f),
                0, 0, 174, 0, 0, 0, Color.White, 0.3f + (0.1f * ChargeLevel));
            Main.dust[d].noGravity = true;
            Main.dust[d].velocity *= 0.3f;

            //use manaflower
            if (manaCost > player.statMana)
            {
                if (player.manaFlower)
                {
                    player.QuickMana();
                }
            }

            //can't instantly use
            bool littleCharge = ChargeLevel == 0 && ChargeTick < chargeTicksMax / 2;

            //cancelled if no longer channelling or can't act, also always channel if moving
            if ((player.channel || player.velocity.X != 0 || player.velocity.Y != 0 || littleCharge) 
                && canChannel(player) && ChargeLevel < maxCharge && player.statMana >= manaCost)
            {
                //hold player usage
                playerFaceProjectileChannel(player);
                ChargeTick += 1; //increase charge a bit
                if (player.velocity.X == 0 && player.velocity.Y == 0)
                {
                    ChargeTick += chargeTicksIdle; //increase charge

                    //extra dust to indicate charging faster
                    d = Dust.NewDust(staffTip - new Vector2(3 - player.direction * 2 + ChargeLevel * 0.05f, 3 + ChargeLevel * 0.05f),
                        0, 0, 174, 0, 0, 0, Color.White, 0.5f + (0.1f * ChargeLevel));
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.7f;

                    //dust fly into staff
                    Vector2 circleVector;
                    float randAng = Main.rand.Next(-31416, 31417) * 0.0001f;
                    circleVector = new Vector2(
                        ChargeLevel * 8 * (float)Math.Cos(randAng),
                        ChargeLevel * 8 * (float)Math.Sin(randAng)
                        );
                    //dust spawn at cricle and move in
                    d = Dust.NewDust(staffTip + circleVector, 0, 0, 
                        106, circleVector.X, circleVector.Y, 0, Color.White, 0.1f);
                    Main.dust[d].fadeIn = 0.8f;
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= -0.1f;
                }
                if (ChargeTick >= chargeTicksMax)
                {
                    ChargeTick = 0;
                    ChargeLevel++;
                    projectile.timeLeft += chargeTicksMax;

                    float recentre = projectile.width;
                    projectile.Size *= explosionScale;
                    recentre = (projectile.width - recentre) / 2f;
                    projectile.Center -= new Vector2(recentre, recentre * 1.75f);
                    textureSizes[ChargeLevel - 1] = projectile.width;

                    player.CheckMana(manaCost, true);

                    Main.NewText("Increase... tier " + ChargeLevel + " | current manacost: " + manaCost + "| size: " + textureSizes[Math.Min(maxCharge - 1, ChargeLevel)]);
                }
            }
            else
            {
                ExplosionState = 1;
                projectile.netUpdate = true;
                Main.NewText("Changing to CastState");
            }
        }
        
        private bool start = true;
        private int chargeTime;
        private void castState(Player player)
        {
            chargeTime = (2 + ChargeLevel) * castTicksTime;
            if (start) { ChargeTick = chargeTime; start = false; }

            //release at lower power if can't act
            if (canChannel(player))
            {
                player.itemRotation = -1.5708f * player.direction;
            }
            else
            {
                Main.NewText("Changing to weak FireState");
                ChargeLevel /= 2;
                ExplosionState = 2;
                projectile.netUpdate = true;
            }

            if (ChargeTick > 0)
            {
                ChargeTick--;

                //player stays still to cast
                player.itemAnimation = player.itemAnimationMax / 2;
                player.itemTime = player.itemAnimationMax / 2;
                player.noKnockback = true;
                player.velocity /= 2f;

                // -- casting code --
                
                Vector2 staffTip = player.MountedCenter + new Vector2(
                    player.inventory[player.selectedItem].width * 1.1f * (float)Math.Cos(player.itemRotation),
                    player.inventory[player.selectedItem].width * 1.1f * (float)Math.Sin(player.itemRotation)
                ) * player.direction;

                /*
                //Main.NewText("casting... " + ((float)ChargeTick / castTicksTime));
                int d = Dust.NewDust(staffTip - new Vector2(3 - player.direction * 2, 3),
                    0, 0, 130, 0, 0, 0, Color.White, 0.5f + (0.1f * ((float)ChargeTick / castTicksTime)));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = new Vector2(Main.dust[d].velocity.X * 0.1f, -5f + Main.dust[d].velocity.Y - ChargeLevel * 0.1f);
                 */
            }
            else
            {
                Main.NewText("Changing to FireState");
                ExplosionState = 2;
                projectile.netUpdate = true;
            }
        }
        
        private void fireState(Player player)
        {
            if (ChargeTick >= 0)
            {
                ChargeTick = -1;
                projectile.timeLeft = fireTicksTime * (1 + ChargeLevel);

                if (ChargeLevel > 0)
                {
                    //goes up to x1000 damage
                    Damage = Damage + (int)(Damage * Math.Pow(2, ChargeLevel * 10f / maxCharge));
                    projectile.knockBack *= 1 + (int)Math.Log10(ChargeLevel * 10);
                }

                //damage is reduced over a period of time due to fire time
                //in this case, since it hits every 10 ticks
                int divDamage = Damage / (1 + (projectile.timeLeft / 10));
                projectile.damage = divDamage;
                Main.NewText("lifetime: " + projectile.timeLeft + " | dmg: " + divDamage + "/" + Damage);

                explosionStart();
            }

            projectile.scale += (explosionScale - 1) / fireTicksTime;

            explosionTime(
                projectile.timeLeft /
                (float)(fireTicksTime * (1 + ChargeLevel))
                );
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit) { faceExplosion(target); }
        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) { faceExplosion(target); }
        public override void ModifyHitPvp(Player target, ref int damage, ref bool crit) { faceExplosion(target); }
        private void faceExplosion(Entity entity)
        {
            if (entity.Center.X < projectile.Center.X)
            {
                projectile.direction = -1;
            }
            else
            {
                projectile.direction = 1;
            }
        }
        
        #endregion

        #region Visuaudio
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 circleVector;

            //intitial dist
            for (int i = 0; i < 2; i++)
            {
                float randAng = Main.rand.Next(-31416, 31417) * 0.0001f;
                circleVector = new Vector2(
                    50 * (float)Math.Cos(randAng) - 4,
                    50 * (float)Math.Sin(randAng) - 4
                    );
                //still dust
                int d = Dust.NewDust(GraphicCentre + circleVector, 0, 0, 174, 0, 0, 0, Color.White, 0.1f);
                Main.dust[d].fadeIn = 0.4f;
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.25f;
                //dust moving out
                d = Dust.NewDust(GraphicCentre, 0, 0, 183, circleVector.X / 30, circleVector.Y / 30, 0, Color.White, 0.1f);
                Main.dust[d].fadeIn = 0.8f;
                Main.dust[d].noGravity = true;
            }

            drawChargeCircles(spriteBatch);

            drawCastingCircles(spriteBatch);

            return false;
        }
        
        private void drawChargeCircles(SpriteBatch spriteBatch)
        {
            //Main.NewText("draw " + i + " with alpha " + alpha);
            float size;
            float alpha;
            Vector2 castCentre;
            float sizeCircle;
            float sizeOffset;
            float angle;
            for (int i = 0; i < Math.Min(textureSizes.Length, ChargeLevel); i++)
            {
                size = textureSizes[i];
                alpha = textureAlphas[i];
                if (ExplosionState < 2)
                {
                    if (alpha < 1f) { alpha += 0.02f; } else { alpha = 1f; }
                }
                else
                {
                    if (alpha > 0f) { alpha -= 0.03f; } else { alpha = 0; }
                }
                switch (i)
                {
                    case 0: ///////////////////////////////////////////////////////////////////// initial circle
                        castCentre = GraphicCentre - Main.screenPosition;

                        spriteBatch.Draw(textureTargetM,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            (float)(Main.time * 0.05f),
                            textureTargetM.Bounds.Center.ToVector2(),
                            size * projectile.scale / textureTargetM.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 1: ///////////////////////////////////////////////////////////////////// flat centre
                        castCentre = GraphicCentre - Main.screenPosition;

                        spriteBatch.Draw(textureTargetS,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            0,
                            textureTargetS.Bounds.Center.ToVector2(),
                            new Vector2(size, size / 3) * projectile.scale / textureTargetS.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 2: ///////////////////////////////////////////////////////////////////// initial circle inner
                        castCentre = GraphicCentre - Main.screenPosition;

                        spriteBatch.Draw(textureTargetS,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            (float)(Main.time * 0.05f),
                            textureTargetS.Bounds.Center.ToVector2(),
                            textureSizes[0] * projectile.scale / textureTargetS.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 3: ///////////////////////////////////////////////////////////////////// backspin circle
                        castCentre = GraphicCentre - Main.screenPosition;

                        spriteBatch.Draw(textureTargetS,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            (float)(Main.time * 0.08f),
                            textureTargetS.Bounds.Center.ToVector2(),
                            new Vector2(size, size / 3) * projectile.scale / textureTargetS.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 4: ///////////////////////////////////////////////////////////////////// orbiter circle 1
                        sizeCircle = size * (2 / 5f);
                        sizeOffset = size * (3 / 5f);
                        angle = (float)(Main.time % 62831) * -0.02f;
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            sizeOffset * 0.5f * (float)Math.Cos(angle),
                            sizeOffset * 0.5f * (float)Math.Sin(angle)
                        );

                        spriteBatch.Draw(textureTargetM,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            (float)(Main.time * 0.03f),
                            textureTargetM.Bounds.Center.ToVector2(),
                            new Vector2(sizeCircle, sizeCircle) * projectile.scale / textureTargetM.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 5: ///////////////////////////////////////////////////////////////////// orbiter circle 2
                        sizeCircle = size * (2 / 5f);
                        sizeOffset = size * (3 / 5f);
                        angle = (float)((Main.time + 20943) % 62831) * -0.02f;
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            sizeOffset * 0.5f * (float)Math.Cos(angle),
                            sizeOffset * 0.5f * (float)Math.Sin(angle)
                        );

                        spriteBatch.Draw(textureTargetM,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            (float)(Main.time * 0.02f),
                            textureTargetM.Bounds.Center.ToVector2(),
                            new Vector2(sizeCircle, sizeCircle) * projectile.scale / textureTargetM.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 6: ///////////////////////////////////////////////////////////////////// orbiter circle 3
                        sizeCircle = size * (2 / 5f);
                        sizeOffset = size * (3 / 5f);
                        angle = (float)((Main.time - 20943) % 62831) * -0.02f;
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            sizeOffset * 0.5f * (float)Math.Cos(angle),
                            sizeOffset * 0.5f * (float)Math.Sin(angle)
                        );

                        spriteBatch.Draw(textureTargetM,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            (float)(Main.time * 0.015f),
                            textureTargetM.Bounds.Center.ToVector2(),
                            new Vector2(sizeCircle, sizeCircle) * projectile.scale / textureTargetM.Width,
                            SpriteEffects.None,
                            0f);
                        break;

                    case 7: ///////////////////////////////////////////////////////////////////// larger circle
                        if (alpha > 0.3f) { alpha = 0.3f; }
                        castCentre = GraphicCentre - Main.screenPosition;

                        spriteBatch.Draw(textureTargetL,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            (float)(Main.time * 0.01f),
                            textureTargetL.Bounds.Center.ToVector2(),
                            size * projectile.scale / textureTargetL.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 8: ///////////////////////////////////////////////////////////////////// top circle
                        if (alpha > 0.5f) { alpha = 0.5f; }
                        sizeCircle = size * (1 / 7f);
                        sizeOffset = size * (6 / 7f);
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            0,
                            -sizeOffset * 0.5f
                        );

                        spriteBatch.Draw(textureTargetM,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            0,
                            textureTargetM.Bounds.Center.ToVector2(),
                            new Vector2(size, sizeCircle) * projectile.scale / textureTargetM.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 9: ///////////////////////////////////////////////////////////////////// bottom circle
                        if (alpha > 0.5f) { alpha = 0.5f; }
                        sizeCircle = size * (1 / 6f);
                        sizeOffset = size * (5 / 6f);
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            0,
                            sizeOffset * 0.5f
                        );

                        spriteBatch.Draw(textureTargetM,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            0,
                            textureTargetM.Bounds.Center.ToVector2(),
                            new Vector2(size, sizeCircle) * projectile.scale / textureTargetM.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 10: ///////////////////////////////////////////////////////////////////// mid circle
                        if (alpha > 0.5f) { alpha = 0.5f; }
                        sizeCircle = size * (1 / 7f);
                        castCentre = GraphicCentre - Main.screenPosition;

                        spriteBatch.Draw(textureTargetL,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            0,
                            textureTargetL.Bounds.Center.ToVector2(),
                            new Vector2(size, sizeCircle) * projectile.scale / textureTargetL.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 11: ///////////////////////////////////////////////////////////////////// guide topper 1
                        if (alpha > 0.5f) { alpha = 0.5f; }
                        sizeCircle = size * (1 / 9f);
                        sizeOffset = size * (8 / 9f);
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            0,
                            -sizeOffset * 0.5f
                        );

                        spriteBatch.Draw(textureTargetS,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            0,
                            textureTargetS.Bounds.Center.ToVector2(),
                            new Vector2(size * 0.7f, sizeCircle) * projectile.scale / textureTargetS.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 12: ///////////////////////////////////////////////////////////////////// guide topper 2
                        if (alpha > 0.5f) { alpha = 0.5f; }
                        sizeCircle = size * (1 / 9f);
                        sizeOffset = size * (8 / 9f);
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            0,
                            -sizeOffset * 0.5f
                        );

                        spriteBatch.Draw(textureTargetM,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            0,
                            textureTargetM.Bounds.Center.ToVector2(),
                            new Vector2(size * 0.8f, sizeCircle) * projectile.scale / textureTargetM.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 13: ///////////////////////////////////////////////////////////////////// guide topper 3
                        if (alpha > 0.5f) { alpha = 0.5f; }
                        sizeCircle = size * (1 / 9f);
                        sizeOffset = size * (8 / 9f);
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            0,
                            -sizeOffset * 0.5f
                        );

                        spriteBatch.Draw(textureTargetM,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            0,
                            textureTargetM.Bounds.Center.ToVector2(),
                            new Vector2(size * 0.85f, sizeCircle) * projectile.scale / textureTargetM.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                    case 14: ///////////////////////////////////////////////////////////////////// guide topper 4
                        if (alpha > 0.5f) { alpha = 0.5f; }
                        sizeCircle = size * (1 / 9f);
                        sizeOffset = size * (8 / 9f);
                        castCentre = GraphicCentre - Main.screenPosition
                            + new Vector2(
                            0,
                            -sizeOffset * 0.5f
                        );

                        spriteBatch.Draw(textureTargetS,
                            castCentre,
                            null,
                            new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                            0,
                            textureTargetS.Bounds.Center.ToVector2(),
                            new Vector2(size * 0.6f, sizeCircle) * projectile.scale / textureTargetS.Width,
                            SpriteEffects.None,
                            0f);
                        break;
                }
                textureAlphas[i] = alpha;
            }
        }
        private void drawCastingCircles(SpriteBatch spriteBatch)
        {
            float size;
            float alpha;
            Vector2 castCentre;
            float sizeCircle;
            if (ExplosionState != 0)
            {
                //set up casting from player
                Player player = Main.player[projectile.owner];
                int distance = 120 + ChargeLevel * 3;


                //Casting cicle
                if (ExplosionState == 1) textureCastAlphas[0] = 1f;
                if (ExplosionState == 2) textureCastAlphas[0] *= 0.9f;
                //Main.NewText("cast circle target alpha " + textureCastAlphas[0]);
                alpha = textureCastAlphas[0];

                size = projectile.width;
                sizeCircle = size * (1 / 9f);
                castCentre = GraphicCentre
                    + new Vector2(
                    0,
                    -size * 0.75f
                );

                spriteBatch.Draw(textureTargetL,
                    castCentre - Main.screenPosition,
                    null,
                    new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                    0,
                    textureTargetL.Bounds.Center.ToVector2(),
                    new Vector2(size, sizeCircle) * projectile.scale / textureTargetL.Width,
                    SpriteEffects.None,
                    0f);

                //EPXLOSION LASER
                if (ExplosionState == 1)
                {
                    //burst FX
                    int d = Dust.NewDust(castCentre - new Vector2(2 + ChargeLevel * 0.2f, 0), 8, 0, 162,
                        0, 2, 0, Color.White, 1 + ChargeLevel * 0.4f);
                    Main.dust[d].noGravity = true; //loses velocity very fast
                    Main.dust[d].velocity.X *= ChargeLevel;

                    //from staff
                    drawLaser(spriteBatch, player.Top + new Vector2(0, -34), player.Top + new Vector2(0, -distance));

                    //to explosion
                    drawLaser(spriteBatch, castCentre, GraphicCentre);
                }

                if (ExplosionState == 1)
                {
                    //dust fly into top circle
                    Vector2 circleVector;
                    float randAng = Main.rand.Next(-31416, 31417) * 0.0001f;
                    circleVector = new Vector2(
                        58 * (float)Math.Cos(randAng),
                        10 * (float)Math.Sin(randAng)
                        );
                    //dust spawn at cricle and move in
                    int d = Dust.NewDust(player.Top + new Vector2(-4, -distance - 4) + circleVector, 0, 0,
                        170, circleVector.X, circleVector.Y, 0, Color.White, 0.1f);
                    Main.dust[d].fadeIn = 1f;
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= -0.07f;
                }

                //Player top circles
                spriteBatch.Draw(textureTargetS,
                    player.Top + new Vector2(0, -distance) - Main.screenPosition,
                    null,
                    new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                    0,
                    textureTargetS.Bounds.Center.ToVector2(),
                    new Vector2(player.width * 8, player.width * 1.6f) * projectile.scale / textureTargetS.Width,
                    SpriteEffects.None,
                    0f);
                if (ChargeLevel < 8) return; //STOP if less than 8 charge
                spriteBatch.Draw(textureTargetM,
                    player.Top + new Vector2(0, -distance * 0.6f) - Main.screenPosition,
                    null,
                    new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                    0,
                    textureTargetM.Bounds.Center.ToVector2(),
                    new Vector2(player.width * 5, player.width) * projectile.scale / textureTargetM.Width,
                    SpriteEffects.None,
                    0f);
                if (ChargeLevel < 12) return; //STOP if less than 12 charge
                spriteBatch.Draw(textureTargetL,
                    player.Top + new Vector2(0, -distance * 0.3f) - Main.screenPosition,
                    null,
                    new Color(1f * alpha, 1f * alpha, 1f * alpha, alpha),
                    0,
                    textureTargetL.Bounds.Center.ToVector2(),
                    new Vector2(player.width * 4, player.width * 0.8f) * projectile.scale / textureTargetL.Width,
                    SpriteEffects.None,
                    0f);
            }
        }

        public void explosionStart()
        {
            //Always make it sound closer
            Vector2 soundPoint = (Main.player[Main.myPlayer].position + projectile.Center * 2) / 3;

            Main.PlaySound(2, soundPoint, 14); //default explosion 
            if (ChargeLevel >= 2 && ChargeLevel < 4) Main.PlaySound(2, soundPoint, 20); //fire cast noise
            if (ChargeLevel >= 4) Main.PlaySound(2, soundPoint, 45); //inferno fork noise
            if (ChargeLevel >= 5) Main.PlaySound(2, soundPoint, 62); //grenade explosion(sounds beefier)
            if (ChargeLevel >= 7 && ChargeLevel < 9) Main.PlaySound(2, soundPoint, 69); //add staff of earth why not
            if (ChargeLevel >= 9) Main.PlaySound(2, soundPoint, 74); //inferno explosion
            if (ChargeLevel >= 10) Main.PlaySound(4, soundPoint, 43); //death explosion of some kind
            if (ChargeLevel >= 12) Main.PlaySound(2, soundPoint, 119); //roar lol
        }
        public void explosionTime(float normalTime)
        {
            try
            {
                float denominator = 1 + Vector2.DistanceSquared(Main.player[Main.myPlayer].Center, projectile.Center) / 500000;
                WeaponOut.shakeIntensity = Math.Max(WeaponOut.shakeIntensity, (int)(2 * ChargeLevel / denominator));
                //Main.NewText("shake: " + ChargeLevel + "/" + denominator);
            }
            catch { }
                

            //TEMPORARY dust indicates size
            for (int i = 0; i < (ChargeLevel + 1) * 3; i++)
            {
                int d = Dust.NewDust(projectile.position, projectile.width, projectile.height, 6, 0, 0, 0, Color.White, 1 + ChargeLevel * 0.5f);
                Main.dust[d].fadeIn = 0.1f;
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 1.5f;
            }

        }

        private void drawLaser(SpriteBatch spritebatch, Vector2 start, Vector2 end)
        {
            try
            {
                Main.NewText("charge: " + ChargeTick + " / "  + chargeTime);
                Utils.DrawLaser(
                    spritebatch,
                    textureLaser,
                    start - Main.screenPosition,
                    end - Main.screenPosition,
                    new Vector2(Math.Max(0.1f, 2f / (1f + chargeTime - ChargeTick))),
                    new Utils.LaserLineFraming(ExplosionLaser)); //uses delegate (see method below)
            }
            catch { }
        }
        //define which frames are used in each stage (0 = start, 1 = mid, 2 = end
        private void ExplosionLaser(int stage, Vector2 currentPosition, float distanceLeft, Rectangle lastFrame, out float distCovered, out Rectangle frame, out Vector2 origin, out Color color)
        {
            color = Color.White;
            if (stage == 0)
            {
                distCovered = 33f;
                frame = new Rectangle(0, 0, 22, 22);
                origin = frame.Size() / 2f;
                return;
            }
            if (stage == 1)
            {
                frame = new Rectangle(0, 22, 22, 22);
                distCovered = (float)frame.Height;
                origin = new Vector2((float)(frame.Width / 2), 0f);
                return;
            }
            if (stage == 2)
            {
                distCovered = 22f;
                frame = new Rectangle(0, 44, 22, 22);
                origin = new Vector2((float)(frame.Width / 2), 1f);
                return;
            }
            distCovered = 9999f;
            frame = Rectangle.Empty;
            origin = Vector2.Zero;
            color = Color.Transparent;
        }
        #endregion
    }
}
