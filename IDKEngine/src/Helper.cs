﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace IDKEngine
{
    static class Helper
    {
        public static readonly double APIVersion = Convert.ToDouble($"{GL.GetInteger(GetPName.MajorVersion)}{GL.GetInteger(GetPName.MinorVersion)}") / 10.0;

        private static HashSet<string> GetExtensions()
        {
            HashSet<string> extensions = new HashSet<string>(GL.GetInteger(GetPName.NumExtensions));
            for (int i = 0; i < GL.GetInteger(GetPName.NumExtensions); i++)
                extensions.Add(GL.GetString(StringNameIndexed.Extensions, i));
            
            return extensions;
        }

        private static readonly HashSet<string> glExtensions = GetExtensions();


        /// <summary>
        /// </summary>
        /// <param name="extension">The extension to check against. Examples: GL_ARB_bindless_texture or WGL_EXT_swap_control</param>
        /// <returns>True if the extension is available</returns>
        public static bool IsExtensionsAvailable(string extension)
        {
            return glExtensions.Contains(extension);
        }

        /// <summary>
        /// </summary>
        /// <param name="extension">Extension to check against. Examples: GL_ARB_direct_state_access or GL_ARB_compute_shader</param>
        /// <param name="first">API version the extension became part of the core profile</param>
        /// <returns>True if this GL version >=<paramref name="first"/> or the extension is otherwise available</returns>
        public static bool IsCoreExtensionAvailable(string extension, double first)
        {
            return (APIVersion >= first) || IsExtensionsAvailable(extension);
        }

        public static DebugProc DebugCallback = Debug;
        private static void Debug(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            // Filter shader compile error and "... will use bla bla ..."
            if (id != 2000 && id != 131185)
            {
                Console.WriteLine($"\nType: {type},\nSeverity: {severity},\nMessage: {Marshal.PtrToStringAnsi(message, length - 1)}");
                if (severity == DebugSeverity.DebugSeverityHigh)
                {
                    Console.WriteLine($"Critical error detected, press enter to continue");
                    Console.ReadLine();
                }
                Console.WriteLine();
            }
        }

        public static unsafe T* Malloc<T>(int count = 1) where T : unmanaged
        {
            return (T*)Marshal.AllocHGlobal(sizeof(T) * count);
        }

        public static unsafe void MemCpy(void* src, void* dest, int len)
        {
            // I don't like having to specify a destination size so yeah
            System.Buffer.MemoryCopy(src, dest, long.MaxValue, len);
        }

        public static void Swap<T>(ref T first, ref T other) where T : struct
        {
            T temp = first;
            first = other;
            other = temp;
        }

        private static readonly Random rng = new Random();
        public static Vector3 RandomVec3(float min, float max)
        {
            return new Vector3(min) + new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()) * (max - min);
        }

        public static Vector3 RandomVec3(Vector3 min, Vector3 max)
        {
            return min + new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()) * (max - min);
        }

        public static float RandomFloat(float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}