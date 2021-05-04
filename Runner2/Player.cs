using System;

using Microsoft.Xna.Framework;
using System.Collections.Generic;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

using PolyOne;
using PolyOne.Collision;
using PolyOne.Scenes;
using PolyOne.Engine;
using PolyOne.Input;
using PolyOne.Animation;
using PolyOne.Components;
using PolyOne.Utility;

namespace Runner2
{
    public class Player : Entity
    {
        public PlayerCamera Camera { get; private set; }

        private float sign;
        private float prevSign;
        private float direction;
        private bool wallJump;

        private Vector2 remainder;
        private Vector2 velocity;
        private Level level;

        private bool isOnGround;
        private bool isOnRightWall;
        private bool isOnLeftWall;

        private Texture2D playerTexture;
        private Texture2D playerTextureNormal;
        private Texture2D playerTextureSlide;
        private Texture2D redTexture;

        private const float runAccel = 0.12f;
        private const float airAccel = 0.3f;
        private const float turnMul = 0.15f;
        private const float normMaxHorizSpeed = 7.0f;
        private const float groundFriction = 0.85f;
        private const float wallFriction = 0.3f;

        private const float airDragXLimit = 0.2f;
        private const float wallAccelTime = 400.0f;
        private const float wallAccel = 2.0f;
        private const float wallMaxHoriSpeed = 6.0f;
        private const float wallUpHeight = -7.0f;
        private const float wallUpHeightHalf = -3.5f;
        private bool onControls = false;

        private const float slideOffSet = 100.0f;
        private const float slideFrictionOffSet = -0.03f;
        private const float slideMimimum = 1.0f;
        private float slideFriction = 1.0f;
        private float slideTime = 450.0f;
        private float slideTimer;
        private int slideSign;
        private bool slideSignBool;

        private const float endPositionOffSet = 32.0f;
        private const float climbSpeed = -3.5f;
        private float endPositionClimb;
        private bool climbed;

        private const float wallSlideFriction = 0.8f;

        private const float fallspeed = 0.4f;

        private const float gravityUp = 0.31f;
        private const float gravityDown = 0.17f;

        private const float initialJumpHeight = -7.5f;
        private const float halfJumpHeight = -3.1f;
        private const float airFriction = 0.8f;
        private const float airInteria = 0.89f;
        private const float airDrag = 0.95f;

        private bool buttonPushed;
        private bool pushedUp;

        private const float graceTime = 66.9f;
        private const float graceTimePush = 66.9f;

        private const float wallTime = 300.0f;

        CounterSet<string> counters = new CounterSet<string>();

        private bool controllerMode;
        private bool keyboardMode;
        private List<Keys> keyList = new List<Keys>(new Keys[] { Keys.W, Keys.A, Keys.S, Keys.D, Keys.Up,
                                                                 Keys.Down, Keys.Left, Keys.Right ,Keys.Space });

        private StateMachine state;

        public Player(Vector2 position)
            : base(position)
        {
          
            this.Tag((int)GameTags.Player);
            this.Collider = new Hitbox((float)16.0f, (float)20.0f, 0.0f, 0.0f);

            Camera = new PlayerCamera();
            Camera.Trap = new Rectangle((int)this.Right, (int)this.Bottom - 100, 100, 100);

            playerTextureNormal = Engine.Instance.Content.Load<Texture2D>("Player");
            playerTextureSlide = Engine.Instance.Content.Load<Texture2D>("PlayerSlide");
            redTexture = Engine.Instance.Content.Load<Texture2D>("Tiles/Red");
            this.Visible = true;

            state = new StateMachine(4);

            state.SetCallbacks(0, new Func<int>(NormalUpdate), null, new Action(EnterNormal), new Action(LeaveNormal));
            state.SetCallbacks(1, new Func<int>(WallJumpUpdate), null, new Action(EnterWallJump), new Action(LeaveWallJump));
            state.SetCallbacks(2, new Func<int>(SlideUpdate), null, new Action(EnterSlide), new Action(LeaveSlide));
            state.SetCallbacks(3, new Func<int>(WallClimbUpdate), null, new Action(EnterWallClimb), new Action(LeaveWallClimb));

            this.Add(state);
            this.Add(counters);

            playerTexture = playerTextureNormal;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (base.Scene is Level) {
                this.level = (base.Scene as Level);
            }
        }

        private void InputMode()
        {
            foreach (Keys key in keyList)
            {
                if (PolyInput.Keyboard.Check(key) == true)
                {
                    controllerMode = false;
                    keyboardMode = true;
                }
            }
            if (PolyInput.GamePads[0].ButtonCheck() == true)
            {
                controllerMode = true;
                keyboardMode = false;
            }

            if (controllerMode == false && keyboardMode == false) {
                keyboardMode = true;
            }
        }

        public override void Update()
        {
            InputMode();

            isOnGround = base.CollideCheck((int)GameTags.Solid, this.Position + Vector2.UnitY);
            isOnRightWall = base.CollideCheck((int)GameTags.Solid, this.Position + Vector2.UnitX);
            isOnLeftWall = base.CollideCheck((int)GameTags.Solid, this.Position - Vector2.UnitX);


            Camera.LockToTarget(this.Rectangle, Engine.VirtualWidth, Engine.VirtualHeight);
            Camera.ClampToArea((int)level.Tile.MapWidthInPixels - Engine.VirtualWidth, (int)level.Tile.MapHeightInPixels - Engine.VirtualHeight);

            if (isOnGround == true) {
                Camera.MoveTrapUp(Bottom);
            }

            sign = 0;

            if(controllerMode == true)
            {
                if (PolyInput.GamePads[0].LeftStickHorizontal(0.3f) > 0.1f ||
                   PolyInput.GamePads[0].DPadRightCheck == true)
                {
                    sign = 1;
                }
                else if (PolyInput.GamePads[0].LeftStickHorizontal(0.3f) < -0.1f ||
                         PolyInput.GamePads[0].DPadLeftCheck == true)
                {
                    sign = -1;
                }
            }
            else if(keyboardMode == true)
            {
                if (PolyInput.Keyboard.Check(Keys.Right) ||
                  PolyInput.Keyboard.Check(Keys.D))
                {
                    sign = 1;
                }
                else if (PolyInput.Keyboard.Check(Keys.Left) ||
                         PolyInput.Keyboard.Check(Keys.A))
                {
                    sign = -1;
                }
            }

            if (isOnGround == false && isOnLeftWall == false &&
               isOnRightWall == false) {
                climbed = false;
            }
            else if (isOnGround == true) {
                climbed = false;
            }


            base.Update();

            prevSign = sign;

            if (isOnGround == true && sign == 0) {
                velocity.X *= groundFriction;
            }
            else if(sign == 0) {
                velocity.X *= airInteria;
            }

            if (graceTimePush > 0 && velocity.Y < 0) {
                velocity.Y += gravityUp;
            }
            else {
                velocity.Y += gravityDown;
            }

            if (isOnRightWall == true && sign > 0 &&
                isOnGround == false && velocity.Y > 0)
            {
                remainder.Y *= wallSlideFriction;
            }
            else if (isOnLeftWall == true && sign < 0 &&
                     isOnGround == false && velocity.Y > 0)
            {
                remainder.Y *= wallSlideFriction;
            }

            if (velocity.Y < 0 && velocity.Y > -halfJumpHeight)
            {
                if (Math.Abs(velocity.X) > airDragXLimit) {
                    velocity.X *= airDrag;
                }
            }

            velocity.X = MathHelper.Clamp(velocity.X, -normMaxHorizSpeed, normMaxHorizSpeed);
            MovementHorizontal(velocity.X);

            velocity.Y = MathHelper.Clamp(velocity.Y, initialJumpHeight, fallspeed);
            MovementVerical(velocity.Y);

        }

        private void EnterNormal()
        {
            PolyDebug.Log("Started Enter Normal");
        }

        private void LeaveNormal()
        {
            //PolyDebug.Log("Started Leave Normal");
        }

        private int NormalUpdate()
        {
            VerticalInput();

            if(controllerMode == true)
            {
                if ((PolyInput.GamePads[0].Check(Buttons.X) == true ||
                     PolyInput.GamePads[0].DPadDownCheck == true || 
                     PolyInput.GamePads[0].LeftStickVertical(0.3f) < -0.1f) &&
                     isOnGround == true)
                {
                    return 2;
                }

                if((PolyInput.GamePads[0].LeftStickVertical(0.3f) > 0.1f || 
                    PolyInput.GamePads[0].DPadUpCheck == true) && 
                    (isOnLeftWall == true || isOnRightWall == true ) && climbed == false)
                {
                    climbed = true;
                    return 3;
                }
            }
            else if(keyboardMode == true)
            {
                if((PolyInput.Keyboard.Check(Keys.C) == true || 
                    PolyInput.Keyboard.Check(Keys.Down) == true || 
                    PolyInput.Keyboard.Check(Keys.S) == true) &&
                   isOnGround == true)
                {
                    return 2;
                }

                if((PolyInput.Keyboard.Check(Keys.Up) == true || 
                    PolyInput.Keyboard.Check(Keys.W) == true) &&
                    (isOnLeftWall == true || isOnRightWall == true) && climbed == false)
                {
                    climbed = true;
                    return 3;
                }
            }

            if (isOnLeftWall == true && isOnGround == false && sign >= 0) {
                return 1;
            }

            if (isOnRightWall == true && isOnGround == false && sign <= 0) {
                return 1;
            }
            return HorizontalInput();
        }

        private void EnterSlide()
        {
            PolyDebug.Log("Started Enter Slide");
            Position.Y += 10;
            playerTexture = playerTextureSlide;
            this.Collider = new Hitbox((float)20.0f, (float)10.0f, 0.0f, 0.0f);
        }

        private void LeaveSlide()
        {
            slideTimer = 0;
            Position.Y -= 10;
            slideTime = 450.0f;
            slideFriction = 1.0f;
            slideSignBool = false;
            playerTexture = playerTextureNormal;
            this.Collider = new Hitbox((float)16.0f, (float)20.0f, 0.0f, 0.0f);
        }

        private int SlideUpdate()
        {
            bool isBlockOnTop = base.CollideCheck((int)GameTags.Solid, new Vector2(Position.X, Position.Y - 10)); 

            if (controllerMode == true)
            {
                if (PolyInput.GamePads[0].Check(Buttons.X) == false &&
                    PolyInput.GamePads[0].DPadDownCheck == false &&
                    PolyInput.GamePads[0].LeftStickVertical(0.3f) > -0.1f &&
                    isBlockOnTop == false)
                {
                    return 1;
                }
            }
            else if(keyboardMode == true)
            {
                if(PolyInput.Keyboard.Check(Keys.C) == false && 
                   PolyInput.Keyboard.Check(Keys.Down) == false &&
                   PolyInput.Keyboard.Check(Keys.S) == false && 
                   isBlockOnTop == false)
                {
                    return 1;
                }
            }

            slideTimer += Engine.DeltaTime;

            if (slideTimer > slideTime)  {
                slideTime += slideOffSet;
                slideFriction += slideFrictionOffSet;
            }

            if (Math.Abs(velocity.X) > slideMimimum) {
                velocity.X *= slideFriction;
            }

            if(Math.Abs(velocity.X) <= slideMimimum && slideSignBool == false) {
                slideSign = Math.Sign(velocity.X);
                slideSignBool = true;
            }
            
            if(Math.Abs(velocity.X) <= slideMimimum && sign != 0) {
                velocity.X = slideMimimum * sign;
                slideSignBool = false;
            }
            else if(slideSignBool == true) {
                velocity.X = slideMimimum * slideSign;
            }

            return 2;
        }

        private void EnterWallClimb()
        {
            PolyDebug.Log("Started Enter Wall Climb");
            endPositionClimb = Position.Y - endPositionOffSet;
        }

        private void LeaveWallClimb()
        {
            climbed = true;
        }

        private int WallClimbUpdate()
        {
            if (controllerMode == true)
            {
               if ((PolyInput.GamePads[0].LeftStickVertical(0.3f) > 0.1f ||
                     PolyInput.GamePads[0].DPadUpCheck == true) &&
                     (isOnLeftWall == true || isOnRightWall == true))
                {
                    velocity.Y = climbSpeed;
                }
                else if (PolyInput.GamePads[0].Pressed(Buttons.A) == true)
                {
                    return 2;
                }
                else {
                    return 1;
                }
            }
            else if(keyboardMode == true)
            {
                if ((PolyInput.Keyboard.Check(Keys.Up) == true ||
                     PolyInput.Keyboard.Check(Keys.W) == true) &&
                     (isOnLeftWall == true || isOnRightWall == true))
                {
                    velocity.Y = climbSpeed;
                }
                else if(PolyInput.Keyboard.Pressed(Keys.Space) == true) {
                    return 2;
                }
                else {
                    return 1;
                }
            }

            if (endPositionClimb > Position.Y) {
                return 1;
            }

            return 3;
        }

        private void EnterWallJump()
        {
            PolyDebug.Log("Started Enter Wall Jump");
        }

        private void LeaveWallJump()
        {
            onControls = false;
            direction = 0;
            sign = 0;
            counters["onRightWall"] = 0;
            counters["onLeftWall"] = 0;
            counters["AccelTime"] = 0;
            wallJump = false;
            velocity.Y = 0;
        }

        private int WallJumpUpdate()
        {
            if (isOnRightWall == true && isOnGround == false && counters["onRightWall"] <= 0 && sign >= 0) {
                onControls = false;
                counters["onRightWall"] = wallTime;
            }
            else if (isOnLeftWall == true && isOnGround == false && counters["onLeftWall"] <= 0 && sign <= 0) {
                onControls = false;
                counters["onLeftWall"] = wallTime;
            }

            float onRightWallTime = counters["onRightWall"];
            float onLeftWallTime = counters["onLeftWall"];

            if (controllerMode == true)
            {
                if ((PolyInput.GamePads[0].LeftStickVertical(0.3f) > 0.1f ||
                    PolyInput.GamePads[0].DPadUpCheck == true) &&
                    (isOnLeftWall == true || isOnRightWall == true) && climbed == false)
                {
                    climbed = true;
                    return 3;
                }

                if (PolyInput.GamePads[0].Pressed(Buttons.A) == true) {
                    counters["graceTimerPushWall"] = graceTimePush;
                }

                if (counters["graceTimerPushWall"] > 0 && isOnRightWall == true && sign <= 0)
                {
                    counters["AccelTime"] = wallAccelTime;
                    counters["onRightWall"] = 0;
                    wallJump = true;
                }
                else if (counters["graceTimerPushWall"] > 0 && isOnLeftWall == true && sign >= 0)
                {
                    counters["AccelTime"] = wallAccelTime;
                    counters["onLeftWall"] = 0;
                    wallJump = true;
                }
                else if (PolyInput.GamePads[0].Released(Buttons.A) == true && 
                         isOnLeftWall == false && isOnRightWall == false)
                {
                    wallJump = false;
                }

                if (isOnRightWall == true && wallJump == true) {
                    direction = -1;
                }
                else if (isOnLeftWall == true && wallJump == true) {
                    direction = 1;
                }

                if (wallJump == false && PolyInput.GamePads[0].Released(Buttons.A) == true && 
                    velocity.Y < 0 && velocity.Y < wallUpHeightHalf)
                {
                    velocity.Y = wallUpHeightHalf;
                    velocity.X = velocity.X / 2.0f;
                }
            }
            else if(keyboardMode == true)
            {
                if ((PolyInput.Keyboard.Check(Keys.Up) == true ||
                    PolyInput.Keyboard.Check(Keys.W) == true) &&
                    (isOnLeftWall == true || isOnRightWall == true) && climbed == false)
                {
                    climbed = true;
                    return 3;
                }

                if (PolyInput.Keyboard.Pressed(Keys.Space) == true) {
                    counters["graceTimerPushWall"] = graceTimePush;
                }

                if (counters["graceTimerPushWall"] > 0 && isOnRightWall == true && sign <= 0)
                {
                    counters["AccelTime"] = wallAccelTime;
                    counters["onRightWall"] = 0;
                    wallJump = true;
                }
                else if (counters["graceTimerPushWall"] > 0 && isOnLeftWall == true && sign >= 0)
                {
                    counters["AccelTime"] = wallAccelTime;
                    counters["onLeftWall"] = 0;
                    wallJump = true;
                }
                else if (PolyInput.Keyboard.Pressed(Keys.Space) == true &&
                         isOnLeftWall == false && isOnRightWall == false)
                {
                    wallJump = false;
                }

                if (isOnRightWall == true && wallJump == true) {
                    direction = -1;
                }
                else if (isOnLeftWall == true && wallJump == true) {
                    direction = 1;
                }

                if (wallJump == false && PolyInput.Keyboard.Released(Keys.Space) == true &&
                    velocity.Y < 0 && velocity.Y < wallUpHeightHalf)
                {
                    velocity.Y = wallUpHeightHalf;
                    velocity.X = velocity.X / 2.0f;
                }
            }

            if (sign != 0) {

                if (isOnLeftWall == false && isOnRightWall == false) {
                    onControls = true;
                }

                if (counters["AccelTime"] > 0) {
                    velocity.X += wallAccel * sign;
                }
                else {
                    velocity.X += airAccel * sign;
                }
            }
            else if(onControls == false) {
                velocity.X += wallAccel * direction;
            }


            if (counters["onRightWall"] > 0 && wallJump == false) {
                velocity.X = 0;
            }
            else if (counters["onLeftWall"] > 0 && wallJump == false) {
                velocity.X = 0;
            }

            if (wallJump == true) {
                wallJump = false;
                velocity.Y = wallUpHeight;
            }

            if (isOnGround == true) {
                return 0;
            }

            float currentSign = Math.Sign(velocity.X);

            if (sign != 0 && currentSign != sign) {
                velocity.X *= turnMul;
            }

            velocity.X = MathHelper.Clamp(velocity.X, -wallMaxHoriSpeed, wallMaxHoriSpeed);

            return 1;
        }


        private int HorizontalInput()
        {
            float currentSign = Math.Sign(velocity.X);

            if (sign != 0 && currentSign != sign) {
                velocity.X *= turnMul;
            }

            if (isOnGround == true) {
                velocity.X += runAccel * sign;
            }
            else {
                velocity.X += airAccel * sign;
            }

            return 0;
        }

        private void VerticalInput()
        {
            if (isOnGround == true) {
                buttonPushed = false;
                counters["graceTimer"] = graceTime;
            }

            if (controllerMode == true)
            {
                if (PolyInput.GamePads[0].Pressed(Buttons.A) == true) {
                    counters["graceTimerPush"] = graceTimePush;
                }

                if (counters["graceTimerPush"] > 0)
                {
                    if(isOnGround == true || counters["graceTimer"] > 0)
                    {
                        buttonPushed = true;
                        counters["graceTimerPush"] = 0.0f;
                        velocity.Y = initialJumpHeight;
                    }
                }
                else if (PolyInput.GamePads[0].Released(Buttons.A) == true && velocity.Y < 0.0f &&
                         velocity.Y < halfJumpHeight)
                {
                    counters["graceTimerPush"] = 0.0f;
                    velocity.Y = halfJumpHeight;
                }

            }
            else if(keyboardMode == true)
            {
                if (PolyInput.Keyboard.Pressed(Keys.Space) == true) {
                    counters["graceTimerPush"] = graceTimePush;
                }

                if (counters["graceTimerPush"] > 0)
                {
                    if (isOnGround == true || counters["graceTimer"] > 0)
                    {
                        buttonPushed = true;
                        counters["graceTimerPush"] = 0.0f;
                        velocity.Y = initialJumpHeight;
                    }
                }
                else if (PolyInput.Keyboard.Released(Keys.Space) == true && velocity.Y < 0.0f &&
                         velocity.Y < halfJumpHeight)
                {
                    counters["graceTimerPush"] = 0.0f;
                    velocity.Y = halfJumpHeight;
                }
            }
        }

        private void MovementHorizontal(float amount)
        {
            remainder.X += amount;
            int move = (int)Math.Round((double)remainder.X);

            if (move != 0)
            {
                remainder.X -= move;
                int sign = Math.Sign(move);

                while (move != 0)
                {
                    Vector2 newPosition = Position + new Vector2(sign, 0);

                    if (this.CollideFirst((int)GameTags.Solid, newPosition) != null)
                    {
                        velocity.X = 0.0f;
                        remainder.X = 0;
                        break;
                    }
                    Position.X += sign;
                    move -= sign;
                }
            }
        }

        private void MovementVerical(float amount)
        {
            remainder.Y += amount;
            int move = (int)Math.Round((double)remainder.Y);

            if (move < 0)
            {
                remainder.Y -= move;
                while (move != 0)
                {
                    Vector2 newPosition = Position + new Vector2(0, -1.0f);
                    if (this.CollideFirst((int)GameTags.Solid, newPosition) != null)
                    {
                        velocity.Y = 0.0f;
                        remainder.Y = 0;
                        break;
                    }
                    Position.Y += -1.0f;
                    move -= -1;
                }
            }
            else if (move > 0)
            {
                while (move != 0)
                {
                    Vector2 newPosition = Position + new Vector2(0, 1.0f);
                    if (this.CollideFirst((int)GameTags.Solid, newPosition) != null)
                    {
                        remainder.Y = 0;
                        break;
                    }
                    Position.Y += 1.0f;
                    move -= 1;
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
            //Engine.SpriteBatch.Draw(redTexture, Camera.Trap, Color.White);
            Engine.SpriteBatch.Draw(playerTexture, Position, Color.White);
        }
    }
}