#if UNIGINE_DOUBLE
using Vec3 = Unigine.dvec3;
using Vec4 = Unigine.dvec4;
using Mat4 = Unigine.dmat4;
using Scalar = System.Double;
#else
using Vec3 = Unigine.vec3;
using Vec4 = Unigine.vec4;
using Mat4 = Unigine.mat4;
using Scalar = System.Single;
#endif


using Unigine;
using ExitGames.Client.Photon;
using System;

internal class Utils
{
	public enum EventData
	{
		Transform = 0,
		Shot,
		HP
	}

	public class PhotonVec3
	{
		public static readonly byte[] memVec3 = new byte[3 * sizeof(Scalar)];

		public static void RegisterSerializable()
		{
			PhotonPeer.RegisterType(typeof(PhotonVec3), (byte)0, Serialize, Deserialize);
		}

		private static short Serialize(StreamBuffer outStream, object customObject)
		{
			PhotonVec3 obj = (PhotonVec3)customObject;
			lock (memVec3)
			{
				#if UNIGINE_DOUBLE
				byte[] buffer = memVec3;
				int index = 0;
				SerializeDouble(obj.vec.x, buffer, ref index);
				SerializeDouble(obj.vec.y, buffer, ref index);
				SerializeDouble(obj.vec.z, buffer, ref index);
				outStream.Write(buffer, 0, 3 * sizeof(Scalar));
				#else
				byte[] buffer = memVec3;
				int index = 0;
				Protocol.Serialize(obj.vec.x, buffer, ref index);
				Protocol.Serialize(obj.vec.y, buffer, ref index);
				Protocol.Serialize(obj.vec.z, buffer, ref index);
				outStream.Write(buffer, 0, 3 * sizeof(Scalar));
				#endif
			}

			return 3 * sizeof(Scalar);
		}

		private static object Deserialize(StreamBuffer inStream, short length)
		{
			PhotonVec3 obj = new PhotonVec3();
			lock(memVec3)
			{
				#if UNIGINE_DOUBLE
				inStream.Read(memVec3, 0, 3 * sizeof (Scalar));
				int index = 0;
				DeserializeDouble(out obj.x, memVec3, ref index);
				DeserializeDouble(out obj.y, memVec3, ref index);
				DeserializeDouble(out obj.z, memVec3, ref index);
				#else
				inStream.Read(memVec3, 0, 3 * sizeof (Scalar));
				int index = 0;
				Protocol.Deserialize(out obj.x, memVec3, ref index);
				Protocol.Deserialize(out obj.y, memVec3, ref index);
				Protocol.Deserialize(out obj.z, memVec3, ref index);
				#endif
				obj.vec.x = obj.x;
				obj.vec.y = obj.y;
				obj.vec.z = obj.z;
			}

			return obj;
		}

		public Vec3 vec;
		private Scalar x;
		private Scalar y;
		private Scalar z;

		private PhotonVec3() {}

		public PhotonVec3(Vec3 value)
		{
			vec = value;
		}
	}

	public class PhotonMat4
	{
		#if UNIGINE_DOUBLE
		public static readonly byte[] memMat4 = new byte[3 * 4 * sizeof(Scalar)];
		#else
		public static readonly byte[] memMat4 = new byte[4 * 4 * sizeof(Scalar)];
		#endif

		public static void RegisterSerializable()
		{
			PhotonPeer.RegisterType(typeof(PhotonMat4), (byte)1, Serialize, Deserialize);
		}

		private static short Serialize(StreamBuffer outStream, object customObject)
		{
			PhotonMat4 obj = (PhotonMat4)customObject;
			lock (memMat4)
			{
				#if UNIGINE_DOUBLE
				byte[] buffer = memMat4;
				int index = 0;
				SerializeDouble(obj.mat.m00, buffer, ref index);
				SerializeDouble(obj.mat.m01, buffer, ref index);
				SerializeDouble(obj.mat.m02, buffer, ref index);
				SerializeDouble(obj.mat.m03, buffer, ref index);
				SerializeDouble(obj.mat.m10, buffer, ref index);
				SerializeDouble(obj.mat.m11, buffer, ref index);
				SerializeDouble(obj.mat.m12, buffer, ref index);
				SerializeDouble(obj.mat.m13, buffer, ref index);
				SerializeDouble(obj.mat.m20, buffer, ref index);
				SerializeDouble(obj.mat.m21, buffer, ref index);
				SerializeDouble(obj.mat.m22, buffer, ref index);
				SerializeDouble(obj.mat.m23, buffer, ref index);

				outStream.Write(buffer, 0, 3 * 4 * sizeof(Scalar));
				#else
				byte[] buffer = memMat4;
				int index = 0;
				Protocol.Serialize(obj.mat.m00, buffer, ref index);
				Protocol.Serialize(obj.mat.m01, buffer, ref index);
				Protocol.Serialize(obj.mat.m02, buffer, ref index);
				Protocol.Serialize(obj.mat.m03, buffer, ref index);
				Protocol.Serialize(obj.mat.m10, buffer, ref index);
				Protocol.Serialize(obj.mat.m11, buffer, ref index);
				Protocol.Serialize(obj.mat.m12, buffer, ref index);
				Protocol.Serialize(obj.mat.m13, buffer, ref index);
				Protocol.Serialize(obj.mat.m20, buffer, ref index);
				Protocol.Serialize(obj.mat.m21, buffer, ref index);
				Protocol.Serialize(obj.mat.m22, buffer, ref index);
				Protocol.Serialize(obj.mat.m23, buffer, ref index);
				Protocol.Serialize(obj.mat.m30, buffer, ref index);
				Protocol.Serialize(obj.mat.m31, buffer, ref index);
				Protocol.Serialize(obj.mat.m32, buffer, ref index);
				Protocol.Serialize(obj.mat.m33, buffer, ref index);

				outStream.Write(buffer, 0, 4 * 4 * sizeof(Scalar));
				#endif
			}

			return 4 * 4 * sizeof(Scalar);
		}

		private static object Deserialize(StreamBuffer inStream, short length)
		{
			PhotonMat4 obj = new PhotonMat4();
			lock (memMat4)
			{
				#if UNIGINE_DOUBLE
				inStream.Read(memMat4, 0, 3 * 4 * sizeof(Scalar));
				int index = 0;
				DeserializeDouble(out obj.m00, memMat4, ref index);
				DeserializeDouble(out obj.m01, memMat4, ref index);
				DeserializeDouble(out obj.m02, memMat4, ref index);
				DeserializeDouble(out obj.m03, memMat4, ref index);
				DeserializeDouble(out obj.m10, memMat4, ref index);
				DeserializeDouble(out obj.m11, memMat4, ref index);
				DeserializeDouble(out obj.m12, memMat4, ref index);
				DeserializeDouble(out obj.m13, memMat4, ref index);
				DeserializeDouble(out obj.m20, memMat4, ref index);
				DeserializeDouble(out obj.m21, memMat4, ref index);
				DeserializeDouble(out obj.m22, memMat4, ref index);
				DeserializeDouble(out obj.m23, memMat4, ref index);
				obj.mat.m00 = obj.m00;
				obj.mat.m01 = obj.m01;
				obj.mat.m02 = obj.m02;
				obj.mat.m03 = obj.m03;
				obj.mat.m10 = obj.m10;
				obj.mat.m11 = obj.m11;
				obj.mat.m12 = obj.m12;
				obj.mat.m13 = obj.m13;
				obj.mat.m20 = obj.m20;
				obj.mat.m21 = obj.m21;
				obj.mat.m22 = obj.m22;
				obj.mat.m23 = obj.m23;
				#else
				inStream.Read(memMat4, 0, 4 * 4 * sizeof(Scalar));
				int index = 0;
				Protocol.Deserialize(out obj.m00, memMat4, ref index);
				Protocol.Deserialize(out obj.m01, memMat4, ref index);
				Protocol.Deserialize(out obj.m02, memMat4, ref index);
				Protocol.Deserialize(out obj.m03, memMat4, ref index);
				Protocol.Deserialize(out obj.m10, memMat4, ref index);
				Protocol.Deserialize(out obj.m11, memMat4, ref index);
				Protocol.Deserialize(out obj.m12, memMat4, ref index);
				Protocol.Deserialize(out obj.m13, memMat4, ref index);
				Protocol.Deserialize(out obj.m20, memMat4, ref index);
				Protocol.Deserialize(out obj.m21, memMat4, ref index);
				Protocol.Deserialize(out obj.m22, memMat4, ref index);
				Protocol.Deserialize(out obj.m23, memMat4, ref index);
				Protocol.Deserialize(out obj.m30, memMat4, ref index);
				Protocol.Deserialize(out obj.m31, memMat4, ref index);
				Protocol.Deserialize(out obj.m32, memMat4, ref index);
				Protocol.Deserialize(out obj.m33, memMat4, ref index);
				obj.mat.m00 = obj.m00;
				obj.mat.m01 = obj.m01;
				obj.mat.m02 = obj.m02;
				obj.mat.m03 = obj.m03;
				obj.mat.m10 = obj.m10;
				obj.mat.m11 = obj.m11;
				obj.mat.m12 = obj.m12;
				obj.mat.m13 = obj.m13;
				obj.mat.m20 = obj.m20;
				obj.mat.m21 = obj.m21;
				obj.mat.m22 = obj.m22;
				obj.mat.m23 = obj.m23;
				obj.mat.m30 = obj.m30;
				obj.mat.m31 = obj.m31;
				obj.mat.m32 = obj.m32;
				obj.mat.m33 = obj.m33;
				#endif
			}

			return obj;
		}

		public Mat4 mat;
		private Scalar m00;
		private Scalar m01;
		private Scalar m02;
		private Scalar m03;
		private Scalar m10;
		private Scalar m11;
		private Scalar m12;
		private Scalar m13;
		private Scalar m20;
		private Scalar m21;
		private Scalar m22;
		private Scalar m23;
		#if !UNIGINE_DOUBLE
		private Scalar m30;
		private Scalar m31;
		private Scalar m32;
		private Scalar m33;
		#endif

		private PhotonMat4() { }

		public PhotonMat4(Mat4 value)
		{
			mat = value;
		}
	}

	public static void SerializeDouble(double value, byte[] target, ref int targetOffset)
	{
		lock (memDoubleBlock)
		{
			memDoubleBlock[0] = value;
			Buffer.BlockCopy(memDoubleBlock, 0, target, targetOffset, 8);
		}

		if (BitConverter.IsLittleEndian)
		{
			byte b = target[targetOffset];
			byte b2 = target[targetOffset + 1];
			byte b3 = target[targetOffset + 2];
			byte b4 = target[targetOffset + 3];
			target[targetOffset] = target[targetOffset + 7];
			target[targetOffset + 1] = target[targetOffset + 6];
			target[targetOffset + 2] = target[targetOffset + 5];
			target[targetOffset + 3] = target[targetOffset + 4];
			target[targetOffset + 4] = b4;
			target[targetOffset + 5] = b3;
			target[targetOffset + 6] = b2;
			target[targetOffset + 7] = b;
		}

		targetOffset += 8;
	}

	public static void DeserializeDouble(out double value, byte[] source, ref int offset)
	{
		if (BitConverter.IsLittleEndian)
		{
			lock (memDeserialize)
			{
				byte[] array = memDeserialize;
				array[7] = source[offset++];
				array[6] = source[offset++];
				array[5] = source[offset++];
				array[4] = source[offset++];
				array[3] = source[offset++];
				array[2] = source[offset++];
				array[1] = source[offset++];
				array[0] = source[offset++];
				value = BitConverter.ToDouble(array, 0);
			}
		}
		else
		{
			value = BitConverter.ToDouble(source, offset);
			offset += 8;
		}
	}

	private static readonly double[] memDoubleBlock = new double[1];
	private static readonly byte[] memDeserialize = new byte[8];
}
