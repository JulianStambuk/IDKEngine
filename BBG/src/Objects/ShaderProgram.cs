﻿using System.Diagnostics;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using BBLogger;

namespace BBOpenGL
{
    public static partial class BBG
    {
        public class ShaderProgram : IDisposable
        {
            public readonly int ID;
            public ShaderProgram(Shader[] others)
            {
                ID = GL.CreateProgram();
                Link(others);
            }
            public ShaderProgram(Shader first, params Shader[] others)
                : this(others.Concat([first]).ToArray())
            {
            }

            public void Link(Shader first, params Shader[] others)
            {
                Shader[] shaders = others.Concat([first]).ToArray();
                Link(shaders);
            }

            public void Link(Shader[] shaders)
            {
                if (shaders.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < shaders.Length; i++)
                {
                    Shader shader = shaders[i];
                    if (shader.GetCompileStatus())
                    {
                        GL.AttachShader(ID, shaders[i].ID);
                    }
                }

                GL.LinkProgram(ID);

                GL.GetProgramInfoLog(ID, out string infoLog);
                if (infoLog != string.Empty)
                {
                    string shaderNames = string.Join(", ", shaders.Select(shader => $"{shader.Name}").ToArray());

                    Logger.LogLevel level = GetLinkStatus() ? Logger.LogLevel.Info : Logger.LogLevel.Error;
                    Logger.Log(level, $"ShaderProgram [{shaderNames}] log:\n{infoLog}");
                }

                for (int i = 0; i < shaders.Length; i++)
                {
                    Shader shader = shaders[i];
                    if (shader.GetCompileStatus())
                    {
                        GL.DetachShader(ID, shaders[i].ID);
                    }
                }
            }


            public bool GetLinkStatus()
            {
                int success = 0;
                GL.GetProgrami(ID, ProgramProperty.LinkStatus, ref success);
                return success == 1;
            }

            public void Use()
            {
                GL.UseProgram(ID);
            }

            public unsafe void Upload(int location, in Matrix4 matrix4, int count = 1, bool transpose = false)
            {
                fixed (float* ptr = &matrix4.Row0.X)
                {
                    GL.ProgramUniformMatrix4fv(ID, location, count, transpose, ptr);
                }
            }
            public unsafe void Upload(string name, in Matrix4 matrix4, int count = 1, bool transpose = false)
            {
                fixed (float* ptr = &matrix4.Row0.X)
                {
                    GL.ProgramUniformMatrix4fv(ID, GetUniformLocation(name), count, transpose, ptr);
                }
            }

            public unsafe void Upload(int location, in Vector4 vector4, int count = 1)
            {
                fixed (float* ptr = &vector4.X)
                {
                    GL.ProgramUniform4fv(ID, location, count, ptr);
                }
            }
            public unsafe void Upload(string name, in Vector4 vector4, int count = 1)
            {
                fixed (float* ptr = &vector4.X)
                {
                    GL.ProgramUniform4fv(ID, GetUniformLocation(name), count, ptr);
                }
            }

            public unsafe void Upload(int location, in Vector3 vector3, int count = 1)
            {
                fixed (float* ptr = &vector3.X)
                {
                    GL.ProgramUniform3fv(ID, location, count, ptr);
                }
            }
            public unsafe void Upload(string name, in Vector3 vector3, int count = 1)
            {
                fixed (float* ptr = &vector3.X)
                {
                    GL.ProgramUniform3fv(ID, GetUniformLocation(name), count, ptr);
                }
            }

            public unsafe void Upload(int location, in Vector2 vector2, int count = 1)
            {
                fixed (float* ptr = &vector2.X)
                {
                    GL.ProgramUniform2fv(ID, location, count, ptr);
                }
            }
            public unsafe void Upload(string name, in Vector2 vector2, int count = 1)
            {
                fixed (float* ptr = &vector2.X)
                {
                    GL.ProgramUniform2fv(ID, GetUniformLocation(name), count, ptr);
                }
            }

            public unsafe void Upload(int location, float x, int count = 1)
            {
                GL.ProgramUniform1fv(ID, location, count, &x);
            }
            public unsafe void Upload(string name, float x, int count = 1)
            {
                GL.ProgramUniform1fv(ID, GetUniformLocation(name), count, &x);
            }

            public unsafe void Upload(int location, int x, int count = 1)
            {
                GL.ProgramUniform1iv(ID, location, count, &x);
            }
            public unsafe void Upload(string name, int x, int count = 1)
            {
                GL.ProgramUniform1iv(ID, GetUniformLocation(name), count, &x);
            }

            public unsafe void Upload(int location, uint x, int count = 1)
            {
                GL.ProgramUniform1uiv(ID, location, count, &x);
            }
            public unsafe void Upload(string name, uint x, int count = 1)
            {
                GL.ProgramUniform1uiv(ID, GetUniformLocation(name), count, &x);
            }

            public void Upload(int location, bool x)
            {
                Upload(location, x ? 1 : 0);
            }
            public void Upload(string name, bool x)
            {
                Upload(name, x ? 1 : 0);
            }

            public int GetUniformLocation(string name)
            {
                return GL.GetUniformLocation(ID, name);
            }

            public void Dispose()
            {
                GL.DeleteProgram(ID);
            }
        }

        public class AbstractShaderProgram : ShaderProgram, IDisposable
        {
            private static readonly List<AbstractShaderProgram> globalInstances = new List<AbstractShaderProgram>();

            public AbstractShader[] Shaders { get; private set; }
            public AbstractShaderProgram(AbstractShader[] shaders)
                : base(shaders)
            {
                Shaders = shaders;
                globalInstances.Add(this);
            }

            public AbstractShaderProgram(AbstractShader first, params AbstractShader[] others)
                : this(others.Concat([first]).ToArray())
            {
            }
            public void Link(AbstractShader first, params AbstractShader[] others)
            {
                AbstractShader[] abstractShaders = others.Concat([first]).ToArray();
                Link(abstractShaders);
            }

            public void Link(AbstractShader[] shaders)
            {
                DisposeShaders();

                base.Link(shaders);
                Shaders = shaders;
            }

            private void DisposeShaders()
            {
                if (Shaders != null)
                {
                    for (int i = 0; i < Shaders.Length; i++)
                    {
                        Shaders[i].Dispose();
                    }
                }
            }

            public new void Dispose()
            {
                DisposeShaders();
                globalInstances.Remove(this);

                base.Dispose();
            }

            // "Singleton"-workarround for "CS0720: 'static class': cannot declare indexers in a static class"
            public class ShaderInsertionsSingleton
            {
                /// <summary>
                /// Maps Shader-required <see cref="Shader.Preprocessor.Keyword.AppInsert"/> key -> the coressponding value filled in by the preprocessing
                /// </summary>
                public IReadOnlyDictionary<string, string> GlobalAppInsertions => globalAppInsertions;

                private readonly Dictionary<string, string> globalAppInsertions = new Dictionary<string, string>();

                public string this[string key]
                {
                    get
                    {
                        if (globalAppInsertions.TryGetValue(key, out string value))
                        {
                            return value;
                        }
                        return null;
                    }

                    set
                    {
                        if (globalAppInsertions.TryGetValue(key, out string prevValue) && prevValue == value)
                        {
                            return;
                        }
                        globalAppInsertions[key] = value;

                        string recompiledShadersNames = string.Empty;
                        for (int i = 0; i < globalInstances.Count; i++)
                        {
                            AbstractShaderProgram shaderProgram = globalInstances[i];

                            bool programIncludesAppInsertionKey = false;
                            for (int j = 0; j < shaderProgram.Shaders.Length; j++)
                            {
                                AbstractShader shader = shaderProgram.Shaders[j];

                                string srcCode = File.ReadAllText(shader.FullShaderPath);
                                AbstractShader.Preprocessor.PreProcess(srcCode, GlobalAppInsertions, out AbstractShader.Preprocessor.PreProcessInfo preprocessInfo);

                                if (preprocessInfo.UsedAppInsertionKeys.Contains(key))
                                {
                                    programIncludesAppInsertionKey = true;
                                    break;
                                }
                            }

                            if (programIncludesAppInsertionKey)
                            {
                                Recompile(shaderProgram);

                                recompiledShadersNames += $"[{string.Join(", ", shaderProgram.Shaders.Select(shader => $"{shader.Name}"))}]";
                            }
                        }

                        if (recompiledShadersNames != string.Empty)
                        {
                            Logger.Log(Logger.LogLevel.Info,
                                   $"{nameof(AbstractShader.Preprocessor.Keyword.AppInclude)} \"{key}\" was assigned new value \"{value}\", " +
                                   $"causing shader recompilation for {recompiledShadersNames}"
                               );
                        }
                    }
                }
            }
            public static readonly ShaderInsertionsSingleton ShaderInsertions = new ShaderInsertionsSingleton();

            public static void RecompileAll()
            {
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < globalInstances.Count; i++)
                {
                    AbstractShaderProgram shaderProgram = globalInstances[i];
                    Recompile(shaderProgram);
                }
                sw.Stop();

                int numShaders = globalInstances.Sum(it => it.Shaders.Length);
                Logger.Log(Logger.LogLevel.Info, $"Parsed and recompiled {numShaders} shaders in {sw.ElapsedMilliseconds} milliseconds");
            }

            public static void Recompile(AbstractShaderProgram shaderProgram)
            {
                AbstractShader[] recompiledShaders = new AbstractShader[shaderProgram.Shaders.Length];
                for (int i = 0; i < recompiledShaders.Length; i++)
                {
                    AbstractShader existingShader = shaderProgram.Shaders[i];
                    recompiledShaders[i] = new AbstractShader(existingShader.ShaderType, existingShader.LocalShaderPath);
                }
                shaderProgram.Link(recompiledShaders);
            }
        }
    }
}