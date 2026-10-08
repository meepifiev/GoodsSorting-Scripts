using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class ReferenceSpriteMesh
    {
        private const int StreamAlignment = 16;
        private const int PositionChannel = 0;
        private const int FloatFormat = 0;
        private const int FloatSize = 4;

        private struct Channel
        {
            public int Stream;
            public int Offset;
            public int Format;
            public int Dimension;
        }

        private const int PackingRotationShift = 2;
        private const int PackingRotationMask = 0xF;
        private const int FlipHorizontalRotation = 1;
        private const int FlipVerticalRotation = 2;
        private const int Rotate180Rotation = 3;

        public Vector3[] Positions { get; private set; }
        public Vector2[] PixelPositions { get; private set; }
        public Vector2 PivotPixel { get; private set; }
        public int[] Indices { get; private set; }
        public bool PackedFlippedHorizontally { get; private set; }
        public bool PackedFlippedVertically { get; private set; }

        public bool TryRead(UnityYamlNode sprite)
        {
            UnityYamlNode renderData = sprite["m_RD"];

            if (renderData == null)
            {
                return false;
            }

            UnityYamlNode vertexData = renderData["m_VertexData"];

            if (vertexData == null)
            {
                return false;
            }

            int vertexCount = vertexData.GetInt("m_VertexCount");
            byte[] data = ParseHex(vertexData.GetString("_typelessdata"));
            byte[] indexBytes = ParseHex(renderData.GetString("m_IndexBuffer"));

            if (vertexCount < 3 || data.Length == 0 || indexBytes.Length < 6)
            {
                return false;
            }

            List<Channel> channels = new List<Channel>();

            foreach (UnityYamlNode channelNode in vertexData.GetOrEmpty("m_Channels").Items)
            {
                channels.Add(new Channel
                {
                    Stream = channelNode.GetInt("stream"),
                    Offset = channelNode.GetInt("offset"),
                    Format = channelNode.GetInt("format"),
                    Dimension = channelNode.GetInt("dimension")
                });
            }

            if (channels.Count <= PositionChannel)
            {
                return false;
            }

            Channel position = channels[PositionChannel];

            if (position.Dimension < 2 || position.Format != FloatFormat)
            {
                return false;
            }

            int[] streamStrides = ComputeStrides(channels);
            int[] streamOffsets = ComputeStreamOffsets(streamStrides, vertexCount);

            if (streamOffsets[position.Stream] + vertexCount * streamStrides[position.Stream] > data.Length)
            {
                return false;
            }

            UnityYamlNode uvTransform = renderData["uvTransform"];

            if (uvTransform == null || uvTransform.IsMap == false)
            {
                return false;
            }

            Vector2 pixelScale = new Vector2(uvTransform.GetFloat("x"), uvTransform.GetFloat("z"));
            PivotPixel = new Vector2(uvTransform.GetFloat("y"), uvTransform.GetFloat("w"));

            int packingRotation = (renderData.GetInt("settingsRaw") >> PackingRotationShift) & PackingRotationMask;
            PackedFlippedHorizontally = packingRotation == FlipHorizontalRotation || packingRotation == Rotate180Rotation;
            PackedFlippedVertically = packingRotation == FlipVerticalRotation || packingRotation == Rotate180Rotation;

            if (pixelScale.x <= 0f || pixelScale.y <= 0f)
            {
                return false;
            }

            Positions = new Vector3[vertexCount];
            PixelPositions = new Vector2[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                int positionOffset = streamOffsets[position.Stream] + i * streamStrides[position.Stream] + position.Offset;

                Positions[i] = new Vector3(
                    BitConverter.ToSingle(data, positionOffset),
                    BitConverter.ToSingle(data, positionOffset + FloatSize),
                    position.Dimension > 2 ? BitConverter.ToSingle(data, positionOffset + FloatSize * 2) : 0f);

                PixelPositions[i] = new Vector2(
                    Positions[i].x * pixelScale.x + PivotPixel.x,
                    Positions[i].y * pixelScale.y + PivotPixel.y);
            }

            int indexCount = indexBytes.Length / 2;
            Indices = new int[indexCount];

            for (int i = 0; i < indexCount; i++)
            {
                Indices[i] = BitConverter.ToUInt16(indexBytes, i * 2);

                if (Indices[i] >= vertexCount)
                {
                    return false;
                }
            }

            return Indices.Length >= 3;
        }

        private int[] ComputeStrides(List<Channel> channels)
        {
            int streamCount = 0;

            foreach (Channel channel in channels)
            {
                if (channel.Dimension > 0)
                {
                    streamCount = Mathf.Max(streamCount, channel.Stream + 1);
                }
            }

            int[] strides = new int[Mathf.Max(streamCount, 1)];

            foreach (Channel channel in channels)
            {
                if (channel.Dimension > 0)
                {
                    strides[channel.Stream] += channel.Dimension * FloatSize;
                }
            }

            return strides;
        }

        private int[] ComputeStreamOffsets(int[] strides, int vertexCount)
        {
            int[] offsets = new int[strides.Length];
            int offset = 0;

            for (int i = 0; i < strides.Length; i++)
            {
                offsets[i] = offset;
                offset += strides[i] * vertexCount;
                offset = (offset + StreamAlignment - 1) / StreamAlignment * StreamAlignment;
            }

            return offsets;
        }

        private byte[] ParseHex(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
    }
}
