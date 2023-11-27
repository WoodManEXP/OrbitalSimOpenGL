using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// https://github.com/opentk/LearnOpenTK/blob/master/Common/Shader.
    /// https://github.com/dwmkerr/sharpgl/blob/main/source/SharpGL/Core/SharpGL/Shaders/ShaderProgram.cs
    /// </summary>
    internal class Shader
    {
        #region Properties
        public int ShaderHandle { get; set; }

        // Dictionary to hold the uniform locations.
        private readonly Dictionary<string, int> UniformLocations = new Dictionary<string, int>();
        #endregion

        public Shader(String vertexShaderStr, String fragmentShaderStr)
        {

            // Vertex shader
            // https://github.com/opentk/LearnOpenTK/blob/master/Common/Shader.cs

            // GL.CreateShader will create an empty shader (obviously).
            // The ShaderType enum denotes which type of shader will be created.
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderStr); // Now, bind the GLSL source code
            Shader.CompileShader(vertexShader); // And then compile

            // Fragment shader
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderStr);
            Shader.CompileShader(fragmentShader);

            // These two shaders must then be merged into a shader program, which can then be used by OpenGL.
            // To do this, create a program...
            ShaderHandle = GL.CreateProgram();

            // Attach both shaders...
            GL.AttachShader(ShaderHandle, vertexShader);
            GL.AttachShader(ShaderHandle, fragmentShader);

            // And then link them together.
            Shader.LinkProgram(ShaderHandle);

            // When the shader program is linked, it no longer needs the individual shaders attached to it; the compiled
            // code is copied into the shader program. Detach them, and then delete them.
            GL.DetachShader(ShaderHandle, vertexShader);
            GL.DetachShader(ShaderHandle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            // The shader is now ready to go, but first, we're going to cache all the shader uniform locations.
            // Querying this from the shader is very slow, so we do it once on initialization and reuse those values
            // later.

            // First, we have to get the number of active uniforms in the shader.
            GL.GetProgram(ShaderHandle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms); 

            // Loop over all the uniforms,
            for (var i = 0; i < numberOfUniforms; i++)
            {
                // get the name of this uniform,
                var key = GL.GetActiveUniform(ShaderHandle, i, out _, out _);

                // get the location,
                var location = GL.GetUniformLocation(ShaderHandle, key);

                // and then add it to the dictionary.
                UniformLocations.Add(key, location);
            }
        }

        public static void CompileShader(int shader)
        {
            // Try to compile the shader
            GL.CompileShader(shader);

            // Check for compilation errors
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                // We can use `GL.GetShaderInfoLog(shader)` to get information about the error.
                var infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        public static void LinkProgram(int program)
        {
            // We link the program
            GL.LinkProgram(program);

            // Check for linking errors
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                // We can use `GL.GetProgramInfoLog(program)` to get information about the error.
                throw new Exception($"Error occurred whilst linking Program({program})");
            }
        }

        // A wrapper function that enables the shader program.
        public void Use()
        {
            GL.UseProgram(ShaderHandle);
        }

        public void Unuse()
        {
            GL.UseProgram(0);
        }

        // Uniform setters
        // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
        // You use VBOs for vertex-related data, and uniforms for almost everything else.

        // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
        //     1. Bind the program you want to set the uniform on
        //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
        //     3. Use the appropriate GL.Uniform* function to set the uniform.

        /// <summary>
        /// Set a uniform int on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetInt(String name, int data)
        {
            Use();
            GL.Uniform1(UniformLocations[name], data);
        }

        /// <summary>
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetFloat(String name, float data)
        {
            Use();
            GL.Uniform1(UniformLocations[name], data);
        }

        /// <summary>
        /// Set a uniform Matrix4 on this shader
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        /// <remarks>
        ///   <para>
        ///   The matrix is transposed before being sent to the shader.
        ///   </para>
        /// </remarks>
        public void SetMatrix4(String name, OpenTK.Mathematics.Matrix4 data)
        {
            Use();
            GL.UniformMatrix4(UniformLocations[name], true, ref data);
        }

        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetVector3(String name, Vector3 data)
        {
            Use();
            GL.Uniform3(UniformLocations[name], data);
        }

        public void SetVector4(String name, Vector4 data)
        {
            Use();
            GL.Uniform4(UniformLocations[name], data);
        }

        public void SetColor4(String name, Color4 data)
        {
            Use();
            GL.Uniform4(UniformLocations[name], data);
        }

    }
}
