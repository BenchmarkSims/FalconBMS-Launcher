using System;

using Microsoft.DirectX.DirectInput;

namespace FalconBMS.Launcher.Input
{
    public class ButtonStateTracker
    {
        JoyAssgn _joy;
        Device _hwDevice;

        byte[] _lastButtons = new byte[CommonConstants.DX_MAX_BUTTONS];
        int[] _lastPovHats = new int[CommonConstants.DX_MAX_HATS];

        public delegate void ButtonStateChangeCallback(JoyAssgn joy, int buttonId, bool newState);
        public delegate void PovHatStateChangeCallback(JoyAssgn joy, int povhatId, int newDirection);

        ButtonStateChangeCallback _buttonCallback;
        PovHatStateChangeCallback _povhatCallback;

        public ButtonStateTracker(JoyAssgn joy, ButtonStateChangeCallback buttonCallback, PovHatStateChangeCallback povhatCallback)
        {
            _joy = joy;
            _hwDevice = joy.GetDevice();
            System.Diagnostics.Debug.Assert(_hwDevice != null);

            _buttonCallback = buttonCallback;
            _povhatCallback = povhatCallback;

            EstablishNeutralPositions();
        }

        public void EstablishNeutralPositions()
        {
            // Init button buffer with current button-states -- this is important for "stateful" knobs, dials and switches, where 
            // one of a group of buttons is always in a signalled (on) state.
            try
            {
                byte[] buttonState = _hwDevice.CurrentJoystickState.GetButtons();

                Array.Copy(buttonState, _lastButtons, buttonState.Length);

                if (buttonState.Length < _lastButtons.Length)
                    Array.Clear(_lastButtons, buttonState.Length, (_lastButtons.Length - buttonState.Length));
            }
            catch (Exception ex)
            {
                // Microsoft.DirectX.DirectInput.InputLostException happens on some systems - reasons unclear.
                Diagnostics.Log(ex);

                Array.Clear(_lastButtons, 0, _lastButtons.Length);
            }

            // Init povhat buffer, in same way.
            try
            {
                //NB: the values returned by DirectInput are in compass-direction x100. (eg. "right" == 9_000)
                int[] povhatState = _hwDevice.CurrentJoystickState.GetPointOfView();

                Array.Copy(povhatState, _lastPovHats, povhatState.Length);

                if (povhatState.Length < _lastPovHats.Length)
                    Array.Clear(_lastPovHats, povhatState.Length, (_lastPovHats.Length - povhatState.Length));
            }
            catch (Exception ex)
            {
                // Microsoft.DirectX.DirectInput.InputLostException happens on some systems - reasons unclear.
                Diagnostics.Log(ex);

                Array.Clear(_lastPovHats, 0, _lastPovHats.Length);
            }

            return;
        }

        public void PollUpdate()
        {
            // Poll and scan button states.
            try
            {
                byte[] buttonState = _hwDevice.CurrentJoystickState.GetButtons();

                for (int i = 0; i < buttonState.Length; i++)
                {
                    byte bCurr = buttonState[i];
                    byte bLast = _lastButtons[i];

                    if (bCurr != bLast)
                    {
                        bool isPressed = (bCurr == CommonConstants.PRS128); //TODO: should this be ==128 or !=0
                        _buttonCallback(_joy, i, isPressed);

                        _lastButtons[i] = bCurr;
                    }
                }
            }
            catch (Exception ex)
            {
                // Microsoft.DirectX.DirectInput.InputLostException happens on some systems - reasons unclear.
                Diagnostics.Log(ex);
            }

            // Poll and scan povhat states.
            try
            {
                int[] povhatState = _hwDevice.CurrentJoystickState.GetPointOfView();

                for (int i = 0; i < povhatState.Length; i++)
                {
                    int iCurr = povhatState[i];
                    int iLast = _lastPovHats[i];

                    if (iCurr != iLast)
                    {
                        // DirectInput transmits pov-hat direction in compass-degrees x100 -- we divide by 4500 to get range [-1|0-7].
                        int eightway = (iCurr <= 7 ? iCurr : iCurr / CommonConstants.POV45);
                        _povhatCallback(_joy, i, eightway);

                        _lastPovHats[i] = iCurr;
                    }
                }
            }
            catch (Exception ex)
            {
                // Microsoft.DirectX.DirectInput.InputLostException happens on some systems - reasons unclear.
                Diagnostics.Log(ex);
            }

            return;
        }

    }
}
