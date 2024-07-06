using System;

namespace FalconBMS.Launcher.Input
{
    /// <summary>
    /// Means each actual POV switches on a joystick.
    /// </summary>
    public class PovAssgn
    {
        /// <summary>
        /// One POV switch has 8 directions.
        /// </summary>
        public DirAssgn[] direction = new DirAssgn[8];

        // Constructor
        public PovAssgn()
        {
            for (int i = 0; i < direction.Length; i++)
                direction[i] = new DirAssgn();
        }
        public PovAssgn(PovAssgn otherInstance)
        {
            for (int i = 0; i < direction.Length; i++)
                direction[i] = otherInstance.direction[i].Clone();
        }

        // Method
        public void Assign(int povDir, string callback, Pinky pinky, int soundID)
        {
            if (povDir > 7)
                povDir /= 4500;
            direction[povDir].Assign(callback, pinky, soundID);
        }

        public string GetCurrentCallback(int povDir, Pinky pinky)
        {
            if (povDir > 7)
                povDir /= 4500;
            return this.direction[povDir].GetCallback(pinky);
        }

        public static string GetDirectionLabel(int povDir)
        {
            // If no direction (hat is centered) the value is -1.
            if (povDir < 0)
                return String.Empty;

            // DirectInput transmits pov-hat direction in compass-degrees x100 -- we divide by 4500 to get [0-7].
            int pov0to7 = (povDir <= 7 ? povDir : povDir / CommonConstants.POV45);
            switch (pov0to7)
            {
                case 0:
                    return "UP";
                case 1:
                    return "UPRIGHT";
                case 2:
                    return "RIGHT";
                case 3:
                    return "DOWNRIGHT";
                case 4:
                    return "DOWN";
                case 5:
                    return "DOWNLEFT";
                case 6:
                    return "LEFT";
                case 7:
                    return "UPLEFT";
                default:
                    throw new ApplicationException("Unexpected pov-hat direction from DirectInput: " + povDir);
            }
        }

        public PovAssgn Clone()
        {
            return new PovAssgn(this);
        }
    }

    /// <summary>
    /// Means each direction on a POV switch,
    /// </summary>
    public class DirAssgn
    {
        // Member
        protected string[] callback = { CommonConstants.SIMDONOTHING, CommonConstants.SIMDONOTHING };
        protected int[] soundID = { 0, 0 };
        // [0]=PRESS
        // [1]=PRESS + SHIFT

        // Property for XML
        public string[] Callback { get => callback;
            set => callback = value;
        }
        public int[] SoundID { get => soundID;
            set => soundID = value;
        }

        // Constructor
        public DirAssgn() { }
        public DirAssgn(DirAssgn otherInstance)
        {
            callback[0] = otherInstance.callback[0];
            callback[1] = otherInstance.callback[1];
            soundID[0] = otherInstance.soundID[0];
            soundID[1] = otherInstance.soundID[1];
        }

        // Method
        public string GetCallback(Pinky pinky) { return callback[(int)pinky]; }
        public int GetSoundID(Pinky pinky) { return soundID[(int)pinky]; }

        public void Assign(string callback, Pinky pinky, int soundID)
        {
            this.callback[(int)pinky] = callback;
            this.soundID[(int)pinky] = soundID;
        }

        public void UnAssign(Pinky pinky)
        {
            callback[(int)pinky] = CommonConstants.SIMDONOTHING;
            soundID[(int)pinky] = 0;
        }

        public DirAssgn Clone()
        {
            return new DirAssgn(this);
        }
    }
}
