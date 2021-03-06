﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GNHSP.Gate.Properties;
using RawInputProcessor;

namespace GNHSP.Gate
{
    static class Keyboard
    {
        private static RawInput _rawInput;

        public static void Hook(Visual visual)
        {
            if (_rawInput == null)
            {
                _rawInput = new RawPresentationInput(visual, RawInputCaptureMode.ForegroundAndBackground);
                _rawInput.KeyPressed += RawInputOnKeyPressed;
            }
        }

        private static StringBuilder _input = new StringBuilder(10);

        private static string GetScannerId(RawKeyboardDevice device)
        {
            var regex = new Regex(@"(?<={)(.*)(?=})");
            if (regex.IsMatch(device.Name))
                return regex.Match(device.Name).Value;
            return device.Name;
        }

        private static Dictionary<Key, Action> _watchKeys = new Dictionary<Key, Action>();

        public static void WatchKey(Key key, Action callback)
        {
            _watchKeys.Add(key, callback);
        }

        public static Action<string> ExclusiveScan { get; set; }

        private static void RawInputOnKeyPressed(object sender, RawInputEventArgs e)
        {
            if (e.KeyPressState == KeyPressState.Up && _watchKeys.ContainsKey(e.Key))
            {
                _watchKeys[e.Key]?.Invoke();
            }

            if (IsWaitingForScanner)
            {
                Settings.Default.ScannerId = GetScannerId(e.Device);
                Settings.Default.ScannerDescription = e.Device.Description;
                Settings.Default.ScannerType = e.Device.Type.ToString();
                Settings.Default.ScannerName = e.Device.Name;
                Settings.Default.Save();
                
                IsWaitingForScanner = false;
                Messenger.Default.Broadcast(Messages.ScannerRegistered);
                _input.Clear();
                return;
            }

            //if (e.Device.Name == Settings.Default.ScannerName)
            //{
                if (e.KeyPressState != KeyPressState.Down) return;
                if (e.Key != Key.Enter)
                {
                    if((e.Key>=Key.A && e.Key <= Key.Z)||(e.Key>=Key.D0 && e.Key<= Key.D9))
                    {
                        _input.Append((char) e.VirtualKey);
                    }
                }
                else
                {
                    if (_input.Length == 0) return;
                    _input.Remove(0, 1);
                    //if (ExclusiveScan != null)
                    //{
                    //    ExclusiveScan.Invoke(_input.ToString());
                    //    _input.Clear();
                    //    return;
                    //}
                    //e.Handled = true;
                    Messenger.Default.Broadcast(Messages.Scan, _input.ToString());
                    _input.Clear();
                }
                //  TrapKey = true;
            //}

        }

      

        public static bool IsWaitingForScanner { get; set; }
        
        public static void RegisterScanner()
        {
            IsWaitingForScanner = true;
        }

        public static void CancelRegistration()
        {
            IsWaitingForScanner = false;
        }

        public static void UnHook()
        {
            //if(hookPtr != IntPtr.Zero)
            //{
            //    UnhookWindowsHookEx(hookPtr);
            //    hookPtr = IntPtr.Zero;
            //}
            if (_rawInput == null) return;
            _rawInput.KeyPressed -= RawInputOnKeyPressed;
            _rawInput.Dispose();
        }

    }
}
