using System;
using Terraria.Audio;
using Terraria.GameContent;
using System.Collections.Generic;
using Terraria.Graphics;

namespace AerovelenceMod.Common.Bases
{
    public abstract class BaseSwingSwordProj : ModProjectile
    {

        #region variables

        // ------ Things you probably want to change ------

        //The angle of Half the swing arc (IN DEGREES)
        public float SwingHalfAngle = 145;

        //The progress you want easingProgress to start at
        public float startingProgress = 0.0f;

        //Distance projectile should be from player
        public float offset = 40;

        //Move the position of the sword by a bit
        public Vector2 positionOffset = Vector2.Zero;

        //Adds a delay before starting the swing
        public int frameToStartSwing = 2;

        //How much to add to easingProgress per frame
        public float easingAdditionAmount = 0.01f;

        //Adds a delay to the projectile dying after the swing is done
        public float timeAfterEnd = 4;

        public float progressToKill = 0.99f;

        public bool useMeleeSpeed = true;

        // ------ Things used locally ------
        public int timer = 0;

        public float startingAngle;

        public float currentAngle;

        //Stores the angle to mouse on frame 1, mostly used to help with vfx
        public float originalAngle;

        private bool firstFrame = true;

        //Progress should be from 0 to 1
        public float easingProgress;

        //Player direction on the first frame
        public int storedDirection;

        //Can't decrement timeAfterEnd because we are constantly setting it in the projectile, so we use this to store the value 
        private float storedTimeAfterEnd = 4;

        //For hitlag
        public int justHitTime = 0;
        #endregion

        public void StandardHeldProjCode()
        {
            Player player = Main.player[Projectile.owner];
            player.heldProj = Projectile.whoAmI;

            float angleToProj = (Projectile.Center - player.MountedCenter).ToRotation();

            //Store player direction
            if (firstFrame)
                storedDirection = player.direction;

            //Make sure itemRotation is right
            float itemrotate = storedDirection < 0 ? MathHelper.Pi : 0;

            if (player.direction != storedDirection)
                itemrotate += MathHelper.Pi;

            player.itemRotation = angleToProj + itemrotate;
            player.itemRotation = MathHelper.WrapAngle(player.itemRotation);

            //Composite arms
            Vector2 frontHandPos = Main.GetPlayerArmPosition(Projectile);
            Vector2 positionToGet = (frontHandPos + currentAngle.ToRotationVector2() * 100);

            //player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, currentAngle - MathHelper.PiOver2);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Projectile.Center - player.Center).ToRotation() + MathHelper.PiOver2 + MathHelper.Pi);


            //Delete proj if it shouldn't be there
            if (!player.active || player.dead || player.CCed || player.noItems || player.frozen)
            {
                Projectile.Kill();
            }
        }

        public void StandardSwingUpdate()
        {
            Player player = Main.player[Projectile.owner];


            //This is were we set the beggining and ending angle of the sword 
            if (firstFrame)
            {
                //For drawing correctly
                Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;

                storedTimeAfterEnd = timeAfterEnd;
                easingProgress = startingProgress;

                //No getting the mouse Direction via Main.mouse world did not work
                Vector2 mouseDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);

                //I don't know why this is called sus but this is the angle we will compare the mouseDir to 
                Vector2 sus1 = new Vector2(-10, 0).SafeNormalize(Vector2.UnitX);

                //Yes I did have to normalize it again (i don't fuckin know why but it is needed)
                Vector2 sus2 = mouseDir.SafeNormalize(Vector2.UnitX);

                startingAngle = sus1.AngleTo(sus2) * 2; //psure the * 2 is from double normalization
                originalAngle = startingAngle;

                //Fixes bug 
                if (startingAngle == 0 && Projectile.spriteDirection == -1)
                {
                    startingAngle += MathHelper.Pi;
                    originalAngle = startingAngle;
                }

                //we set Projectile.ai[0] in the wep. This is so the sword alternates direction
                if (Projectile.ai[0] == 1)
                {
                    startingAngle = startingAngle - MathHelper.ToRadians(-SwingHalfAngle);
                }
                else
                {
                    startingAngle = startingAngle + MathHelper.ToRadians(-SwingHalfAngle);
                }

                currentAngle = startingAngle;
                firstFrame = false;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;

            }


            if (timer >= frameToStartSwing && justHitTime <= 0)
            {
                if (Projectile.ai[0] == 1)
                    currentAngle = startingAngle - MathHelper.ToRadians((SwingHalfAngle * 2) * getProgress(easingProgress));
                else
                    currentAngle = startingAngle + MathHelper.ToRadians((SwingHalfAngle * 2) * getProgress(easingProgress));

                float meleeSpeed = useMeleeSpeed ? Main.player[Projectile.owner].GetTotalAttackSpeed(DamageClass.Melee) : 1f;

                easingProgress = Math.Clamp(easingProgress + easingAdditionAmount * meleeSpeed, 0.01f, 1f);
            }

            Projectile.rotation = currentAngle + MathHelper.PiOver4;

            Projectile.Center = (currentAngle.ToRotationVector2() * offset) + player.RotatedRelativePoint(player.MountedCenter);// + new Vector2(-5 * player.direction,-2.5f);
            player.itemTime = 10;
            player.itemAnimation = 10;

            justHitTime--;
            timer++;



            if (getProgress(easingProgress) >= progressToKill)
            {
                if (storedTimeAfterEnd <= 0)
                {
                    player.itemTime = 0;
                    player.itemAnimation = 0;
                    Projectile.Kill();
                }
                storedTimeAfterEnd--;

            }
            //Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PortalBoltTrail);
            //d.noGravity = true;

        }

        public void BaseDrawing()
        {

        }

        //input will be from 0-1
        //use with functions from here: https://easings.net
        //TODO at easing function presets to this
        public virtual float getProgress(float x) //From 0 to 1 and returns 0-1
        {
            float toReturn = 0f;

            //easeInOutExpo
            if (x <= 0.5f)
            {
                toReturn = (float)(Math.Pow(2, (16 * x) - 8)) / 2;
            }
            else if (x > 0.5)
            {
                toReturn = (float)(2 - ((Math.Pow(2, (-16 * x) + 8)))) / 2;
            }

            //post 0.5
            if (x == 0)
                toReturn = 0;
            if (x == 1)
                toReturn = 1;

            return toReturn;
        }

        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(SwingHalfAngle);
            writer.Write(startingProgress);
            writer.Write(offset);
            writer.Write(positionOffset.X);
            writer.Write(positionOffset.Y);
            writer.Write(frameToStartSwing);
            writer.Write(easingAdditionAmount);
            writer.Write(timeAfterEnd);
            writer.Write(progressToKill);
            writer.Write(useMeleeSpeed);
            writer.Write(timer);
            writer.Write(startingAngle);
            writer.Write(currentAngle);
            writer.Write(originalAngle);
            writer.Write(firstFrame);
            writer.Write(easingProgress);
            writer.Write(storedDirection);
            writer.Write(storedTimeAfterEnd);
            writer.Write(justHitTime);
            writer.Write(Projectile.rotation);
            writer.Write(Projectile.scale);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            SwingHalfAngle = reader.ReadSingle();
            startingProgress = reader.ReadSingle();
            offset = reader.ReadSingle();
            positionOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            frameToStartSwing = reader.ReadInt32();
            easingAdditionAmount = reader.ReadSingle();
            timeAfterEnd = reader.ReadSingle();
            progressToKill = reader.ReadSingle();
            useMeleeSpeed = reader.ReadBoolean();
            timer = reader.ReadInt32();
            startingAngle = reader.ReadSingle();
            currentAngle = reader.ReadSingle();
            originalAngle = reader.ReadSingle();
            firstFrame = reader.ReadBoolean();
            easingProgress = reader.ReadSingle();
            storedDirection = reader.ReadInt32();
            storedTimeAfterEnd = reader.ReadSingle();
            justHitTime = reader.ReadInt32();
            Projectile.rotation = reader.ReadSingle();
            Projectile.scale = reader.ReadSingle();
        }
    }
}
