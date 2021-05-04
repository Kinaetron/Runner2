using System;

using Microsoft.Xna.Framework;

using PolyOne.Utility;

namespace Runner2
{
    public class PlayerCamera : Camera
    {
        private float lerpFactorSide;
        private const float lerpLimit = 0.55f;
        private const float lerpFactorIncrease = 0.011f;
        private const float lerpFactorSideConst = 0.02f;

        private const float cameraLerpFactorUp = 0.02f;
        private float multiplyBy = 0;
        private float newX;

        private bool isOnSide;
        private bool prevSide;

        public Rectangle Trap
        {
            get { return trap; }
            set { trap = value; }
        }
        private Rectangle trap;

        public void LockToTarget(Rectangle collider, int screenWidth, int screenHeight)
        {
            isOnSide = false;

            if(collider.Right >= trap.Right || collider.Left <= trap.Left) {
                isOnSide = true;
            }

            if (collider.Right > trap.Right) {
                multiplyBy = 0.1f;
                trap.X = collider.Right - trap.Width;
            }

            if (collider.Left < trap.Left) {
                multiplyBy = 0.9f;
                trap.X = collider.Left;
            }

            if (collider.Bottom > trap.Bottom) {
                trap.Y = collider.Bottom - trap.Height;
            }

            if (collider.Top < trap.Top) {
                trap.Y = collider.Top;
            }

            if(prevSide != isOnSide) {
                lerpFactorSide = lerpFactorSideConst;
            }
            else {
                lerpFactorSide += lerpFactorIncrease;
            }

            lerpFactorSide = MathHelper.Clamp(lerpFactorSide, lerpFactorSideConst, lerpLimit);

            newX = trap.X + (trap.Width * multiplyBy) - (screenWidth * multiplyBy);
            Position.X = (int)Math.Round(MathHelper.Lerp(Position.X, newX, lerpFactorSide));
            Position.Y = (int)Math.Round((double)trap.Y + (trap.Height / 2) - (screenHeight / 2));

            prevSide = isOnSide;
        }

        public void MoveTrapUp(float target)
        {
            float moveCamera = target - trap.Height;
            trap.Y = (int)MathHelper.Lerp(trap.Y, moveCamera, cameraLerpFactorUp);
        }
    }
}
