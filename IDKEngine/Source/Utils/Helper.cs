﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using ReFuel.Stb;
using BBLogger;
using BBOpenGL;

namespace IDKEngine.Utils
{
    public static class Helper
    {
        public static void GLDebugCallback(
            BBG.Debugging.DebugSource source,
            BBG.Debugging.DebugType type,
            BBG.Debugging.DebugSeverity severity,
            uint messageID,
            string message)
        {
            const bool FILTER_UNWANTED = true;

            switch (severity)
            {
                case BBG.Debugging.DebugSeverity.Low:
                    Logger.Log(Logger.LogLevel.Info, message);
                    break;

                case BBG.Debugging.DebugSeverity.Medium:
                    if (FILTER_UNWANTED && messageID == 0) return; // Shader compile warning, Intel
                    // if (FILTER_UNWANTED && messageID == 131186) return; // Buffer object is being copied/moved from VIDEO memory to HOST memory, NVIDIA
                    if (FILTER_UNWANTED && messageID == 131154) return; // Pixel-path performance warning: Pixel transfer is synchronized with 3D rendering, NVIDIA

                    Logger.Log(Logger.LogLevel.Warn, message);
                    break;

                case BBG.Debugging.DebugSeverity.High:
                    if (FILTER_UNWANTED && messageID == 0) return; // Shader compile error, Intel
                    if (FILTER_UNWANTED && messageID == 2000) return; // Shader compile error, AMD
                    if (FILTER_UNWANTED && messageID == 2001) return; // Program link error, AMD

                    Logger.Log(Logger.LogLevel.Error, message);
                    break;

                case BBG.Debugging.DebugSeverity.Notification:
                    if (FILTER_UNWANTED && messageID == 131185) return; // Buffer detailed info, NVIDIA

                    Logger.Log(Logger.LogLevel.Info, message);
                    break;

                case BBG.Debugging.DebugSeverity.DontCare:
                default:
                    break;
            }
        }

        // TODO: In .NET 9 rid of all SizeInBytes overloads in favour of ReadOnlySpan<T>, as it can be implciliy converted in extensions

        public static unsafe int SizeInBytes<T>(this T[] data) where T : unmanaged
        {
            return sizeof(T) * data.Length;
        }

        public static unsafe int SizeInBytes<T>(this ReadOnlyArray<T> data) where T : unmanaged
        {
            return sizeof(T) * data.Length;
        }

        public static unsafe int SizeInBytes<T>(this NativeMemoryView<T> data) where T : unmanaged
        {
            return sizeof(T) * data.Length;
        }

        public static T[] DeepClone<T>(this T[] array)
        {
            T[] newArr = new T[array.Length];
            Array.Copy(array, newArr, newArr.Length);
            return newArr;
        }

        public static void ArrayAdd<T>(ref T[] array, ReadOnlySpan<T> toAdd)
        {
            int prevLength = array.Length;
            Array.Resize(ref array, prevLength + toAdd.Length);
            toAdd.CopyTo(new Span<T>(array, prevLength, toAdd.Length));
        }

        public static void ArrayRemoveRange<T>(ref T[] array, int start, int count)
        {
            int end = start + count;
            Array.Copy(array, end, array, start, array.Length - end);
            Array.Resize(ref array, array.Length - count);
        }

        public static int Sum<T>(Span<T> array, Func<T, int> func)
        {
            int sum = 0;
            for (int i = 0; i < array.Length; i++)
            {
                sum += func(array[i]);
            }

            return sum;
        }

        public static void FillIncreasing(Span<uint> array, uint startValue = 0u)
        {
            FillIncreasing(array, 0, array.Length, startValue);
        }

        public static void FillIncreasing(Span<uint> array, int start, int count, uint startValue = 0u)
        {
            for (int i = start; i < start + count; i++)
            {
                array[i] = (uint)(startValue + i);
            }
        }

        public static Vector4 ToOpenTK(this System.Numerics.Vector4 vector4)
        {
            return Unsafe.As<System.Numerics.Vector4, Vector4>(ref vector4);
        }

        public static System.Numerics.Vector3 ToNumerics(this Vector3 vector3)
        {
            return Unsafe.As<Vector3, System.Numerics.Vector3>(ref vector3);
        }
        public static Vector3 ToOpenTK(this System.Numerics.Vector3 vector3)
        {
            return Unsafe.As<System.Numerics.Vector3, Vector3>(ref vector3);
        }

        public static System.Numerics.Vector2 ToNumerics(this Vector2 vector2)
        {
            return Unsafe.As<Vector2, System.Numerics.Vector2>(ref vector2);
        }
        public static Vector2 ToOpenTK(this System.Numerics.Vector2 vector2)
        {
            return Unsafe.As<System.Numerics.Vector2, Vector2>(ref vector2);
        }

        public static Matrix4 ToOpenTK(this System.Numerics.Matrix4x4 Matrix4)
        {
            return Unsafe.As<System.Numerics.Matrix4x4, Matrix4>(ref Matrix4);
        }

        public static System.Numerics.Matrix4x4 ToNumerics(this Matrix4 Matrix4)
        {
            return Unsafe.As<Matrix4, System.Numerics.Matrix4x4>(ref Matrix4);
        }

        public static Quaternion ToOpenTK(this System.Numerics.Quaternion quaternion)
        {
            return Unsafe.As<System.Numerics.Quaternion, Quaternion>(ref quaternion);
        }

        public static string ToOnOff(this bool val)
        {
            return val ? "On" : "Off";
        }

        public static unsafe bool TryReadFromFile<T>(string path, out T[] readData) where T : unmanaged
        {
            readData = null;

            using FileStream fileStream = File.OpenRead(path);
            if (fileStream.Length % sizeof(T) != 0)
            {
                Logger.Log(Logger.LogLevel.Error, $"Cannot load \"{path}\", because file size is not a multiple of {sizeof(T)} bytes");
                return false;
            }
            if (fileStream.Length == 0)
            {
                Logger.Log(Logger.LogLevel.Warn, $"Cannot load \"{path}\", because it's an empty file");
                return false;
            }

            readData = new T[fileStream.Length / sizeof(T)];

            Span<byte> byteData = MemoryMarshal.AsBytes<T>(readData);
            fileStream.Read(byteData);

            return true;
        }

        public static void WriteToFile<T>(string path, ReadOnlySpan<T> data) where T : unmanaged
        {
            using FileStream file = File.OpenWrite(path);
            ReadOnlySpan<byte> byteData = MemoryMarshal.AsBytes(data);
            file.Write(byteData);
        }

        public static unsafe void TextureToDiskJpg(BBG.Texture texture, string path, int quality = 100, bool flipVertically = true)
        {
            int nChannels = 3;

            byte* pixels = Memory.Malloc<byte>(texture.Width * texture.Height * nChannels);
            texture.Download(BBG.Texture.NumChannelsToPixelFormat(nChannels), BBG.Texture.PixelType.UByte, pixels, texture.Width * texture.Height * nChannels * sizeof(byte));

            using FileStream fileStream = File.OpenWrite($"{path}.jpg");
            
            StbImage.FlipVerticallyOnSave = flipVertically;
            StbImage.WriteJpg(
                new ReadOnlySpan<byte>(pixels, texture.Width * texture.Height * nChannels),
                texture.Width,
                texture.Height,
                StbiImageFormat.Rgb,
                fileStream,
                quality,
                false
            );

            Memory.Free(pixels);
        }
    }
}