using CommandLine;
using MSTS_Converter.Formats;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TrainSim.Formats;
using static Program;

public static class VectorExtesions
{
    public static void Write(this Vector3 vec, BinaryWriter bw)
    {
        bw.Write(vec.X);
        bw.Write(vec.Y);
        bw.Write(vec.Z);
    }

    public static Vector3 ReadVector3(this BinaryReader br)
    {
        return new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
    }

    public static void Write(this Vector2 vec, BinaryWriter bw)
    {
        bw.Write(vec.X);
        bw.Write(vec.Y);
    }

    public static Vector2 ReadVector2(this BinaryReader br)
    {
        return new Vector2(br.ReadSingle(), br.ReadSingle());
    }

    public static void Write(this Matrix4 matrix,  BinaryWriter bw)
    {
        bw.Write(matrix.M11);
        bw.Write(matrix.M12);
        bw.Write(matrix.M13);
        bw.Write(matrix.M14);

        bw.Write(matrix.M21);
        bw.Write(matrix.M22);
        bw.Write(matrix.M23);
        bw.Write(matrix.M24);

        bw.Write(matrix.M31);
        bw.Write(matrix.M32);
        bw.Write(matrix.M33);
        bw.Write(matrix.M34);

        bw.Write(matrix.M41);
        bw.Write(matrix.M42);
        bw.Write(matrix.M43);
        bw.Write(matrix.M44);
    }

    public static Matrix4 ReadMatrix4(this BinaryReader br)
    {
        return new Matrix4(
            br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
            br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
            br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
            br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()
            );
    }
}

struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;

    public void Write(BinaryWriter bw)
    {
        Position.Write(bw);
        Normal.Write(bw);
        TexCoord.Write(bw);
    }

    public static Vertex Read(BinaryReader br)
    {
        var vertex = new Vertex();
    
        vertex.Position = br.ReadVector3();
        vertex.Normal = br.ReadVector3();
        vertex.TexCoord = br.ReadVector2();

        return vertex;
    }
}

class TSModelPrimitive
{
    public int iHierarchy;
    public ushort[] Triangles;
    public string Texture;
    public string MaterialName;
    public int Options;

    public void Write(BinaryWriter bw)
    {
        bw.Write(iHierarchy);
        bw.Write(Triangles.Length);
        foreach (var tri in Triangles)
            bw.Write(tri);
        bw.WriteString(Texture);
        bw.WriteString(MaterialName);
        bw.Write(Options);
    }

    public static TSModelPrimitive Read(BinaryReader br)
    {
        var primitive = new TSModelPrimitive();

        primitive.iHierarchy = br.ReadInt32();
        primitive.Triangles = new ushort[br.ReadInt32()];

        for (var iTrig = 0; iTrig < primitive.Triangles.Length; iTrig++)
            primitive.Triangles[iTrig] = br.ReadUInt16();

        primitive.Texture = br.ReadStringLen();
        primitive.MaterialName = br.ReadStringLen();
        primitive.Options = br.ReadInt32();

        return primitive;
    }
}

class TSModelSubObject
{
    public Vertex[] Vertices;
    public TSModelPrimitive[] Primitives;

    public void Write(BinaryWriter bw)
    {
        bw.Write(Vertices.Length);
        foreach (var vertex in Vertices)
            vertex.Write(bw);

        bw.Write(Primitives.Length);
        foreach (var primitive in Primitives)
            primitive.Write(bw);
    }

    public static TSModelSubObject Read(BinaryReader br)
    {
        var subObject = new TSModelSubObject();

        subObject.Vertices = new Vertex[br.ReadInt32()];

        for (var iVert = 0; iVert < subObject.Vertices.Length; iVert++)
            subObject.Vertices[iVert] = Vertex.Read(br);

        subObject.Primitives = new TSModelPrimitive[br.ReadInt32()];

        for (var iPrim = 0; iPrim < subObject.Primitives.Length; iPrim++)
            subObject.Primitives[iPrim] = TSModelPrimitive.Read(br);

        return subObject;
    }
}

class TSModelLod
{
    public float Distance;
    public TSModelSubObject[] SubObjects;
    public int[] Hierarchy;

    public void Write(BinaryWriter bw)
    {
        bw.Write(Distance);

        Console.WriteLine("SubObjects.Length: " + SubObjects.Length);

        bw.Write(SubObjects.Length);
        foreach (var subObject in SubObjects)
            subObject.Write(bw);

        bw.Write(Hierarchy.Length);
        foreach (var hierarchy in Hierarchy)
            bw.Write(hierarchy);
    }

    public static TSModelLod Read(BinaryReader br)
    {
        var lod = new TSModelLod();

        lod.Distance = br.ReadSingle();

        lod.SubObjects = new TSModelSubObject[br.ReadInt32()];
        for (var iSubObj = 0; iSubObj < lod.SubObjects.Length; iSubObj++)
            lod.SubObjects[iSubObj] = TSModelSubObject.Read(br);

        lod.Hierarchy = new int[br.ReadInt32()];
        for (var iHier = 0; iHier < lod.Hierarchy.Length; iHier++)
            lod.Hierarchy[iHier] = br.ReadInt32();

        return lod;
    }
}

class TSModel
{
    public TSModelLod[] Lods;
    public Matrix4[] Matrices;
    public string[] MatricesName;

    public void Write(BinaryWriter bw)
    {
        bw.Write(Matrices.Length);
        foreach (var matrix in Matrices)
            matrix.Write(bw);

        foreach (var matrixName in MatricesName)
            bw.WriteString(matrixName);

        bw.Write(Lods.Length);
        foreach (var lod in Lods)
            lod.Write(bw);
    }

    public static TSModel Read(BinaryReader br)
    {
        var model = new TSModel();

        var numMatrix = br.ReadInt32();

        model.Matrices = new Matrix4[numMatrix];
        model.MatricesName = new string[numMatrix];

        for (int iMatrix = 0; iMatrix < numMatrix; iMatrix++)
            model.Matrices[iMatrix] = br.ReadMatrix4();

        for (int iMatrix = 0; iMatrix < numMatrix; iMatrix++)
            model.MatricesName[iMatrix] = br.ReadStringLen();

        model.Lods = new TSModelLod[br.ReadInt32()];
        for (int iLod = 0; iLod < model.Lods.Length; iLod++)
            model.Lods[iLod] = TSModelLod.Read(br);

        return model;
    }
}

[Flags]
public enum SceneryMaterialOptions
{
    None = 0,
    // Diffuse
    Diffuse = 0x1,
    // Alpha test
    AlphaTest = 0x2,
    // Blending
    AlphaBlendingNone = 0x0,
    AlphaBlendingBlend = 0x4,
    AlphaBlendingAdd = 0x8,
    AlphaBlendingMask = 0xC,
    // Shader
    ShaderImage = 0x00,
    ShaderDarkShade = 0x10,
    ShaderHalfBright = 0x20,
    ShaderFullBright = 0x30,
    ShaderVegetation = 0x40,
    ShaderMask = 0x70,
    // Lighting
    Specular0 = 0x000,
    Specular25 = 0x080,
    Specular750 = 0x100,
    SpecularMask = 0x180,
    // Texture address mode
    TextureAddressModeWrap = 0x000,
    TextureAddressModeMirror = 0x200,
    TextureAddressModeClamp = 0x400,
    TextureAddressModeBorder = 0x600,
    TextureAddressModeMask = 0x600,
    // Night texture
    NightTexture = 0x800,
}

public enum FindFileFrom
{
    FromGlobal,
    FromRoute,
    FromTrain,
}

public enum FileType
{
    Texture,
    Shape,
    Sound,
    Text,
}

public static class Extensions
{

    public static readonly Dictionary<string, SceneryMaterialOptions> ShaderNames = new()
    {
        { "Tex", SceneryMaterialOptions.None },
        { "TexDiff", SceneryMaterialOptions.Diffuse },
        { "BlendATex", SceneryMaterialOptions.AlphaBlendingBlend },
        { "BlendATexDiff", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.Diffuse },
        { "AddATex", SceneryMaterialOptions.AlphaBlendingAdd },
        { "AddATexDiff", SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.Diffuse },
    };

    public static readonly SceneryMaterialOptions[] VertexLightModeMap = [
        SceneryMaterialOptions.ShaderDarkShade,
        SceneryMaterialOptions.ShaderHalfBright,
        SceneryMaterialOptions.ShaderVegetation, // Not certain this is right.
        SceneryMaterialOptions.ShaderVegetation,
        SceneryMaterialOptions.ShaderFullBright,
        SceneryMaterialOptions.None | SceneryMaterialOptions.Specular750,
        SceneryMaterialOptions.None | SceneryMaterialOptions.Specular25,
        SceneryMaterialOptions.None | SceneryMaterialOptions.None,
    ];

    public static Matrix4 MatrixFromMSTS(matrix m)
    {
        Matrix4 TKMatrix = new Matrix4(
             m.AX,  m.AY, m.AZ, 0,
             m.BX,  m.BY, m.BZ, 0,
             m.CX,  m.CY, m.CZ, 0,
             m.DX,  m.DY, m.DZ, 1);

        return TKMatrix;
    }

    public static void WriteString(this BinaryWriter bw, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        bw.Write((ushort)bytes.Length);
        bw.Write(bytes);
    }

    public static string ReadStringLen(this BinaryReader br)
    {
        return Encoding.UTF8.GetString(br.ReadBytes(br.ReadUInt16()));
    }

    public static string[] GetTexturesFromShape(shape shape)
    {
        return shape.images.ToArray();
    }

    public static FileType GetFileType(string filename)
    {
        var ext = Path.GetExtension(filename);

        switch (ext)
        {
            case ".s":
                return FileType.Shape;
            case ".ace":
                return FileType.Texture;
            case ".sms":
                return FileType.Sound;
            default:
                return FileType.Text;
        }
    }

    public static string? GetFilepath(string filename, string rootpath, string mstspath, FindFileFrom from)
    {
        var mstsglobal = Path.Combine(mstspath ?? "", "GLOBAL");

        var filetype = GetFileType(filename);

        string? resultpath = null;

        if (from == FindFileFrom.FromRoute)
        {
            if (filetype == FileType.Shape)
                resultpath = Path.Combine(rootpath, "SHAPES", filename);

            if (filetype == FileType.Texture)
                resultpath = Path.Combine(rootpath, "TEXTURES", filename);
        }

        if (from == FindFileFrom.FromGlobal)
        {
            if (filetype == FileType.Shape)
                resultpath = Path.Combine(mstsglobal, "SHAPES", filename);

            if (filetype == FileType.Texture)
                resultpath = Path.Combine(mstsglobal, "TEXTURES", filename);
        }

        if (from == FindFileFrom.FromTrain)
        {
            if (filetype == FileType.Shape || filetype == FileType.Texture)
                resultpath = Path.Combine(rootpath, filename);
        }

        if (resultpath != null)
            return Path.GetFullPath(resultpath);

        return null;
    }

    public static string? FindFile(string filename, string rootpath, string mstspath, FindFileFrom from)
    {
        string? path = null;

        if (from == FindFileFrom.FromRoute)
        {
            path = GetFilepath(filename, rootpath, mstspath, FindFileFrom.FromRoute);

            if (!File.Exists(path))
                path = GetFilepath(filename, rootpath, mstspath, FindFileFrom.FromGlobal);
        }

        if (from == FindFileFrom.FromTrain)
        {
            path = GetFilepath(filename, rootpath, mstspath, FindFileFrom.FromTrain);
        }

        return path;
    }
}

public class Options
{
    [Option('i', "input", Required = true, HelpText = "Input MSTS File or Directory")]
    public string Input { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output file (.ts_*) or Directory")]
    public string? Output { get; set; }

    [Option('m', "mstspath", Required = false, HelpText = "MSTS Root Folder")]
    public string? MSTSPath { get; set; }

    [Option('r', "route", Default = false, HelpText = "Convert Route")]
    public bool Route { get; set; }

    [Option('t', "texonly", Default = false, HelpText = "Texture only convert")]
    public bool TextureOnly { get; set; }

    [Option('l', "info", Default = false, HelpText = "Show info for file")]
    public bool Info { get; set; }

    [Option('z', "recursive", Default = false, HelpText = "Recursive convert for directory")]
    public bool Recursive { get; set; }

    public override string ToString()
    {
        return $"Options:\n\tInput: {Input}\n\tOutput: {Output ?? "null"}\n\tRecursive: {Recursive}";
    }
}

public static class TextureManager
{
    public static void ConvertTexture(string input, string? output, Options options)
    {
        //output = Path.ChangeExtension(output, "ts_tex");

        if (!File.Exists(input))
        {
            Console.WriteLine("Texture " + input + " not found!");
            return;
        }

        if (output == null) output = Path.ChangeExtension(input, ".ts_tex");

        Console.WriteLine("Texture: ");
        Console.WriteLine("\t" + input);
        Console.WriteLine("\t" + output);

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        using (var stream = File.Open(output, FileMode.Create))
        {
            using (var writer = new BinaryWriter(stream))
            {
                var data = AceFile.LoadTextureDataFromFile(input);

                writer.Write('T');
                writer.Write('S');
                writer.Write('T');
                writer.Write('E');
                writer.Write('X');
                writer.Write('T');
                writer.WriteTexture(data);
            }
        }
        stopwatch.Stop();

        Console.WriteLine("\t" + $"Texture converted in {stopwatch.Elapsed.TotalSeconds} sec\n");
    }

    public static void ShowInfoTexture(string input, Options options)
    {
        var data = AceFile.LoadTextureDataFromFile(input);

        Console.WriteLine("");

        Console.WriteLine($"Texture: {input}\n");

        Console.WriteLine($"Width: {data.Width}");
        Console.WriteLine($"Height: {data.Height}");
        Console.WriteLine($"Format: {data.Format}");
        Console.WriteLine($"Data length: {data.Data.Length} bytes");
    }

    public static void ShowInfoTSTexture(string input, Options options)
    {
        using (var stream = File.Open(input, FileMode.Open))
        {
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadBytes(6); //header skip

                var data = reader.ReadTexture();

                Console.WriteLine("");

                Console.WriteLine($"Texture: {input}\n");

                Console.WriteLine($"Width: {data.Width}");
                Console.WriteLine($"Height: {data.Height}");
                Console.WriteLine($"Format: {data.Format}");
                Console.WriteLine($"Data length: {data.Data.Length} bytes");
            }
        }
    }

    public static void WriteTexture(this BinaryWriter bw, AceFileData ace)
    {
        bw.Write(ace.Width);
        bw.Write(ace.Height);
        bw.Write((int)ace.Format);
        bw.Write(ace.Data.Length);
        bw.Write(ace.Data);
    }
    public static AceFileData ReadTexture(this BinaryReader br)
    {
        var data = new AceFileData();

        data.Width = br.ReadInt32();
        data.Height = br.ReadInt32();
        data.Format = (TextureFormat)br.ReadInt32();
        data.Data = br.ReadBytes(br.ReadInt32());

        return data;
    }
}

public static class ShapeManager
{
    public static void ConvertShape(string input, string? output, Options options)
    {
        if (output == null) output = input;

        output = Path.ChangeExtension(output, ".ts_model");

        Console.WriteLine("Shape: ");
        Console.WriteLine("\t" + input);
        Console.WriteLine("\t" + output);

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        using (var stream = File.Open(output, FileMode.Create))
        {
            using (var writer = new BinaryWriter(stream))
            {
                var sFile = new SFile(input, false);

                writer.Write('T');
                writer.Write('S');
                writer.Write('M');
                writer.Write('O');
                writer.Write('D');
                writer.Write('L');
                writer.WriteShape(sFile.shape);
            }
        }
        stopwatch.Stop();

        Console.WriteLine("\t" + $"Shape converted in {stopwatch.Elapsed.TotalSeconds} sec\n");
    }

    public static void ConvertShapeTextures(string input_shape, string? output_texture_folder, string? root_path, string? msts_path, FindFileFrom from, Options options)
    {
        if (output_texture_folder == null)
        {
            var dir = Path.GetDirectoryName(input_shape);

            output_texture_folder = Path.Combine(dir, Path.GetFileNameWithoutExtension(input_shape)) + "_converted_textures";
            //output = Path.Combine(dir, Path.GetFileNameWithoutExtension(input));

            Directory.CreateDirectory(output_texture_folder);
        }

        bool disable_double_dot = false;

        if (root_path == null)
        {
            from = FindFileFrom.FromTrain;

            root_path = Path.GetDirectoryName(input_shape);
            disable_double_dot = true;
        }

        var shape = new SFile(input_shape, false).shape;

        foreach (var image in shape.images)
        {
            var output_path = Path.Combine(output_texture_folder, image);
            var input_path = Extensions.FindFile(image, root_path, msts_path, from);

            output_path = Path.ChangeExtension(output_path, "ts_tex");

            Directory.CreateDirectory(Path.GetDirectoryName(output_path));

            if (disable_double_dot)
                output_path = output_path.Replace("../", "");

            //File.WriteAllText(output_path, "123");
            TextureManager.ConvertTexture(input_path, output_path, options);

            Console.WriteLine(output_path);
        }
    }

    public static void ShowInfoShape(string input, Options options)
    {
        var shape = new SFile(input, false).shape;

        var point_count = shape.points.Count;
        var normals_count = shape.normals.Count;
        var images_count = shape.images.Count;
        var matrices_count = shape.matrices.Count;

        Console.WriteLine("");

        Console.WriteLine($"Shape: {input}\n");

        Console.WriteLine($"Points count: {point_count}");
        Console.WriteLine($"Normals count: {normals_count}");
        Console.WriteLine($"Images count: {images_count}");
        Console.WriteLine($"Matrices count: {matrices_count}");

        Console.WriteLine("\nImages:");
        foreach (var image in shape.images)
        {
            Console.WriteLine($"\tImage - {image}");
        }

        Console.WriteLine("\nMatrices:");
        foreach (var matrix in shape.matrices)
        {
            Console.WriteLine($"\tMatrix - {matrix.Name}");
        }
    }

    public static void ShowInfoTSModel(string input, Options options)
    {
        using (var stream = File.Open(input, FileMode.Open))
        {
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadBytes(6); //header skip

                var model = TSModel.Read(reader);

                Console.WriteLine("");

                Console.WriteLine($"Model: {input}\n");

                Console.WriteLine("Lods:");

                foreach (var lod in model.Lods)
                {
                    Console.WriteLine($"\tLod ({lod.Distance})");

                    foreach (var subobject in lod.SubObjects)
                    {
                        Console.WriteLine($"\t\tSubobject:");

                        foreach (var primitive in subobject.Primitives)
                        {
                            Console.WriteLine($"\t\t\tPrimitive ({model.MatricesName[primitive.iHierarchy]}):");

                            Console.WriteLine($"\t\t\t\tTexture: {primitive.Texture}");
                            Console.WriteLine($"\t\t\t\tMaterial Name: {primitive.MaterialName}");
                        }
                    }
                }
            }
        }
    }

    public static void WriteShape(this BinaryWriter bw, shape shape)
    {
        var model = new TSModel();

        var path = bw.BaseStream.Cast<FileStream>().Name;

        var shapeid = new Random().Next(9999);

        model.Matrices = new Matrix4[shape.matrices.Count];
        model.MatricesName = new string[shape.matrices.Count];

        var iMatrix = 0;
        foreach (var matrix in shape.matrices)
        {
            model.Matrices[iMatrix] = Extensions.MatrixFromMSTS(matrix);
            model.MatricesName[iMatrix] = matrix.Name;
            iMatrix++;
        }

        foreach (var lod_control in shape.lod_controls)
        {
            model.Lods = new TSModelLod[lod_control.distance_levels.Count];

            var iLod = 0;
            foreach (var distance_level in lod_control.distance_levels)
            {
                var dist = distance_level.distance_level_header.dlevel_selection;

                var shapeLod = new TSModelLod { Distance = dist, Hierarchy = distance_level.distance_level_header.hierarchy };

                shapeLod.SubObjects = new TSModelSubObject[distance_level.sub_objects.Count];

                var iSubObject = 0;

                foreach (var sub_object in distance_level.sub_objects)
                {
                    var shapeSubObject = new TSModelSubObject { };

                    shapeSubObject.Vertices = new Vertex[sub_object.vertices.Count];
                    shapeSubObject.Primitives = new TSModelPrimitive[sub_object.primitives.Count];

                    var iVert = 0;
                    foreach (var vertex in sub_object.vertices)
                    {
                        var point = shape.points[vertex.ipoint];
                        var normal = shape.normals[vertex.inormal];
                        var uv = shape.uv_points[vertex.vertex_uvs[0]];

                        var vert = new Vertex
                        {
                            Position = new Vector3(point.X, point.Y, point.Z),
                            Normal = new Vector3(normal.X, normal.Y, normal.Z),
                            TexCoord = new Vector2(uv.U, uv.V)
                        };

                        shapeSubObject.Vertices[iVert] = vert;

                        iVert++;
                    }

                    var iPrim = 0;

                    foreach (var primitive in sub_object.primitives)
                    {
                        var prim_state = shape.prim_states[primitive.prim_state_idx];
                        var vtx_state = shape.vtx_states[prim_state.ivtx_state];
                        var light_model_cfg = shape.light_model_cfgs[vtx_state.LightCfgIdx];

                        var imatrix = vtx_state.imatrix;

                        var shapePrimitive = new TSModelPrimitive { iHierarchy = imatrix };

                        shapePrimitive.Triangles = new ushort[primitive.indexed_trilist.vertex_idxs.Count * 3];

                        var iTrig = 0;
                        foreach (var vertex_idx in primitive.indexed_trilist.vertex_idxs)
                        {
                            shapePrimitive.Triangles[iTrig * 3 + 0] = (ushort)vertex_idx.a;
                            shapePrimitive.Triangles[iTrig * 3 + 1] = (ushort)vertex_idx.b;
                            shapePrimitive.Triangles[iTrig * 3 + 2] = (ushort)vertex_idx.c;
                            iTrig++;
                        }

                        var options = SceneryMaterialOptions.None;

                        if (prim_state.alphatestmode == 1)
                            options |= SceneryMaterialOptions.AlphaTest;


                        if (Extensions.ShaderNames.ContainsKey(shape.shader_names[prim_state.ishader]))
                            options |= Extensions.ShaderNames[shape.shader_names[prim_state.ishader]];


                        if (12 + vtx_state.LightMatIdx >= 0 && 12 + vtx_state.LightMatIdx < Extensions.VertexLightModeMap.Length)
                            options |= Extensions.VertexLightModeMap[12 + vtx_state.LightMatIdx];

                        var image = shape.images[shape.textures[prim_state.tex_idxs[0]].iImage];

                        image = Path.ChangeExtension(image, "ts_tex");

                        shapePrimitive.Texture = image;
                        shapePrimitive.MaterialName =
                            Path.GetFileNameWithoutExtension(path) +
                            Path.GetFileNameWithoutExtension(image) +
                            ((int)options) +
                            shapeid;
                        shapePrimitive.Options = (int)options;

                        shapeSubObject.Primitives[iPrim] = shapePrimitive;
                        iPrim++;
                    }

                    shapeLod.SubObjects[iSubObject] = shapeSubObject;

                    iSubObject++;
                }

                model.Lods[iLod] = shapeLod;

                iLod++;
            }
        }

        model.Write(bw);
    }
}

public static class WorldManager
{
    public static void ConvertWorld(string input, string? output, Options options)
    {
        if (output == null)
            output = Path.ChangeExtension(input, ".ts_world");

        var file_world = new WFile(input);

        var world = new JObject();

        var objects = new JArray();

        world["TileX"] = file_world.TileX;
        world["TileZ"] = file_world.TileZ;
        world["Objects"] = objects;

        foreach (var obj in file_world.Tr_Worldfile)
        {
            var obj_json = new JObject();
            objects.Add(obj_json);

            obj_json["Type"] = obj.GetType().Name;
            obj_json["UID"] = obj.UID;

            obj_json["Position"] = new JArray
            {
                obj.Position.X,
                obj.Position.Y,
                obj.Position.Z
            };

            obj_json["Direction"] = new JArray
            {
                obj.QDirection.A,
                obj.QDirection.B,
                obj.QDirection.C,
                obj.QDirection.D,
            };

            obj_json["Filename"] = Path.ChangeExtension(obj.FileName, ".ts_model");
        }

        File.WriteAllText(output, world.ToString(Newtonsoft.Json.Formatting.Indented));
    }
}

public static class RouteManager
{
    public static void ConvertRoute(string folder, string mstspath, Options options)
    {
        var output = folder + "_converted";

        Console.WriteLine($"Route Folder {folder}");
        Console.WriteLine($"Output Folder {output}");
        Console.WriteLine($"MSTS Folder {mstspath}");

        Directory.CreateDirectory(output);
        Directory.CreateDirectory(Path.Combine(output, "world"));
        Directory.CreateDirectory(Path.Combine(output, "models"));
        Directory.CreateDirectory(Path.Combine(output, "textures"));

        var wfiles = Directory.GetFiles(Path.Combine(folder, "WORLD"));

        var alreadyConverted = new Dictionary<string, bool>();

        foreach (var wfile in wfiles)
        {
            if (!wfile.EndsWith(".w"))
                continue;

            var world = new WFile(Path.Combine(folder, wfile));
            
            foreach (var obj in world.Tr_Worldfile)
            {
                if (obj is StaticObj)
                {
                    if (!alreadyConverted.TryGetValue(obj.FileName, out bool _))
                    {
                        var filepath = Extensions.FindFile(obj.FileName, folder, mstspath, FindFileFrom.FromRoute);

                        var modelout = Path.Combine(output, "models", obj.FileName);

                        ShapeManager.ConvertShape(filepath, modelout, options);

                        var textureout = Path.Combine(output, "textures");

                        ShapeManager.ConvertShapeTextures(filepath, textureout, folder, mstspath, FindFileFrom.FromRoute, options);

                        alreadyConverted[obj.FileName] = true;
                    }
                }
            }
        }
    }
}

public static class Program
{
    static void ConvertFile(string input, string? output, Options options)
    {
        var ext = Path.GetExtension(input);
        switch (ext)
        {
            case ".s":
                if (options.TextureOnly)
                    ShapeManager.ConvertShapeTextures(input, output, null, null, FindFileFrom.FromTrain, options);
                else
                    ShapeManager.ConvertShape(input, output, options);

                break;
            case ".ace":
                TextureManager.ConvertTexture(input, output, options);
                break;
            case ".w":
                WorldManager.ConvertWorld(input, output, options);
                break;
            default:
                Console.WriteLine("Unsupported file type!");
                break;
        }
    }

    static void ConvertFolder(string input, string? output, bool recursive, Options options)
    {
        input = Path.GetFullPath(input);

        var files = Directory.GetFiles(input);

        var directoryName = Path.GetFileNameWithoutExtension(input);

        if (output == null)
            output = Path.Combine(Path.GetDirectoryName(input), directoryName + "_converted");
        else
            output = Path.GetFullPath(output);

        Directory.CreateDirectory(output);

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        foreach (var file in files)
        {
            ConvertFile(Path.Combine(input, file), Path.Combine(output, Path.GetFileName(file)), options);
        }
        stopwatch.Stop();

        Console.Write("\n----------\n");
        Console.WriteLine($"Folder converted in {stopwatch.Elapsed.TotalSeconds} sec");
    }

    static void ShowInfo(string input, Options options)
    {
        var ext = Path.GetExtension(input);

        switch (ext)
        {
            case ".s":
                ShapeManager.ShowInfoShape(input, options);
                break;
            case ".ts_model":
                ShapeManager.ShowInfoTSModel(input, options);
                break;
            case ".ace":
                TextureManager.ShowInfoTexture(input, options);
                break;
            case ".ts_mat":
                TextureManager.ShowInfoTSTexture(input, options);
                break;
            default:
                break;
        }
    }

    static void Main(string[] args)
    {
        var value = Parser.Default.ParseArguments<Options>(args).Value;

        if (value == null) return;

        var attr = File.GetAttributes(value.Input);

        //Console.WriteLine("Press key to continue");
        //Console.ReadKey();

        if (value.Route)
        {
            var mstspath = value.MSTSPath;

            if (!Directory.Exists(mstspath))
                mstspath = Path.GetDirectoryName(Path.GetDirectoryName(value.Input));

            RouteManager.ConvertRoute(value.Input, mstspath, value);

            return;
        }

        if (value.Info)
        {
            if (attr.HasFlag(FileAttributes.Directory))
                Console.WriteLine("info only for file");
            else
                ShowInfo(value.Input, value);
        }
        else
        {
            if (attr.HasFlag(FileAttributes.Directory))
                ConvertFolder(value.Input, value.Output == null ? value.Input : value.Output, value.Recursive, value);
            else
                ConvertFile(value.Input, value.Output, value);
        }

        /*
        var convert = true;
        var type = ConvertType.Texture;

        if (convert)
        {
            if (type == ConvertType.Texture)
            {

                using (var stream = File.Open("converted_texture.ts_tex", FileMode.Create))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        //var path = "M:/Games/gorails/ROUTES/TestRoute_RTS/load.ace";
                        var path = @"M:\Games\gorails\UTILS\AceIt\test_one_alpha.ace";
                        var data = AceFile.LoadTextureDataFromFile(path);

                        Console.WriteLine("{0}x{1}", data.Width, data.Height);
                        Console.WriteLine("Format: {0}", data.Format.ToString());

                        writer.WriteTexture(data);
                    }
                }
            }
        }
        else
        {

        }
        */
        /*
        var convert = true;

        if (convert)
        {
            using (var stream = File.Open("test.bin", FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var path = @"M:\Games\gorails\TRAINS\TRAINSET\mdd_2ES4k-123\mdd_2es4ka.s";
                    var sFile = new SFile(path, false);

                    writer.WriteShape(sFile.shape);
                }
            }
        }
        else
        {
            using (var stream = File.Open("test.bin", FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    //Console.WriteLine(GC.GetTotalMemory(false) / 1024 / 1024 + " mb");
                    Thread.Sleep(5000);
                    //Console.WriteLine("Start");
                    //var stopwatch = new Stopwatch();
                    //stopwatch.Start();
                    {
                        var model = TestModel.Read(reader);

                        foreach (var item in model.MatricesName)
                            Console.WriteLine(item);
                    }
                    //stopwatch.Stop();
                    //Console.WriteLine($"Time: {stopwatch.Elapsed.TotalMilliseconds} ms");
                    Thread.Sleep(5000);
                    //Console.WriteLine("Collect");
                    GC.Collect();
                    Thread.Sleep(5000);
                    //Console.WriteLine(GC.GetTotalMemory(false) / 1024 / 1024 + " mb");
                    //Console.WriteLine("Press key");
                    //Console.ReadKey();
                }
            }
        }
        */

        /*
        using (var stream = File.Open("test.bin", FileMode.Create))
        {
            using (var writer = new BinaryWriter(stream))
            {
                Thread.Sleep(5000);
                {
                    var path = @"M:\Games\gorails\TRAINS\TRAINSET\mdd_2ES4k-123\mdd_2es4ka.s";
                    var sFile = new SFile(path, false);
                }

                Thread.Sleep(5000);

                GC.Collect();

                Thread.Sleep(5000);

                Console.Write("Press key");
                Console.ReadKey();

                /*
                var model = new TestModel();

                model.Matrices = new Matrix4[sFile.shape.matrices.Count];
                model.MatricesName = new string[sFile.shape.matrices.Count];

                var iMatrix = 0;
                foreach (var matrix in sFile.shape.matrices)
                {
                    model.Matrices[iMatrix] = TKMatrixFromMSTS(matrix);
                    model.MatricesName[iMatrix] = matrix.Name;
                    iMatrix++;
                }

                foreach (var lod_control in sFile.shape.lod_controls)
                {
                    model.Lods = new TestModelLod[lod_control.distance_levels.Count];

                    var iLod = 0;
                    foreach (var distance_level in lod_control.distance_levels)
                    {
                        var dist = distance_level.distance_level_header.dlevel_selection;
                    
                        var shapeLod = new TestModelLod { Distance = dist, Hierarchy = distance_level.distance_level_header.hierarchy };

                        shapeLod.SubObjects = new TestModelSubObject[distance_level.sub_objects.Count];

                        var iSubObject = 0;

                        foreach (var sub_object in distance_level.sub_objects)
                        {
                            var shapeSubObject = new TestModelSubObject { };

                            shapeSubObject.Vertices = new Vertex[sub_object.vertices.Count];
                            shapeSubObject.Primitives = new TestModelPrimitive[sub_object.primitives.Count];

                            var iVert = 0;
                            foreach (var vertex in sub_object.vertices)
                            {
                                var point = sFile.shape.points[vertex.ipoint];
                                var normal = sFile.shape.normals[vertex.inormal];
                                var uv = sFile.shape.uv_points[vertex.vertex_uvs[0]];

                                var vert = new Vertex
                                {
                                    Position = new Vector3(point.X, point.Y, point.Z),
                                    Normal = new Vector3(normal.X, normal.Y, normal.Z),
                                    TexCoord = new Vector2(uv.U, uv.V)
                                };

                                shapeSubObject.Vertices[iVert] = vert;

                                iVert++;
                            }

                            var iPrim = 0;

                            foreach (var primitive in sub_object.primitives)
                            {
                                var prim_state = sFile.shape.prim_states[primitive.prim_state_idx];
                                var vtx_state = sFile.shape.vtx_states[prim_state.ivtx_state];
                                var light_model_cfg = sFile.shape.light_model_cfgs[vtx_state.LightCfgIdx];

                                var imatrix = vtx_state.imatrix;

                                var shapePrimitive = new TestModelPrimitive { iHierarchy = imatrix };

                                shapePrimitive.Triangles = new ushort[primitive.indexed_trilist.vertex_idxs.Count * 3];

                                var iTrig = 0;
                                foreach (var vertex_idx in primitive.indexed_trilist.vertex_idxs)
                                {
                                    shapePrimitive.Triangles[iTrig * 3 + 0] = (ushort)vertex_idx.a;
                                    shapePrimitive.Triangles[iTrig * 3 + 1] = (ushort)vertex_idx.b;
                                    shapePrimitive.Triangles[iTrig * 3 + 2] = (ushort)vertex_idx.c;
                                    iTrig++;
                                }

                                var options = SceneryMaterialOptions.None;

                                if (prim_state.alphatestmode == 1)
                                    options |= SceneryMaterialOptions.AlphaTest;


                                if (ShaderNames.ContainsKey(sFile.shape.shader_names[prim_state.ishader]))
                                    options |= ShaderNames[sFile.shape.shader_names[prim_state.ishader]];


                                if (12 + vtx_state.LightMatIdx >= 0 && 12 + vtx_state.LightMatIdx < VertexLightModeMap.Length)
                                    options |= VertexLightModeMap[12 + vtx_state.LightMatIdx];

                                var image = sFile.shape.images[sFile.shape.textures[prim_state.tex_idxs[0]].iImage];

                                shapePrimitive.Texture = image;

                                shapeSubObject.Primitives[iPrim] = shapePrimitive;
                                iPrim++;
                            }

                            shapeLod.SubObjects[iSubObject] = shapeSubObject;

                            iSubObject++;
                        }

                        model.Lods[iLod] = shapeLod;

                        iLod++;
                    }
                }

                model.Write(writer);
                */
                /*
                TestModel model = new TestModel
                {
                    Lods = new TestModelLod[]
                    {
                        new TestModelLod
                        {
                            Distance = 100,
                            SubObjects = new TestModelSubObject[]
                            {
                                new TestModelSubObject
                                {
                                    Primitives = new[]
                                    {
                                        new TestModelPrimitive
                                        {
                                            Texture = "ExampleTexture",
                                            Triangles = new ushort[]
                                            {
                                                1, 2, 3,
                                                4, 5, 6
                                            }
                                        },
                                        new TestModelPrimitive
                                        {
                                            Texture = "ExampleTexture3",
                                            Triangles = new ushort[]
                                            {
                                                9, 1, 4,
                                                6, 5, 8
                                            }
                                        },
                                    },
                                    Vertices = new Vertex[]
                                    {
                                        new Vertex
                                        {
                                            Position = new Vector3 { X = 0, Y = 0, Z = 0},
                                            Normal = new Vector3 { X = 0, Y = 0, Z = 0},
                                            TexCoord = new Vector2 { X = 0, Y = 0 },
                                        },
                                        new Vertex
                                        {
                                            Position = new Vector3 { X = 1, Y = 2, Z = 3},
                                            Normal = new Vector3 { X = 4, Y = 5, Z = 6},
                                            TexCoord = new Vector2 { X = 7, Y = 8 },
                                        },
                                    }
                                }
                            }
                        }
                    }
                };

                model.Write(writer);
                
            }
        }
        */
    }
}