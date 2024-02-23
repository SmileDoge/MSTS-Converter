using CommandLine;
using MSTS_Converter.Formats;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSTS_Converter.Formats
{
    public enum TextureFormat
    {
        RGBA32,
        BGRA32,
        BGR565,
        BGRA5551,
        BGRA4444,
        DXT1,
        DXT3,
        DXT5,
    }

    public class AceFileData
    {
        public int Width;
        public int Height;
        public TextureFormat Format;
        public byte[] Data;
    }

    public class AceFile
    {
        /*
        public static Texture2D LoadTextureFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                filename = Path.Combine(Path.GetDirectoryName(filename), Path.Combine("../TEXTURES", Path.GetFileName(filename)));
            }

            if (!File.Exists(filename))
            {
                filename = Path.Combine(PathHelper.GetRoute(GameManager.Instance.RouteName), "TEXTURES", Path.GetFileName(filename));
            }


            var data = LoadTextureDataFromFile(filename);

            var texture = new Texture2D(data.Width, data.Height, data.Format, false);

            texture.LoadRawTextureData(data.Data);

            texture.Apply();

            return texture;
        }
        */

        public static AceFileData LoadTextureDataFromFile(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return LoadTextureFromStream(stream);
            }
        }

        public static AceFileData LoadTextureFromStream(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var signature = new string(reader.ReadChars(8));

                if (signature == "SIMISA@F")
                {
                    reader.ReadUInt32();
                    signature = new string(reader.ReadChars(4));
                    if (signature != "@@@@") throw new InvalidDataException($"Invalid signature, Expected @@@@, got {signature}");

                    var zlib = reader.ReadUInt16();
                    if ((zlib & 0x20FF) != 0x0078) throw new InvalidDataException($"Invalid signature, Expected xx78, got {zlib & 0x20FF}");

                    return LoadTextureFromReader(new BinaryReader(new DeflateStream(stream, CompressionMode.Decompress)));
                }
                if (signature == "SIMISA@@")
                {
                    signature = new string(reader.ReadChars(8));

                    if (signature != "@@@@@@@@") throw new InvalidDataException($"Invalid signature, Expected @@@@@@@@, got {signature}");

                    return LoadTextureFromReader(reader);
                }
                throw new InvalidDataException($"Invalid signature, Expected SIMISA@F or SIMISA@@, got {signature}");
            }
        }

        public static AceFileData LoadTextureFromReader(BinaryReader reader)
        {
            var signature = new string(reader.ReadChars(4));
            if (signature != "\x01\x00\x00\x00") throw new InvalidDataException("Incorrect signature; expected 01 00 00 00");
            var options = (AceFormatOptions)reader.ReadUInt32();
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var surfaceFormat = reader.ReadInt32();
            var channelCount = reader.ReadInt32();
            reader.ReadBytes(128);

            var textureFormat = TextureFormat.RGBA32;

            if ((options & AceFormatOptions.RawData) != 0)
            {
                if (!AceSurfaceFormats.ContainsKey(surfaceFormat)) throw new InvalidDataException($"Unsupported surface format {surfaceFormat}");
                textureFormat = AceSurfaceFormats[surfaceFormat];
            }

            var imageCount = 1 + (int)((options & AceFormatOptions.MipMaps) != 0 ? Math.Log(width) / Math.Log(2) : 0);

            //Texture2D texture = new Texture2D(width, height, textureFormat, false);
            var textureData = new AceFileData() { Width = width, Height = height, Format = textureFormat };

            var channels = new List<AceChannel>();
            for (var channel = 0; channel < channelCount; channel++)
            {
                var size = (byte)reader.ReadUInt64();
                if ((size != 1) && (size != 8)) throw new InvalidDataException(string.Format("Unsupported color channel size {0}", size));
                var type = reader.ReadUInt64();
                if ((type < 2) || (type > 6)) throw new InvalidDataException(string.Format("Unknown color channel type {0}", type));
                Console.WriteLine($"Type: {(AceChannelId)type}, Size: {size}");
                channels.Add(new AceChannel(size, (AceChannelId)type));
            }

            if (channels.Any(c => c.Type == AceChannelId.Alpha))
                Console.WriteLine($"Alpha: 8 bit");
            else if (channels.Any(c => c.Type == AceChannelId.Mask))
                Console.WriteLine($"Alpha: 1 bit");

            if ((options & AceFormatOptions.RawData) != 0)
            {
                reader.ReadBytes(imageCount * 4);

                var buffer = reader.ReadBytes(reader.ReadInt32());

                textureData.Data = buffer;
                //texture.LoadRawTextureData(buffer);
            }
            else
            {
                for (var imageIndex = 0; imageIndex < imageCount; imageIndex++)
                    reader.ReadBytes(4 * height / (int)Math.Pow(2, imageIndex));

                var buffer = new byte[width * height * 4];
                var channelBuffers = new byte[8][];

                for (var y = 0; y < height; y++)
                {
                    foreach (var channel in channels)
                    {
                        if (channel.Size == 1)
                        {
                            var bytes = reader.ReadBytes((int)Math.Ceiling((double)channel.Size * width / 8));
                            channelBuffers[(int)channel.Type] = new byte[width];
                            for (var x = 0; x < width; x++)
                                channelBuffers[(int)channel.Type][x] = (byte)(((bytes[x / 8] >> (7 - (x % 8))) & 1) * 0xFF);
                        }
                        else
                        {
                            channelBuffers[(int)channel.Type] = reader.ReadBytes(width);
                        }
                    }

                    for (var x = 0; x < width; x++)
                    {
                        var pos = width * y + x;

                        byte alpha = 0xFF;

                        if (channelBuffers[(int)AceChannelId.Alpha] != null)
                            alpha = channelBuffers[(int)AceChannelId.Alpha][x];
                        else if (channelBuffers[(int)AceChannelId.Mask] != null)
                            alpha = channelBuffers[(int)AceChannelId.Mask][x];

                        buffer[pos * 4 + 0] = channelBuffers[(int)AceChannelId.Red][x];
                        buffer[pos * 4 + 1] = channelBuffers[(int)AceChannelId.Green][x];
                        buffer[pos * 4 + 2] = channelBuffers[(int)AceChannelId.Blue][x];
                        buffer[pos * 4 + 3] = alpha;
                    }
                }

                textureData.Data = buffer;
                //texture.LoadRawTextureData(buffer);
            }

            //texture.Apply();

            return textureData;
        }

        [Flags]
        public enum AceFormatOptions
        {
            Default = 0,
            MipMaps = 0x01,
            RawData = 0x10,
        }

        public enum AceChannelId
        {
            Mask = 2,
            Red = 3,
            Green = 4,
            Blue = 5,
            Alpha = 6,
        }
        public class AceChannel
        {
            public readonly int Size;
            public readonly AceChannelId Type;

            public AceChannel(int size, AceChannelId type)
            {
                Size = size;
                Type = type;
            }
        }

        static readonly Dictionary<int, TextureFormat> AceSurfaceFormats = new Dictionary<int, TextureFormat>()
        {
            { 0x0E, TextureFormat.BGR565 },
            { 0x10, TextureFormat.BGRA5551 },
            { 0x11, TextureFormat.BGRA4444 },
            { 0x12, TextureFormat.DXT1 },
            { 0x14, TextureFormat.DXT3 },
            { 0x16, TextureFormat.DXT5 },
        };
    }
}
