﻿//
// JoystickState.cs
//
// Author:
//       Stefanos A. <stapostol@gmail.com>
//
// Copyright (c) 2006-2014 Stefanos Apostolopoulos
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace osuTK.Input
{
    /// <summary>
    /// Describes the current state of a <see cref="JoystickDevice"/>.
    /// </summary>
    public struct JoystickState : IEquatable<JoystickState>
    {
        // If we ever add more values to JoystickAxis or JoystickButton
        // then we'll need to increase these limits.
        internal const int MaxAxes = 64;
        internal const int MaxButtons = 128;
        internal const int MaxHats = (int)JoystickHat.Last + 1;

        private const float ConversionFactor = 1.0f / (short.MaxValue + 0.5f);

        private unsafe fixed bool buttons[MaxButtons];
        private unsafe fixed short axes[MaxAxes];
        private JoystickHatState hat0;
        private JoystickHatState hat1;
        private JoystickHatState hat2;
        private JoystickHatState hat3;

        /// <summary>
        /// Gets a value between -1.0 and 1.0 representing the current offset of the specified axis.
        /// </summary>
        /// <returns>
        /// A value between -1.0 and 1.0 representing offset of the specified axis.
        /// If the specified axis does not exist, then the return value is 0.0. Use <see cref="Joystick.GetCapabilities"/>
        /// to query the number of available axes.
        /// </returns>
        /// <param name="axis">The axis to query.</param>
        public float GetAxis(int axis)
        {
            return GetAxisRaw(axis) * ConversionFactor;
        }

        /// <summary>
        /// Gets the current <see cref="ButtonState"/> of the specified button.
        /// </summary>
        /// <returns><see cref="ButtonState.Pressed"/> if the specified button is pressed; otherwise, <see cref="ButtonState.Released"/>.</returns>
        /// <param name="button">The button to query.</param>
        public ButtonState GetButton(int button)
        { 
            return IsButtonDown(button) ? ButtonState.Pressed : ButtonState.Released;
        }

        /// <summary>
        /// Gets the hat.
        /// </summary>
        /// <returns>The hat.</returns>
        /// <param name="hat">Hat.</param>
        public JoystickHatState GetHat(JoystickHat hat)
        {
            switch (hat)
            {
                case JoystickHat.Hat0:
                    return hat0;
                case JoystickHat.Hat1:
                    return hat1;
                case JoystickHat.Hat2:
                    return hat2;
                case JoystickHat.Hat3:
                    return hat3;
                default:
                    return new JoystickHatState();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the specified button is currently pressed.
        /// </summary>
        /// <returns>true if the specified button is pressed; otherwise, false.</returns>
        /// <param name="button">The button to query.</param>
        public bool IsButtonDown(int button)
        {
            if (button < 0 || button >= MaxButtons)
            {
                Debug.Print("[Joystick] IsButtonDown on invalid button {0}", button);
                return false;
            }

            unsafe
            {
                fixed (bool* pbuttons = buttons)
                {
                    return *(pbuttons + button);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the specified button is currently released.
        /// </summary>
        /// <returns>true if the specified button is released; otherwise, false.</returns>
        /// <param name="button">The button to query.</param>
        public bool IsButtonUp(int button)
        {
            return !IsButtonDown(button);
        }

        /// <summary>
        /// Gets a value indicating whether any button is down.
        /// </summary>
        /// <value><c>true</c> if any button is down; otherwise, <c>false</c>.</value>
        public bool IsAnyButtonDown
        {
            get
            {
                for (int i = 0; i < MaxButtons; i++)
                    if (IsButtonDown(i))
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value><c>true</c> if this instance is connected; otherwise, <c>false</c>.</value>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="osuTK.Input.JoystickState"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="osuTK.Input.JoystickState"/>.</returns>
        public override string ToString()
        {
            StringBuilder sbAxes = new StringBuilder();
            for (int i = 0; i < MaxAxes; i++)
            {
                sbAxes.Append(" ");
                sbAxes.Append(String.Format("{0:f4}", GetAxis(i)));
            }
            
            StringBuilder sbBtns = new StringBuilder();
            unsafe
            {
                for (int i = 0; i < MaxButtons; i++)
                {
                    sbAxes.Append(IsButtonDown(i) ? "1" : "0");
                }
            }

            return String.Format(
                "{{Axes:{0}; Buttons: {1}; Hat: {2}; IsConnected: {3}}}",
                sbAxes,
                sbBtns,
                hat0,
                IsConnected);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="osuTK.Input.JoystickState"/> object.
        /// </summary>
        /// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        /// hash table.</returns>
        public override int GetHashCode()
        {
            int hash = IsConnected.GetHashCode();
            for (int i = 0; i < MaxButtons; i++)
            {
                hash ^= IsButtonDown(i).GetHashCode();
            }
            for (int i = 0; i < MaxAxes; i++)
            {
                hash ^= GetAxisUnsafe(i).GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="osuTK.Input.JoystickState"/>.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with the current <see cref="osuTK.Input.JoystickState"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to the current
        /// <see cref="osuTK.Input.JoystickState"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return
                obj is JoystickState &&
                Equals((JoystickState)obj);
        }

        internal int PacketNumber { get; private set; }

        internal short GetAxisRaw(int axis)
        {
            short value = 0;
            if (axis >= 0 && axis < MaxAxes)
            {
                value = GetAxisUnsafe(axis);
            }
            else
            {
                Debug.Print("[Joystick] GetAxisRaw on invalid axis {0}", axis);
            }
            return value;
        }

        internal void SetAxis(int axis, short value)
        {
            int index = axis;
            if (index < 0 || index >= MaxAxes)
            {
                Debug.Print("[Joystick] Attempted SetAxis on invalid axis {0}", axis);
                return;
            }

            unsafe
            {
                fixed (short* paxes = axes)
                {
                    *(paxes + index) = value;
                }
            }
        }

        internal void ClearButtons()
        {
            for (int i = 0; i < MaxButtons; i++)
                SetButton(i, false);
        }

        internal void SetButton(int button, bool value)
        {
            if (button < 0 || button >= MaxButtons)
            {
                Debug.Print("[Joystick] SetButton on invalid button {0}", button);
                return;
            }

            unsafe
            {
                fixed (bool* pbuttons = buttons)
                {
                    *(pbuttons + button) = value;
                }
            }
        }

        internal void SetHat(JoystickHat hat, JoystickHatState value)
        {
            switch (hat)
            {
                case JoystickHat.Hat0:
                    hat0 = value;
                    break;
                case JoystickHat.Hat1:
                    hat1 = value;
                    break;
                case JoystickHat.Hat2:
                    hat2 = value;
                    break;
                case JoystickHat.Hat3:
                    hat3 = value;
                    break;
                default:
                    return;
            }
        }

        internal void SetIsConnected(bool value)
        {
            IsConnected = value;
        }

        internal void SetPacketNumber(int number)
        {
            PacketNumber = number;
        }

        private short GetAxisUnsafe(int index)
        {
            unsafe
            {
                fixed (short* paxis = axes)
                {
                    return *(paxis + index);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="osuTK.Input.JoystickState"/> is equal to the current <see cref="osuTK.Input.JoystickState"/>.
        /// </summary>
        /// <param name="other">The <see cref="osuTK.Input.JoystickState"/> to compare with the current <see cref="osuTK.Input.JoystickState"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="osuTK.Input.JoystickState"/> is equal to the current
        /// <see cref="osuTK.Input.JoystickState"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(JoystickState other)
        {
            bool equals = IsConnected == other.IsConnected;
            for (int i = 0; equals && i < MaxButtons; i++)
            {
                equals &= IsButtonDown(i) == other.IsButtonDown(i);
            }
            for (int i = 0; equals && i < MaxAxes; i++)
            {
                equals &= GetAxisUnsafe(i) == other.GetAxisUnsafe(i);
            }
            for (int i = 0; equals && i < MaxHats; i++)
            {
                JoystickHat hat = JoystickHat.Hat0 + i;
                equals &= GetHat(hat).Equals(other.GetHat(hat));
            }
            return equals;
        }
    }
}
