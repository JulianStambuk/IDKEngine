﻿using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace IDKEngine.Render.Objects
{
    public class BufferObject : IDisposable
    {
        public readonly int ID;
        public int Size { get; private set; }

        public BufferObject()
        {
            GL.CreateBuffers(1, out ID);
        }

        public void BindBufferBase(BufferRangeTarget target, int index)
        {
            GL.BindBufferBase(target, index, ID);
        }

        public void Bind(BufferTarget bufferTarget)
        {
            GL.BindBuffer(bufferTarget, ID);
        }

        public void SubData<T>(int offset, int size, T data) where T : unmanaged
        {
            GL.NamedBufferSubData(ID, (IntPtr)offset, size, ref data);
        }
        public void SubData<T>(int offset, int size, T[] data) where T : unmanaged
        {
            GL.NamedBufferSubData(ID, (IntPtr)offset, size, data);
        }
        public void SubData(int offset, int size, IntPtr data)
        {
            GL.NamedBufferSubData(ID, (IntPtr)offset, size, data);
        }

        public void MutableAllocate<T>(int size, T data) where T : unmanaged
        {
            GL.NamedBufferData(ID, size, ref data, BufferUsageHint.StaticDraw);
            Size = size;
        }
        public void MutableAllocate<T>(int size, T[] data) where T : unmanaged
        {
            GL.NamedBufferData(ID, size, data, BufferUsageHint.StaticDraw);
            Size = size;
        }
        public void MutableAllocate(int size, IntPtr data)
        {
            GL.NamedBufferData(ID, size, data, BufferUsageHint.StaticDraw);
            Size = size;
        }

        public void ImmutableAllocate<T>(int size, T data, BufferStorageFlags bufferStorageFlags) where T : unmanaged
        {
            GL.NamedBufferStorage(ID, size, ref data, bufferStorageFlags);
            Size = size;
        }
        public void ImmutableAllocate<T>(int size, T[] data, BufferStorageFlags bufferStorageFlags) where T : unmanaged
        {
            GL.NamedBufferStorage(ID, size, data, bufferStorageFlags);
            Size = size;
        }
        public void ImmutableAllocate(int size, IntPtr data, BufferStorageFlags bufferStorageFlags)
        {
            GL.NamedBufferStorage(ID, size, data, bufferStorageFlags);
            Size = size;
        }

        public void GetSubData<T>(int offset, int size, out T data) where T : unmanaged
        {
            data = new T();
            GL.GetNamedBufferSubData(ID, (IntPtr)offset, size, ref data);
        }
        public void GetSubData<T>(int offset, int size, T[] data) where T : unmanaged
        {
            GL.GetNamedBufferSubData(ID, (IntPtr)offset, size, data);
        }
        public void GetSubData(int offset, int size, IntPtr data)
        {
            GL.GetNamedBufferSubData(ID, (IntPtr)offset, size, data);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(ID);
        }
    }
}
